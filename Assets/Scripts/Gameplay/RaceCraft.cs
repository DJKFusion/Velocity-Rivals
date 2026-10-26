using System;

namespace VelocityRivals
{
    public static class RaceCraft
    {
        public const int RulesVersion = 2;
        public static float DraftStrength(float ahead,float side,float height,float alignment,float speedKmh)
        {
            if(float.IsNaN(ahead+side+height+alignment+speedKmh) || float.IsInfinity(ahead+side+height+alignment+speedKmh))return 0;
            if(ahead<5 || ahead>30 || Math.Abs(side)>2.5f || Math.Abs(height)>2 || alignment<.9f || speedKmh<45)return 0;
            float distance=Math.Min(1,(30-ahead)/15);
            return Math.Max(0,distance*(1-Math.Abs(side)/2.5f));
        }
        public static float ChooseLane(float current,float leftGap,float centreGap,float rightGap,int preferredSide)
        {
            int slot=current< -2?0:current>2?2:1;
            float Gap(int index) => index==0?leftGap:index==1?centreGap:rightGap;
            if(Gap(slot)>=18)return slot==0?-4:slot==2?4:0;
            int best=slot;
            for(int i=0;i<3;i++)
                if(Math.Abs(i-slot)<=1 && (Gap(i)>Gap(best)+2 || (Gap(i)==Gap(best) && i==(preferredSide<0?0:2))))best=i;
            // Insist on meaningful clearance; avoid darting into another car's side.
            return Gap(best)>=10 ? (best-1)*4 : (slot-1)*4;
        }
        public static float CornerSpeed(float topSpeedKmh,float bendDegrees,int personality)
        {
            float bend=Math.Max(0,Math.Min(1,Math.Abs(bendDegrees)/65));
            float caution=personality==1?.94f:personality==2?1.03f:1;
            return Math.Max(40,(topSpeedKmh*.9f*(1-bend)+55*bend)*caution);
        }
    }
}
