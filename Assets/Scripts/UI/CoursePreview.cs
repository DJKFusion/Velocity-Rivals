using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VelocityRivals
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CoursePreview : MaskableGraphic
    {
        readonly List<Vector2> points = new List<Vector2>();
        public void Configure(int environment, bool circuit)
        {
            points.Clear();
            if (circuit)
                foreach (var p in CircuitLayout.Build(environment)) points.Add(new Vector2(p.X, p.Z));
            else points.AddRange(new[] { new Vector2(-45,-50), new Vector2(45,-50), new Vector2(45,50), new Vector2(-45,50) });
            if (!circuit) for (int i=0;i<points.Count;i++) points[i]*=CircuitLayout.ArenaExpansion;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (points.Count == 0) return;
            Rect rect = rectTransform.rect;
            Vector2 min = points[0], max = min;
            foreach (var p in points) { min = Vector2.Min(min,p); max = Vector2.Max(max,p); }
            float scale = Mathf.Min((rect.width-36)/Mathf.Max(1,max.x-min.x), (rect.height-36)/Mathf.Max(1,max.y-min.y));
            Vector2 Map(Vector2 p) => rect.center + (p-(min+max)*.5f)*scale;
            for (int i=0;i<points.Count;i++)
            {
                Vector2 a=Map(points[i]), b=Map(points[(i+1)%points.Count]);
                Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*4;
                int n=mesh.currentVertCount;
                Color tint=Color.Lerp(new Color(.1f,.85f,.95f),new Color(.55f,.35f,1),i/(float)points.Count);
                mesh.AddVert(a-side,tint,Vector2.zero);mesh.AddVert(a+side,tint,Vector2.zero);
                mesh.AddVert(b+side,tint,Vector2.zero);mesh.AddVert(b-side,tint,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
            Vector2 start=Map(points[0]);int index=mesh.currentVertCount;
            mesh.AddVert(start+new Vector2(-7,-7),Color.white,Vector2.zero);
            mesh.AddVert(start+new Vector2(-7,7),Color.white,Vector2.zero);
            mesh.AddVert(start+new Vector2(7,7),Color.white,Vector2.zero);
            mesh.AddVert(start+new Vector2(7,-7),Color.white,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
        }
    }
}
