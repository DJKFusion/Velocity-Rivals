using NUnit.Framework;
using UnityEngine;

namespace VelocityRivals.Tests
{
    public sealed class GhostIntegrityTests
    {
        static GhostReplay.Run Valid() => new GhostReplay.Run { Duration=10, Frames=new System.Collections.Generic.List<GhostReplay.Frame> {
            new GhostReplay.Frame {Time=0,Position=Vector3.zero,Rotation=Quaternion.identity},
            new GhostReplay.Frame {Time=9.9f,Position=Vector3.forward*20,Rotation=Quaternion.Euler(0,45,0)} } };
        [Test]
        public void AcceptsFiniteOrderedReplayAndRejectsIncompleteData()
        {
            Assert.IsTrue(GhostReplay.ValidRun(Valid()));
            Assert.IsFalse(GhostReplay.ValidRun(null));
            var run=Valid();run.Frames=null;Assert.IsFalse(GhostReplay.ValidRun(run));
            run=Valid();run.Frames.RemoveAt(1);Assert.IsFalse(GhostReplay.ValidRun(run));
            run=Valid();run.Duration=0;Assert.IsFalse(GhostReplay.ValidRun(run));
        }
        [Test]
        public void RejectsDamagedTransformsAndTimeline()
        {
            foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f,1201f})
            {
                var run=Valid();run.Duration=invalid;Assert.IsFalse(GhostReplay.ValidRun(run));
            }
            var sample=Valid();var frame=sample.Frames[1];frame.Rotation=default;sample.Frames[1]=frame;
            Assert.IsFalse(GhostReplay.ValidRun(sample));
            sample=Valid();frame=sample.Frames[1];frame.Position=new Vector3(float.NaN,0,0);sample.Frames[1]=frame;
            Assert.IsFalse(GhostReplay.ValidRun(sample));
            foreach(float time in new[]{0f,-1f,11f,float.NaN})
            {
                sample=Valid();frame=sample.Frames[1];frame.Time=time;sample.Frames[1]=frame;
                Assert.IsFalse(GhostReplay.ValidRun(sample));
            }
        }
    }
}
