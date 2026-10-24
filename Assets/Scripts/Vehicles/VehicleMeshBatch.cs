using System.Collections.Generic;
using UnityEngine;

namespace VelocityRivals
{
    /// <summary>Combine visual parts per material, retaining independent rotating wheel pivots.</summary>
    public sealed class VehicleMeshBatch : MonoBehaviour
    {
        readonly List<Mesh> owned = new List<Mesh>();
        public static void Bake(Transform root, List<Transform> wheels)
        {
            if (!Application.isPlaying) return; // Editor prefab generation retains import references.
            var owner = root.gameObject.AddComponent<VehicleMeshBatch>();
            var groups = new Dictionary<Transform, Dictionary<Material, List<CombineInstance>>>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                Transform pivot = root;
                foreach (var wheel in wheels) if (filter.transform.IsChildOf(wheel)) { pivot = wheel; break; }
                if (!groups.TryGetValue(pivot, out var materials)) { materials = new Dictionary<Material,List<CombineInstance>>(); groups.Add(pivot,materials); }
                var slots = renderer.sharedMaterials;
                if (slots.Length != filter.sharedMesh.subMeshCount) continue;
                bool valid = true;
                foreach (var material in slots) if (material == null) valid = false;
                if (!valid) continue;
                for (int i=0; i<slots.Length; i++)
                {
                    if (!materials.TryGetValue(slots[i],out var list)) { list=new List<CombineInstance>();materials.Add(slots[i],list); }
                    list.Add(new CombineInstance { mesh=filter.sharedMesh, subMeshIndex=i, transform=pivot.worldToLocalMatrix*filter.transform.localToWorldMatrix });
                }
                renderer.enabled=false;
                // Combat hides/restores renderers; remove originals so respawn cannot show both versions.
                Destroy(renderer); Destroy(filter);
            }
            foreach(var pivot in groups) foreach(var group in pivot.Value)
            {
                var mesh=new Mesh { name="Vehicle surface batch", indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(),true,true);owner.owned.Add(mesh);
                var go=new GameObject("Batched "+group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));
                go.layer=root.gameObject.layer;go.transform.SetParent(pivot.Key,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
        }
        void OnDestroy() { foreach(var mesh in owned) if(mesh!=null) Destroy(mesh); }
    }
}
