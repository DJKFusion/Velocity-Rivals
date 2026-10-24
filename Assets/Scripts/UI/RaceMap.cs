using UnityEngine;
using UnityEngine.UI;

namespace VelocityRivals
{
    /// <summary>A lightweight top-down HUD map; no extra camera or render texture.</summary>
    public sealed class RaceMap : MaskableGraphic
    {
        public GameSession Session;
        private float refreshAt;

        private void Update()
        {
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .1f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!Session || !Session.Player) return;
            Rect bounds = rectTransform.rect;
            Quad(mesh, bounds.center, bounds.size, new Color(.02f, .04f, .08f, .85f));
            var track = Session.Circuit;
            float extent = 60;
            foreach (var point in track) extent = Mathf.Max(extent, Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z)) + 15);
            float scale = Mathf.Min(bounds.width, bounds.height) / (extent * 2);
            Vector2 Map(Vector3 p) => bounds.center + new Vector2(p.x, p.z) * scale;
            Color route = new Color(.3f, .46f, .55f);
            if (Session.IsCircuit)
            {
                for (int i = 0; i < track.Count; i++) Line(mesh, Map(track[i]), Map(track[(i + 1) % track.Count]), 5, route);
                if (Session.NextCheckpoint >= 0)
                    Quad(mesh, Map(track[Session.NextCheckpoint]), Vector2.one * 12, new Color(1, .85f, .15f));
            }
            else
            {
                var a = Map(new Vector3(-45, 0, -50)); var b = Map(new Vector3(45, 0, -50));
                var c = Map(new Vector3(45, 0, 50)); var d = Map(new Vector3(-45, 0, 50));
                Line(mesh, a, b, 3, route); Line(mesh, b, c, 3, route);
                Line(mesh, c, d, 3, route); Line(mesh, d, a, 3, route);
            }
            foreach (var car in Session.Vehicles)
            {
                if (!car || car == Session.Player || !car.enabled) continue;
                Quad(mesh, Map(car.transform.position), Vector2.one * 7, new Color(1, .4f, .2f));
            }
            if (Session.HasArenaCore) Quad(mesh, Map(Session.ArenaCorePosition), Vector2.one * 9, Color.white);
            var player = Session.Player.transform;
            Vector2 centre = Map(player.position);
            Vector2 forward = new Vector2(player.forward.x, player.forward.z).normalized;
            Vector2 side = new Vector2(forward.y, -forward.x);
            int start = mesh.currentVertCount;
            Color cyan = new Color(.1f, 1, 1);
            mesh.AddVert(centre + forward * 10, cyan, Vector2.zero);
            mesh.AddVert(centre - forward * 6 - side * 6, cyan, Vector2.zero);
            mesh.AddVert(centre - forward * 6 + side * 6, cyan, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private static void Quad(VertexHelper mesh, Vector2 centre, Vector2 size, Color color)
        {
            int start = mesh.currentVertCount;
            Vector2 half = size * .5f;
            mesh.AddVert(centre + new Vector2(-half.x, -half.y), color, Vector2.zero);
            mesh.AddVert(centre + new Vector2(-half.x, half.y), color, Vector2.zero);
            mesh.AddVert(centre + new Vector2(half.x, half.y), color, Vector2.zero);
            mesh.AddVert(centre + new Vector2(half.x, -half.y), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }

        private static void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x) * width * .5f;
            int start = mesh.currentVertCount;
            mesh.AddVert(from - side, color, Vector2.zero); mesh.AddVert(from + side, color, Vector2.zero);
            mesh.AddVert(to + side, color, Vector2.zero); mesh.AddVert(to - side, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
