using UnityEngine;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    /// <summary>Optional local-controller feedback. Never leaves a motor running in menus or pause.</summary>
    public sealed class PlayerFeedback : MonoBehaviour
    {
        Gamepad pad;
        VehicleController tracked;
        Vector3 previousVelocity, previousPosition;
        bool wasGrounded, driving;
        float pulse;
        public float LowMotor { get; private set; }
        public float HighMotor { get; private set; }
        void Update()
        {
            var session = GameSession.Instance;
            var car = session ? session.Player : null;
            var next = car ? InputSystem.GetDeviceById(car.LastInputDeviceId) as Gamepad : null;
            if (pad != next) { StopMotors(); pad = next; }
            bool active = session && session.ActionEnabled && car && car.ControlsEnabled && pad != null && pad.added && SaveStore.Current.vibration > 0;
            if (!active) { StopMotors(); driving = false; tracked = car; pulse = 0; return; }
            if (!driving || tracked != car || Vector3.Distance(previousPosition, car.PhysicsPosition) > 10)
            {
                previousVelocity = car.Velocity; wasGrounded = car.Grounded; pulse = 0;
            }
            else if ((car.Velocity - previousVelocity).magnitude > 8 || (!wasGrounded && car.Grounded && previousVelocity.y < -3)) pulse = .5f;
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 2.5f);
            float strength = SaveStore.Current.vibration;
            LowMotor = Mathf.Clamp01(Mathf.Max(pulse, car.IsBoosting ? .3f : 0) * strength);
            HighMotor = Mathf.Clamp01(Mathf.Max(pulse * .7f, car.IsDrifting ? .22f : 0) * strength);
            pad.SetMotorSpeeds(LowMotor, HighMotor);
            previousVelocity = car.Velocity; previousPosition = car.PhysicsPosition;
            wasGrounded = car.Grounded; driving = true; tracked = car;
        }
        void StopMotors()
        {
            if (pad != null && pad.added && (LowMotor > 0 || HighMotor > 0)) pad.SetMotorSpeeds(0, 0);
            LowMotor = HighMotor = 0;
        }
        void OnApplicationFocus(bool focused) { if (!focused) { StopMotors(); driving = false; } }
        void OnDisable() { StopMotors(); driving = false; }
        void OnDestroy() { StopMotors(); }
    }
}
