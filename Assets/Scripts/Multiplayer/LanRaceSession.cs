using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    /// <summary>Two-player LAN gate sprint. All physics and scoring run on the host.</summary>
    public sealed class LanRaceSession : MonoBehaviour
    {
        [Serializable] sealed class Packet
        {
            public string kind;
            public float throttle, steering;
            public bool brake, boost, jump, airRoll;
            public Vector3 position0, position1;
            public Quaternion rotation0, rotation1;
            public float seconds;
            public int score0, score1, gate0, gate1;
        }

        // Framing is bounded and nonblocking: no networking thread touches Unity objects.
        sealed class Peer : IDisposable
        {
            readonly Socket socket;
            readonly LanFrameBuffer incoming = new LanFrameBuffer();
            readonly Queue<byte[]> outgoing = new Queue<byte[]>();
            readonly byte[] readBuffer = new byte[8192];
            int sent;
            public float LastReceived { get; private set; }
            public Peer(Socket value) { socket = value; socket.Blocking = false; socket.NoDelay = true; LastReceived = Time.realtimeSinceStartup; }
            public void Send(Packet packet)
            {
                if (outgoing.Count > 100) throw new InvalidOperationException("Peer cannot keep up with snapshots.");
                byte[] payload = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
                outgoing.Enqueue(LanFrameBuffer.Encode(payload));
            }
            public void Pump(Action<Packet> receive)
            {
                int reads = 0;
                while (reads++ < 8 && socket.Poll(0, SelectMode.SelectRead))
                {
                    int count;
                    try { count = socket.Receive(readBuffer); }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock) { break; }
                    if (count == 0) throw new InvalidOperationException("The other player disconnected.");
                    incoming.Feed(readBuffer, count);
                    while (incoming.TryRead(out var payload))
                    {
                        string json = System.Text.Encoding.UTF8.GetString(payload);
                        var message = JsonUtility.FromJson<Packet>(json);
                        if (message == null) throw new InvalidOperationException("Invalid LAN packet.");
                        LastReceived = Time.realtimeSinceStartup; receive(message);
                    }
                }
                while (outgoing.Count > 0 && socket.Poll(0, SelectMode.SelectWrite))
                {
                    var bytes = outgoing.Peek(); int count;
                    try { count = socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None); }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock) { break; }
                    if (count == 0) break;
                    sent += count;
                    if (sent == bytes.Length) { sent = 0; outgoing.Dequeue(); }
                }
            }
            public void Dispose() { try { socket.Close(); } catch (SocketException) { } }
        }

        public const int Port = 7777;
        public bool Active => hosting || peer != null || connecting != null;
        public string ConnectionStatus { get; private set; } = "LAN gate sprint: F8 opens controls.";
        readonly Vector3[] gates = { new Vector3(0, 0, 24), new Vector3(15, 0, 0), new Vector3(0, 0, -24), new Vector3(-15, 0, 0) };
        TcpListener listener;
        TcpClient connectingClient;
        Task connecting;
        Peer peer;
        bool hosting, showPanel, pendingJump, completed;
        string address = "127.0.0.1";
        float connectionStarted, nextSend, remaining = 120, remoteInputReceived, readyAt;
        readonly int[] scores = new int[2];
        readonly int[] nextGate = new int[2];
        VehicleController localCar, otherCar;
        GameObject markers;
        DriveInput remoteInput;
        Packet snapshot;
        VehicleStats networkStats;

        public void StartHost()
        {
            StopSession();
            try
            {
                PrepareWorld(false);
                listener = new TcpListener(IPAddress.Any, Port); listener.Start(1);
                hosting = true; ConnectionStatus = "Hosting on TCP 7777. Waiting for one player.";
            }
            catch (Exception ex) { Fail(ex); }
        }

        public void Join(string host)
        {
            StopSession();
            if (!IPAddress.TryParse(host, out var ip)) { ConnectionStatus = "Enter a numeric LAN IP address."; return; }
            try
            {
                connectingClient = new TcpClient(ip.AddressFamily);
                connecting = connectingClient.ConnectAsync(ip, Port);
                connectionStarted = Time.realtimeSinceStartup;
                ConnectionStatus = "Connecting to " + host + "…";
            }
            catch (Exception ex) { Fail(ex); }
        }

        void PrepareWorld(bool client)
        {
            var session = GameSession.Instance;
            if (session == null) throw new InvalidOperationException("Start from the Velocity Rivals bootstrap scene.");
            session.StartMatch(GameMode.FreePlay, 3, 0, Color.cyan);
            Time.timeScale = 1;
            localCar = session.Player;
            localCar.Respawn(new Vector3(client ? 3 : -3, 2, 0), Quaternion.identity);
            localCar.IsPlayer = !client;
            networkStats = ScriptableObject.CreateInstance<VehicleStats>();
            networkStats.DisplayName = "Viper";
            otherCar = VehicleController.Spawn("LAN Rival", new Vector3(client ? -3 : 3, 2, 0), Quaternion.identity, networkStats, new Color(1f, .24f, .48f), false);
            if (client)
            {
                localCar.ControlsEnabled = false; otherCar.ControlsEnabled = false;
                MakeProxy(localCar); MakeProxy(otherCar);
            }
            scores[0] = scores[1] = nextGate[0] = nextGate[1] = 0;
            remaining = 120; completed = false; nextSend = 0; snapshot = null;
            markers = new GameObject("LAN Sprint Gates");
            for (int i = 0; i < gates.Length; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Gate " + (i + 1); marker.transform.SetParent(markers.transform);
                marker.transform.position = gates[i] + Vector3.up * .12f;
                marker.transform.localScale = new Vector3(10, .1f, 10);
                Destroy(marker.GetComponent<Collider>());
                var renderer = marker.GetComponent<Renderer>();
                renderer.material.color = Color.Lerp(Color.cyan, Color.magenta, i / 3f);
            }
        }

        static void MakeProxy(VehicleController car)
        {
            car.enabled = false;
            foreach (var body in car.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
            foreach (var collider in car.GetComponentsInChildren<Collider>()) collider.enabled = false;
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
                    PrepareWorld(true);
                    peer = new Peer(connectingClient.Client); connecting = null; connectingClient = null;
                    ConnectionStatus = "Connected. Host controls physics and scoring.";
                }
                if (GameSession.Instance == null || !GameSession.Instance.Running || localCar == null || otherCar == null)
                { StopSession(); return; }
                if (GameSession.Instance.Paused) GameSession.Instance.SetPaused(false);
                if (hosting && listener != null && listener.Pending())
                {
                    TcpClient accepted = listener.AcceptTcpClient();
                    if (peer != null) accepted.Close();
                    else { peer = new Peer(accepted.Client); remoteInputReceived = Time.realtimeSinceStartup; readyAt = Time.realtimeSinceStartup + 3; ConnectionStatus = "Player joined. Gate sprint starts in three seconds."; }
                }
                peer?.Pump(Receive);
                if (peer != null && Time.realtimeSinceStartup - peer.LastReceived > 10) throw new TimeoutException("Other player stopped responding.");
                if (hosting)
                {
                    localCar.ControlsEnabled = otherCar.ControlsEnabled = peer != null && !completed && Time.realtimeSinceStartup >= readyAt;
                    if (peer != null && !completed && Time.realtimeSinceStartup >= readyAt)
                    {
                        remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
                        CheckGate(localCar, 0); CheckGate(otherCar, 1);
                        if (remaining <= 0) { completed = true; localCar.ControlsEnabled = false; otherCar.ControlsEnabled = false; }
                    }
                    if (peer != null && Time.realtimeSinceStartup >= nextSend)
                    {
                        nextSend = Time.realtimeSinceStartup + .05f;
                        peer.Send(new Packet { kind = "state", position0 = localCar.transform.position, rotation0 = localCar.transform.rotation,
                            position1 = otherCar.transform.position, rotation1 = otherCar.transform.rotation, seconds = remaining,
                            score0 = scores[0], score1 = scores[1], gate0 = nextGate[0], gate1 = nextGate[1] });
                    }
                }
                else if (peer != null)
                {
                    pendingJump |= Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
                    pendingJump |= Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
                    if (Time.realtimeSinceStartup >= nextSend)
                    {
                        nextSend = Time.realtimeSinceStartup + .05f;
                        var k = Keyboard.current; var g = Gamepad.current;
                        float throttle = k == null ? 0 : (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
                        float steer = k == null ? 0 : (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
                        if (g != null) { throttle += g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue(); steer += g.leftStick.x.ReadValue(); }
                        peer.Send(new Packet { kind = "input", throttle = Mathf.Clamp(throttle, -1, 1), steering = Mathf.Clamp(steer, -1, 1),
                            brake = (k != null && k.leftCtrlKey.isPressed) || (g != null && g.buttonWest.isPressed),
                            boost = (k != null && k.leftShiftKey.isPressed) || (g != null && g.buttonEast.isPressed), jump = pendingJump,
                            airRoll = (k != null && (k.qKey.isPressed || k.eKey.isPressed)) || (g != null && g.leftShoulder.isPressed) });
                        pendingJump = false;
                    }
                    if (snapshot != null)
                    {
                        float blend = 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime);
                        localCar.transform.SetPositionAndRotation(Vector3.Lerp(localCar.transform.position, snapshot.position1, blend), Quaternion.Slerp(localCar.transform.rotation, snapshot.rotation1, blend));
                        otherCar.transform.SetPositionAndRotation(Vector3.Lerp(otherCar.transform.position, snapshot.position0, blend), Quaternion.Slerp(otherCar.transform.rotation, snapshot.rotation0, blend));
                    }
                }
            }
            catch (Exception ex) { Fail(ex); }
        }

        void FixedUpdate()
        {
            if (!hosting || otherCar == null) return;
            if (Time.realtimeSinceStartup - remoteInputReceived > .5f || completed) remoteInput = new DriveInput();
            otherCar.SetInput(remoteInput);
            remoteInput.Jump = false;
        }

        void CheckGate(VehicleController car, int player)
        {
            Vector3 delta = car.transform.position - gates[nextGate[player]];
            if (new Vector2(delta.x, delta.z).sqrMagnitude < 25 && Mathf.Abs(delta.y) < 5)
            { scores[player]++; nextGate[player] = (nextGate[player] + 1) % gates.Length; }
        }

        void Receive(Packet message)
        {
            if (hosting)
            {
                if (message.kind != "input" || float.IsNaN(message.throttle) || float.IsInfinity(message.throttle) || float.IsNaN(message.steering) || float.IsInfinity(message.steering))
                    throw new InvalidOperationException("Invalid client input.");
                remoteInput = new DriveInput { Throttle = Mathf.Clamp(message.throttle, -1, 1), Steering = Mathf.Clamp(message.steering, -1, 1),
                    Brake = message.brake, Boost = message.boost, Jump = remoteInput.Jump || message.jump, AirRoll = message.airRoll };
                remoteInputReceived = Time.realtimeSinceStartup;
            }
            else
            {
                if (message.kind != "state") throw new InvalidOperationException("Invalid host snapshot.");
                snapshot = message; remaining = message.seconds; scores[0] = message.score0; scores[1] = message.score1;
                nextGate[0] = message.gate0; nextGate[1] = message.gate1; completed = remaining <= 0;
            }
        }

        void OnGUI()
        {
            if (!showPanel && !Active) return;
            GUILayout.BeginArea(new Rect(18, 125, 340, Active ? 250 : 210), GUI.skin.box);
            GUILayout.Label("VELOCITY RIVALS · LAN GATE SPRINT");
            GUILayout.Label(ConnectionStatus);
            if (!Active)
            {
                address = GUILayout.TextField(address, 64);
                if (GUILayout.Button("Host LAN session")) StartHost();
                if (GUILayout.Button("Join entered IP")) Join(address);
                GUILayout.Label("F8: hide panel. Two players; TCP port 7777.");
            }
            else
            {
                GUILayout.Label("Time: " + Mathf.CeilToInt(remaining) + "    Host " + scores[0] + " : " + scores[1] + " Guest");
                GUILayout.Label("Your next gate: " + (nextGate[hosting ? 0 : 1] + 1));
                GUILayout.Label("Gate 1 north, 2 east, 3 south, 4 west.");
                if (completed) GUILayout.Label(scores[0] == scores[1] ? "RESULT: DRAW" : scores[0] > scores[1] ? "RESULT: HOST WINS" : "RESULT: GUEST WINS");
                if (GUILayout.Button("Disconnect / return to menu")) { StopSession(); GameSession.Instance?.ReturnToMenu(); }
            }
            GUILayout.EndArea();
        }

        void Fail(Exception ex) { StopSession(); ConnectionStatus = ex.Message; showPanel = true; }
        public void StopSession()
        {
            peer?.Dispose(); peer = null;
            listener?.Stop(); listener = null;
            connectingClient?.Close(); connectingClient = null; connecting = null;
            hosting = false; pendingJump = false; remoteInput = new DriveInput();
            if (otherCar != null) Destroy(otherCar.gameObject);
            if (markers != null) Destroy(markers);
            if (networkStats != null) Destroy(networkStats);
            if (localCar != null)
            {
                localCar.enabled = true;
                localCar.IsPlayer = true; localCar.ControlsEnabled = true;
                foreach (var body in localCar.GetComponentsInChildren<Rigidbody>()) body.isKinematic = false;
                foreach (var collider in localCar.GetComponentsInChildren<Collider>()) collider.enabled = true;
            }
            localCar = otherCar = null;
            ConnectionStatus = "LAN disconnected.";
        }
        void OnDestroy() { StopSession(); }
        void OnApplicationQuit() { StopSession(); }
    }
}
