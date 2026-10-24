using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    /// <summary>Two-player host-authoritative matches over direct LAN or a private WebSocket relay room.</summary>
    public sealed class LanRaceSession : MonoBehaviour
    {
        public const int ProtocolVersion = 3;
        public const int Port = 7777;
        public static readonly string[] ModeNames = { "Gate Sprint", "Circuit Duel", "Arena Duel" };
        [Serializable] sealed class Packet
        {
            public string kind;
            public int version, round, mode, environment, hostCar, guestCar, phase, winner;
            public float throttle, steering, stamp, seconds, countdown, fuel0, fuel1;
            public bool brake, boost, jump, airRoll, recover, boosting0, boosting1, grounded0, grounded1;
            public Vector3 position0, position1, velocity0, velocity1, corePosition;
            public Quaternion rotation0, rotation1;
            public int score0, score1, gate0, gate1, lap0, lap1;
        }
        [Serializable] sealed class RelayControl { public string relay, code, message; public int version; }
        [Serializable] sealed class RoomRequest { public string kind, code; public int version = ProtocolVersion; }

        abstract class Peer : IDisposable
        {
            public float LastReceived { get; protected set; } = Time.realtimeSinceStartup;
            public virtual bool Paired => true;
            public virtual string RoomCode => "";
            public abstract void Send(Packet packet);
            public abstract void Pump(Action<Packet> receive);
            public abstract void Dispose();
            protected string Encode(Packet packet) { packet.version = ProtocolVersion; return JsonUtility.ToJson(packet); }
            protected void Deliver(string json, Action<Packet> receive)
            {
                var packet = JsonUtility.FromJson<Packet>(json);
                if (packet == null || packet.version != ProtocolVersion) throw new InvalidOperationException("Both players need the same latest game version.");
                LastReceived = Time.realtimeSinceStartup; receive(packet);
            }
        }
        sealed class TcpPeer : Peer
        {
            readonly Socket socket;
            readonly LanFrameBuffer incoming = new LanFrameBuffer();
            readonly Queue<byte[]> outgoing = new Queue<byte[]>();
            readonly byte[] buffer = new byte[8192];
            int sent;
            public TcpPeer(Socket value) { socket = value; socket.Blocking = false; socket.NoDelay = true; }
            public override void Send(Packet packet)
            {
                if (outgoing.Count >= 100) throw new InvalidOperationException("Peer cannot keep up with snapshots.");
                outgoing.Enqueue(LanFrameBuffer.Encode(System.Text.Encoding.UTF8.GetBytes(Encode(packet))));
            }
            public override void Pump(Action<Packet> receive)
            {
                int reads = 0;
                while (reads++ < 8 && socket.Poll(0, SelectMode.SelectRead))
                {
                    int count;
                    try { count = socket.Receive(buffer); }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock) { break; }
                    if (count == 0) throw new InvalidOperationException("The other player disconnected.");
                    incoming.Feed(buffer, count);
                    while (incoming.TryRead(out var payload)) Deliver(System.Text.Encoding.UTF8.GetString(payload), receive);
                }
                while (outgoing.Count > 0 && socket.Poll(0, SelectMode.SelectWrite))
                {
                    byte[] bytes = outgoing.Peek(); int count;
                    try { count = socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None); }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock) { break; }
                    if (count == 0) break;
                    sent += count;
                    if (sent == bytes.Length) { sent = 0; outgoing.Dequeue(); }
                }
            }
            public override void Dispose() { try { socket.Close(); } catch (SocketException) { } }
        }
        sealed class RelayPeer : Peer
        {
            readonly RelayTransport transport;
            bool paired;
            string code = "";
            public override bool Paired => paired;
            public override string RoomCode => code;
            public RelayPeer(string endpoint, bool host, string room)
            {
                transport = new RelayTransport(endpoint);
                transport.Send(JsonUtility.ToJson(new RoomRequest { kind = host ? "create" : "join", code = room }));
            }
            public override void Send(Packet packet) => transport.Send(Encode(packet));
            public override void Pump(Action<Packet> receive)
            {
                while (transport.TryReceive(out string json))
                {
                    var control = JsonUtility.FromJson<RelayControl>(json);
                    if (!string.IsNullOrEmpty(control.relay))
                    {
                        if (control.relay == "error" || control.relay == "closed") throw new InvalidOperationException(control.message);
                        if (control.version != ProtocolVersion) throw new InvalidOperationException("Relay and game versions differ.");
                        if (control.relay != "created" && control.relay != "paired") throw new InvalidOperationException("Unknown relay response.");
                        code = control.code; paired = control.relay == "paired"; LastReceived = Time.realtimeSinceStartup;
                    }
                    else Deliver(json, receive);
                }
                if (transport.Error != null) throw new InvalidOperationException(transport.Error);
            }
            public override void Dispose() => transport.Dispose();
        }

        public bool Active => hosting || peer != null || connecting != null;
        public bool RoundActive => loaded && Phase != LanPhase.Complete;
        public string ConnectionStatus { get; private set; } = "Create a private online room or join a LAN host.";
        public string RoomCode => peer != null ? peer.RoomCode : "";
        public float PingMilliseconds { get; private set; }
        public LanMode SelectedMode { get; set; } = LanMode.CircuitDuel;
        public int SelectedEnvironment { get; set; }
        public LanPhase Phase => rules != null ? rules.Phase : snapshot != null ? (LanPhase)snapshot.phase : LanPhase.Waiting;
        public string MatchStatus
        {
            get
            {
                if (!loaded) return RoomCode.Length > 0 ? "ROOM CODE: " + RoomCode : ConnectionStatus;
                int mine = hosting ? 0 : 1;
                int a = rules != null ? rules.Score(0) : snapshot != null ? snapshot.score0 : 0;
                int b = rules != null ? rules.Score(1) : snapshot != null ? snapshot.score1 : 0;
                float time = rules != null ? rules.Remaining : snapshot != null ? snapshot.seconds : 0;
                float countdown = rules != null ? rules.Countdown : snapshot != null ? snapshot.countdown : 3;
                int next = rules != null ? rules.NextGate(mine) : snapshot != null ? (mine == 0 ? snapshot.gate0 : snapshot.gate1) : 0;
                string extra = SelectedMode == LanMode.CircuitDuel ? "LAP " + (rules != null ? rules.Lap(mine) : snapshot != null ? (mine == 0 ? snapshot.lap0 : snapshot.lap1) : 1) + "/3 · NEXT GATE " + (next + 1) : "HOST " + a + " — " + b + " GUEST";
                if (Phase == LanPhase.Complete)
                {
                    int winner = rules != null ? rules.Winner : snapshot.winner;
                    return winner < 0 ? "DRAW — host can start a rematch" : (winner == mine ? "YOU WIN" : "RIVAL WINS") + " — host can start a rematch";
                }
                if (Phase == LanPhase.Waiting) return "Waiting for the other player's world to load…";
                if (Phase == LanPhase.Countdown) return "GET READY — " + Mathf.CeilToInt(countdown);
                return ModeNames[(int)SelectedMode] + " · " + time.ToString("0") + "s · " + extra + " · " + PingMilliseconds.ToString("0") + " ms";
            }
        }

        readonly Vector3[] sprintGates = { new Vector3(0, 0, 24), new Vector3(15, 0, 0), new Vector3(0, 0, -24), new Vector3(-15, 0, 0) };
        readonly Vector3[] previous = new Vector3[2];
        readonly float[] recoverAt = new float[2];
        readonly float[] invertedFor = new float[2];
        readonly List<Material> markerMaterials = new List<Material>();
        TcpListener listener;
        TcpClient connectingClient;
        Task connecting;
        Peer peer;
        LanMatchRules rules;
        bool hosting, online, showPanel, pendingJump, pendingRecover, loaded, guestKnown, helloSent, guestLoaded;
        string address = "127.0.0.1";
        float connectionStarted, nextSend, nextPing, remoteInputReceived, goalAt;
        int round, hostVehicle, guestVehicle, lastCountdown = -1, lastGoalScore;
        LanPhase lastPhase;
        VehicleController localCar, otherCar;
        Rigidbody core;
        GameObject markers;
        DriveInput remoteInput;
        bool remoteRecover;
        Packet snapshot;
        VehicleStats networkStats;

        public void StartHost()
        {
            StopSession();
            try
            {
                hostVehicle = SaveStore.Current.selectedVehicle;
                listener = new TcpListener(IPAddress.Any, Port); listener.Start(2);
                hosting = true; showPanel = true;
                ConnectionStatus = "LAN host on TCP 7777. Waiting for your friend.";
            }
            catch (Exception ex) { Fail(ex); }
        }
        public void Join(string host)
        {
            StopSession();
            if (!IPAddress.TryParse(host, out var ip)) { ConnectionStatus = "Enter the host's numeric LAN IP address."; return; }
            try
            {
                connectingClient = new TcpClient(ip.AddressFamily); connecting = connectingClient.ConnectAsync(ip, Port);
                connectionStarted = Time.realtimeSinceStartup; showPanel = true;
                ConnectionStatus = "Connecting to " + host + "…";
            }
            catch (Exception ex) { Fail(ex); }
        }
        public void CreateOnlineRoom(string endpoint) => OpenOnline(endpoint, "", true);
        public void JoinOnlineRoom(string endpoint, string code) => OpenOnline(endpoint, code.Trim().ToUpperInvariant(), false);
        void OpenOnline(string endpoint, string code, bool host)
        {
            StopSession();
            try
            {
                if (!host && (code.Length != 8 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-HJ-NP-Z2-9]{8}$")))
                    throw new ArgumentException("Enter the eight-character room code from your friend.");
                peer = new RelayPeer(endpoint.Trim(), host, code);
                hosting = host; online = true; showPanel = true;
                hostVehicle = SaveStore.Current.selectedVehicle;
                connectionStarted = Time.realtimeSinceStartup;
                ConnectionStatus = "Connecting to the online relay…";
            }
            catch (Exception ex) { Fail(ex); }
        }
        public void StartRound()
        {
            if (!hosting || !guestKnown || (loaded && Phase != LanPhase.Complete)) return;
            try
            {
                round++; PrepareWorld(false);
                peer.Send(new Packet { kind = "start", round = round, mode = (int)SelectedMode, environment = SelectedEnvironment, hostCar = hostVehicle, guestCar = guestVehicle });
                ConnectionStatus = "Loading both players…"; showPanel = false;
            }
            catch (Exception ex) { Fail(ex); }
        }
        void PrepareWorld(bool client)
        {
            ClearWorld();
            var session = GameSession.Instance;
            if (!session) throw new InvalidOperationException("Open the Velocity Rivals entry scene.");
            GameMode mode = SelectedMode == LanMode.CircuitDuel ? GameMode.TimeAttack : SelectedMode == LanMode.ArenaDuel ? GameMode.ArenaClash : GameMode.FreePlay;
            session.StartMatch(mode, SelectedEnvironment, client ? guestVehicle : hostVehicle, client ? new Color(1, .3f, .1f) : Color.cyan, true);
            localCar = session.Player;
            int otherModel = client ? hostVehicle : guestVehicle;
            var template = Resources.Load<VehicleStats>("VehicleStats/" + GameSession.VehicleNames[otherModel]);
            networkStats = template ? Instantiate(template) : ScriptableObject.CreateInstance<VehicleStats>();
            networkStats.DisplayName = GameSession.VehicleNames[otherModel];
            if (SelectedEnvironment == 4) networkStats.Grip *= .45f;
            otherCar = VehicleController.Spawn("Network Rival", Vector3.zero, Quaternion.identity, networkStats, client ? Color.cyan : new Color(1, .3f, .1f), false);
            otherCar.transform.SetParent(localCar.transform.parent);
            localCar.AllowLocalRecovery = otherCar.AllowLocalRecovery = false;
            localCar.ControlsEnabled = otherCar.ControlsEnabled = false;
            core = session.NetworkCore;
            if (client)
            {
                MakeProxy(localCar); MakeProxy(otherCar);
                if (core) { core.isKinematic = true; foreach (var collider in core.GetComponents<Collider>()) collider.enabled = false; }
            }
            rules = client ? null : new LanMatchRules(SelectedMode, SelectedMode == LanMode.CircuitDuel ? session.Circuit.Count : sprintGates.Length);
            RecoverCar(0, true); RecoverCar(1, true);
            loaded = true; guestLoaded = false; snapshot = null; nextSend = 0; goalAt = 0;
            remoteInput = default; remoteRecover = pendingJump = pendingRecover = false;
            lastCountdown = -1; lastPhase = LanPhase.Waiting; lastGoalScore = 0;
            if (SelectedMode == LanMode.GateSprint)
            {
                markers = new GameObject("Sprint gates"); markers.transform.SetParent(localCar.transform.parent);
                for (int i = 0; i < sprintGates.Length; i++)
                {
                    var gate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); gate.transform.SetParent(markers.transform);
                    gate.transform.position = sprintGates[i] + Vector3.up * .12f; gate.transform.localScale = new Vector3(10, .1f, 10);
                    gate.GetComponent<Collider>().enabled = false; Destroy(gate.GetComponent<Collider>());
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.color = Color.Lerp(Color.cyan, Color.magenta, i / 3f); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", material.color * 2);
                    gate.GetComponent<Renderer>().sharedMaterial = material; markerMaterials.Add(material);
                }
            }
        }
        static void MakeProxy(VehicleController car)
        {
            car.SetNetworkPresentation(Vector3.zero, 1, false, false);
            car.GetComponent<Rigidbody>().isKinematic = true;
            foreach (var collider in car.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        VehicleController Car(int player) => (player == 0) == hosting ? localCar : otherCar;
        void RecoverCar(int player, bool spawn = false)
        {
            var car = Car(player); if (!car) return;
            if (!spawn && Time.realtimeSinceStartup < recoverAt[player]) return;
            recoverAt[player] = Time.realtimeSinceStartup + 2;
            invertedFor[player] = 0;
            Vector3 position; Quaternion rotation;
            if (SelectedMode == LanMode.CircuitDuel)
            {
                var track = GameSession.Instance.Circuit;
                int next = !spawn && rules != null ? rules.NextGate(player) : 1;
                int before = (next + track.Count - 1) % track.Count;
                rotation = Quaternion.LookRotation(track[next] - track[before]);
                position = track[before] + Vector3.up * 1.4f + rotation * Vector3.right * (player == 0 ? -3 : 3);
            }
            else { position = new Vector3(SelectedMode == LanMode.GateSprint ? player * 6 - 3 : 0, 1.4f, SelectedMode == LanMode.ArenaDuel ? (player == 0 ? -18 : 18) : 0); rotation = Quaternion.Euler(0, SelectedMode == LanMode.ArenaDuel && player == 1 ? 180 : 0, 0); }
            if (car.NetworkProxy) car.transform.SetPositionAndRotation(position, rotation);
            else car.Respawn(position, rotation);
            previous[player] = position;
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) showPanel = !showPanel;
            if (!Active) return;
            try
            {
                if (connecting != null)
                {
                    if (Time.realtimeSinceStartup - connectionStarted > 10) throw new TimeoutException("LAN connection timed out.");
                    if (!connecting.IsCompleted) return;
                    if (connecting.IsFaulted) throw connecting.Exception.GetBaseException();
                    if (connecting.IsCanceled) throw new InvalidOperationException("Connection cancelled.");
                    peer = new TcpPeer(connectingClient.Client); connecting = null; connectingClient = null;
                }
                if (hosting && listener != null && listener.Pending())
                {
                    var accepted = listener.AcceptTcpClient();
                    if (peer != null) accepted.Close(); else peer = new TcpPeer(accepted.Client);
                }
                peer?.Pump(Receive);
                if (online && peer != null && !peer.Paired)
                {
                    ConnectionStatus = RoomCode.Length > 0 ? "Share room " + RoomCode + " with your friend." : "Waking the relay… this can take up to a minute.";
                    if (RoomCode.Length == 0 && Time.realtimeSinceStartup - connectionStarted > 65) throw new TimeoutException("Relay did not respond. Check its hosting address.");
                }
                if (peer != null && peer.Paired)
                {
                    if (!hosting && !helloSent) { peer.Send(new Packet { kind = "hello", guestCar = SaveStore.Current.selectedVehicle }); helloSent = true; ConnectionStatus = "Connected. Waiting for the host to start."; }
                    if (Time.realtimeSinceStartup - peer.LastReceived > 15) throw new TimeoutException("Other player stopped responding.");
                    if (Time.realtimeSinceStartup >= nextPing) { nextPing = Time.realtimeSinceStartup + 2; peer.Send(new Packet { kind = "ping", stamp = Time.realtimeSinceStartup }); }
                }
                if (!loaded) return;
                if (!localCar || !otherCar || !GameSession.Instance || !GameSession.Instance.Running) throw new InvalidOperationException("The network world was closed.");
                if (hosting) UpdateHost(); else UpdateGuest();
                PlayCues();
            }
            catch (Exception ex) { Fail(ex); }
        }
        void UpdateHost()
        {
            rules.Tick(Time.unscaledDeltaTime);
            bool driving = guestLoaded && Phase == LanPhase.Racing;
            localCar.ControlsEnabled = otherCar.ControlsEnabled = driving;
            if (driving)
            {
                if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)) RecoverCar(0);
                for (int i = 0; i < 2; i++)
                {
                    var car = Car(i);
                    invertedFor[i] = Vector3.Dot(car.transform.up, Vector3.up) < .15f && car.SpeedKmh < 5 ? invertedFor[i] + Time.unscaledDeltaTime : 0;
                    if (invertedFor[i] > 3 || car.transform.position.y < -12 || car.transform.position.sqrMagnitude > 90000) RecoverCar(i);
                    CheckGate(car, i);
                }
                if (core)
                {
                    if (core.position.y < 6 && core.position.y > -1 && Mathf.Abs(core.position.x) < 10 && Mathf.Abs(core.position.z) > 41 && Time.realtimeSinceStartup >= goalAt)
                    {
                        rules.Goal(core.position.z > 0 ? 0 : 1); goalAt = Time.realtimeSinceStartup + 2;
                        MatchEffects.Burst(core.position, Color.cyan);
                        ResetCore(); RecoverCar(0, true); RecoverCar(1, true);
                    }
                    if (core.position.y < -5 || core.position.sqrMagnitude > 15000) ResetCore();
                }
            }
            if (Phase != LanPhase.Racing) { localCar.ControlsEnabled = otherCar.ControlsEnabled = false; Freeze(Car(0)); Freeze(Car(1)); }
            if (peer != null && Time.realtimeSinceStartup >= nextSend)
            {
                nextSend = Time.realtimeSinceStartup + .05f;
                peer.Send(new Packet { kind = "state", round = round, phase = (int)Phase, winner = rules.Winner,
                    seconds = rules.Remaining, countdown = rules.Countdown, score0 = rules.Score(0), score1 = rules.Score(1), gate0 = rules.NextGate(0), gate1 = rules.NextGate(1), lap0 = rules.Lap(0), lap1 = rules.Lap(1),
                    position0 = localCar.transform.position, position1 = otherCar.transform.position, rotation0 = localCar.transform.rotation, rotation1 = otherCar.transform.rotation,
                    velocity0 = localCar.Velocity, velocity1 = otherCar.Velocity, fuel0 = localCar.BoostAmount, fuel1 = otherCar.BoostAmount, boosting0 = localCar.IsBoosting, boosting1 = otherCar.IsBoosting,
                    grounded0 = localCar.Grounded, grounded1 = otherCar.Grounded, corePosition = core ? core.position : Vector3.zero });
            }
        }
        static void Freeze(VehicleController car) { var body = car.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        void ResetCore() { core.position = new Vector3(0, 3, 0); core.linearVelocity = core.angularVelocity = Vector3.zero; }
        void UpdateGuest()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            pendingJump |= (k != null && k.spaceKey.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame);
            pendingRecover |= (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
            if (Time.realtimeSinceStartup >= nextSend)
            {
                nextSend = Time.realtimeSinceStartup + .05f;
                float throttle = k == null ? 0 : (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
                float steer = k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                if (k != null && (k.qKey.isPressed || k.eKey.isPressed)) steer = (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0);
                if (g != null) { throttle += g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue(); steer += g.leftStick.x.ReadValue(); }
                peer.Send(new Packet { kind = "input", round = round, throttle = Mathf.Clamp(throttle, -1, 1), steering = Mathf.Clamp(steer * SaveStore.Current.steeringSensitivity, -1, 1),
                    brake = (k != null && k.leftCtrlKey.isPressed) || (g != null && g.buttonWest.isPressed), boost = (k != null && k.leftShiftKey.isPressed) || (g != null && g.buttonEast.isPressed),
                    jump = pendingJump, recover = pendingRecover, airRoll = (k != null && (k.qKey.isPressed || k.eKey.isPressed)) || (g != null && g.leftShoulder.isPressed) });
                pendingJump = pendingRecover = false;
            }
            if (snapshot == null) return;
            float blend = 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime);
            Present(localCar, snapshot.position1, snapshot.rotation1, snapshot.velocity1, snapshot.fuel1, snapshot.boosting1, snapshot.grounded1, blend);
            Present(otherCar, snapshot.position0, snapshot.rotation0, snapshot.velocity0, snapshot.fuel0, snapshot.boosting0, snapshot.grounded0, blend);
            if (core) core.transform.position = Vector3.Lerp(core.transform.position, snapshot.corePosition, blend);
        }
        static void Present(VehicleController car, Vector3 position, Quaternion rotation, Vector3 velocity, float fuel, bool boosting, bool grounded, float blend)
        {
            if (Vector3.Distance(car.transform.position, position) > 10) blend = 1;
            car.transform.SetPositionAndRotation(Vector3.Lerp(car.transform.position, position, blend), Quaternion.Slerp(car.transform.rotation, rotation, blend));
            car.SetNetworkPresentation(velocity, fuel, boosting, grounded);
        }
        void FixedUpdate()
        {
            if (!hosting || !loaded || !otherCar) return;
            if (Time.realtimeSinceStartup - remoteInputReceived > .5f || Phase != LanPhase.Racing) { remoteInput = default; remoteRecover = false; }
            if (remoteRecover) { RecoverCar(1); remoteRecover = false; }
            otherCar.SetInput(remoteInput); remoteInput.Jump = false;
        }
        void CheckGate(VehicleController car, int player)
        {
            if (SelectedMode == LanMode.ArenaDuel) return;
            int next = rules.NextGate(player); Vector3 position = car.transform.position;
            if (SelectedMode == LanMode.GateSprint)
            {
                Vector3 delta = position - sprintGates[next];
                if (new Vector2(delta.x, delta.z).sqrMagnitude < 25 && Mathf.Abs(delta.y) < 5) rules.CrossGate(player, next, true);
            }
            else
            {
                var track = GameSession.Instance.Circuit; Vector3 gate = track[next];
                Vector3 forward = (track[(next + 1) % track.Count] - track[(next + track.Count - 1) % track.Count]).normalized;
                float before = Vector3.Dot(previous[player] - gate, forward), after = Vector3.Dot(position - gate, forward);
                if (before <= 0 && after > 0)
                {
                    Vector3 crossing = Vector3.Lerp(previous[player], position, -before / (after - before)) - gate;
                    if (Mathf.Abs(Vector3.Dot(crossing, Vector3.Cross(Vector3.up, forward))) < 10 && Mathf.Abs(crossing.y) < 8) rules.CrossGate(player, next, true);
                }
            }
            previous[player] = position;
        }
        void Receive(Packet message)
        {
            if (message.kind == "ping") { if (!Finite(message.stamp)) throw new InvalidOperationException("Invalid ping."); peer.Send(new Packet { kind = "pong", stamp = message.stamp }); return; }
            if (message.kind == "pong") { if (!Finite(message.stamp)) throw new InvalidOperationException("Invalid pong."); PingMilliseconds = Mathf.Clamp((Time.realtimeSinceStartup - message.stamp) * 1000, 0, 60000); return; }
            if (hosting)
            {
                if (message.kind == "hello" && !guestKnown)
                {
                    if (message.guestCar < 0 || message.guestCar >= 6) throw new InvalidOperationException("Invalid guest vehicle.");
                    guestVehicle = message.guestCar; guestKnown = true;
                    peer.Send(new Packet { kind = "lobby", mode = (int)SelectedMode, environment = SelectedEnvironment });
                    ConnectionStatus = "Your friend is ready. Press START MATCH."; return;
                }
                if (message.kind == "loaded" && message.round == round && loaded && !guestLoaded)
                { guestLoaded = true; rules.Start(); ConnectionStatus = "Both players loaded."; return; }
                if (message.kind != "input" || !Finite(message.throttle) || !Finite(message.steering)) throw new InvalidOperationException("Invalid client input.");
                if (!loaded || message.round != round) return;
                remoteInput = new DriveInput { Throttle = Mathf.Clamp(message.throttle, -1, 1), Steering = Mathf.Clamp(message.steering, -1, 1), Brake = message.brake, Boost = message.boost, Jump = remoteInput.Jump || message.jump, AirRoll = message.airRoll };
                remoteRecover |= message.recover; remoteInputReceived = Time.realtimeSinceStartup;
            }
            else if (message.kind == "lobby" || message.kind == "start")
            {
                if (message.mode < 0 || message.mode > 2 || message.environment < 0 || message.environment >= 5) throw new InvalidOperationException("Invalid match configuration.");
                SelectedMode = (LanMode)message.mode; SelectedEnvironment = message.environment;
                if (message.kind == "lobby") { ConnectionStatus = ModeNames[message.mode] + " — waiting for the host."; return; }
                if (message.round != round + 1 || message.hostCar < 0 || message.hostCar >= 6 || message.guestCar < 0 || message.guestCar >= 6) throw new InvalidOperationException("Invalid round or vehicle selection.");
                round = message.round; hostVehicle = message.hostCar; guestVehicle = message.guestCar; PrepareWorld(true);
                peer.Send(new Packet { kind = "loaded", round = round }); showPanel = false;
            }
            else if (message.kind == "state")
            {
                if (!loaded || message.round != round) return;
                ValidateState(message); snapshot = message;
            }
            else throw new InvalidOperationException("Unexpected network message.");
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool ValidVector(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z) && v.sqrMagnitude < 100000000;
        static bool ValidRotation(Quaternion q) { float n = q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w; return Finite(n) && n > .9f && n < 1.1f; }
        void ValidateState(Packet p)
        {
            int gates = SelectedMode == LanMode.CircuitDuel ? GameSession.Instance.Circuit.Count : 4;
            if (!ValidVector(p.position0) || !ValidVector(p.position1) || !ValidVector(p.velocity0) || !ValidVector(p.velocity1) || !ValidVector(p.corePosition) ||
                !ValidRotation(p.rotation0) || !ValidRotation(p.rotation1) || !Finite(p.seconds) || p.seconds < 0 || p.seconds > 600 ||
                !Finite(p.countdown) || p.countdown < 0 || p.countdown > 3 || !Finite(p.fuel0) || !Finite(p.fuel1) || p.fuel0 < 0 || p.fuel0 > 1 || p.fuel1 < 0 || p.fuel1 > 1 ||
                p.phase < 0 || p.phase > 3 || p.winner < -1 || p.winner > 1 || p.score0 < 0 || p.score1 < 0 || p.score0 > 10000 || p.score1 > 10000 ||
                p.gate0 < 0 || p.gate0 >= gates || p.gate1 < 0 || p.gate1 >= gates || p.lap0 < 0 || p.lap0 > 3 || p.lap1 < 0 || p.lap1 > 3)
                throw new InvalidOperationException("Invalid host state.");
        }
        void PlayCues()
        {
            int beat = Mathf.CeilToInt(rules != null ? rules.Countdown : snapshot != null ? snapshot.countdown : 3);
            if (Phase == LanPhase.Countdown && beat != lastCountdown) { SoundDirector.Instance?.Cue(GameCue.Countdown); lastCountdown = beat; }
            if (Phase == LanPhase.Racing && lastPhase != LanPhase.Racing) SoundDirector.Instance?.Cue(GameCue.Go);
            if (Phase == LanPhase.Complete && lastPhase != LanPhase.Complete) { SoundDirector.Instance?.Finish(); showPanel = true; }
            int goals = rules != null ? rules.Score(0) + rules.Score(1) : snapshot != null ? snapshot.score0 + snapshot.score1 : 0;
            if (SelectedMode == LanMode.ArenaDuel && goals > lastGoalScore) SoundDirector.Instance?.Cue(GameCue.Goal);
            lastGoalScore = goals; lastPhase = Phase;
        }
        void OnGUI()
        {
            if (!showPanel && (!Active || loaded)) return;
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUILayout.BeginArea(new Rect(24, 150, 410, 320), GUI.skin.box);
            GUILayout.Label("VELOCITY RIVALS · " + (online ? "PRIVATE ONLINE ROOM" : "LAN MULTIPLAYER"));
            GUILayout.Label(ConnectionStatus);
            if (RoomCode.Length > 0) { GUILayout.Label("ROOM CODE: " + RoomCode); if (GUILayout.Button("COPY ROOM CODE")) GUIUtility.systemCopyBuffer = RoomCode; }
            if (!Active)
            {
                address = GUILayout.TextField(address, 64);
                if (GUILayout.Button("Host LAN session")) StartHost();
                if (GUILayout.Button("Join entered LAN IP")) Join(address);
                GUILayout.Label("Use the main MULTIPLAYER menu for online rooms.");
                if (GUILayout.Button("CLOSE")) showPanel = false;
            }
            else
            {
                GUILayout.Label(MatchStatus);
                if (hosting && guestKnown && (!loaded || Phase == LanPhase.Complete))
                    if (GUILayout.Button(loaded ? "REMATCH" : "START MATCH")) StartRound();
                if (loaded && Phase == LanPhase.Racing && SelectedMode == LanMode.GateSprint)
                    GUILayout.Label("Next gate: " + ((rules != null ? rules.NextGate(0) : snapshot != null ? snapshot.gate1 : 0) + 1) + " · 1 north, 2 east, 3 south, 4 west");
                GUILayout.Label("F8 shows/hides this panel. Online matches keep running when you switch windows.");
                if (GUILayout.Button("LEAVE ROOM")) StopSession();
            }
            GUILayout.EndArea(); GUI.matrix = matrix;
        }
        void Fail(Exception ex) { StopSession(); ConnectionStatus = ex.Message; showPanel = true; }
        void ClearWorld()
        {
            if (otherCar) { otherCar.gameObject.SetActive(false); Destroy(otherCar.gameObject); }
            if (markers) Destroy(markers);
            foreach (var material in markerMaterials) if (material) Destroy(material);
            markerMaterials.Clear();
            if (networkStats) Destroy(networkStats);
            if (GameSession.Instance && GameSession.Instance.NetworkControlled) GameSession.Instance.ReturnToMenu();
            localCar = otherCar = null; core = null; markers = null; networkStats = null; loaded = false; rules = null; snapshot = null;
        }
        public void StopSession()
        {
            peer?.Dispose(); peer = null; listener?.Stop(); listener = null;
            connectingClient?.Close(); connectingClient = null; connecting = null;
            ClearWorld();
            hosting = online = pendingJump = pendingRecover = guestKnown = guestLoaded = helloSent = remoteRecover = false;
            showPanel = false;
            remoteInput = default; round = 0; nextPing = 0; PingMilliseconds = 0;
            ConnectionStatus = "Disconnected.";
        }
        void OnDestroy() => StopSession();
        void OnApplicationQuit() => StopSession();
    }
}
