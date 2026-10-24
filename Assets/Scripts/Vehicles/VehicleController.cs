using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        public VehicleStats Stats;
        public bool IsPlayer;
        public bool ControlsEnabled = true;
        public float SpeedKmh => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float BoostAmount { get; private set; } = 1f;
        public bool Grounded { get; private set; }
        public int StuntScore { get; private set; }
        public bool IsBoosting => ControlsEnabled && input.Boost && BoostAmount > .01f;
        Rigidbody body;
        DriveInput input;
        InputActionMap controls;
        InputAction throttle, steering, brake, boost, jump, roll, rollAxis, reset;
        bool jumpQueued, previousJump;
        int jumps;
        float lastJump = -10f, invertedTime, airRotation, airTime;
        Vector3 safePosition;
        Quaternion safeRotation;
        Vector3 groundNormal = Vector3.up;
        readonly RaycastHit[] hits = new RaycastHit[32];
        readonly List<Material> ownedMaterials = new List<Material>();
        [SerializeField] List<Transform> wheels = new List<Transform>();
        readonly Dictionary<Transform, Vector3> wheelScales = new Dictionary<Transform, Vector3>();
        [SerializeField] Transform spoilerObject, decalObject;
        [SerializeField] TrailRenderer exhaust;
        [SerializeField] Material paintMaterial, trimMaterial;
        AudioSource engine;
        AudioClip engineClip;

        void Start()
        {
            if (paintMaterial == null) BuildVisuals(Color.cyan);
            foreach (var wheel in wheels) if (wheel != null && !wheelScales.ContainsKey(wheel)) wheelScales[wheel] = wheel.localScale;
            if (IsPlayer && engine == null) CreateEngineAudio();
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 1100f;
            body.centerOfMass = new Vector3(0, .18f, 0);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = .12f;
            body.angularDamping = 2f;
            safePosition = transform.position;
            safeRotation = transform.rotation;
            controls = new InputActionMap("Vehicle");
            throttle = controls.AddAction("Throttle", InputActionType.Value);
            throttle.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
            throttle.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/downArrow").With("Positive", "<Keyboard>/upArrow");
            throttle.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/leftTrigger").With("Positive", "<Gamepad>/rightTrigger");
            steering = controls.AddAction("Steering", InputActionType.Value);
            steering.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            steering.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            steering.AddBinding("<Gamepad>/leftStick/x");
            brake = Button("Handbrake", "<Keyboard>/leftCtrl", "<Gamepad>/buttonWest");
            boost = Button("Boost", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            roll = Button("AirRoll", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            rollAxis = controls.AddAction("AirRollAxis", InputActionType.Value);
            rollAxis.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            reset = Button("Recover", "<Keyboard>/r", "<Gamepad>/buttonNorth");
            controls.Enable();
        }

        InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = controls.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboard); action.AddBinding(gamepad); return action;
        }

        public void SetInput(DriveInput value)
        {
            if (!ControlsEnabled) { input = default; jumpQueued = false; previousJump = false; return; }
            if (value.Jump && !previousJump) jumpQueued = true;
            previousJump = value.Jump;
            input = value;
        }

        void Update()
        {
            if (IsPlayer && ControlsEnabled && Time.timeScale > 0)
            {
                float airRollInput = rollAxis.ReadValue<float>();
                SetInput(new DriveInput { Throttle = throttle.ReadValue<float>(), Steering = Mathf.Clamp((Mathf.Abs(airRollInput) > .1f ? airRollInput : steering.ReadValue<float>()) * SaveStore.Current.steeringSensitivity, -1, 1),
                    Brake = brake.IsPressed(), Boost = boost.IsPressed(), Jump = jump.IsPressed(), AirRoll = roll.IsPressed() || Mathf.Abs(airRollInput) > .1f });
                if (reset.WasPressedThisFrame()) Respawn(safePosition + Vector3.up, safeRotation);
                if (IsBoosting) SoundDirector.Instance?.Cue(GameCue.Boost);
                if (Grounded && input.Brake && SpeedKmh > 35) SoundDirector.Instance?.Cue(GameCue.Drift);
            }
            if (!ControlsEnabled) { input = default; jumpQueued = false; }
            if (exhaust != null) exhaust.emitting = IsBoosting;
            foreach (var wheel in wheels) wheel.Rotate(Vector3.right, SpeedKmh * Time.deltaTime * 9f, Space.Self);
            if (engine != null)
            {
                engine.pitch = .55f + Mathf.Clamp01(SpeedKmh / 180f) * 1.5f;
                engine.volume = (ControlsEnabled ? .10f + Mathf.Abs(input.Throttle) * .04f : .015f) * SaveStore.Current.effectsVolume;
            }
        }

        void FixedUpdate()
        {
            if (!ControlsEnabled) { input = default; jumpQueued = false; }
            float acceleration = Stats != null ? Stats.Acceleration : 24f;
            float maxSpeed = Stats != null ? Stats.MaxSpeed : 38f;
            float grip = Stats != null ? Stats.Grip : 8f;
            float steerSpeed = Stats != null ? Stats.Steering : 95f;
            bool wasGrounded = Grounded;
            Grounded = false;
            Vector3 normalSum = Vector3.zero;
            int contacts = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 local = new Vector3((corner % 2 == 0 ? -1 : 1) * .76f, .7f, (corner < 2 ? -1 : 1) * 1.03f);
                Vector3 origin = transform.TransformPoint(local);
                if (!FindGround(origin, -transform.up, out var hit)) continue;
                Grounded = true; contacts++; normalSum += hit.normal;
                float compression = 1.07f - hit.distance;
                float damp = Vector3.Dot(body.GetPointVelocity(origin), hit.normal) * 2.1f;
                float spring = Mathf.Clamp(compression * 22f - damp, 0f, 12f);
                body.AddForceAtPosition(hit.normal * spring, origin, ForceMode.Acceleration);
            }
            if (contacts > 0) groundNormal = normalSum.normalized;
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            if (Grounded)
            {
                if (Time.time - lastJump > .25f) jumps = 0;
                if (!wasGrounded && airTime > .35f && Vector3.Dot(transform.up, groundNormal) > .65f)
                    StuntScore += Mathf.RoundToInt(airRotation * 12f + airTime * 20f);
                airRotation = 0; airTime = 0;
                Vector3 side = Vector3.ProjectOnPlane(transform.right, groundNormal).normalized;
                body.AddForce(-side * Vector3.Dot(body.linearVelocity, side) * (input.Brake ? grip * .16f : grip), ForceMode.Acceleration);
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
                if ((input.Throttle > 0 && forwardSpeed < maxSpeed) || (input.Throttle < 0 && forwardSpeed > -maxSpeed * .34f))
                    body.AddForce(forward * input.Throttle * acceleration, ForceMode.Acceleration);
                if (input.Brake) body.AddForce(-forward * forwardSpeed * .8f, ForceMode.Acceleration);
                float speedFactor = Mathf.Lerp(1f, .43f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed));
                float moving = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f);
                float desiredYaw = input.Steering * steerSpeed * Mathf.Deg2Rad * speedFactor * moving * (forwardSpeed < -.5f ? -1f : 1f);
                body.AddTorque(groundNormal * (desiredYaw - Vector3.Dot(body.angularVelocity, groundNormal)) * 8f, ForceMode.Acceleration);
                Vector3 tiltVelocity = Vector3.ProjectOnPlane(body.angularVelocity, groundNormal);
                body.AddTorque(Vector3.Cross(transform.up, groundNormal) * 18f - tiltVelocity * 4f, ForceMode.Acceleration);
                // Adhesion works when the suspension rays can reach a smoothly joined wall surface.
                body.AddForce(-groundNormal * Mathf.Clamp(Mathf.Abs(forwardSpeed) * .12f, 0, 8), ForceMode.Acceleration);
            }
            else
            {
                airTime += Time.fixedDeltaTime;
                airRotation += body.angularVelocity.magnitude * Time.fixedDeltaTime;
                Vector3 torque = transform.right * (-input.Throttle * 4f);
                torque += (input.AirRoll ? -transform.forward : transform.up) * input.Steering * 4f;
                body.AddTorque(torque, ForceMode.Acceleration);
            }
            if (ControlsEnabled && input.Boost && BoostAmount > .01f)
            {
                body.AddForce(transform.forward * (Stats != null ? Stats.BoostForce : 24f), ForceMode.Acceleration);
                BoostAmount = Mathf.Max(0, BoostAmount - Time.fixedDeltaTime * .24f);
            }
            else BoostAmount = Mathf.Min(1, BoostAmount + Time.fixedDeltaTime * (Grounded ? .14f : .05f));
            if (jumpQueued && ControlsEnabled && jumps < 2 && (Grounded || jumps == 1))
            {
                if (IsPlayer) SoundDirector.Instance?.Cue(GameCue.Jump);
                body.AddForce((Grounded ? groundNormal : Vector3.up) * 7f, ForceMode.VelocityChange);
                if (jumps == 1 && (Mathf.Abs(input.Throttle) + Mathf.Abs(input.Steering)) > .4f)
                {
                    Vector3 dodge = transform.forward * input.Throttle + transform.right * input.Steering;
                    body.AddForce(dodge.normalized * 9f, ForceMode.VelocityChange);
                    body.AddTorque(Vector3.Cross(Vector3.up, dodge.normalized) * 5f, ForceMode.VelocityChange);
                }
                jumps++; lastJump = Time.time;
            }
            jumpQueued = false;
            float cap = maxSpeed * 1.65f;
            if (body.linearVelocity.magnitude > cap) body.linearVelocity = body.linearVelocity.normalized * cap;
            if (Vector3.Dot(transform.up, Vector3.up) < .15f && SpeedKmh < 5f) invertedTime += Time.fixedDeltaTime;
            else invertedTime = 0;
            if (invertedTime > 3f || transform.position.y < -25f) Respawn(safePosition + Vector3.up, safeRotation);
            if (Grounded && Vector3.Dot(transform.up, Vector3.up) > .85f && Time.time - lastJump > 1f)
            { safePosition = transform.position; safeRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0); }
        }

        bool FindGround(Vector3 origin, Vector3 direction, out RaycastHit result)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, 1.25f, ~0, QueryTriggerInteraction.Ignore);
            result = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                if (hits[i].collider.attachedRigidbody != body && hits[i].distance < nearest)
                { result = hits[i]; nearest = result.distance; }
            return nearest < float.PositiveInfinity;
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            body.position = position; body.rotation = rotation;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            safePosition = position; safeRotation = rotation;
            jumps = 0; invertedTime = 0; airTime = 0; airRotation = 0;
            jumpQueued = false;
        }

        public void SetPaint(Color color)
        {
            if (paintMaterial != null) paintMaterial.color = color;
        }

        public void SetCosmetics(int wheelStyle, int trail, int decal, int spoiler, int finish)
        {
            foreach (var wheel in wheels)
            {
                if (!wheelScales.ContainsKey(wheel)) wheelScales[wheel] = wheel.localScale;
                wheel.localScale = wheelScales[wheel] * (1f + Mathf.Clamp(wheelStyle, 0, 3) * .025f);
            }
            if (exhaust != null)
            {
                Color c = Color.HSVToRGB((Mathf.Abs(trail) % 8) / 8f, .75f, 1f);
                exhaust.startColor = c; exhaust.endColor = new Color(c.r, c.g, c.b, 0f);
            }
            if (decalObject != null)
            {
                decalObject.gameObject.SetActive(decal != 0);
                decalObject.localScale = decal == 2 ? new Vector3(.75f, .008f, .5f) : new Vector3(.18f, .008f, .9f);
            }
            if (spoilerObject != null)
            {
                spoilerObject.gameObject.SetActive(spoiler != 0);
                spoilerObject.localScale = spoiler == 2 ? new Vector3(.12f, .65f, .65f) : new Vector3(2f, .1f, .4f);
            }
            if (paintMaterial != null)
            {
                paintMaterial.SetFloat("_Smoothness", finish == 0 ? .65f : .15f);
                paintMaterial.SetFloat("_Metallic", finish == 2 ? .9f : .35f);
            }
        }

        public static VehicleController Spawn(string name, Vector3 pos, Quaternion rot, VehicleStats stats, Color color, bool isPlayer)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(pos, rot);
            var collision = root.AddComponent<BoxCollider>();
            collision.center = new Vector3(0, .55f, 0); collision.size = new Vector3(1.8f, .65f, 3.4f);
            var car = root.AddComponent<VehicleController>();
            car.Stats = stats; car.IsPlayer = isPlayer;
            car.BuildVisuals(color);
            return car;
        }

        Material MakeMaterial(Color color, bool emissive = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 3f); }
            ownedMaterials.Add(material); return material;
        }

        Transform Part(string name, Vector3 position, Vector3 scale, Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name; part.transform.SetParent(transform, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            if (Application.isPlaying) Destroy(part.GetComponent<Collider>());
            else DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        void BuildVisuals(Color color)
        {
            paintMaterial = MakeMaterial(color); trimMaterial = MakeMaterial(new Color(.025f, .035f, .06f));
            var glow = MakeMaterial(new Color(.2f, .85f, 1), true);
            string[] modelNames = { "Viper", "Bulwark", "Phantom", "Apex", "Nomad", "Comet" };
            int style = Stats != null ? System.Array.IndexOf(modelNames, Stats.DisplayName) : 0;
            float width = style == 3 ? 1.95f : style == 2 ? 1.55f : 1.8f;
            var model = Stats != null ? Resources.Load<GameObject>("VehicleModels/" + Stats.DisplayName) : null;
            if (model != null)
            {
                var visual = Instantiate(model, transform);
                // Exported meshes have their tires at y=0; the suspension root rests about .25m high.
                visual.transform.localPosition = new Vector3(0, -.22f, 0);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    Material material = renderer.name.StartsWith("Body") ? paintMaterial : renderer.name.StartsWith("Trim") ? glow : trimMaterial;
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                    if (renderer.name.StartsWith("Wheel")) wheels.Add(renderer.transform);
                }
            }
            else
            {
                Part("Body", new Vector3(0, .55f, 0), new Vector3(width, .6f, 3.35f), paintMaterial);
                Part("Canopy", new Vector3(0, 1f, -.25f), new Vector3(width * .72f, .52f + Mathf.Max(style, 0) * .015f, 1.4f), trimMaterial);
                Part("Nose", new Vector3(0, .57f, 1.5f), new Vector3(width * .88f, .25f, .6f), paintMaterial).localRotation = Quaternion.Euler(10, 0, 0);
                Part("Light strip", new Vector3(0, .6f, 1.83f), new Vector3(width * .8f, .09f, .06f), glow);
                for (int i = 0; i < 4; i++)
                {
                    var wheel = Part("Wheel " + i, new Vector3((i % 2 == 0 ? -1 : 1) * width * .52f, .2f, (i < 2 ? -1 : 1) * 1.07f), new Vector3(.34f, .76f, .76f), trimMaterial, PrimitiveType.Sphere);
                    wheels.Add(wheel);
                }
            }
            spoilerObject = Part("Spoiler", new Vector3(0, 1.05f, -1.45f), new Vector3(width + .2f, .1f, .4f), paintMaterial);
            decalObject = Part("Center stripe", new Vector3(0, .858f, .95f), new Vector3(.18f, .008f, .9f), glow);
            var jet = new GameObject("Boost trail"); jet.transform.SetParent(transform, false); jet.transform.localPosition = new Vector3(0, .5f, -1.8f);
            exhaust = jet.AddComponent<TrailRenderer>(); exhaust.time = .23f; exhaust.startWidth = .65f; exhaust.endWidth = .03f;
            exhaust.sharedMaterial = glow; exhaust.startColor = Color.cyan; exhaust.endColor = Color.clear; exhaust.emitting = false;
            if (IsPlayer && Application.isPlaying) CreateEngineAudio();
        }

        void CreateEngineAudio()
        {
            engine = gameObject.AddComponent<AudioSource>(); engine.loop = true; engine.spatialBlend = .1f;
            const int sampleRate = 22050; var data = new float[sampleRate];
            for (int i = 0; i < data.Length; i++) data[i] = (Mathf.Sin(i * 2f * Mathf.PI * 70 / sampleRate) + .3f * Mathf.Sin(i * 2f * Mathf.PI * 140 / sampleRate)) * .23f;
            engineClip = AudioClip.Create("Original procedural engine", data.Length, 1, sampleRate, false); engineClip.SetData(data, 0); engine.clip = engineClip; engine.Play();
        }

        void OnDestroy()
        {
            controls?.Disable(); controls?.Dispose();
            foreach (var material in ownedMaterials) if (material != null) Release(material);
            if (engineClip != null) Release(engineClip);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsPlayer || collision.relativeVelocity.magnitude < 6 || collision.contactCount == 0) return;
            SoundDirector.Instance?.Cue(GameCue.Impact);
            MatchEffects.Burst(collision.GetContact(0).point, Color.yellow);
        }

        static void Release(Object resource)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(resource)) return;
#endif
            if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
        }
    }
}
