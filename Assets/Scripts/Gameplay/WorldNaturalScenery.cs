using UnityEngine;

namespace VelocityRivals
{
    public sealed partial class WorldScenery
    {
        readonly Mesh[] rockMeshes = new Mesh[8];
        Mesh crownMesh;
        Mesh CrownMesh()
        {
            if(crownMesh)return crownMesh;
            var vertices=new Vector3[10];vertices[8]=Vector3.up;vertices[9]=Vector3.zero;
            var triangles=new int[48];
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;
                vertices[i]=new Vector3(Mathf.Sin(angle)*.5f,0,Mathf.Cos(angle)*.5f);
                int next=(i+1)%8;
                triangles[i*6]=i;triangles[i*6+1]=next;triangles[i*6+2]=8;
                triangles[i*6+3]=i;triangles[i*6+4]=9;triangles[i*6+5]=next;
            }
            crownMesh=new Mesh { name="Evergreen crown" };crownMesh.vertices=vertices;crownMesh.triangles=triangles;
            crownMesh.RecalculateNormals();crownMesh.RecalculateBounds();meshes.Add(crownMesh);return crownMesh;
        }
        Mesh RockMesh(int variant)
        {
            if(rockMeshes[variant])return rockMeshes[variant];
            var source=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var original=source.GetComponent<MeshFilter>().sharedMesh;
            var vertices=original.vertices;
            source.SetActive(false);Destroy(source);
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 p=vertices[i];
                float variation=1+.16f*Mathf.Sin(p.x*17+variant*3)*Mathf.Cos(p.z*13-variant)+.08f*Mathf.Sin(p.y*27+variant);
                vertices[i]=new Vector3(p.x*variation,Mathf.Pow(Mathf.Clamp01(p.y+.5f),.8f)*variation,p.z*variation);
            }
            var mesh=new Mesh { name="Eroded rock formation "+variant };
            mesh.vertices=vertices;mesh.triangles=original.triangles;mesh.uv=original.uv;
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            meshes.Add(mesh);rockMeshes[variant]=mesh;return mesh;
        }
        void NaturalFormation(Vector3 centre,int seed,int theme,Material rock,Material snow,Material bark,Material foliage)
        {
            for(int part=0;part<3;part++)
            {
                int variant=(seed/30+part)%8;
                Vector3 p=centre+new Vector3((part-1)*6,0,part==1?4:-3);
                Vector3 size=new Vector3(15-part*2,9+(seed+part*7)%18,17-part*2);
                Quaternion rotation=Quaternion.Euler(0,(seed+part*47)%180,0);
                var mesh=RockMesh(variant);
                groups[rock].Add(new CombineInstance { mesh=mesh,transform=Matrix4x4.TRS(p,rotation,size) });
                if(theme==4)
                    groups[snow].Add(new CombineInstance { mesh=mesh,transform=Matrix4x4.TRS(p+Vector3.up*size.y*.62f,rotation,new Vector3(size.x*.67f,size.y*.41f,size.z*.67f)) });
            }
            if(theme==4)
                for(int tree=0;tree<3;tree++)
                {
                    Vector3 p=centre+new Vector3((tree-1)*8,0,-11);
                    float height=6+(seed+tree)%4;
                    Shape(PrimitiveType.Cylinder,p+Vector3.up*height*.35f,new Vector3(.4f,height*.35f,.4f),Quaternion.identity,bark);
                    // Overlapping tapered crowns produce an evergreen silhouette without stacked cubes.
                    for(int tier=0;tier<3;tier++)
                    {
                        Vector3 size=new Vector3(4-tier,height*.45f,4-tier);
                        groups[foliage].Add(new CombineInstance { mesh=CrownMesh(),transform=Matrix4x4.TRS(p+Vector3.up*(height*.25f+tier*height*.2f),Quaternion.identity,size) });
                    }
                }
        }
    }
}
