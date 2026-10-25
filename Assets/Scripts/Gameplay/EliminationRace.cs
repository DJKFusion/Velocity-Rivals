using System;

namespace VelocityRivals
{
    /// <summary>Match-clock elimination; distance is supplied in ordered-checkpoint units.</summary>
    public sealed class EliminationRace
    {
        readonly bool[] active;
        readonly float interval;
        public float NextCutoff { get; private set; }
        public int ActiveCount { get; private set; }
        public int Winner { get; private set; } = -1;
        public bool IsActive(int id) => id>=0 && id<active.Length && active[id];
        public EliminationRace(int drivers=4,float intervalSeconds=45)
        {
            if(drivers<2 || drivers>16 || intervalSeconds<=0 || float.IsNaN(intervalSeconds) || float.IsInfinity(intervalSeconds))throw new ArgumentOutOfRangeException();
            active=new bool[drivers];for(int i=0;i<drivers;i++)active[i]=true;
            ActiveCount=drivers;interval=NextCutoff=intervalSeconds;
        }
        public int EliminateDue(float elapsed,double[] progress)
        {
            if(elapsed<0 || float.IsNaN(elapsed) || float.IsInfinity(elapsed))throw new ArgumentOutOfRangeException(nameof(elapsed));
            if(progress==null || progress.Length!=active.Length)throw new ArgumentException("One progress value is required per driver.");
            for(int i=0;i<progress.Length;i++)if(active[i] && (double.IsNaN(progress[i]) || double.IsInfinity(progress[i])))throw new ArgumentException("Progress must be finite.");
            if(ActiveCount<=1 || elapsed<NextCutoff)return -1;
            int last=-1;
            for(int i=0;i<active.Length;i++)
                if(active[i] && (last<0 || progress[i]<progress[last] || (progress[i]==progress[last] && i>last)))last=i;
            active[last]=false;ActiveCount--;NextCutoff+=interval;
            if(ActiveCount==1)for(int i=0;i<active.Length;i++)if(active[i])Winner=i;
            return last;
        }
    }
}
