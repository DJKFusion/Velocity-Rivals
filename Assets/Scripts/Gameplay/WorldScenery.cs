using System.Collections.Generic;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Collision-free scenery combined by material. Match destruction releases owned resources.</summary>
    public sealed class WorldScenery : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
        static readonly Color[] Accents = { new Color(.05f,.8f,1), new Color(.6f,.3f,1), new Color(1,.55f,.12f), new Color(.85f,.1f,.5f), new Color(.4f,.8f,1) };

        public static void Build(Transform parent, List<Vector3> route, int theme, bool racing)
        {
            var go = new GameObject("Combined scenery"); go.transform.SetParent(parent, false);
            go.AddComponent<WorldScenery>().Generate(route, theme, racing);
        }
        Material Surface(string name, Color colour, float metal = 0, bool glow = false)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = colour };
            mat.SetFloat("_Metallic", metal); mat.SetFloat("_Smoothness", metal > 0 ? .7f : .22f);
            if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", colour * 2.5f); }
            materials.Add(mat); groups.Add(mat, new List<CombineInstance>()); return mat;
        }
        void Shape(PrimitiveType type, Vector3 position, Vector3 size, Quaternion rotation, Material material)
        {
            var temporary = GameObject.CreatePrimitive(type);
            var mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
            groups[material].Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, size) });
            temporary.SetActive(false); Destroy(temporary);
        }
        void Box(Vector3 p, Vector3 size, Material material, Quaternion rotation = default)
            => Shape(PrimitiveType.Cube, p, size, rotation == default ? Quaternion.identity : rotation, material);

        void Generate(List<Vector3> route, int theme, bool racing)
        {
            var paint = Surface("Road markings", new Color(.7f,.8f,.86f));
            var dark = Surface("Architectural graphite", new Color(.035f,.055f,.085f), .5f);
            var glass = Surface("Tower glass", new Color(.05f,.12f,.2f), .85f);
            var neon = Surface("Scenery neon", Accents[theme], .2f, true);
            var warm = Surface("Window lights", new Color(.9f,.65f,.3f), 0, true);
            var terrain = Surface("Natural formations", theme == 2 ? new Color(.45f,.23f,.11f) : new Color(.47f,.73f,.86f), theme == 4 ? .3f : 0);
            if (racing)
            {
                for (int i = 0; i < route.Count; i++)
                {
                    Vector3 a = route[i], b = route[(i + 1) % route.Count];
                    Quaternion facing = Quaternion.LookRotation(b-a);
                    for (int dash = 0; dash < 2; dash++)
                        Box(Vector3.Lerp(a,b,.25f+dash*.5f)+Vector3.up*.025f,new Vector3(.12f,.025f,2),paint,facing);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Box((a+b)*.5f+facing*Vector3.right*(side*9.4f)+Vector3.up*.025f,new Vector3(.16f,.025f,Vector3.Distance(a,b)),paint,facing);
                        for (int stripe = 0; stripe < 4; stripe++)
                            Box(Vector3.Lerp(a,b,stripe/4f)+facing*Vector3.right*(side*9.8f)+Vector3.up*.03f,new Vector3(.6f,.035f,Vector3.Distance(a,b)/4),stripe%2==0?neon:paint,facing);
                    }
                    if (i % 4 == 0)
                    {
                        Box(a+Vector3.up*6,new Vector3(20,.5f,.6f),dark,facing);
                        Box(a+Vector3.up*5.7f,new Vector3(18,.12f,.65f),neon,facing);
                    }
                    if (i % 2 != 0) continue;
                    Vector3 radial = new Vector3(a.x,0,a.z).normalized;
                    Vector3 centre = a + radial * 29;
                    if (theme == 2 || theme == 4)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Vector3 p = centre + Vector3.Cross(radial,Vector3.up)*j*6;
                            float height = 9 + (i+j*7)%17;
                            Shape(theme==4?PrimitiveType.Capsule:PrimitiveType.Sphere,p+Vector3.up*height*.35f,new Vector3(7,height*.5f,8),Quaternion.Euler(8,i*19,15),terrain);
                        }
                    }
                    else
                    {
                        float height=18+i%7*5;
                        Box(centre+Vector3.up*height*.5f,new Vector3(9,height,9),glass);
                        Box(centre+Vector3.up*(height+.2f),new Vector3(9.5f,.4f,9.5f),neon);
                        for(int floor=2;floor<height;floor+=3)
                        {
                            Box(centre+Vector3.up*floor,new Vector3(9.1f,.12f,9.1f),dark);
                            for(int window=-3;window<=3;window+=2)
                                if ((floor+window+i)%3!=0)
                                    Box(centre+new Vector3(window,floor+1,-4.56f),new Vector3(.65f,1.1f,.05f),floor%2==0?warm:neon);
                        }
                    }
                }
                if(route.Count>1)
                {
                    Quaternion facing=Quaternion.LookRotation(route[1]-route[0]);
                    for(int x=0;x<20;x++) for(int z=0;z<3;z++)
                        Box(route[0]+facing*new Vector3(x-9.5f,.045f,z-1),new Vector3(1,.03f,1),(x+z)%2==0?paint:dark,facing);
                }
            }
            else
            {
                // Spectator tiers stay outside the collision walls; roof ribs leave driving/aerial clearance.
                for(int side=-1;side<=1;side+=2)
                {
                    for(int tier=0;tier<5;tier++)
                    {
                        Box(new Vector3(side*(48+tier*2),5+tier*1.5f,0),new Vector3(2,1,104),dark);
                        Box(new Vector3(side*(48+tier*2),5.55f+tier*1.5f,0),new Vector3(.2f,.1f,104),neon);
                    }
                    for(int z=-40;z<=40;z+=20)
                    {
                        Box(new Vector3(side*46,11,z),new Vector3(.7f,22,.7f),dark);
                        Box(new Vector3(side*45.5f,10,z),new Vector3(.14f,20,.8f),neon);
                    }
                }
                for(int z=-40;z<=40;z+=20)
                    for(int segment=0;segment<12;segment++)
                    {
                        float a=segment*Mathf.PI/12,b=(segment+1)*Mathf.PI/12;
                        Vector3 from=new Vector3(Mathf.Cos(a)*46,20+Mathf.Sin(a)*14,z);
                        Vector3 to=new Vector3(Mathf.Cos(b)*46,20+Mathf.Sin(b)*14,z);
                        Box((from+to)*.5f,new Vector3(.45f,.45f,Vector3.Distance(from,to)),dark,Quaternion.LookRotation(to-from));
                    }
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                    Vector3 from=new Vector3(Mathf.Sin(a)*10,.03f,Mathf.Cos(a)*10),to=new Vector3(Mathf.Sin(b)*10,.03f,Mathf.Cos(b)*10);
                    Box((from+to)*.5f,new Vector3(.14f,.02f,Vector3.Distance(from,to)),paint,Quaternion.LookRotation(to-from));
                }
                Box(new Vector3(0,.025f,0),new Vector3(88,.02f,.12f),paint);
            }
            foreach(var group in groups)
            {
                if(group.Value.Count==0) continue;
                var mesh=new Mesh { name=group.Key.name+" combined", indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(),true,true);meshes.Add(mesh);
                var go=new GameObject(group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
            groups.Clear();
            var probeObject = new GameObject("Arena reflections", typeof(ReflectionProbe));
            probeObject.transform.SetParent(transform, false);
            probeObject.transform.localPosition = Vector3.up * 4;
            var probe = probeObject.GetComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 128; probe.size = new Vector3(280, 80, 260);
            probe.boxProjection = true; probe.nearClipPlane = .3f; probe.farClipPlane = 250;
            probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            probe.backgroundColor = new Color(.18f, .25f, .35f);
        }
        void OnDestroy()
        {
            foreach(var mesh in meshes) if(mesh!=null) Destroy(mesh);
            foreach(var material in materials) if(material!=null) Destroy(material);
        }
    }
}
