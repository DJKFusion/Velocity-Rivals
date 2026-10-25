using System.Collections.Generic;
using UnityEngine;

namespace VelocityRivals
{
    public sealed partial class WorldScenery
    {
        // Footprints are checked against the whole route, including concave bends.
        static bool ClearFootprint(Vector3 p, float radius, List<Vector3> route, bool racing)
        {
            if (!racing) return Mathf.Abs(p.x) > 46 + radius || Mathf.Abs(p.z) > 51 + radius;
            for (int i = 0; i < route.Count; i++)
            {
                Vector3 a = route[i], delta = route[(i + 1) % route.Count] - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                if ((p - (a + delta * t)).sqrMagnitude < (13 + radius) * (13 + radius)) return false;
            }
            return true;
        }
        void BuildDistricts(List<Vector3> route, int theme, bool racing, Material dark, Material glass,
            Material neon, Material warm, Material rock, Material paint)
        {
            var concrete = Surface("District concrete", theme == 2 ? new Color(.52f,.38f,.25f) : new Color(.21f,.26f,.31f), .15f);
            var roof = Surface("District roof", theme == 4 ? new Color(.8f,.9f,.96f) : new Color(.08f,.13f,.19f), .35f);
            var foliage = Surface("Alpine foliage",new Color(.065f,.16f,.15f));
            int placed = 0;
            int extent = racing ? 270 : 150, spacing = racing ? 45 : 30;
            for (int x = -extent; x <= extent; x += spacing)
                for (int z = -extent; z <= extent; z += spacing)
                {
                    Vector3 centre = new Vector3(x, 0, z);
                    if (centre.sqrMagnitude > (extent + 40) * (extent + 40) || !ClearFootprint(centre, 16, route, racing)) continue;
                    int seed = Mathf.Abs(x * 37 + z * 19);
                    if (theme == 2 || theme == 4)
                    {
                        if ((seed / 30) % 4 != 0)
                        {
                            NaturalFormation(centre,seed,theme,rock,roof,dark,foliage);
                        }
                        else Outpost(centre, theme, concrete, roof, neon, glass);
                    }
                    else if (theme == 1)
                    {
                        Box(centre + Vector3.down * 5, new Vector3(24, 10, 24), concrete);
                        Tower(centre, 12 + seed % 25, glass, dark, neon, warm);
                        Box(centre + Vector3.down * 13, new Vector3(5, 18, 5), dark);
                    }
                    else Tower(centre, 16 + seed % 43, glass, concrete, neon, warm);
                    placed++;
                }
            // A central landmark gives the inner field a purpose instead of leaving an empty oval.
            if (ClearFootprint(Vector3.zero, 24, route, racing))
            {
                BuildLandmark(theme, concrete, glass, neon, roof, rock);
            }
            if (racing)
            {
                if (theme == 1)
                    for (int i = 0; i < route.Count; i++)
                    {
                        Vector3 a=route[i], b=route[(i+1)%route.Count], p=(a+b)*.5f;
                        Quaternion facing=Quaternion.LookRotation(b-a);
                        Box(p+Vector3.down*1.3f,new Vector3(22,2,Vector3.Distance(a,b)+1),concrete,facing);
                        if(i%4==0)
                        {
                            Box(a+Vector3.down*14,new Vector3(5,28,5),dark);
                            Box(a+Vector3.down*3,new Vector3(26,2,3),concrete,facing);
                        }
                    }
                for (int i = 0; i < route.Count; i += 3)
                {
                    var forward = Quaternion.LookRotation(route[(i + 1) % route.Count] - route[i]);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 p = route[i] + forward * Vector3.right * side * 13;
                        Box(p + Vector3.up * 4, new Vector3(.22f,8,.22f),dark);
                        Box(p + Vector3.up * 7.9f - forward * Vector3.right * side, new Vector3(2.5f,.18f,.45f),paint,forward);
                    }
                }
                for (int sector = 0; sector < 4; sector++)
                {
                    int index = sector * route.Count / 4;
                    Vector3 p = route[index]; Quaternion facing = Quaternion.LookRotation(route[(index+1)%route.Count]-p);
                    Box(p + Vector3.up * 8, new Vector3(25,2,.8f),dark,facing);
                    Sign(CircuitLayout.Section(theme,index,route.Count).ToUpperInvariant(),p+Vector3.up*8,facing,neon.color, .18f);
                    foreach (int side in new[] {-1,1}) Box(p+facing*Vector3.right*side*12+Vector3.up*4,new Vector3(.7f,8,.7f),concrete);
                }
                // A covered district: continuous roof bays and open sides, with eight metres of clearance.
                int tunnelStart = route.Count / 2;
                for (int j = 0; j < 6; j++)
                {
                    int i = (tunnelStart + j) % route.Count;
                    Vector3 a=route[i], b=route[(i+1)%route.Count], p=(a+b)*.5f;
                    Quaternion facing=Quaternion.LookRotation(b-a);
                    Box(p+Vector3.up*9,new Vector3(26,1.2f,Vector3.Distance(a,b)+1.5f),theme==2?rock:roof,facing);
                    foreach(int side in new[]{-1,1})
                    {
                        Box(p+facing*Vector3.right*side*12+Vector3.up*4,new Vector3(1,8,1),concrete);
                        Box(p+facing*Vector3.right*side*10.8f+Vector3.up*7.9f,new Vector3(.2f,.2f,Vector3.Distance(a,b)+.5f),neon,facing);
                    }
                }
                Vector3 start = route[0]; Quaternion startFacing=Quaternion.LookRotation(route[1]-start);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 stand=start+startFacing*new Vector3(side*38,0,-8);
                    if(ClearFootprint(stand,16,route,true)) Grandstand(stand,startFacing,side,dark,paint,roof,neon);
                }
                Sign("VELOCITY RIVALS  /  START",start+Vector3.up*6.7f,startFacing,Color.white,.16f);
            }
            else
            {
                // Close the stadium ends with facades, concourses and illuminated scoreboards.
                foreach(int end in new[]{-1,1})
                {
                    Vector3 p=new Vector3(0,0,end*60); Quaternion face=Quaternion.Euler(0,end<0?0:180,0);
                    Box(p+Vector3.up*8,new Vector3(108,16,6),concrete);
                    Box(p+Vector3.up*16.5f,new Vector3(112,1,10),roof);
                    Box(new Vector3(0,13,end*55.9f),new Vector3(32,7,.6f),dark);
                    Sign(end>0?"RIVALS END":"VELOCITY END",new Vector3(0,13,end*55.5f),face,neon.color,.3f);
                    for(int lane=-3;lane<=3;lane++)
                        Box(new Vector3(lane*14,3,end*56.8f),new Vector3(8,6,.5f),glass);
                }
                foreach(int side in new[]{-1,1})
                {
                    Box(new Vector3(side*61,8,0),new Vector3(3,16,120),concrete);
                    Box(new Vector3(side*56,17,0),new Vector3(15,1.2f,122),roof);
                    for(int z=-45;z<=45;z+=10)
                        for(int row=0;row<4;row++)
                            Box(new Vector3(side*(48+row*2),6+row*1.5f,z),new Vector3(1,.7f,6),row%2==0?paint:neon);
                }
            }
            gameObject.name = "Structured districts ("+placed+" blocks)";
        }
        void Tower(Vector3 p,float height,Material glass,Material baseMaterial,Material neon,Material windows)
        {
            int style=Mathf.Abs(Mathf.RoundToInt(p.x/30*13+p.z/30*7))%3;
            Box(p+Vector3.up*2,new Vector3(24,4,24),baseMaterial);
            if(style==1)
            {
                Shape(PrimitiveType.Cylinder,p+Vector3.up*(height*.5f+4),new Vector3(18,height*.5f,18),Quaternion.identity,glass);
                for(int floor=8;floor<height+4;floor+=4)
                    Shape(PrimitiveType.Cylinder,p+Vector3.up*floor,new Vector3(18.3f,.12f,18.3f),Quaternion.identity,floor%8==0?windows:baseMaterial);
                Shape(PrimitiveType.Cylinder,p+Vector3.up*(height+4),new Vector3(19,.3f,19),Quaternion.identity,neon);
                return;
            }
            if(style==2)
            {
                for(int tier=0;tier<3;tier++)
                {
                    float width=21-tier*4;
                    Box(p+Vector3.up*(4+height*(tier+.5f)/3),new Vector3(width,height/3,width),glass);
                    Box(p+Vector3.up*(4+height*(tier+1)/3),new Vector3(width+.3f,.3f,width+.3f),neon);
                    for(int column=-1;column<=1;column++)
                        foreach(int side in new[]{-1,1})
                            Box(p+new Vector3(column*width*.3f,4+height*(tier+.5f)/3,side*(width*.5f+.06f)),new Vector3(.25f,height/3,.12f),windows);
                }
                return;
            }
            Box(p+Vector3.up*(4+height*.5f),new Vector3(16,height,17),glass);
            Box(p+Vector3.up*(height+4.5f),new Vector3(18,1,19),baseMaterial);
            Box(p+Vector3.up*(height+5.1f),new Vector3(18,.18f,19),neon);
            for(int floor=7;floor<height+3;floor+=4)
            {
                Box(p+Vector3.up*floor,new Vector3(16.2f,.3f,17.2f),baseMaterial);
                foreach(int side in new[]{-1,1})
                    for(int window=-5;window<=5;window+=5)
                    {
                        Box(p+new Vector3(window,floor+1.4f,side*8.55f),new Vector3(2.3f,1.5f,.08f),windows);
                        Box(p+new Vector3(side*8.05f,floor+1.4f,window),new Vector3(.08f,1.5f,2.3f),windows);
                    }
            }
        }
        void Outpost(Vector3 p,int theme,Material concrete,Material roof,Material neon,Material glass)
        {
            Box(p+Vector3.up*4,new Vector3(22,8,18),concrete);
            Box(p+Vector3.up*8.5f,new Vector3(24,1,20),roof);
            Box(p+new Vector3(0,4,-9.1f),new Vector3(18,3,.15f),glass);
            Box(p+new Vector3(0,7,-9.2f),new Vector3(19,.2f,.2f),neon);
            Box(p+new Vector3(7,13,0),new Vector3(.3f,10,.3f),concrete);
            Shape(PrimitiveType.Sphere,p+new Vector3(7,17,0),new Vector3(5,1.5f,5),Quaternion.Euler(20,0,0),roof);
            if(theme==2) for(int i=-1;i<=1;i++) Box(p+new Vector3(i*6,9.5f,0),new Vector3(5,.2f,12),glass,Quaternion.Euler(-18,0,0));
        }
        void Grandstand(Vector3 p,Quaternion rotation,int side,Material concrete,Material seats,Material roof,Material neon)
        {
            for(int row=0;row<5;row++)
            {
                Vector3 position=p+rotation*new Vector3(side*row*1.8f,2+row*1.2f,0);
                Box(position,new Vector3(2,2,24),concrete,rotation);
                Box(position+Vector3.up*1.1f,new Vector3(1,.35f,23),seats,rotation);
            }
            Box(p+Vector3.up*10,new Vector3(20,.7f,28),roof,rotation);
            Box(p+Vector3.up*9.5f,new Vector3(20,.15f,28),neon,rotation);
        }
        void Sign(string text,Vector3 position,Quaternion rotation,Color color,float size)
        {
            var go=new GameObject("District sign");go.transform.SetParent(transform,false);
            go.transform.position=position;
            var label=go.AddComponent<TextMesh>();label.text=text;label.anchor=TextAnchor.MiddleCenter;
            label.alignment=TextAlignment.Center;label.fontSize=48;label.characterSize=size;label.color=color;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            go.AddComponent<ReadableWorldSign>();
        }
    }
}
