using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace VelocityRivals
{
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        public VehicleController Target;
        Camera view;
        Vector3 velocity;
        VehicleController previousTarget;
        Vector2 look;
        float idleLook;
        public Vector2 LookAngles => look;
        public void ApplyLook(Vector2 input, float seconds, bool reset)
        {
            if (reset) { look = Vector2.zero; idleLook = 0; return; }
            if (seconds <= 0) return;
            if (input.magnitude > .15f)
            {
                input = Vector2.ClampMagnitude(input, 1);
                idleLook = 0;
                float sensitivity = SaveStore.Current.cameraSensitivity;
                look.x = Mathf.Repeat(look.x + input.x * 150 * sensitivity * seconds + 180, 360) - 180;
                look.y = Mathf.Clamp(look.y + input.y * (SaveStore.Current.invertCameraY ? -1 : 1) * 90 * sensitivity * seconds, -25, 45);
            }
            else if ((idleLook += seconds) > 1.5f)
            {
                look.x = Mathf.MoveTowards(look.x, 0, seconds * 120);
                look.y = Mathf.MoveTowards(look.y, 0, seconds * 65);
            }
        }
        void Awake()
        {
            view = GetComponent<Camera>();
            view.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
        void LateUpdate()
        {
            if (Target == null) return;
            var session = GameSession.Instance;
            if (session != null && (session.Paused || session.Finished)) return;
            if (Target != previousTarget) ApplyLook(Vector2.zero, 0, true);
            var pad = Gamepad.current;
            ApplyLook(pad != null ? pad.rightStick.ReadValue() : Vector2.zero, Time.deltaTime,
                (pad != null && pad.rightStickButton.wasPressedThisFrame) || (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame));
            Vector3 forward = Vector3.ProjectOnPlane(Target.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            float speed = Mathf.Clamp01(Target.SpeedKmh / 180f);
            Vector3 focus = Target.transform.position + Vector3.up * 1.1f;
            Vector3 orbit = Quaternion.AngleAxis(look.x, Vector3.up) * -forward;
            float elevation = (21 + look.y) * Mathf.Deg2Rad;
            Vector3 desired = focus + (orbit * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation)) * (8.1f + speed * 2);
            if (Physics.Linecast(focus, desired, out var hit, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(Target.transform))
                desired = hit.point + hit.normal * .4f;
            bool snap = Target != previousTarget || Vector3.Distance(transform.position, desired) > 35;
            if (snap) { velocity = Vector3.zero; transform.position = desired; }
            else transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, .13f, float.PositiveInfinity, Time.deltaTime);
            var rotation = Quaternion.LookRotation(focus + forward * (3 * (1-Mathf.Clamp01(Mathf.Abs(look.x)/60))) - transform.position);
            transform.rotation = snap ? rotation : Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 10f);
            previousTarget = Target;
            float targetFov = SaveStore.Current.reducedEffects ? 65 : 65f + speed * 15f + (Target.IsBoosting ? 5f : 0f);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, targetFov, Time.deltaTime * 5f);
        }
    }
}
