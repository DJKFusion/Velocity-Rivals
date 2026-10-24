using UnityEngine;

namespace VelocityRivals
{
    [CreateAssetMenu(menuName = "Velocity Rivals/Vehicle Stats")]
    public sealed class VehicleStats : ScriptableObject
    {
        public string DisplayName = "Pulse GT";
        public float MaxSpeed = 38f;
        public float Acceleration = 24f;
        public float Steering = 95f;
        public float Grip = 8f;
        public float BoostForce = 24f;
    }

    public struct DriveInput
    {
        public float Throttle, Steering;
        public bool Brake, Boost, Jump, AirRoll;
    }
}
