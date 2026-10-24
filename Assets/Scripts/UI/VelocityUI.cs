using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VelocityRivals
{
    /// <summary>Runtime-built controller-navigable UI; no hand-wired scene references.</summary>
    public sealed class VelocityUI : MonoBehaviour
    {
        private GameSession session;
        private Canvas canvas;
        private RectTransform panel;
        private Font font;
        private Text hud;
        private Text hint;
        private Text title;
        private Text nowPlaying;
        private GameObject mapPanel;
        private SoundDirector sound;
        private GameMode selectedMode;
        private int selectedEnvironment;
        private bool showingResults;
        private LanRaceSession lan;
        private bool networkWasActive;
        private readonly Color cyan = new Color(0.12f, 0.9f, 0.95f);
        public static readonly Color[] Paints = {
            new Color(0.05f, 0.85f, 1), new Color(1, 0.18f, 0.32f), new Color(0.55f, 0.25f, 1),
            new Color(1, 0.75f, 0.1f), new Color(0.2f, 1, 0.4f), Color.white,
            new Color(1, 0.4f, 0.05f), new Color(0.12f, 0.15f, 0.22f)
        };
        private static readonly string[] PaintNames = { "Ion cyan", "Solar crimson", "Ultraviolet", "Championship gold", "Reactor green", "Arctic white", "Plasma orange", "Carbon midnight" };

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            session = FindFirstObjectByType<GameSession>();
            if (!session) session = gameObject.AddComponent<GameSession>();
            lan = FindFirstObjectByType<LanRaceSession>();
            sound = FindFirstObjectByType<SoundDirector>();
            if (!sound) sound = gameObject.AddComponent<SoundDirector>();
            if (!FindFirstObjectByType<EventSystem>())
            {
                var events = new GameObject("UI input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var canvasObject = new GameObject("Velocity UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            hud = Label(canvas.transform, "", 28, 160);
            var hr = hud.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0, 1);
            hr.pivot = new Vector2(0, 1);
            hr.anchoredPosition = new Vector2(35, -24);
            hr.sizeDelta = new Vector2(1450, 260);
            hud.alignment = TextAnchor.UpperLeft;
            hint = Label(canvas.transform, "", 20, 55);
            var br = hint.rectTransform;
            br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0);
            br.pivot = new Vector2(0.5f, 0);
            br.anchoredPosition = new Vector2(0, 20); br.sizeDelta = new Vector2(-50, 65);
            mapPanel = new GameObject("Race map", typeof(RectTransform), typeof(CanvasRenderer), typeof(RaceMap));
            mapPanel.transform.SetParent(canvas.transform, false);
            var map = mapPanel.GetComponent<RaceMap>();
            map.Session = session;
            map.raycastTarget = false;
            var mapRect = map.rectTransform;
            mapRect.anchorMin = mapRect.anchorMax = mapRect.pivot = Vector2.one;
            mapRect.anchoredPosition = new Vector2(-30, -30);
            mapRect.sizeDelta = new Vector2(260, 260);
            var mapCaption = Label(mapPanel.transform, "YOU ▲   •   NEXT GATE ■", 17, 25);
            mapCaption.rectTransform.anchorMin = new Vector2(0, 0);
            mapCaption.rectTransform.anchorMax = new Vector2(1, 0);
            mapCaption.rectTransform.pivot = new Vector2(.5f, 1);
            mapCaption.rectTransform.sizeDelta = new Vector2(0, 30);
            mapPanel.SetActive(false);
            session.MatchEnded += OnMatchEnded;
            ApplyQuality(SaveStore.Current.quality);
            MainMenu();
        }

        private void Update()
        {
            if (!session) return;
            mapPanel.SetActive(session.Running && !session.Finished && !session.Paused && !(lan != null && lan.Active));
            if (nowPlaying) nowPlaying.text = sound.PlaylistName.ToUpperInvariant() + " PLAYLIST\n" + sound.NowPlaying;
            if (lan != null && lan.Active)
            {
                if (!networkWasActive) { ClearPanel(); networkWasActive = true; }
                hud.text = "LAN GATE SPRINT\n" + lan.ConnectionStatus;
                hint.text = "Drive through the numbered gates in order. F8 opens LAN controls.\nLAN sessions continue running when menus lose focus.";
                return;
            }
            if (networkWasActive) { networkWasActive = false; MainMenu(); }
            if (title) title.color = Color.Lerp(Color.white, cyan, 0.25f + Mathf.Sin(Time.unscaledTime * 1.5f) * 0.15f);
            bool pausePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (pausePressed && session.Running && !session.Finished)
            {
                if (session.Paused) Resume(); else PauseMenu();
            }
            if (session.Paused && !panel) PauseMenu();
            if (session.Running && session.Player)
            {
                var car = session.Player;
                string clock = session.CurrentMode == GameMode.FreePlay ? "PRACTICE" : $"{session.RemainingTime:0.0}s REMAINING";
                hud.text = $"{session.CurrentMode}   |   {clock}\n" +
                    $"{car.SpeedKmh:0} km/h    BOOST {car.BoostAmount * 100:0}%\n{session.StatusText}";
                if (session.IsCircuit)
                {
                    var laps = session.LapTimes;
                    hud.text += $"\nLAP TIME {laps.CurrentLap(session.Elapsed):0.00}s   LAST {LapDisplay(laps.LastLap)}   BEST {LapDisplay(laps.BestLap)}";
                    if (session.WrongWay) hud.text += "\n<color=#FFB347>WRONG WAY — follow the route to the yellow gate</color>";
                }
                hint.text = "WASD / triggers + left stick: drive   •   Shift / B: boost   •   Space / A: jump   •   Ctrl / X: drift\nR / Y: recover   •   Q/E / LB + stick: air roll   •   Esc / Start: pause";
            }
            else { hud.text = ""; hint.text = "Mouse or arrows / D-pad to navigate • Enter / A to select"; }
        }

        private void OnDestroy() { if (session) session.MatchEnded -= OnMatchEnded; }

        private void ClearPanel()
        {
            if (panel) { panel.gameObject.SetActive(false); Destroy(panel.gameObject); }
            title = null;
            nowPlaying = null;
            panel = null;
        }
        private static string LapDisplay(float seconds) => seconds > 0 ? seconds.ToString("0.00") + "s" : "—";
        private void Page(string heading, string subtitle)
        {
            ClearPanel();
            var go = new GameObject(heading, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(canvas.transform, false);
            panel = go.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1040, 900);
            go.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.09f, 0.97f);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(44, 44, 28, 28);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            title = Label(panel, heading.ToUpperInvariant(), 48, 68);
            title.color = cyan;
            Label(panel, subtitle, 23, Mathf.Clamp(30 * subtitle.Split('\n').Length, 58, 250));
        }

        private Text Label(Transform parent, string text, int size, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size;
            label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return label;
        }

        private Button Button(string caption, Action action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            go.GetComponent<Image>().color = new Color(0.075f, 0.16f, 0.24f);
            go.GetComponent<LayoutElement>().preferredHeight = 57;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(0.4f, 1, 1);
            colors.selectedColor = new Color(0.4f, 1, 1); colors.pressedColor = new Color(0.2f, 0.65f, 0.8f);
            button.colors = colors;
            var text = Label(go.transform, caption, 25, 0);
            Stretch(text.rectTransform, 8);
            button.onClick.AddListener(() => { sound.Click(); action(); });
            return button;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin);
        }
        private void Focus(Button button)
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        private Button Cycle(string name, IList<string> values, Func<int> read, Action<int> write)
        {
            Button button = null;
            button = Button(name + ": " + values[Mathf.Clamp(read(), 0, values.Count - 1)], () => {
                int next = (read() + 1) % values.Count;
                write(next); button.GetComponentInChildren<Text>().text = name + ": " + values[next];
            });
            return button;
        }

        private void MainMenu()
        {
            showingResults = false;
            if (session.Running || session.Finished) session.ReturnToMenu();
            Page("Velocity Rivals", $"LEVEL {SaveStore.Current.Level} • {SaveStore.Current.xp} XP • {SaveStore.Current.wins} WINS\nAn original arcade racing and vehicle sports game");
            Focus(Button("PLAY", ModeMenu));
            Button("GARAGE", Garage);
            Button("RECORDS & ACHIEVEMENTS", Records);
            Button("SETTINGS", () => Settings(false));
            Button("HOW TO PLAY", () => HowToPlay(false));
            Button("MULTIPLAYER STATUS", Multiplayer);
            Button("CREDITS", Credits);
            Button("QUIT", () => Application.Quit());
            Label(panel, "Complete events to earn XP. Each 500 XP unlocks another vehicle.", 22, 58);
            if (!string.IsNullOrEmpty(SaveStore.LastError)) Label(panel, SaveStore.LastError, 18, 50);
        }

        private void ModeMenu()
        {
            Page("Choose your event", "Championship • arena sport • combat • stunts • time trial • training");
            Focus(Cycle("Mode", new[] { "Velocity Championship", "Arena Clash", "Boost Battle", "Stunt Rush", "Time Attack", "Free Play" }, () => (int)selectedMode, value => selectedMode = (GameMode)value));
            Cycle("Environment", GameSession.EnvironmentNames, () => selectedEnvironment, value => selectedEnvironment = value);
            Cycle("AI difficulty", new[] { "Rookie", "Rival", "Elite" }, () => SaveStore.Current.difficulty, value => { SaveStore.Current.difficulty = value; SaveStore.Save(); });
            var vehicles = new List<string>();
            for (int i = 0; i < SaveStore.Current.UnlockedVehicles; i++) vehicles.Add(GameSession.VehicleNames[i]);
            Cycle("Vehicle", vehicles, () => SaveStore.Current.selectedVehicle, value => { SaveStore.Current.selectedVehicle = value; SaveStore.Save(); });
            Label(panel, "Championship: ordered checkpoints, three laps.\nArena: move the core into the scoring zone. Battle: boost into opponents.\nStunts: jump, rotate and land. Time Attack: beat your record.", 23, 145);
            Button("START EVENT", StartMatch);
            Button("BACK", MainMenu);
        }

        private void StartMatch()
        {
            ClearPanel();
            showingResults = false;
            var p = SaveStore.Current;
            session.StartMatch(selectedMode, selectedEnvironment, p.selectedVehicle, Paints[Mathf.Clamp(p.paint, 0, Paints.Length - 1)]);
            if (session.Player) session.Player.SetCosmetics(p.wheels, p.trail, p.decal, p.spoiler, p.finish);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Garage()
        {
            var p = SaveStore.Current;
            Page("Garage", $"{p.UnlockedVehicles}/6 vehicles unlocked • Next unlock at {p.UnlockedVehicles * 500} XP\nSelections apply to your vehicle on the next event.");
            var preview = new GameObject("Paint and finish preview", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement), typeof(GaragePreview));
            preview.transform.SetParent(panel, false);
            preview.GetComponent<RawImage>().raycastTarget = false;
            preview.GetComponent<LayoutElement>().preferredHeight = 140;
            Focus(Cycle("Paint", PaintNames, () => p.paint, value => { p.paint = value; SaveStore.Save(); }));
            Cycle("Wheels", new[] { "Street", "Sport", "Aero" }, () => p.wheels, value => { p.wheels = value; SaveStore.Save(); });
            Cycle("Boost trail", new[] { "Ion", "Plasma", "Solar" }, () => p.trail, value => { p.trail = value; SaveStore.Save(); });
            Cycle("Decal", new[] { "Clean", "Twin stripes", "Race panel" }, () => p.decal, value => { p.decal = value; SaveStore.Save(); });
            Cycle("Spoiler", new[] { "Low profile", "Racing wing", "Aero fin" }, () => p.spoiler, value => { p.spoiler = value; SaveStore.Save(); });
            Cycle("Finish", new[] { "Gloss", "Matte", "Metallic" }, () => p.finish, value => { p.finish = value; SaveStore.Save(); });
            Button("BACK", MainMenu);
        }

        private void Settings(bool paused)
        {
            var p = SaveStore.Current;
            Page("Settings", "Audio levels and quality are saved on this computer.");
            string[] volumes = { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
            Focus(Cycle("Master volume", volumes, () => Mathf.RoundToInt(p.masterVolume * 10), value => { p.masterVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save(); }));
            Cycle("Music volume", volumes, () => Mathf.RoundToInt(p.musicVolume * 10), value => { p.musicVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save(); });
            Button("SOUNDTRACK", () => Soundtrack(paused));
            Cycle("Effects volume", volumes, () => Mathf.RoundToInt(p.effectsVolume * 10), value => { p.effectsVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save(); });
            Cycle("Graphics", new[] { "Performance", "Balanced", "Quality" }, () => p.quality, value => { p.quality = value; ApplyQuality(value); SaveStore.Save(); });
            Cycle("Steering sensitivity", new[] { "50%", "75%", "100%", "125%", "150%" }, () => Mathf.RoundToInt((p.steeringSensitivity - .5f) * 4), value => { p.steeringSensitivity = .5f + value * .25f; SaveStore.Save(); });
            Label(panel, "Keyboard: WASD drive, Shift boost, Space jump, Ctrl drift, Q/E roll, R recover.\nGamepad: RT/LT throttle/reverse, stick steer, A jump, B boost, X drift, Y recover.\nEsc / Start pauses. Button remapping is not implemented yet.", 22, 140);
            Button("BACK", () => { if (paused) PauseMenu(); else MainMenu(); });
        }

        private void Soundtrack(bool paused)
        {
            Page("Soundtrack", "Electronic / synthwave • music changes automatically between menus and events");
            nowPlaying = Label(panel, "", 32, 120);
            Label(panel, $"{sound.MenuTrackCount} menu tracks • {sound.RaceTrackCount} race tracks\nEach playlist shuffles without repeating a track until the cycle ends.", 24, 100);
            Focus(Button("NEXT TRACK", () => sound.NextTrack()));
            string[] volumes = { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
            Cycle("Music volume", volumes, () => Mathf.RoundToInt(SaveStore.Current.musicVolume * 10), value => {
                SaveStore.Current.musicVolume = value / 10f; sound.ApplyVolumes(); SaveStore.Save();
            });
            Button("BACK", () => Settings(paused));
        }

        private void HowToPlay(bool paused)
        {
            Page("How to play", "Start with Free Play in Cyber Dome to learn your car.");
            Label(panel, "DRIVE\nW / S accelerate and reverse • A / D steer\nShift boost • Ctrl handbrake • Space jump, press again for an aerial jump\nQ / E roll in the air • R recover your car\nController: triggers + left stick, B boost, X drift, A jump, Y recover.", 24, 205);
            Label(panel, "WIN YOUR EVENT\nChampionship / Time Attack: follow the yellow map gate and complete three laps.\nArena Clash: push the white core into the orange goal. First to five wins.\nBoost Battle: hit opponents at speed; collect shields, overdrive and pulse pickups.\nStunt Rush: jump, rotate and land upright. Reach 1,000 points before time runs out.", 23, 215);
            Label(panel, "Esc / Start pauses. Earn XP to unlock cars in the garage.\nCyan on the map is you, orange is another vehicle, yellow is the next gate.", 23, 85);
            Focus(Button("BACK", () => { if (paused) PauseMenu(); else MainMenu(); }));
        }

        private static void ApplyQuality(int quality)
        {
            int q = Mathf.Clamp(quality, 0, 2);
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(Mathf.Min(q, QualitySettings.names.Length - 1), true);
            QualitySettings.shadowDistance = q == 0 ? 30 : q == 1 ? 80 : 150;
            QualitySettings.lodBias = q == 0 ? 0.6f : q == 1 ? 1 : 1.5f;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.renderScale = q == 0 ? .75f : 1f;
                pipeline.msaaSampleCount = q == 0 ? 1 : q == 1 ? 2 : 4;
                pipeline.shadowDistance = q == 0 ? 30 : q == 1 ? 80 : 150;
            }
            Application.targetFrameRate = 60;
        }

        private void PauseMenu()
        {
            session.SetPaused(true);
            Page("Paused", "Take a breather. Your event is paused.");
            Focus(Button("RESUME", Resume));
            Button("RESTART EVENT", StartMatch);
            Button("SETTINGS", () => Settings(true));
            Button("HOW TO PLAY", () => HowToPlay(true));
            Button("MAIN MENU", MainMenu);
        }
        private void Resume() { ClearPanel(); session.SetPaused(false); if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
        private void OnMatchEnded()
        {
            if (showingResults) return;
            showingResults = true;
            sound.Finish();
            Page("Event complete", session.ResultText);
            Label(panel, $"LEVEL {SaveStore.Current.Level} • {SaveStore.Current.xp} XP\n{SaveStore.Current.UnlockedVehicles}/6 VEHICLES UNLOCKED", 30, 100);
            if (!string.IsNullOrEmpty(SaveStore.LastError)) Label(panel, SaveStore.LastError, 18, 70);
            if (session.CanAdvanceChampionship)
            {
                Focus(Button("NEXT CHAMPIONSHIP ROUND", () => {
                    ClearPanel(); showingResults = false; session.AdvanceChampionship();
                    var p = SaveStore.Current;
                    if (session.Player) session.Player.SetCosmetics(p.wheels, p.trail, p.decal, p.spoiler, p.finish);
                    if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                }));
                Button("RESTART CHAMPIONSHIP", StartMatch);
            }
            else Focus(Button("RACE AGAIN", StartMatch));
            Button("MAIN MENU", MainMenu);
        }

        private void Records()
        {
            Page("Personal records", "Local records only. Online leaderboards are not connected.");
            string text = "";
            foreach (var r in SaveStore.Current.records)
                text += $"{r.mode}  •  {r.finishes} events  •  Best {r.bestSeconds:0.00}s  •  Score {r.bestScore}\n";
            Label(panel, string.IsNullOrEmpty(text) ? "Finish your first event to set a record." : text, 23, 280);
            Label(panel, "ACHIEVEMENTS\n" + (SaveStore.Current.achievements.Count > 0 ? string.Join(" • ", SaveStore.Current.achievements) : "First finish • First victory • Ten victories • Full garage • Stunt specialist"), 23, 130);
            Focus(Button("BACK", MainMenu));
        }
        private void Multiplayer()
        {
            Page("LAN gate sprint", "Experimental two-player direct-IP multiplayer • Unity playtesting pending");
            Label(panel, "Host on one PC and join its LAN IP from another. For two instances on one PC use 127.0.0.1.\n\nBoth players race through four gates for two minutes. The host simulates vehicles and decides results.\n\nNo public matchmaking, account service or console networking is connected.", 23, 230);
            var fieldObject = new GameObject("Host IP", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            fieldObject.transform.SetParent(panel, false);
            fieldObject.GetComponent<Image>().color = new Color(.08f, .12f, .18f);
            fieldObject.GetComponent<LayoutElement>().preferredHeight = 55;
            var address = fieldObject.GetComponent<InputField>();
            var label = Label(fieldObject.transform, "", 25, 0);
            Stretch(label.rectTransform, 10);
            address.textComponent = label;
            address.characterLimit = 45;
            address.text = "127.0.0.1";
            Focus(Button("HOST LAN SESSION", () => { if (lan != null) lan.StartHost(); }));
            Button("JOIN ENTERED IP", () => { if (lan != null) lan.Join(address.text.Trim()); });
            Label(panel, lan != null ? lan.ConnectionStatus : "LAN component is missing from the entry scene.", 20, 60);
            Button("BACK", MainMenu);
        }
        private void Credits()
        {
            Page("Credits", "VELOCITY RIVALS • Original game project for DJKFusion");
            Label(panel, "Original vehicles, environments and electronic soundtrack.\nMidnight Grid • Neon Pursuit • Afterburn\n\nBuilt with Unity and the Universal Render Pipeline.\nVehicle models created with Blender.\n\nGame project for DJKFusion.", 25, 350);
            Focus(Button("BACK", MainMenu));
        }
    }
}
