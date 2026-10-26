using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>Primitive-built Phase 2 props: gathering nodes (ready and regrowing looks), Clover the hen, her fence and the restoration effects.</summary>
    internal static class GameplayPieces
    {
        // ---- Resource nodes: each prefab has a "Ready" and a "Depleted" child; ResourceNode toggles them ----------------

        public static GameObject HerbNode(MaterialLibrary m)
        {
            GameObject root = new GameObject("Node_Herb");
            Transform ready = Pieces.Group("Ready", root.transform, Vector3.zero);
            Pieces.Prim(ready, PrimitiveType.Sphere, "Core", new Vector3(0f, 0.12f, 0f), Vector3.one * 0.24f, m["HerbDark"]);
            for (int i = 0; i < 6; i++)
            {
                float yaw = i * 60f;
                GameObject leaf = Pieces.Prim(ready, PrimitiveType.Sphere, "Leaf" + i, Vector3.zero, new Vector3(0.13f, 0.48f, 0.26f), m["HerbLeaf"]);
                leaf.transform.localPosition = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.27f, 0.22f);
                leaf.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(30f, 0f, 0f);
            }

            Transform depleted = Pieces.Group("Depleted", root.transform, Vector3.zero);
            Pieces.Prim(depleted, PrimitiveType.Sphere, "SproutA", new Vector3(0.06f, 0.06f, 0.03f), new Vector3(0.14f, 0.12f, 0.14f), m["HerbDark"]);
            Pieces.Prim(depleted, PrimitiveType.Sphere, "SproutB", new Vector3(-0.08f, 0.05f, -0.04f), new Vector3(0.11f, 0.10f, 0.11f), m["HerbDark"]);
            depleted.gameObject.SetActive(false);
            Pieces.NoShadows(root);
            return root;
        }

        public static GameObject FlowerNode(MaterialLibrary m)
        {
            GameObject root = new GameObject("Node_Flower");
            Transform ready = Pieces.Group("Ready", root.transform, Vector3.zero);
            Transform depleted = Pieces.Group("Depleted", root.transform, Vector3.zero);
            Vector2[] spots = { new Vector2(0f, 0f), new Vector2(0.28f, 0.12f), new Vector2(-0.24f, 0.2f), new Vector2(0.06f, -0.28f) };
            float[] heights = { 0.62f, 0.5f, 0.55f, 0.44f };
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 foot = new Vector3(spots[i].x, 0f, spots[i].y);
                Pieces.Prim(ready, PrimitiveType.Cylinder, "Stem" + i, foot + new Vector3(0f, heights[i] * 0.5f, 0f), new Vector3(0.045f, heights[i] * 0.5f, 0.045f), m["Stem"]);
                Pieces.Prim(ready, PrimitiveType.Sphere, "Bloom" + i, foot + new Vector3(0f, heights[i] + 0.05f, 0f), new Vector3(0.34f, 0.2f, 0.34f), m["BlossomCoral"]);
                Pieces.Prim(ready, PrimitiveType.Sphere, "Glow" + i, foot + new Vector3(0f, heights[i] + 0.12f, 0f), Vector3.one * 0.13f, m["BlossomGlow"]);
                Pieces.Prim(depleted, PrimitiveType.Cylinder, "Stub" + i, foot + new Vector3(0f, 0.06f, 0f), new Vector3(0.04f, 0.06f, 0.04f), m["Stem"]);
            }
            depleted.gameObject.SetActive(false);
            Pieces.NoShadows(root);
            return root;
        }

        public static GameObject OreNode(MaterialLibrary m)
        {
            GameObject root = new GameObject("Node_Ore");
            Pieces.Prim(root.transform, PrimitiveType.Sphere, "Base", new Vector3(0f, 0.24f, 0f), new Vector3(0.95f, 0.55f, 0.8f), m["OreRock"], Col.Box, new Vector3(0f, 25f, 0f));
            Transform ready = Pieces.Group("Ready", root.transform, Vector3.zero);
            Transform depleted = Pieces.Group("Depleted", root.transform, Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                float yaw = i * 72f + 15f;
                Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.22f + 0.04f * (i % 2));
                Vector3 tilt = new Vector3(18f + 6f * i, yaw, 8f * (i % 3));
                Pieces.Prim(ready, PrimitiveType.Cube, "Shard" + i, offset + new Vector3(0f, 0.5f, 0f), new Vector3(0.13f, 0.46f - 0.03f * i, 0.13f), m["OreCrystal"], Col.None, tilt);
                Pieces.Prim(depleted, PrimitiveType.Cube, "Stub" + i, offset + new Vector3(0f, 0.46f, 0f), new Vector3(0.1f, 0.1f, 0.1f), m["OreRock"], Col.None, tilt);
            }
            depleted.gameObject.SetActive(false);
            Pieces.NoShadows(ready.gameObject);
            return root;
        }

        // ---- Clover the hen ------------------------------------------------------------------------------------------

        public static GameObject Hen(MaterialLibrary m)
        {
            GameObject root = new GameObject("Hen");
            Pieces.Prim(root.transform, PrimitiveType.Cylinder, "Nest", new Vector3(0f, 0.04f, 0f), new Vector3(1.1f, 0.04f, 1.1f), m["Straw"]);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Pieces.Prim(root.transform, PrimitiveType.Sphere, "NestStraw" + i, new Vector3(Mathf.Cos(angle) * 0.52f, 0.1f, Mathf.Sin(angle) * 0.52f), new Vector3(0.24f, 0.15f, 0.24f), m["Straw"]);
            }

            Transform body = Pieces.Group("Body", root.transform, Vector3.zero);
            Pieces.Prim(body, PrimitiveType.Sphere, "Torso", new Vector3(0f, 0.36f, 0f), new Vector3(0.55f, 0.46f, 0.70f), m["ChickenWhite"]);
            Pieces.Prim(body, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.64f, 0.30f), Vector3.one * 0.27f, m["ChickenWhite"]);
            Pieces.Prim(body, PrimitiveType.Cube, "Comb", new Vector3(0f, 0.80f, 0.30f), new Vector3(0.06f, 0.12f, 0.15f), m["ChickenComb"]);
            Pieces.Prim(body, PrimitiveType.Cube, "Beak", new Vector3(0f, 0.62f, 0.45f), new Vector3(0.09f, 0.06f, 0.12f), m["ChickenBeak"]);
            Pieces.Prim(body, PrimitiveType.Sphere, "Wattle", new Vector3(0f, 0.54f, 0.39f), new Vector3(0.05f, 0.09f, 0.05f), m["ChickenComb"]);
            Pieces.Prim(body, PrimitiveType.Sphere, "EyeL", new Vector3(-0.10f, 0.68f, 0.41f), Vector3.one * 0.045f, m["Eye"]);
            Pieces.Prim(body, PrimitiveType.Sphere, "EyeR", new Vector3(0.10f, 0.68f, 0.41f), Vector3.one * 0.045f, m["Eye"]);
            Pieces.Prim(body, PrimitiveType.Sphere, "Tail", new Vector3(0f, 0.5f, -0.36f), new Vector3(0.30f, 0.36f, 0.22f), m["ChickenWhite"], Col.None, new Vector3(-28f, 0f, 0f));
            Pieces.Prim(body, PrimitiveType.Sphere, "WingL", new Vector3(-0.27f, 0.4f, 0f), new Vector3(0.12f, 0.30f, 0.46f), m["ChickenWhite"]);
            Pieces.Prim(body, PrimitiveType.Sphere, "WingR", new Vector3(0.27f, 0.4f, 0f), new Vector3(0.12f, 0.30f, 0.46f), m["ChickenWhite"]);
            Pieces.Prim(body, PrimitiveType.Cylinder, "LegL", new Vector3(-0.1f, 0.1f, 0.02f), new Vector3(0.035f, 0.1f, 0.035f), m["ChickenBeak"]);
            Pieces.Prim(body, PrimitiveType.Cylinder, "LegR", new Vector3(0.1f, 0.1f, 0.02f), new Vector3(0.035f, 0.1f, 0.035f), m["ChickenBeak"]);

            // State feedback: hungry bubble, laying star, and the egg waiting in the nest.
            GameObject hungry = Pieces.Prim(root.transform, PrimitiveType.Sphere, "HungryIcon", new Vector3(0f, 1.25f, 0f), Vector3.one * 0.22f, m["IconHungry"]);
            Pieces.Prim(hungry.transform, PrimitiveType.Sphere, "Dot", new Vector3(0f, -0.05f, 0.45f), Vector3.one * 0.25f, m["Cream"]);
            GameObject laying = Pieces.Prim(root.transform, PrimitiveType.Cube, "ProducingIcon", new Vector3(0f, 1.25f, 0f), Vector3.one * 0.22f, m["IconStar"], Col.None, new Vector3(45f, 0f, 45f));
            laying.SetActive(false);
            GameObject egg = Pieces.Prim(root.transform, PrimitiveType.Sphere, "Egg", new Vector3(0.50f, 0.17f, 0.20f), new Vector3(0.24f, 0.32f, 0.24f), m["EggShell"]);
            egg.SetActive(false);
            return root;
        }

        /// <summary>Four fence sides around the hen's nest, each with posts, rails and one invisible blocking collider.</summary>
        public static void HenYardFence(Transform parent, Vector3 center, MaterialLibrary m)
        {
            Transform fence = Pieces.Group("HenYardFence", parent, center);
            const float half = 2.3f;
            for (int side = 0; side < 4; side++)
            {
                Transform sideRoot = Pieces.Group("Side" + side, fence, Vector3.zero, side * 90f);
                for (int post = -1; post <= 1; post++)
                    Pieces.Prim(sideRoot, PrimitiveType.Cylinder, "Post" + post, new Vector3(post * half, 0.5f, half), new Vector3(0.14f, 0.5f, 0.14f), m["WoodDark"]);
                Pieces.Prim(sideRoot, PrimitiveType.Cube, "RailLow", new Vector3(0f, 0.38f, half), new Vector3(half * 2f, 0.08f, 0.06f), m["Wood"]);
                Pieces.Prim(sideRoot, PrimitiveType.Cube, "RailHigh", new Vector3(0f, 0.78f, half), new Vector3(half * 2f, 0.08f, 0.06f), m["Wood"]);

                GameObject block = new GameObject("Collider");
                block.transform.SetParent(sideRoot, false);
                block.transform.localPosition = new Vector3(0f, 0.5f, half);
                block.AddComponent<BoxCollider>().size = new Vector3(half * 2f + 0.3f, 1f, 0.3f);
            }
        }

        // ---- Restoration effects (child of the spring) -------------------------------------------------------------------

        /// <summary>A one-shot ring of light that races outward from the spring.</summary>
        public static ParticleSystem Pulse(Transform spring) =>
            RingPulse(spring, "ResonancePulse", new Vector3(0f, 0.35f, 0f), 9f, 1.8f, 0.35f, 120, new Color(1f, 0.90f, 0.55f, 1f));

        /// <summary>
        /// A one-shot ring of light on the ground plane. The ring reaches speed x lifetime metres: about 16 m for the
        /// stream, 18 m for the garden and 58 m (the whole valley) for the final pulse.
        /// </summary>
        public static ParticleSystem RingPulse(Transform parent, string name, Vector3 localPosition, float speed, float lifetime,
            float size, int count, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(200, count + 40);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.radiusThickness = 0f;
            shape.arc = 360f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(SceneKit.FadeInOut(Color.white, 1f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SceneKit.ParticleMaterial();
            return system;
        }

        /// <summary>Droplets leaping from the refilled basin; plays for as long as the valley is awake.</summary>
        public static ParticleSystem Fountain(Transform spring)
        {
            GameObject go = new GameObject("Fountain");
            go.transform.SetParent(spring, false);
            go.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.startColor = new Color(0.75f, 0.92f, 1f, 0.9f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 40f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.25f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SceneKit.ParticleMaterial();
            return system;
        }
    }
}
