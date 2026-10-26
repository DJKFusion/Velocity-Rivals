using System.Collections.Generic;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Match-owned race pickups. No inventory or timers survive a restart.</summary>
    public sealed class RaceItems : MonoBehaviour
    {
        sealed class Driver
        {
            public VehicleController Car;
            public readonly PowerupSlot Slot=new PowerupSlot();
            public float ShieldUntil,ThinkAt;
        }
        sealed class Crate { public Transform Visual; public Vector3 Position; public PowerupKind Kind; public float ReadyAt; }
        readonly List<Driver> drivers=new List<Driver>();
        readonly List<Crate> crates=new List<Crate>();
        readonly List<Material> materials=new List<Material>();
        GameSession session;
        public PowerupKind Held(VehicleController car)=>Find(car)?.Slot.Held??PowerupKind.None;
        public string PlayerEffect => Find(session.Player) is Driver d && d.ShieldUntil>session.Elapsed ? "SHIELD  "+Mathf.CeilToInt(d.ShieldUntil-session.Elapsed)+"s" : "";
        Driver Find(VehicleController car)=>drivers.Find(d=>d.Car==car);
        public static Color Tint(PowerupKind kind)=>kind==PowerupKind.Shield?Color.cyan:kind==PowerupKind.Overdrive?new Color(1,.4f,.08f):new Color(.75f,.3f,1);
        public void Initialize(GameSession owner)
        {
            session=owner;
            foreach(var car in owner.Vehicles)drivers.Add(new Driver{Car=car});
            var route=owner.Circuit;
            for(int station=0;station<4;station++)
            {
                int index=(route.Count*(station*2+1)/8)%route.Count;
                Quaternion facing=Quaternion.LookRotation(route[(index+1)%route.Count]-route[index]);
                for(int lane=-1;lane<=1;lane++)
                {
                    var kind=(PowerupKind)(1+(station+lane+1)%3);
                    Vector3 position=route[index]+facing*Vector3.right*(lane*5)+Vector3.up*1.5f;
                    var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=kind+" item crate";go.transform.SetParent(transform);
                    go.transform.position=position;go.transform.localScale=Vector3.one*1.2f;
                    go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
                    var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=Tint(kind);
                    mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Tint(kind)*2);materials.Add(mat);
                    go.GetComponent<Renderer>().sharedMaterial=mat;
                    crates.Add(new Crate{Visual=go.transform,Position=position,Kind=kind});
                }
            }
        }
        public bool Collect(VehicleController car,PowerupKind kind)
        {
            var d=Find(car);
            if(!session.ActionEnabled || !car || !car.ControlsEnabled || !car.isActiveAndEnabled || d==null || !d.Slot.Collect(kind))return false;
            d.ThinkAt=session.Elapsed+1;
            if(car==session.Player)SoundDirector.Instance?.Cue(GameCue.Pickup);
            MatchEffects.Burst(car.PhysicsPosition+Vector3.up,Tint(kind));return true;
        }
        public bool Use(VehicleController car)
        {
            var d=Find(car);
            if(d==null || !d.Slot.Use(session.ActionEnabled&&car&&car.ControlsEnabled&&car.isActiveAndEnabled,out var kind))return false;
            if(kind==PowerupKind.Shield)d.ShieldUntil=session.Elapsed+6;
            else if(kind==PowerupKind.Overdrive)
            {
                car.RefillBoost(.6f);
                if(car.Grounded)car.GetComponent<Rigidbody>().AddForce(car.PhysicsRotation*Vector3.forward*7,ForceMode.VelocityChange);
            }
            else foreach(var target in drivers)
            {
                if(target==d || !target.Car || !target.Car.isActiveAndEnabled || !target.Car.ControlsEnabled || target.ShieldUntil>session.Elapsed)continue;
                Vector3 delta=target.Car.PhysicsPosition-car.PhysicsPosition;
                if(delta.sqrMagnitude>18*18)continue;
                if(Physics.Linecast(car.PhysicsPosition+Vector3.up,target.Car.PhysicsPosition+Vector3.up,out var hit,~0,QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<VehicleController>()!=target.Car)continue;
                delta.y=0;
                target.Car.GetComponent<Rigidbody>().AddForce(delta.normalized*6+Vector3.up,ForceMode.VelocityChange);
                MatchEffects.Burst(target.Car.PhysicsPosition,Tint(kind));
            }
            MatchEffects.Burst(car.PhysicsPosition+Vector3.up,Tint(kind));
            if(car==session.Player)SoundDirector.Instance?.Cue(GameCue.Boost);
            return true;
        }
        void Update()
        {
            if(!session || !session.ActionEnabled)return;
            float now=session.Elapsed;
            foreach(var crate in crates)
            {
                bool ready=now>=crate.ReadyAt;crate.Visual.gameObject.SetActive(ready);if(!ready)continue;
                crate.Visual.position=crate.Position+Vector3.up*Mathf.Sin(now*2)*.2f;crate.Visual.rotation=Quaternion.Euler(35,now*60,25);
                foreach(var d in drivers)
                    if((d.Car.PhysicsPosition-crate.Position).sqrMagnitude<9 && Collect(d.Car,crate.Kind))
                    {crate.ReadyAt=now+12;crate.Visual.gameObject.SetActive(false);break;}
            }
            foreach(var d in drivers)
            {
                if(d.Car==session.Player || now<d.ThinkAt || d.Slot.Held==PowerupKind.None)continue;
                bool nearby=false;
                foreach(var other in drivers)if(other!=d&&other.Car.isActiveAndEnabled&&(other.Car.PhysicsPosition-d.Car.PhysicsPosition).sqrMagnitude<16*16)nearby=true;
                if(d.Slot.Held==PowerupKind.Overdrive?d.Car.Grounded:nearby)Use(d.Car);
            }
        }
        void OnDestroy(){foreach(var mat in materials)if(mat)Destroy(mat);}
    }
}
