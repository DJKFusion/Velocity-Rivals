using UnityEngine;

namespace VelocityRivals
{
    public sealed partial class WorldScenery
    {
        void BuildLandmark(int theme, Material stone, Material glass, Material neon, Material roof, Material rock)
        {
            if (theme == 0)
            {
                // Central communications spire: a tapered silhouette above the district blocks.
                for (int tier=0;tier<5;tier++)
                {
                    float width=24-tier*3.5f, y=6+tier*12;
                    Shape(PrimitiveType.Cylinder,Vector3.up*y,new Vector3(width,6,width),Quaternion.identity,glass);
                    Shape(PrimitiveType.Cylinder,Vector3.up*(y+6),new Vector3(width+1,.2f,width+1),Quaternion.identity,neon);
                }
                Box(Vector3.up*72,new Vector3(1,24,1),neon);
            }
            else if (theme == 1)
            {
                Box(Vector3.up*3,new Vector3(30,6,30),stone);
                foreach (int side in new[]{-1,1}) Box(new Vector3(side*12,23,0),new Vector3(3,40,3),glass,Quaternion.Euler(0,0,side*12));
                LandmarkRing(new Vector3(0,37,0),17,neon,Quaternion.identity);
                LandmarkRing(new Vector3(0,37,0),12,roof,Quaternion.Euler(0,90,0));
            }
            else if (theme == 2)
            {
                // Rock arch and solar court, kept wholly inside the cleared central footprint.
                foreach (int side in new[]{-1,1})
                {
                    Box(new Vector3(side*13,13,0),new Vector3(10,27,16),rock,Quaternion.Euler(0,0,-side*8));
                    Box(new Vector3(side*8,27,0),new Vector3(18,8,15),rock,Quaternion.Euler(0,0,side*16));
                }
                for(int i=-1;i<=1;i++) Box(new Vector3(i*9,2,-15),new Vector3(7,.4f,8),glass,Quaternion.Euler(-25,0,0));
            }
            else if (theme == 3)
            {
                Shape(PrimitiveType.Cylinder,Vector3.up*8,new Vector3(22,8,22),Quaternion.identity,stone);
                Shape(PrimitiveType.Sphere,Vector3.up*25,new Vector3(13,13,13),Quaternion.identity,glass);
                LandmarkRing(Vector3.up*25,16,neon,Quaternion.Euler(30,0,25));
                LandmarkRing(Vector3.up*25,19,roof,Quaternion.Euler(-30,70,0));
                Box(Vector3.up*12,new Vector3(3,24,3),neon);
            }
            else
            {
                for(int i=0;i<7;i++)
                {
                    float angle=i*Mathf.PI*2/7;
                    Vector3 p=new Vector3(Mathf.Cos(angle)*12,12+i*1.5f,Mathf.Sin(angle)*12);
                    Box(p,new Vector3(6,24+i*3,6),i%2==0?roof:rock,Quaternion.Euler(i*3,45+i*17,12));
                }
                Shape(PrimitiveType.Sphere,Vector3.up*4,new Vector3(20,8,20),Quaternion.identity,glass);
            }
        }
        void LandmarkRing(Vector3 centre,float radius,Material material,Quaternion rotation)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;
                Vector3 p=centre+rotation*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
                Vector3 q=centre+rotation*new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0);
                Box((p+q)*.5f,new Vector3(.65f,.65f,Vector3.Distance(p,q)+.1f),material,Quaternion.LookRotation(q-p));
            }
        }
    }
}
