using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VelocityRivals
{
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        public VehicleController Target;
        Camera view;
        Vector3 velocity;
        void Awake()
        {
            view = GetComponent<Camera>();
            view.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
        void LateUpdate()
        {
            if (Target == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(Target.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            float speed = Mathf.Clamp01(Target.SpeedKmh / 180f);
            Vector3 desired = Target.transform.position - forward * (7.5f + speed * 2f) + Vector3.up * 4f;
            Vector3 focus = Target.transform.position + Vector3.up * 1.1f;
            if (Physics.Linecast(focus, desired, out var hit, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(Target.transform))
                desired = hit.point + hit.normal * .4f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, .13f, float.PositiveInfinity, Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus + forward * 3f - transform.position), Time.deltaTime * 10f);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, 65f + speed * 15f + (Target.IsBoosting ? 5f : 0f), Time.deltaTime * 5f);
        }
    }
}
