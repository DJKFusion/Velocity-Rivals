using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Shared imported clips, per-car mix; no audio-thread scene access.</summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class RacingAudio : MonoBehaviour
    {
        VehicleController car;
        AudioSource engine, tyres, boost, wind, transients;
        AudioSource[] sources;
        int previousGear;
        float nextImpact, revs=.65f;
        bool focused=true;
        public float EngineVolume => engine ? engine.volume : 0;
        public static int Gear(float speed) => Mathf.Clamp(1+Mathf.FloorToInt(Mathf.Abs(speed)/38),1,6);
        void Start()
        {
            car=GetComponent<VehicleController>();
            string name=car.Stats ? car.Stats.DisplayName : "";
            string voice=name=="Bulwark" || name=="Nomad" ? "EngineHeavy" : name=="Phantom" || name=="Apex" ? "EngineExotic" : "EngineSport";
            engine=Source(voice,true);tyres=Source("Tyres",true);boost=Source("Boost",true);
            wind=Source("Wind",true);transients=Source(null,false);
            sources=new[]{engine,tyres,boost,wind,transients};
            previousGear=Gear(car.SpeedKmh);
        }
        AudioSource Source(string clip,bool loop)
        {
            var source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=loop;source.volume=0;
            source.spatialBlend=car.IsPlayer?0:1;source.minDistance=5;source.maxDistance=65;
            source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=car.IsPlayer?0:.35f;
            source.priority=car.IsPlayer?64:160;
            if(clip!=null)source.clip=Resources.Load<AudioClip>("RacingAudio/"+clip);
            if(loop && source.clip)source.Play();
            return source;
        }
        bool Audible()
        {
            var session=GameSession.Instance;
            return focused && car && car.enabled && Time.timeScale>0 && session && session.Running && !session.Paused && !session.Finished && session.ResumeRemaining<=0;
        }
        void Update()
        {
            if(!engine)return;
            if(!Audible()) { Silence();return; }
            float volume=Mathf.Clamp01(SaveStore.Current.effectsVolume)*(car.IsPlayer?1:.45f);
            if(volume<=0) { Silence();return; }
            float speed=car.SpeedKmh, load=Mathf.Abs(car.AudioThrottle);
            int gear=Gear(speed);
            float target=.65f+Mathf.Clamp01((speed-(gear-1)*38)/38)*.8f+load*.25f;
            if(!car.Grounded)target+=load*.25f;
            revs=Mathf.Lerp(revs,target,1-Mathf.Exp(-Time.deltaTime*9));engine.pitch=revs;
            Mix(engine,volume*(.16f+load*.14f+Mathf.Clamp01(speed/180)*.06f));
            Mix(tyres,car.IsDrifting && car.ControlsEnabled?volume*.23f:0);
            tyres.pitch=.85f+Mathf.Clamp01(speed/150)*.3f;
            Mix(boost,car.IsBoosting?volume*.3f:0);
            Mix(wind,car.IsPlayer?volume*Mathf.Pow(Mathf.Clamp01(speed/200),2)*.17f:0);
            transients.volume=volume;
            if(gear!=previousGear && car.ControlsEnabled && car.Grounded && car.IsPlayer)
                Play("Shift",.3f);
            previousGear=gear;
        }
        void Mix(AudioSource source,float target)
        {
            if(source.clip && !source.isPlaying)source.Play();
            source.volume=Mathf.MoveTowards(source.volume,target,Time.unscaledDeltaTime*2);
        }
        void Play(string name,float gain)
        {
            var clip=Resources.Load<AudioClip>("RacingAudio/"+name);
            if(clip)transients.PlayOneShot(clip,gain);
        }
        void OnCollisionEnter(Collision collision)
        {
            if(!transients || !Audible() || Time.time<nextImpact || collision.relativeVelocity.magnitude<4)return;
            nextImpact=Time.time+.18f;
            Play("Impact",Mathf.Clamp01(collision.relativeVelocity.magnitude/22)*.7f);
        }
        void Silence()
        {
            if(sources==null)return;
            foreach(var source in sources)
                if(source) { source.volume=0;source.Stop(); }
        }
        void OnApplicationFocus(bool value) { focused=value;if(!value)Silence(); }
        void OnDisable() { Silence(); }
        void OnDestroy()
        {
            if(sources==null)return;
            foreach(var source in sources) if(source)Destroy(source);
        }
    }
}
