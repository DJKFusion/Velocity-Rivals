using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    /// <summary>One authoritative local match state. Online sessions must not reuse this as authority.</summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance;
        public static readonly string[] EnvironmentNames = { "Neon City Circuit", "Skyfall Speedway", "Desert Velocity", "Cyber Dome", "Glacier Rush" };
        public static readonly string[] VehicleNames = { "Viper", "Bulwark", "Phantom", "Apex", "Nomad", "Comet" };
        public VehicleController Player { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public bool Finished { get; private set; }
        public string StatusText { get; private set; } = "Choose a mode to begin.";
        public string ResultText { get; private set; } = "";
        public float RemainingTime => Mathf.Max(0, duration - elapsed);
        public int PlayerLap => playerProgress == null ? 1 : Mathf.Min(TotalLaps, playerProgress.CompletedLaps + 1);
        public int TotalLaps => 3;
        public event Action MatchEnded;
        public GameMode CurrentMode => mode;
        public float Elapsed => elapsed;
        public float CountdownRemaining => Mathf.Max(0, countdown);
        public int CareerEventIndex { get; private set; } = -1;
        public int LastCareerMedal { get; private set; }
        public bool ResultWon { get; private set; }
        public int PlayerScore => playerScore;
        public int RivalScore => rivalScore;
        public bool ActionEnabled => Running && !Paused && !Finished && countdown <= 0;
        public string CombatStatus => battle != null ? battle.PlayerStatus : "";
        public bool IsCircuit => racing;
        public IReadOnlyList<Vector3> Circuit => track;
        public IReadOnlyList<VehicleController> Vehicles => cars;
        public int NextCheckpoint => playerProgress != null ? playerProgress.NextCheckpoint : -1;
        public bool HasArenaCore => core != null;
        public bool NetworkControlled { get; private set; }
        public Rigidbody NetworkCore => NetworkControlled ? core : null;
        public Vector3 ArenaCorePosition => core != null ? core.position : Vector3.zero;
        public LapTiming LapTimes { get; private set; } = new LapTiming();
        public bool WrongWay => wrongWaySeconds >= 1.5f;
        private float wrongWaySeconds;
        private ArenaBattle battle;
        private int countdownBeat;
        public bool CanAdvanceChampionship => mode == GameMode.Championship && Finished && round < 2 && playerProgress != null && playerProgress.Complete;

        private Transform world;
        private readonly List<VehicleController> cars = new List<VehicleController>();
        private readonly List<VehicleStats> temporaryStats = new List<VehicleStats>();
        private readonly List<Racer> racers = new List<Racer>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Vector3> track = new List<Vector3>();
        private GameMode mode;
        private int environment, selectedVehicle, initialEnvironment;
        private Color paint;
        private float elapsed, countdown, duration, collisionCooldown, goalCooldown, championshipElapsed;
        private int playerScore, rivalScore, round, finishedRivals;
        private readonly int[] championshipPoints = new int[4];
        private RaceProgress playerProgress;
        private Vector3 previousPlayer;
        private Rigidbody core;
        private GhostReplay ghost;
        private PhysicsMaterial corePhysics;
        private bool racing => mode == GameMode.Championship || mode == GameMode.TimeAttack;
        private sealed class Racer
        {
            public VehicleController Car;
            public RaceProgress Progress;
            public Vector3 Previous;
            public float Stuck;
            public float FinishTime = -1;
            public int Id;
        }

        private void Awake() { Instance = this; }
        private void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; Cleanup(); }

        public void StartMatch(GameMode newMode, int newEnvironment, int vehicle, Color color, bool networkControlled = false, int careerEvent = -1)
        {
            if (careerEvent >= 0 && !SaveStore.Current.career.IsUnlocked(careerEvent)) throw new InvalidOperationException("Complete the earlier career events first.");
            CareerEventIndex = careerEvent;
            LastCareerMedal = 0;
            NetworkControlled = networkControlled;
            mode = newMode;
            initialEnvironment = environment = Mathf.Clamp(newEnvironment, 0, 4);
            selectedVehicle = Mathf.Clamp(vehicle, 0, 5);
            paint = color;
            round = 0;
            championshipElapsed = 0;
            Array.Clear(championshipPoints, 0, championshipPoints.Length);
            BeginRound();
        }

        public void StartCareerEvent(int index, int vehicle, Color color)
        {
            var entry = CareerTour.Get(index);
            StartMatch(entry.Mode, entry.Environment, vehicle, color, false, index);
        }

        private void BeginRound()
        {
            Cleanup();
            Time.timeScale = 1;
            Paused = Finished = ResultWon = false;
            Running = true;
            elapsed = collisionCooldown = goalCooldown = 0;
            wrongWaySeconds = 0;
            LapTimes = new LapTiming();
            countdown = 3;
            countdownBeat = 4;
            playerScore = rivalScore = finishedRivals = 0;
            duration = mode == GameMode.StuntRush ? 120 : mode == GameMode.ArenaClash ? 180 : mode == GameMode.BoostBattle ? 150 : 600;
            ResultText = "";
            world = new GameObject("Velocity Rivals — " + EnvironmentNames[environment]).transform;
            BuildWorld();
            WorldAtmosphere.Build(world, environment);
            WorldScenery.Build(world, track, environment, racing);
            Vector3 start = racing ? track[0] + Vector3.up * 1.2f : new Vector3(0, 1.2f, -18);
            Quaternion facing = racing ? Quaternion.LookRotation(track[1] - track[0]) : Quaternion.identity;
            Player = SpawnCar(selectedVehicle, start, facing, paint, true);
            Player.AllowLocalRecovery = !racing;
            previousPlayer = Player.transform.position;
            playerProgress = racing ? new RaceProgress(track.Count, TotalLaps) : null;
            if (!NetworkControlled && (mode == GameMode.Championship || mode == GameMode.ArenaClash || mode == GameMode.BoostBattle))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = racing ? start - facing * Vector3.forward * (5 + 4 * i) + facing * Vector3.right * (i % 2 == 0 ? -3 : 3) : new Vector3((i - 1) * 12, 1.2f, 12);
                    Color rivalPaint = mode == GameMode.ArenaClash ? (i == 0 ? Color.cyan : new Color(1, .4f, .05f)) : Color.HSVToRGB((i + 1) * .22f, .8f, 1);
                    var car = SpawnCar((selectedVehicle + i + 1) % 6, p, racing ? facing : Quaternion.Euler(0, 180, 0), rivalPaint, false);
                    racers.Add(new Racer { Car = car, Previous = p, Progress = new RaceProgress(Mathf.Max(3, track.Count), TotalLaps), Id = i + 1 });
                }
            }
            if (!NetworkControlled && mode == GameMode.BoostBattle)
            {
                battle = world.gameObject.AddComponent<ArenaBattle>();
                battle.Initialize(this, cars);
            }
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Game Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                camera = go.GetComponent<Camera>();
            }
            var follow = camera.GetComponent<FollowCamera>() ?? camera.gameObject.AddComponent<FollowCamera>();
            follow.enabled = true;
            follow.Target = Player;
            if (!NetworkControlled && mode == GameMode.TimeAttack)
            {
                ghost = world.gameObject.AddComponent<GhostReplay>();
                ghost.Begin(environment, selectedVehicle, Player.transform);
            }
        }

        public void RestartMatch() => StartMatch(mode, initialEnvironment, selectedVehicle, paint, false, CareerEventIndex);
        public void AdvanceChampionship()
        {
            if (!CanAdvanceChampionship) return;
            round++;
            environment = (initialEnvironment + round) % EnvironmentNames.Length;
            BeginRound();
        }
        public void ReturnToMenu()
        {
            CareerEventIndex = -1;
            NetworkControlled = false;
            Running = Finished = Paused = false;
            Time.timeScale = 1;
            Cleanup();
            StatusText = "Choose a mode to begin.";
        }
        public void SetPaused(bool value)
        {
            if (!Running || Finished || NetworkControlled) return;
            Paused = value;
            Time.timeScale = value ? 0 : 1;
            foreach (var car in cars) if (car != null) car.ControlsEnabled = !value && countdown <= 0 && car.enabled;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !Application.isBatchMode && !NetworkControlled && Running && !Finished) SetPaused(true);
        }

        private VehicleController SpawnCar(int index, Vector3 position, Quaternion rotation, Color color, bool player)
        {
            var template = Resources.Load<VehicleStats>("VehicleStats/" + VehicleNames[index]);
            var stats = template != null ? Instantiate(template) : ScriptableObject.CreateInstance<VehicleStats>();
            stats.DisplayName = VehicleNames[index];
            if (template == null)
            {
                stats.MaxSpeed = new[] { 40f, 34f, 43f, 46f, 38f, 42f }[index];
                stats.Acceleration = new[] { 25f, 22f, 29f, 27f, 26f, 24f }[index];
                stats.Steering = new[] { 95f, 75f, 115f, 92f, 82f, 105f }[index];
                stats.Grip = new[] { 8f, 7f, 10f, 9f, 8f, 6f }[index];
                stats.BoostForce = index == 5 ? 34 : 24;
            }
            if (environment == 4) stats.Grip *= .45f;
            if (!player)
            {
                int level = CareerEventIndex >= 0 ? CareerTour.Get(CareerEventIndex).Difficulty : SaveStore.Current.difficulty;
                float difficulty = new[] { .8f, 1f, 1.12f }[Mathf.Clamp(level, 0, 2)];
                stats.MaxSpeed *= difficulty; stats.Acceleration *= difficulty;
            }
            temporaryStats.Add(stats);
            var car = VehicleController.Spawn(stats.DisplayName, position, rotation, stats, color, player);
            car.transform.SetParent(world, true);
            car.ControlsEnabled = false;
            if (racing) car.AllowLocalRecovery = false;
            cars.Add(car);
            var hit = car.gameObject.AddComponent<BattleContact>();
            hit.Session = this;
            hit.Car = car;
            return car;
        }

        private void Update()
        {
            if (!Running || Paused || Finished || Player == null || NetworkControlled) return;
            if (countdown > 0)
            {
                countdown -= Time.deltaTime;
                int beat = Mathf.CeilToInt(Mathf.Max(0, countdown));
                if (beat != countdownBeat) { countdownBeat = beat; SoundDirector.Instance?.Cue(beat == 0 ? GameCue.Go : GameCue.Countdown); }
                StatusText = "READY — " + Mathf.CeilToInt(Mathf.Max(.01f, countdown));
                if (countdown <= 0) foreach (var car in cars) car.ControlsEnabled = true;
                return;
            }
            elapsed += Time.deltaTime;
            if (racing && ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)))
                Recover(Player, playerProgress.NextCheckpoint);
            collisionCooldown = Mathf.Max(0, collisionCooldown - Time.deltaTime);
            goalCooldown = Mathf.Max(0, goalCooldown - Time.deltaTime);
            if (Player.NeedsRecovery || Player.transform.position.y < -12 || Player.transform.position.sqrMagnitude > 300 * 300)
                Recover(Player, racing ? playerProgress.NextCheckpoint : 0);
            if (racing) UpdateRace();
            else UpdateArena();
            if (Finished) return;
            UpdateAI();
            if (ghost != null) ghost.Tick(elapsed);
        }

        private void UpdateRace()
        {
            TryCheckpoint(Player.transform.position, previousPlayer, playerProgress);
            LapTimes.CompleteLap(playerProgress.CompletedLaps, elapsed);
            previousPlayer = Player.transform.position;
            int gateIndex = playerProgress.NextCheckpoint;
            Vector3 trackForward = (track[gateIndex] - track[(gateIndex + track.Count - 1) % track.Count]).normalized;
            Vector3 velocity = Player.GetComponent<Rigidbody>().linearVelocity;
            bool backwards = velocity.sqrMagnitude > 25 && Vector3.Dot(velocity.normalized, trackForward) < -.35f;
            wrongWaySeconds = backwards ? wrongWaySeconds + Time.deltaTime : 0;
            foreach (var racer in racers)
            {
                if (racer.Progress.Complete) continue;
                TryCheckpoint(racer.Car.transform.position, racer.Previous, racer.Progress);
                racer.Previous = racer.Car.transform.position;
                if (racer.Progress.Complete)
                {
                    racer.FinishTime = elapsed;
                    finishedRivals++;
                    racer.Car.ControlsEnabled = false;
                }
            }
            int place = 1;
            foreach (var racer in racers)
                if (racer.Progress.Progress > playerProgress.Progress || (racer.Progress.Progress == playerProgress.Progress &&
                    Vector3.Distance(racer.Car.transform.position, track[playerProgress.NextCheckpoint]) < Vector3.Distance(Player.transform.position, track[playerProgress.NextCheckpoint]))) place++;
            StatusText = (mode == GameMode.Championship ? "CHAMPIONSHIP · ROUND " + (round + 1) + "/3 · PLACE " + place + "/4" : "TIME ATTACK · PERSONAL GHOST") +
                "\nLAP " + PlayerLap + "/" + TotalLaps + " · CHECKPOINT " + playerProgress.NextCheckpoint + "/" + track.Count + " · " + elapsed.ToString("0.00") + "s";
            if (playerProgress.Complete)
            {
                if (ghost != null) ghost.Complete(elapsed);
                if (mode == GameMode.Championship)
                {
                    AwardRound();
                    if (round < 2)
                    {
                        Finished = true;
                        ResultText = "ROUND " + (round + 1) + " COMPLETE\n" + Standings() + "\nSelect NEXT ROUND to continue the championship.";
                        StopCarsForResults();
                        MatchEnded?.Invoke();
                    }
                    else
                    {
                        bool champion = true;
                        for (int i = 1; i < 4; i++) if (championshipPoints[i] > championshipPoints[0]) champion = false;
                        EndMatch(champion ? "CHAMPIONSHIP TROPHY" : "CHAMPIONSHIP COMPLETE", Standings(), champion, championshipPoints[0]);
                    }
                }
                else EndMatch("TIME ATTACK COMPLETE", "Three laps: " + elapsed.ToString("0.00") + "s\nYour fastest run is saved as a local ghost.", true, Mathf.Max(0, 10000 - (int)elapsed * 10));
            }
            else if (elapsed >= duration) EndMatch("TIME LIMIT", "Race not completed. Follow each glowing checkpoint in order.", false, 0);
        }

        private void AwardRound()
        {
            championshipElapsed += elapsed;
            var order = new List<Racer>(racers);
            order.Add(new Racer { Id = 0, Progress = playerProgress, FinishTime = elapsed });
            order.Sort((a, b) =>
            {
                if (a.FinishTime >= 0 && b.FinishTime >= 0) return a.FinishTime.CompareTo(b.FinishTime);
                if (a.FinishTime >= 0) return -1;
                if (b.FinishTime >= 0) return 1;
                return b.Progress.Progress.CompareTo(a.Progress.Progress);
            });
            int[] points = { 10, 6, 4, 2 };
            for (int i = 0; i < order.Count; i++) championshipPoints[order[i].Id] += points[i];
        }
        private string Standings() => "YOU " + championshipPoints[0] + " pts · ION " + championshipPoints[1] + " · PRISM " + championshipPoints[2] + " · COMET " + championshipPoints[3];

        private void TryCheckpoint(Vector3 current, Vector3 previous, RaceProgress progress)
        {
            int next = progress.NextCheckpoint;
            Vector3 gate = track[next];
            Vector3 forward = (track[(next + 1) % track.Count] - track[(next + track.Count - 1) % track.Count]).normalized;
            float before = Vector3.Dot(previous - gate, forward);
            float after = Vector3.Dot(current - gate, forward);
            if (before <= 0 && after > 0)
            {
                Vector3 crossing = Vector3.Lerp(previous, current, -before / (after - before));
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                if (Mathf.Abs(Vector3.Dot(crossing - gate, side)) < 10 && Mathf.Abs(crossing.y - gate.y) < 8) progress.Cross(next, true);
            }
        }

        private void UpdateArena()
        {
            if (mode == GameMode.ArenaClash)
            {
                if (core != null && goalCooldown <= 0 && core.position.y < 6 && Mathf.Abs(core.position.x) < 10 && Mathf.Abs(core.position.z) > 41)
                {
                    if (core.position.z > 0) playerScore++; else rivalScore++;
                    SoundDirector.Instance?.Cue(GameCue.Goal);
                    MatchEffects.Burst(core.position, core.position.z > 0 ? Color.cyan : new Color(1, .4f, 0));
                    core.position = new Vector3(0, 3, 0); core.linearVelocity = Vector3.zero; core.angularVelocity = Vector3.zero;
                    goalCooldown = 2;
                    Player.Respawn(new Vector3(0, 1.5f, -18), Quaternion.identity);
                    for (int i = 0; i < racers.Count; i++) racers[i].Car.Respawn(new Vector3((i - 1) * 12, 1.5f, 18), Quaternion.Euler(0, 180, 0));
                }
                if (core != null && (core.position.y < -5 || core.position.sqrMagnitude > 15000)) { core.position = new Vector3(0, 3, 0); core.linearVelocity = Vector3.zero; }
                StatusText = "ARENA CLASH · BLUE " + playerScore + " — " + rivalScore + " ORANGE\nPush the energy core into the orange goal · First to 5";
                if (playerScore >= 5 || rivalScore >= 5 || elapsed >= duration) EndScoreMode("ARENA CLASH");
            }
            else if (mode == GameMode.BoostBattle)
            {
                StatusText = "BOOST BATTLE · YOU " + playerScore + " — " + rivalScore + " RIVALS\n" + CombatStatus + "\nCyan: shield · Orange: double damage · Purple: pulse burst · First to 15";
                if (playerScore >= 15 || rivalScore >= 15 || elapsed >= duration) EndScoreMode("BOOST BATTLE");
            }
            else if (mode == GameMode.StuntRush)
            {
                playerScore = Player.StuntScore;
                StatusText = "STUNT RUSH · " + playerScore + " POINTS\nJump, rotate in the air, land upright · Target 1,000";
                if (elapsed >= duration) EndMatch(playerScore >= 1000 ? "STUNT MASTER" : "STUNT SESSION COMPLETE", playerScore + " points · Target 1,000", playerScore >= 1000, playerScore);
            }
            else StatusText = "FREE PLAY\nDrive, drift, jump and practise aerial control · Pause to return to the menu";
        }
        private void EndScoreMode(string title)
        {
            bool won = playerScore > rivalScore;
            EndMatch(playerScore == rivalScore ? "DRAW" : won ? "VICTORY" : "RIVALS WIN", title + "\n" + playerScore + " — " + rivalScore, won, playerScore * 100);
        }
        private void EndMatch(string heading, string details, bool won, int score)
        {
            if (Finished) return;
            Finished = true;
            ResultWon = won;
            ResultText = heading + "\n" + details;
            StopCarsForResults();
            bool completed = !racing || playerProgress.Complete;
            float seconds = mode == GameMode.Championship ? championshipElapsed : elapsed;
            SaveStore.RecordResult(mode.ToString(), won, seconds, score, initialEnvironment, selectedVehicle, completed);
            if (CareerEventIndex >= 0)
            {
                LastCareerMedal = CareerTour.Get(CareerEventIndex).Medal(completed, won, seconds, score);
                SaveStore.Current.career.Record(CareerEventIndex, LastCareerMedal);
                ResultText += "\nCAREER: " + CareerTour.MedalName(LastCareerMedal);
                if (SaveStore.Current.career.Complete)
                {
                    ResultText += "\nVELOCITY TOUR COMPLETE — replay events to collect every gold medal.";
                    if (!SaveStore.Current.achievements.Contains("Velocity Tour complete")) SaveStore.Current.achievements.Add("Velocity Tour complete");
                }
                SaveStore.Save();
            }
            MatchEnded?.Invoke();
        }

        private void StopCarsForResults()
        {
            foreach (var car in cars)
            {
                if (!car) continue;
                car.ControlsEnabled = false;
                var body = car.GetComponent<Rigidbody>();
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                car.enabled = false;
            }
        }

        private void UpdateAI()
        {
            foreach (var racer in racers)
            {
                if (racing && racer.Progress.Complete) continue;
                Vector3 target;
                if (racing)
                {
                    int next = racer.Progress.NextCheckpoint;
                    Vector3 forward = (track[(next + 1) % track.Count] - track[(next + track.Count - 1) % track.Count]).normalized;
                    target = track[next] + forward * 4 + Vector3.Cross(Vector3.up, forward) * Mathf.Sin(racer.Id * 2.1f) * 1.5f;
                }
                else if (mode == GameMode.ArenaClash && core != null)
                {
                    // One blue teammate; two orange opponents approach from behind the core.
                    bool blue = racer.Id == 1;
                    Vector3 goal = new Vector3(0, 1, blue ? 45 : -45);
                    Vector3 approach = (core.position - goal).normalized;
                    target = Vector3.Distance(racer.Car.transform.position, core.position + approach * 5) > 7 ? core.position + approach * 5 : core.position;
                }
                else target = Player.transform.position + Player.transform.forward * 2;
                if (battle != null && battle.TryPickupTarget(racer.Car, out Vector3 pickupTarget)) target = pickupTarget;
                Vector3 local = racer.Car.transform.InverseTransformPoint(target);
                float steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg / 35, -1, 1);
                bool sharp = Mathf.Abs(steer) > .65f;
                if (Physics.SphereCast(racer.Car.transform.position + Vector3.up * .5f + racer.Car.transform.forward * 2.8f, .6f, racer.Car.transform.forward, out RaycastHit hit, 7) && hit.rigidbody != null && hit.rigidbody != racer.Car.GetComponent<Rigidbody>()) steer += racer.Id % 2 == 0 ? .4f : -.4f;
                racer.Car.SetInput(new DriveInput { Throttle = sharp && racer.Car.SpeedKmh > 65 ? .2f : 1, Steering = Mathf.Clamp(steer, -1, 1), Brake = sharp && racer.Car.SpeedKmh > 95, Boost = !sharp && racer.Car.BoostAmount > .5f && local.z > 20 });
                racer.Stuck = racer.Car.SpeedKmh < 4 ? racer.Stuck + Time.deltaTime : 0;
                if (racer.Stuck > 4 || racer.Car.transform.position.y < -8 || Vector3.Dot(racer.Car.transform.up, Vector3.up) < -.5f)
                {
                    Recover(racer.Car, racing ? racer.Progress.NextCheckpoint : 0);
                    racer.Previous = racer.Car.transform.position;
                    racer.Stuck = 0;
                }
            }
        }
        private void Recover(VehicleController car, int checkpoint)
        {
            if (racing)
            {
                int previous = (checkpoint + track.Count - 1) % track.Count;
                Vector3 position = track[previous] + Vector3.up * 1.3f;
                car.Respawn(position, Quaternion.LookRotation(track[checkpoint] - track[previous]));
                if (car == Player) previousPlayer = position;
            }
            else car.Respawn(new Vector3(UnityEngine.Random.Range(-15, 15), 1.4f, UnityEngine.Random.Range(-15, 15)), Quaternion.identity);
        }

        public void RegisterImpact(VehicleController attacker, VehicleController victim, float impact)
        {
            if (battle != null) { battle.Hit(attacker, victim, impact); return; }
            if (!Running || Finished || Paused || countdown > 0 || mode != GameMode.BoostBattle || collisionCooldown > 0 || impact < 9) return;
            if (attacker != Player && victim != Player) return;
            // Both collision callbacks can fire: choose the vehicle moving into the contact faster.
            if (attacker.SpeedKmh < victim.SpeedKmh) return;
            if (attacker == Player) playerScore++; else rivalScore++;
            collisionCooldown = 1.5f;
            var body = victim.GetComponent<Rigidbody>();
            if (body != null) body.AddForce((victim.transform.position - attacker.transform.position).normalized * 12 + Vector3.up * 4, ForceMode.VelocityChange);
        }

        public void AwardCombatPoints(VehicleController attacker, VehicleController victim, int points)
        {
            if (!ActionEnabled || mode != GameMode.BoostBattle) return;
            if (attacker == Player) playerScore += points;
            else if (victim == Player) rivalScore += points;
        }

        private Material Material(string name, Color color, bool glow = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = name, color = color };
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 2.5f); }
            materials.Add(m);
            return m;
        }
        private GameObject Box(string name, Vector3 position, Vector3 scale, Material mat)
            => Box(name, position, scale, mat, Quaternion.identity);
        private GameObject Box(string name, Vector3 position, Vector3 scale, Material mat, Quaternion rotation)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(world); go.transform.position = position; go.transform.localScale = scale;
            go.transform.rotation = rotation;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
        private void BuildWorld()
        {
            Color[] colors = { new Color(.1f, .9f, 1), new Color(.7f, .3f, 1), new Color(1, .6f, .1f), new Color(1, .1f, .6f), new Color(.4f, .8f, 1) };
            Color accent = colors[environment];
            Material road = Material("Road", new Color(.07f, .09f, .13f));
            road.color = environment == 4 ? new Color(.5f,.65f,.72f) : new Color(.42f,.46f,.5f);
            road.SetTexture("_BaseMap", Resources.Load<Texture2D>("SurfaceTextures/Asphalt"));
            road.SetTextureScale("_BaseMap", new Vector2(4,4));
            road.SetTexture("_BumpMap", Resources.Load<Texture2D>("SurfaceTextures/AsphaltNormal"));
            road.EnableKeyword("_NORMALMAP"); road.SetFloat("_BumpScale", .5f);
            road.SetFloat("_Smoothness", environment == 4 ? .8f : environment == 0 || environment == 3 ? .65f : .25f);
            Material terrain = Material("Terrain", environment == 2 ? new Color(.38f, .23f, .12f) : environment == 4 ? new Color(.65f, .8f, .9f) : new Color(.025f, .035f, .07f));
            Material rail = Material("Energy rail", accent, true);
            Material architecture = Material("Architecture", new Color(.12f, .16f, .24f));
            var light = new GameObject("Sun", typeof(Light)); light.transform.SetParent(world); light.transform.rotation = Quaternion.Euler(48, -25, 0);
            light.GetComponent<Light>().type = LightType.Directional; light.GetComponent<Light>().intensity = environment == 2 ? 1.6f : 1.1f;
            light.GetComponent<Light>().shadows = LightShadows.Soft;
            light.GetComponent<Light>().color = environment == 2 ? new Color(1, .85f, .68f) : new Color(.75f, .85f, 1);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.3f, .4f, .55f);
            RenderSettings.ambientEquatorColor = new Color(.14f, .18f, .24f);
            RenderSettings.ambientGroundColor = new Color(.07f, .08f, .11f);
            RenderSettings.ambientLight = new Color(.35f, .4f, .5f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.04f, .06f, .12f); RenderSettings.fogDensity = .0025f;
            track.Clear();
            if (racing)
            {
                float radiusX = new[] { 62f, 76f, 90f, 58f, 72f }[environment];
                float radiusZ = new[] { 42f, 37f, 50f, 45f, 58f }[environment];
                const int segments = 32;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments;
                    float variation = environment == 4 ? Mathf.Sin(a * 3) * 5 : environment == 2 ? Mathf.Sin(a * 2) * 7 : 0;
                    track.Add(new Vector3(Mathf.Sin(a) * (radiusX + variation), 0, Mathf.Cos(a) * radiusZ));
                }
                for (int i = 0; i < segments; i++)
                {
                    Vector3 a = track[i], b = track[(i + 1) % segments];
                    Quaternion facing = Quaternion.LookRotation(b - a);
                    Vector3 middle = (a + b) / 2;
                    float length = Vector3.Distance(a, b) + 1.6f;
                    Box("Track segment " + i, middle - Vector3.up * .3f, new Vector3(20, .6f, length), road, facing);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Box("Barrier", middle + facing * Vector3.right * side * 10.3f + Vector3.up * .65f, new Vector3(.5f, 1.3f, length), architecture, facing);
                        Box("Light ribbon", middle + facing * Vector3.right * side * 10.3f + Vector3.up * 1.35f, new Vector3(.55f, .12f, length), rail, facing);
                    }
                    var gate = Box("Checkpoint " + i, a + Vector3.up * .025f, new Vector3(19, .05f, .3f), rail, facing);
                    Destroy(gate.GetComponent<Collider>());
                    if (i % 4 == 0)
                    {
                        Box("Gate left", a + facing * Vector3.left * 9.8f + Vector3.up * 3, new Vector3(.3f, 6, .3f), rail);
                        Box("Gate right", a + facing * Vector3.right * 9.8f + Vector3.up * 3, new Vector3(.3f, 6, .3f), rail);
                    }
                    if (environment == 1 && (i == 7 || i == 23))
                        Box("Skyfall launch ramp", middle + Vector3.up * .45f, new Vector3(7, .5f, 8), road, facing * Quaternion.Euler(-10, 0, 0));
                }
                if (environment != 1) Box("Landscape", new Vector3(0, -.9f, 0), new Vector3(260, 1, 220), terrain);
                else Box("Cloud deck", new Vector3(0, -30, 0), new Vector3(300, 1, 260), terrain);
            }
            else
            {
                Box("Arena floor", new Vector3(0, -.5f, 0), new Vector3(90, 1, 100), road);
                Box("West wall", new Vector3(-45, 3, 0), new Vector3(1, 6, 100), architecture);
                Box("East wall", new Vector3(45, 3, 0), new Vector3(1, 6, 100), architecture);
                Box("North wall", new Vector3(0, 3, 50), new Vector3(90, 6, 1), architecture);
                Box("South wall", new Vector3(0, 3, -50), new Vector3(90, 6, 1), architecture);
                foreach (int side in new[] { -1, 1 })
                {
                    Box("Side neon", new Vector3(side * 44.4f, 1, 0), new Vector3(.15f, .2f, 98), rail);
                    if (mode != GameMode.ArenaClash)
                        for (int j = -1; j <= 1; j++) Box("Jump ramp", new Vector3(side * 27, .75f, j * 23), new Vector3(9, .5f, 10), architecture, Quaternion.Euler(-17, side * 90, 0));
                }
                if (mode == GameMode.ArenaClash)
                {
                    foreach (int side in new[] { -1, 1 })
                    {
                        Material goal = Material("Goal", side > 0 ? new Color(1, .35f, .05f) : new Color(.1f, .4f, 1), true);
                        Box("Goal zone", new Vector3(0, .02f, side * 45), new Vector3(20, .04f, 8), goal);
                        Box("Goal post", new Vector3(-10, 3, side * 42), new Vector3(.5f, 6, .5f), goal);
                        Box("Goal post", new Vector3(10, 3, side * 42), new Vector3(.5f, 6, .5f), goal);
                        Box("Goal crossbar", new Vector3(0, 6, side * 42), new Vector3(20, .5f, .5f), goal);
                    }
                    var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "Energy core"; ball.transform.SetParent(world); ball.transform.position = Vector3.up * 3; ball.transform.localScale = Vector3.one * 3;
                    ball.GetComponent<Renderer>().sharedMaterial = Material("Core light", Color.white, true);
                    core = ball.AddComponent<Rigidbody>(); core.mass = 90; core.linearDamping = .2f; core.angularDamping = .3f; core.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    corePhysics = new PhysicsMaterial("Energy bounce") { bounciness = .6f, dynamicFriction = .2f, staticFriction = .2f };
                    ball.GetComponent<Collider>().sharedMaterial = corePhysics;
                }
            }
        }

        private void Cleanup()
        {
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            foreach (var s in temporaryStats) if (s != null) Destroy(s);
            foreach (var m in materials) if (m != null) Destroy(m);
            if (corePhysics != null) Destroy(corePhysics);
            temporaryStats.Clear(); materials.Clear(); racers.Clear(); cars.Clear(); track.Clear();
            Player = null; core = null; ghost = null; world = null; battle = null;
        }
    }

    public sealed class BattleContact : MonoBehaviour
    {
        public GameSession Session;
        public VehicleController Car;
        private void OnCollisionEnter(Collision collision)
        {
            var other = collision.gameObject.GetComponentInParent<VehicleController>();
            if (other != null && Session != null) Session.RegisterImpact(Car, other, collision.relativeVelocity.magnitude);
        }
    }
}
