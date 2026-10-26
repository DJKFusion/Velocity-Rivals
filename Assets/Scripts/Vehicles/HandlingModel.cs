using System;

namespace VelocityRivals
{
    /// <summary>Shared arcade handling rules, in metres/second and degrees/second.</summary>
    public static class HandlingModel
    {
        public const int Revision = 1;
        static float Clamp(float value,float min,float max) => Math.Max(min,Math.Min(max,value));
        public static float YawDegrees(float steering,float forwardSpeed,float topSpeed,float steeringRate,bool drifting)
        {
            if (!Finite(steering+forwardSpeed+topSpeed+steeringRate) || topSpeed<=0 || steeringRate<=0) return 0;
            float speed=Math.Abs(forwardSpeed);
            float retention=1-.25f*Clamp(speed/topSpeed,0,1);
            float moving=Clamp(speed/2.5f,0,1);
            return Clamp(steering,-1,1)*steeringRate*1.25f*retention*moving*(drifting?1.18f:1)*(forwardSpeed<-.5f?-1:1);
        }
        public static float Grip(float baseGrip,bool drifting) => Math.Max(0,baseGrip)*(drifting?.55f:1.4f);
        public static float CornerSpeed(float steering,float topSpeed,bool boosting,bool drifting)
        {
            if(!Finite(steering+topSpeed)||topSpeed<=0)return 0;
            if(boosting||drifting)return topSpeed;
            float turn=Clamp((Math.Abs(steering)-.35f)/.65f,0,1);
            return topSpeed*(1-.35f*turn);
        }
        public static float CornerDeceleration(float steering,float speed,float topSpeed,bool boosting,bool drifting)
        {
            if(!Finite(steering+speed+topSpeed)||topSpeed<=0||speed<=0||boosting||drifting)return 0;
            float turn=Clamp((Math.Abs(steering)-.35f)/.65f,0,1);
            float target=CornerSpeed(steering,topSpeed,boosting,drifting);
            return Clamp((speed-target)*2,0,9)*turn;
        }
        static bool Finite(float value) => !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
