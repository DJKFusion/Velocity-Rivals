using System.Collections.Generic;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Recharge lanes reward choosing a line; each car has its own cooldown.</summary>
    public sealed class TrackBoostPad : MonoBehaviour
    {
        readonly Dictionary<VehicleController,float> readyAt = new Dictionary<VehicleController,float>();
        void OnTriggerEnter(Collider other)
        {
            var session=GameSession.Instance;
            var car=other.GetComponentInParent<VehicleController>();
            if (!session || !session.ActionEnabled || !car || !car.ControlsEnabled) return;
            if(readyAt.TryGetValue(car,out float ready) && Time.time<ready)return;
            readyAt[car]=Time.time+5;
            car.RefillBoost(.45f);
            if(car==session.Player)
            {
                session.NotifyRecharge();
                SoundDirector.Instance?.Cue(GameCue.Pickup);
                MatchEffects.Burst(car.PhysicsPosition,new Color(.1f,1,.75f));
            }
        }
    }
}
