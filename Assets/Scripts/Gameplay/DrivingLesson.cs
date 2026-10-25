using System;

namespace VelocityRivals
{
    /// <summary>Practice objectives advance from observed driving, not elapsed session time.</summary>
    public sealed class DrivingLesson
    {
        public int Step { get; private set; }
        public bool Complete => Step == 4;
        float held;
        bool touchedGround, airborne;
        public string Instruction => Step == 0 ? "1 / 4  DRIVE — hold W / RT and maintain 25 km/h for two seconds." :
            Step == 1 ? "2 / 4  BOOST — hold Shift / B while driving for one second." :
            Step == 2 ? "3 / 4  JUMP & LAND — Space / A jumps. Land back on your wheels." :
            Step == 3 ? "4 / 4  RECOVER — press R / Y to reset your car safely." : "TRAINING COMPLETE";
        public void Tick(float seconds, float speed, bool boosting, bool grounded, bool upright, bool recovered)
        {
            if (Complete || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            if (Step == 0 || Step == 1)
            {
                bool valid = speed >= 25 && (Step == 0 || boosting);
                held = valid ? held + Math.Min(seconds, .1f) : 0;
                if (held >= (Step == 0 ? 2 : 1)) { Step++; held = 0; }
            }
            else if (Step == 2)
            {
                if (grounded && upright && !airborne) touchedGround = true;
                if (touchedGround && !grounded) held += Math.Min(seconds, .1f);
                if (held >= .25f) airborne = true;
                if (airborne && grounded)
                {
                    if (upright) Step++;
                    else { touchedGround = airborne = false; held = 0; }
                }
            }
            else if (recovered) Step++;
        }
    }
}
