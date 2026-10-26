using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    internal enum Col
    {
        None,   // visual only
        Auto,   // keep the primitive's own collider
        Box,    // axis-aligned box matching the primitive's bounds
        Mesh    // exact mesh collider (flat discs and similar static shapes)
    }

    /// <summary>Primitive-based building blocks: groups, characters, trees, rocks and flower patches.</summary>
    internal static class Pieces
    {
        public static Transform Group(string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        public static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale,
            Material material, Col collider = Col.None, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            Collider existing = go.GetComponent<Collider>();
            switch (collider)
            {
                case Col.None:
                    Object.DestroyImmediate(existing);
                    break;
                case Col.Box:
                    Object.DestroyImmediate(existing);
                    go.AddComponent<BoxCollider>();
                    break;
                case Col.Mesh:
                    Object.DestroyImmediate(existing);
                    go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                    break;
            }
            return go;
        }

        public static void NoShadows(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static void MakeStatic(GameObject root)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }

        public static void MakeDynamic(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        // ---- Nature --------------------------------------------------------------------------------------------

        public static GameObject Tree(string name, MaterialLibrary m, string leafMain, string leafTop, float trunkHeight) =>
            PilotArt.Tree(name,trunkHeight,m,leafMain,leafTop);

        public static GameObject Rock(MaterialLibrary m)
        {
            GameObject root = new GameObject("Rock");
            Prim(root.transform, PrimitiveType.Sphere, "Boulder", new Vector3(0f, 0.38f, 0f), new Vector3(1.7f, 1.0f, 1.35f), m["Rock"], Col.Box, new Vector3(0f, 20f, 4f));
            Prim(root.transform, PrimitiveType.Sphere, "Chip", new Vector3(0.9f, 0.18f, 0.35f), new Vector3(0.7f, 0.45f, 0.6f), m["Rock"]);
            Prim(root.transform, PrimitiveType.Sphere, "Moss", new Vector3(0.05f, 0.72f, -0.05f), new Vector3(1.15f, 0.34f, 0.92f), m["GrassBladeLight"], Col.None, new Vector3(0f, 20f, 4f));
            root.transform.Find("Boulder").GetComponent<MeshFilter>().sharedMesh=WorldArt.Rock();
            root.transform.Find("Chip").GetComponent<MeshFilter>().sharedMesh=WorldArt.Rock();
            return root;
        }

        public static GameObject FlowerPatch(string name, MaterialLibrary m, string petals)
        {
            var root=PilotArt.Prop("Flowers",null,Vector3.zero,0,1);
            root.name=name;
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                if(r.name.Contains("Petal"))r.sharedMaterial=m[petals];
            return root.gameObject;
        }

    }
}
