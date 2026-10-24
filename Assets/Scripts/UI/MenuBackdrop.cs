using UnityEngine;
using UnityEngine.UI;

namespace VelocityRivals
{
    /// <summary>Resolution-independent graphic; no external image or shader dependency.</summary>
    public sealed class MenuBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect r = rectTransform.rect;
            int n = mesh.currentVertCount;
            mesh.AddVert(new Vector2(r.xMin,r.yMin),new Color(.035f,.02f,.08f),Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin,r.yMax),new Color(.018f,.04f,.07f),Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax,r.yMax),new Color(.02f,.09f,.12f),Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax,r.yMin),new Color(.02f,.025f,.06f),Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            for (int i=-2;i<16;i++)
            {
                float x=r.xMin+i*r.width/12;
                n=mesh.currentVertCount; Color c=new Color(.15f,.8f,1,.035f);
                mesh.AddVert(new Vector2(x,r.yMin),c,Vector2.zero);
                mesh.AddVert(new Vector2(x+r.width*.32f,r.yMax),c,Vector2.zero);
                mesh.AddVert(new Vector2(x+r.width*.32f+2,r.yMax),c,Vector2.zero);
                mesh.AddVert(new Vector2(x+2,r.yMin),c,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
