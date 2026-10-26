using UnityEngine;
using System.Collections.Generic;

namespace VelocityRivals
{
    public sealed partial class RaceItems
    {
        sealed class Hazard
        {
            public VehicleController Owner;
            public PowerupKind Kind;
            public GameObject Visual;
            public Vector3 Position;
            public float Created;
        }
        readonly List<Hazard> hazards=new List<Hazard>();
        readonly Dictionary<VehicleController,Vector3> previousPositions=new Dictionary<VehicleController,Vector3>();
        Material oilMaterial,mineMaterial;
        float playerHitUntil;
        public int ActiveHazardCount => hazards.Count;
        public static string ItemName(PowerupKind kind)=>kind==PowerupKind.OilSlick?"OIL SLICK":kind==PowerupKind.ShockMine?"SHOCK MINE":kind.ToString().ToUpperInvariant();
        bool TryDropPosition(VehicleController car,out Vector3 position)
        {
            position=Vector3.zero;
            if(!car || !car.Grounded)return false;
            Vector3 behind=car.PhysicsPosition-car.PhysicsRotation*Vector3.forward*4;
            if(!Physics.Raycast(behind+Vector3.up*2,Vector3.down,out var hit,5,~0,QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<VehicleController>() || hit.normal.y<.65f)return false;
            position=hit.point+Vector3.up*.08f;return true;
        }
        void DropHazard(VehicleController car,PowerupKind kind,Vector3 position)
        {
            // Fixed global cap and shared materials keep long races bounded.
            if(hazards.Count>=12)RemoveHazard(0);
            bool oil=kind==PowerupKind.OilSlick;
            var material=oil?oilMaterial:mineMaterial;
            if(!material)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=Tint(kind);
                material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Tint(kind)*1.5f);
                materials.Add(material);if(oil)oilMaterial=material;else mineMaterial=material;
            }
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name=ItemName(kind)+" dropped hazard";visual.transform.SetParent(transform);
            visual.transform.position=position;visual.transform.localScale=oil?new Vector3(4,.035f,4):new Vector3(2.4f,.18f,2.4f);
            var collider=visual.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            visual.GetComponent<Renderer>().sharedMaterial=material;
            hazards.Add(new Hazard{Owner=car,Kind=kind,Visual=visual,Position=position,Created=session.Elapsed});
        }
        void RemoveHazard(int index)
        {
            if(hazards[index].Visual)Destroy(hazards[index].Visual);
            hazards.RemoveAt(index);
        }
        void FixedUpdate()
        {
            if(!session)return;
            if(session.ActionEnabled)
            {
                for(int i=hazards.Count-1;i>=0;i--)
                {
                    var hazard=hazards[i];float age=session.Elapsed-hazard.Created;
                    if(age>=20){RemoveHazard(i);continue;}
                    if(hazard.Visual)hazard.Visual.GetComponent<Renderer>().enabled=age>=1 || Mathf.FloorToInt(age*10)%2==0;
                    if(age<1)continue;
                    foreach(var driver in drivers)
                    {
                        var car=driver.Car;
                        if(!car || !car.isActiveAndEnabled || !car.ControlsEnabled || !car.Grounded || (car==hazard.Owner && age<3))continue;
                        Vector3 end=car.PhysicsPosition;
                        Vector3 start=previousPositions.TryGetValue(car,out var old)?old:end;
                        Vector3 delta=end-start;
                        // Recovery teleports must not trigger traps along the teleport path.
                        if(delta.sqrMagnitude>40*40){start=end;delta=Vector3.zero;}
                        float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(hazard.Position-start,delta)/delta.sqrMagnitude):0;
                        Vector3 nearest=start+delta*t;
                        if(Mathf.Abs(nearest.y-hazard.Position.y)>2)continue;
                        nearest.y=hazard.Position.y;
                        float radius=hazard.Kind==PowerupKind.OilSlick?2.8f:2;
                        if((nearest-hazard.Position).sqrMagnitude>radius*radius)continue;
                        bool shielded=driver.ShieldUntil>session.Elapsed;
                        if(!shielded && !car.ApplyRaceHazard(hazard.Kind==PowerupKind.OilSlick))continue;
                        MatchEffects.Burst(car.PhysicsPosition,shielded?Color.cyan:Tint(hazard.Kind));
                        if(car==session.Player)
                        {
                            if(!shielded)playerHitUntil=session.Elapsed+2;
                            SoundDirector.Instance?.Cue(shielded?GameCue.Pickup:GameCue.Impact);
                        }
                        RemoveHazard(i);break;
                    }
                }
            }
            foreach(var driver in drivers)if(driver.Car)previousPositions[driver.Car]=driver.Car.PhysicsPosition;
        }
    }
}
