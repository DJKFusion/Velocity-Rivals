using UnityEngine;

namespace VelocityRivals
{
    public sealed partial class GameSession
    {
        EliminationRace elimination;
        string eliminatedNotice="";
        float eliminatedNoticeUntil;
        readonly double[] raceDistances=new double[4];
        static readonly string[] DriverNames={"YOU","ION","PRISM","COMET"};
        public int RacePosition { get; private set; } = 1;
        public int ActiveRaceCars => elimination!=null?elimination.ActiveCount:racers.Count+1;
        public float EliminationRemaining => elimination==null?0:Mathf.Max(0,elimination.NextCutoff-elapsed);
        public string EliminationNotice => elapsed<eliminatedNoticeUntil?eliminatedNotice:
            EliminationRemaining<=10 ? (RacePosition==ActiveRaceCars?"DANGER — YOU ARE LAST":"ELIMINATION")+" IN "+Mathf.CeilToInt(EliminationRemaining)+"s" :
            rechargeNotice>0?"BOOST RECHARGED  +45%":"";

        double RaceDistance(VehicleController car,RaceProgress progress)
        {
            int next=progress.NextCheckpoint;
            Vector3 start=track[(next+track.Count-1)%track.Count],segment=track[next]-start;
            float fraction=Mathf.Clamp(Vector3.Dot(car.PhysicsPosition-start,segment)/Mathf.Max(.001f,segment.sqrMagnitude),0,.999f);
            return progress.Progress+fraction;
        }
        int PlayerPosition()
        {
            double playerDistance=RaceDistance(Player,playerProgress);int place=1;
            foreach(var rival in racers)
                if((elimination==null || elimination.IsActive(rival.Id)) &&
                    (rival.FinishTime>=0 || RaceDistance(rival.Car,rival.Progress)>playerDistance))place++;
            return place;
        }
        void UpdateElimination()
        {
            raceDistances[0]=RaceDistance(Player,playerProgress);
            foreach(var rival in racers)raceDistances[rival.Id]=RaceDistance(rival.Car,rival.Progress);
            int removed;
            while((removed=elimination.EliminateDue(elapsed,raceDistances))>=0)
            {
                if(removed==0)
                {
                    EndMatch("ELIMINATED","You were in last place at the cutoff.\nFinished "+(elimination.ActiveCount+1)+" / 4. Use slipstream and recharge lanes to make your next pass.",false,(3-elimination.ActiveCount)*1000);
                    return;
                }
                foreach(var rival in racers)if(rival.Id==removed)
                {
                    rival.Car.ControlsEnabled=false;rival.Car.SetDraft(0);rival.Car.enabled=false;
                    rival.Car.gameObject.SetActive(false);
                }
                eliminatedNotice=DriverNames[removed]+" ELIMINATED";eliminatedNoticeUntil=elapsed+3;
                SoundDirector.Instance?.Cue(GameCue.Goal);
                if(elimination.Winner==0)
                {
                    EndMatch("LAST CAR STANDING","You survived all three eliminations.\nELIMINATION TROPHY • "+elapsed.ToString("0.0")+" seconds",true,3000);
                    return;
                }
            }
            RacePosition=PlayerPosition();
            StatusText=$"PLACE {RacePosition}/{elimination.ActiveCount} · {elimination.ActiveCount} CARS REMAIN\nLAP {playerProgress.CompletedLaps+1} · CHECKPOINT {playerProgress.NextCheckpoint}/{track.Count}";
        }
        void UpdateSlipstreams()
        {
            bool allowed=mode==GameMode.Championship || mode==GameMode.Elimination;
            foreach(var car in cars)
            {
                float strength=0;
                if(allowed && car && car.isActiveAndEnabled && car.ControlsEnabled && car.Grounded)
                    foreach(var leader in cars)
                    {
                        if(leader==car || !leader || !leader.isActiveAndEnabled || !leader.ControlsEnabled || !leader.Grounded)continue;
                        Vector3 forward=car.PhysicsRotation*Vector3.forward,leaderForward=leader.PhysicsRotation*Vector3.forward;
                        if(Vector3.Dot(car.Velocity,forward)<12.5f || Vector3.Dot(leader.Velocity,leaderForward)<12.5f)continue;
                        Vector3 local=Quaternion.Inverse(car.PhysicsRotation)*(leader.PhysicsPosition-car.PhysicsPosition);
                        float candidate=RaceCraft.DraftStrength(local.z,local.x,local.y,Vector3.Dot(forward,leaderForward),Mathf.Min(car.SpeedKmh,leader.SpeedKmh));
                        if(candidate<=strength)continue;
                        if(Physics.Linecast(car.PhysicsPosition+Vector3.up*.6f,leader.PhysicsPosition+Vector3.up*.6f,out var hit,~0,QueryTriggerInteraction.Ignore)
                            && hit.collider.GetComponentInParent<VehicleController>()!=leader)continue;
                        strength=candidate;
                    }
                if(car)car.SetDraft(Mathf.MoveTowards(car.DraftStrength,strength,Time.deltaTime*(strength>car.DraftStrength?.8f:2)));
            }
        }
        void DriveCircuitRival(Racer racer)
        {
            var car=racer.Car;int next=racer.Progress.NextCheckpoint;
            Vector3 forward=(track[(next+1)%track.Count]-track[(next+track.Count-1)%track.Count]).normalized;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            Vector3 origin=car.PhysicsPosition;
            float left=30,centre=30,rightGap=30;
            float currentLateral=Vector3.Dot(origin-track[next],right);
            foreach(var other in cars)
            {
                if(other==car || !other || !other.isActiveAndEnabled || !other.ControlsEnabled)continue;
                Vector3 delta=other.PhysicsPosition-origin;
                float ahead=Vector3.Dot(delta,forward);
                if(ahead< -4 || ahead>30 || Mathf.Abs(delta.y)>2)continue;
                float lateral=currentLateral+Vector3.Dot(delta,right),gap=Mathf.Max(0,ahead-5);
                if(Mathf.Abs(lateral+4)<2.7f)left=Mathf.Min(left,gap);
                if(Mathf.Abs(lateral)<2.7f)centre=Mathf.Min(centre,gap);
                if(Mathf.Abs(lateral-4)<2.7f)rightGap=Mathf.Min(rightGap,gap);
            }
            racer.LaneHold-=Time.deltaTime;
            if(racer.LaneHold<=0)
            {
                float lane=RaceCraft.ChooseLane(racer.Lane,left,centre,rightGap,racer.Id==1?-1:1);
                if(lane!=racer.Lane){racer.Lane=lane;racer.LaneHold=1.2f;}
            }
            Vector3 target=track[next]+forward*3+right*racer.Lane;
            Vector3 local=Quaternion.Inverse(car.PhysicsRotation)*(target-origin);
            float steer=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/35,-1,1);
            Vector3 approach=track[next]-track[(next+track.Count-1)%track.Count];
            float bend=Vector3.Angle(approach,track[(next+3)%track.Count]-track[next]);
            float targetSpeed=RaceCraft.CornerSpeed(car.Stats.MaxSpeed*3.6f,bend,racer.Id);
            if(Mathf.Abs(steer)>.75f)targetSpeed=Mathf.Min(targetSpeed,65);
            float gapHere=currentLateral< -2?left:currentLateral>2?rightGap:centre;
            bool blocked=gapHere<6 && Mathf.Abs(currentLateral-racer.Lane)<2;
            if(blocked)targetSpeed=Mathf.Min(targetSpeed,Mathf.Clamp((gapHere-2)*10,0,40));
            bool boost=bend<12 && Mathf.Abs(steer)<.35f && gapHere>12 && car.BoostAmount>(racer.Id==2?.25f:.5f) && car.SpeedKmh<targetSpeed+10;
            car.SetInput(new DriveInput { Throttle=blocked && targetSpeed<=0?0:car.SpeedKmh>targetSpeed?.05f:1,Steering=steer,
                Brake=(blocked && gapHere<3) || car.SpeedKmh>targetSpeed+7,Boost=boost });
            racer.Stuck=car.SpeedKmh<4?racer.Stuck+Time.deltaTime:0;
            if(racer.Stuck>4 || IsOutsidePlayableArea(origin) || Vector3.Dot(car.transform.up,Vector3.up)<-.5f)
            {
                Recover(car,next);racer.Previous=car.PhysicsPosition;racer.Stuck=0;racer.Lane=0;racer.LaneHold=0;
            }
        }
    }
}
