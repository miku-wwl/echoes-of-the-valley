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

        // ---- Characters ----------------------------------------------------------------------------------------

        /// <summary>Chibi-style placeholder character, about 1.5 m tall, pivot at the feet, facing +Z.</summary>
        public static GameObject Character(string name, MaterialLibrary m, string tunic, string hair, string accent,
            bool hat, bool longHair, bool skirt, bool satchel)
        {
            GameObject root = new GameObject(name);
            Transform body = Group("Body", root.transform, Vector3.zero);

            Prim(body, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.62f, 0f), new Vector3(0.54f, 0.40f, 0.46f), m[tunic]);
            Prim(body, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.22f, 0f), Vector3.one * 0.46f, m["Skin"]);
            Prim(body, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.28f, -0.04f), new Vector3(0.50f, 0.46f, 0.50f), m[hair]);
            Prim(body, PrimitiveType.Sphere, "HairBack", new Vector3(0f, longHair ? 1.0f : 1.12f, -0.17f),
                longHair ? new Vector3(0.40f, 0.78f, 0.28f) : new Vector3(0.40f, 0.46f, 0.28f), m[hair]);
            Prim(body, PrimitiveType.Cylinder, "Scarf", new Vector3(0f, 0.98f, 0f), new Vector3(0.36f, 0.03f, 0.36f), m[accent]);
            Prim(body, PrimitiveType.Sphere, "EyeL", new Vector3(-0.085f, 1.22f, 0.205f), new Vector3(0.055f, 0.075f, 0.04f), m["Eye"]);
            Prim(body, PrimitiveType.Sphere, "EyeR", new Vector3(0.085f, 1.22f, 0.205f), new Vector3(0.055f, 0.075f, 0.04f), m["Eye"]);
            Prim(body, PrimitiveType.Sphere, "CheekL", new Vector3(-0.14f, 1.15f, 0.17f), new Vector3(0.07f, 0.04f, 0.03f), m["Blush"]);
            Prim(body, PrimitiveType.Sphere, "CheekR", new Vector3(0.14f, 1.15f, 0.17f), new Vector3(0.07f, 0.04f, 0.03f), m["Blush"]);

            if (skirt)
                Prim(body, PrimitiveType.Cylinder, "Skirt", new Vector3(0f, 0.34f, 0f), new Vector3(0.60f, 0.20f, 0.60f), m[tunic]);
            if (satchel)
                Prim(body, PrimitiveType.Cube, "Satchel", new Vector3(0f, 0.64f, -0.27f), new Vector3(0.30f, 0.34f, 0.14f), m["Wood"]);
            if (hat)
            {
                Prim(body, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.47f, 0f), new Vector3(0.66f, 0.012f, 0.66f), m["Straw"]);
                Prim(body, PrimitiveType.Sphere, "HatDome", new Vector3(0f, 1.50f, 0f), new Vector3(0.36f, 0.24f, 0.36f), m["Straw"]);
                Prim(body, PrimitiveType.Cylinder, "HatBand", new Vector3(0f, 1.49f, 0f), new Vector3(0.37f, 0.02f, 0.37f), m[accent]);
            }

            Transform left = Prim(root.transform, PrimitiveType.Sphere, "FootL", new Vector3(-0.13f, 0.08f, 0.04f), new Vector3(0.17f, 0.13f, 0.28f), m["Boots"]).transform;
            Transform right = Prim(root.transform, PrimitiveType.Sphere, "FootR", new Vector3(0.13f, 0.08f, 0.04f), new Vector3(0.17f, 0.13f, 0.28f), m["Boots"]).transform;

            CharacterAnimator animator = root.AddComponent<CharacterAnimator>();
            Wire.Set(animator, "body", body);
            Wire.Set(animator, "leftFoot", left);
            Wire.Set(animator, "rightFoot", right);
            return root;
        }

        // ---- Nature --------------------------------------------------------------------------------------------

        public static GameObject Tree(string name, MaterialLibrary m, string leafMain, string leafTop, float trunkHeight)
        {
            GameObject root = new GameObject(name);
            Transform t = root.transform;
            float h = trunkHeight;
            Prim(t, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, h * 0.5f, 0f), new Vector3(0.34f, h * 0.5f, 0.34f), m["Trunk"], Col.Auto);
            Prim(t, PrimitiveType.Sphere, "Leaves", new Vector3(0f, h + 0.7f, 0f), new Vector3(2.6f, 2.1f, 2.6f), m[leafMain]);
            Prim(t, PrimitiveType.Sphere, "LeavesTop", new Vector3(0.15f, h + 1.75f, -0.1f), new Vector3(1.7f, 1.5f, 1.7f), m[leafTop]);
            Prim(t, PrimitiveType.Sphere, "LeavesSide", new Vector3(-0.9f, h + 0.3f, 0.55f), new Vector3(1.5f, 1.3f, 1.5f), m[leafTop]);
            return root;
        }

        public static GameObject Rock(MaterialLibrary m)
        {
            GameObject root = new GameObject("Rock");
            Prim(root.transform, PrimitiveType.Sphere, "Boulder", new Vector3(0f, 0.38f, 0f), new Vector3(1.7f, 1.0f, 1.35f), m["Rock"], Col.Box, new Vector3(0f, 20f, 4f));
            Prim(root.transform, PrimitiveType.Sphere, "Chip", new Vector3(0.9f, 0.18f, 0.35f), new Vector3(0.7f, 0.45f, 0.6f), m["Rock"]);
            return root;
        }

        public static GameObject FlowerPatch(string name, MaterialLibrary m, string petals)
        {
            GameObject root = new GameObject(name);
            Vector2[] spots =
            {
                new Vector2(0.0f, 0.0f), new Vector2(0.45f, 0.20f), new Vector2(-0.40f, 0.30f),
                new Vector2(0.20f, -0.45f), new Vector2(-0.30f, -0.35f), new Vector2(0.65f, -0.20f)
            };
            for (int i = 0; i < spots.Length; i++)
            {
                float stem = 0.20f + 0.05f * (i % 3);
                Vector3 p = new Vector3(spots[i].x, 0f, spots[i].y);
                Prim(root.transform, PrimitiveType.Cylinder, "Stem" + i, p + new Vector3(0f, stem * 0.5f, 0f), new Vector3(0.035f, stem * 0.5f, 0.035f), m["Stem"]);
                Prim(root.transform, PrimitiveType.Sphere, "Bloom" + i, p + new Vector3(0f, stem + 0.04f, 0f), new Vector3(0.17f, 0.11f, 0.17f), m[petals]);
            }
            NoShadows(root);
            return root;
        }
    }
}
