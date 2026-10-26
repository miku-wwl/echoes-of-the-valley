using UnityEngine;

namespace Echoes.EditorTools
{
    internal struct HeartParts
    {
        public Transform Root;
        public Transform CrystalPivot;
        public Renderer Crystal;
    }

    internal struct CottageParts
    {
        public Transform Root;
        public Transform InteractionPoint;
    }

    internal struct SpringParts
    {
        public Transform Root;
        public Renderer Water;
    }

    /// <summary>The three hand-composed set pieces: the ruined Heart of the Valley, the dry spring and the workshop cottage.</summary>
    internal static class Landmarks
    {
        // ---- Heart of the Valley -------------------------------------------------------------------------------

        public static HeartParts Heart(Transform parent, Vector3 position, MaterialLibrary m)
        {
            Transform root = Pieces.Group("HeartOfTheValley", parent, position);

            Pieces.Prim(root, PrimitiveType.Cylinder, "DaisOuter", new Vector3(0f, 0.1f, 0f), new Vector3(10.4f, 0.1f, 10.4f), m["Stone"], Col.Mesh);
            Pieces.Prim(root, PrimitiveType.Cylinder, "DaisInner", new Vector3(0f, 0.2f, 0f), new Vector3(6.8f, 0.1f, 6.8f), m["StoneDark"], Col.Mesh);
            Pieces.Prim(root, PrimitiveType.Cylinder, "Moss", new Vector3(0.9f, 0.305f, -0.6f), new Vector3(2.2f, 0.004f, 1.6f), m["StoneMoss"]);

            // Broken obelisk with a fallen top block
            Pieces.Prim(root, PrimitiveType.Cube, "ObeliskBase", new Vector3(0f, 0.5f, 0f), new Vector3(1.5f, 0.4f, 1.5f), m["StoneDark"], Col.Box);
            Pieces.Prim(root, PrimitiveType.Cube, "ObeliskShaft", new Vector3(0f, 1.9f, 0f), new Vector3(0.95f, 2.4f, 0.95f), m["Stone"], Col.Box, new Vector3(0f, 12f, 3f));
            Pieces.Prim(root, PrimitiveType.Cube, "ObeliskBreak", new Vector3(0.08f, 3.25f, 0f), new Vector3(0.95f, 0.5f, 0.95f), m["Stone"], Col.None, new Vector3(14f, 40f, 10f));
            Pieces.Prim(root, PrimitiveType.Cube, "FallenBlock", new Vector3(1.7f, 0.65f, 0.9f), new Vector3(0.95f, 0.7f, 0.95f), m["Stone"], Col.Box, new Vector3(0f, 25f, 10f));

            // The dormant crystal floats above the obelisk; CompletionFeedback wakes it up.
            Transform pivot = Pieces.Group("CrystalPivot", root, new Vector3(0f, 4.2f, 0f));
            GameObject crystal = Pieces.Prim(pivot, PrimitiveType.Cube, "Crystal", Vector3.zero, Vector3.one * 0.62f, m["Crystal"], Col.None, new Vector3(45f, 0f, 45f));

            // Ring of pillars: tall, short and snapped
            float[] heights = { 2.9f, 1.7f, 3.3f, 0.9f, 2.4f, 1.3f };
            for (int i = 0; i < heights.Length; i++)
            {
                float angle = (i * 60f + 15f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 4.3f, 0.2f + heights[i] * 0.5f, Mathf.Sin(angle) * 4.3f);
                string material = i % 2 == 0 ? "Stone" : "StoneMoss";
                Pieces.Prim(root, PrimitiveType.Cylinder, "Pillar" + i, p, new Vector3(0.8f, heights[i] * 0.5f, 0.8f), m[material], Col.Auto,
                    new Vector3(i % 3 == 0 ? 3f : 0f, 0f, i % 3 == 1 ? -4f : 0f));
            }

            // Fallen drums and rubble
            Pieces.Prim(root, PrimitiveType.Cylinder, "FallenDrumA", new Vector3(-3.4f, 0.55f, 2.9f), new Vector3(0.8f, 0.6f, 0.8f), m["Stone"], Col.Box, new Vector3(0f, 35f, 90f));
            Pieces.Prim(root, PrimitiveType.Cylinder, "FallenDrumB", new Vector3(3.8f, 0.55f, -2.2f), new Vector3(0.75f, 0.5f, 0.75f), m["StoneMoss"], Col.Box, new Vector3(0f, -20f, 90f));
            Vector3[] rubble = { new Vector3(-2.2f, 0.4f, -2.6f), new Vector3(2.6f, 0.4f, 2.4f), new Vector3(-1.2f, 0.4f, 3.0f), new Vector3(3.0f, 0.4f, 0.8f), new Vector3(-3.0f, 0.4f, -0.4f) };
            for (int i = 0; i < rubble.Length; i++)
                Pieces.Prim(root, PrimitiveType.Cube, "Rubble" + i, rubble[i], new Vector3(0.35f + 0.1f * (i % 3), 0.2f, 0.3f), m["StoneDark"], Col.None, new Vector3(0f, 30f * i, 8f * i));

            return new HeartParts { Root = root, CrystalPivot = pivot, Crystal = crystal.GetComponent<Renderer>() };
        }

        // ---- Ruined spring -------------------------------------------------------------------------------------

        public static SpringParts Spring(Transform parent, Vector3 position, float yaw, MaterialLibrary m)
        {
            Transform root = Pieces.Group("RuinedSpring", parent, position, yaw);

            Pieces.Prim(root, PrimitiveType.Cylinder, "Bed", new Vector3(0f, 0.03f, 0f), new Vector3(2.8f, 0.03f, 2.8f), m["Mud"], Col.Mesh);
            // The puddle in the basin: murky and tiny while the valley is silent; ResonanceView fills it when the stream is restored.
            GameObject water = Pieces.Prim(root, PrimitiveType.Cylinder, "SpringWater", new Vector3(0f, 0.065f, 0f), new Vector3(0.9f, 0.006f, 0.9f), m["WaterDull"]);

            // Ring of 10 blocks; one gap faces the stream (+X after yaw), some blocks have collapsed or tilted.
            for (int i = 0; i < 10; i++)
            {
                if (i == 7) continue;
                float angleDeg = i * 36f;
                float angle = angleDeg * Mathf.Deg2Rad;
                bool collapsed = i % 3 == 0;
                float height = collapsed ? 0.28f : 0.6f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 1.65f, height * 0.5f, Mathf.Sin(angle) * 1.65f);
                Pieces.Prim(root, PrimitiveType.Cube, "Block" + i, p, new Vector3(1.05f, height, 0.55f), m[i % 2 == 0 ? "Stone" : "StoneMoss"],
                    Col.Box, new Vector3(i % 4 == 1 ? 6f : 0f, -(angleDeg + 90f), i % 4 == 2 ? -5f : 0f));
            }

            // Broken arch behind the basin
            Pieces.Prim(root, PrimitiveType.Cylinder, "ArchPostL", new Vector3(-1.6f, 0.9f, -2.5f), new Vector3(0.55f, 0.9f, 0.55f), m["Stone"], Col.Auto);
            Pieces.Prim(root, PrimitiveType.Cylinder, "ArchPostR", new Vector3(1.6f, 0.55f, -2.5f), new Vector3(0.55f, 0.55f, 0.55f), m["StoneMoss"], Col.Auto);
            Pieces.Prim(root, PrimitiveType.Cube, "FallenLintel", new Vector3(1.9f, 0.22f, -3.3f), new Vector3(2.4f, 0.4f, 0.6f), m["Stone"], Col.Box, new Vector3(0f, 18f, 4f));
            return new SpringParts { Root = root, Water = water.GetComponent<Renderer>() };
        }

        /// <summary>A thin curl of soft white smoke that rises from the chimney, drifts with the wind and fades.</summary>
        static void ChimneySmoke(Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject("ChimneySmoke");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            main.startColor = new Color(1f, 0.98f, 0.95f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 3f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = 0.14f;
            ParticleSystem.VelocityOverLifetimeModule drift = system.velocityOverLifetime;
            drift.enabled = true;
            drift.space = ParticleSystemSimulationSpace.World;
            // x, y and z must all use the same curve mode or Unity logs an error every time the system starts
            drift.x = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            drift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            drift.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            ParticleSystem.SizeOverLifetimeModule grow = system.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 1.8f)));
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(SceneKit.FadeInOut(Color.white, 0.6f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SceneKit.ParticleMaterial();
        }

        /// <summary>Gives a window a StageGlow: dim in the silent valley, warm and bright once it is restored (or always lit at dusk on the title screen).</summary>
        public static void WindowGlow(GameObject window, bool constant)
        {
            StageGlow glow = window.AddComponent<StageGlow>();
            Wire.Set(glow, "target", window.GetComponent<Renderer>());
            Wire.Set(glow, "color", new Color(1f, 0.78f, 0.38f));
            Wire.Set(glow, "phase", window.name.Length * 1.7f);
            if (constant)
            {
                Wire.Set(glow, "constant", true);
                Wire.Set(glow, "constantIntensity", 1.2f);
            }
        }

        // ---- Workshop cottage ----------------------------------------------------------------------------------

        /// <summary>Gable-front cottage: the door and workshop yard are on local +Z.</summary>
        public static CottageParts Cottage(Transform parent, Vector3 position, float yaw, MaterialLibrary m, bool dusk = false)
        {
            Transform root = Pieces.Group("WorkshopCottage", parent, position, yaw);
            const float width = 5f, depth = 4f, wall = 2.4f, floor = 0.2f;
            float diamond = width / Mathf.Sqrt(2f);
            float eave = floor + wall;

            Pieces.Prim(root, PrimitiveType.Cube, "Foundation", new Vector3(0f, floor * 0.5f, 0f), new Vector3(width + 0.5f, floor, depth + 0.5f), m["Stone"], Col.Box);
            Pieces.Prim(root, PrimitiveType.Cube, "Walls", new Vector3(0f, floor + wall * 0.5f, 0f), new Vector3(width, wall, depth), m["Plaster"], Col.Box);
            Pieces.Prim(root, PrimitiveType.Cube, "GableCore", new Vector3(0f, eave, 0f), new Vector3(diamond, diamond, depth - 0.02f), m["Plaster"], Col.None, new Vector3(0f, 0f, 45f));

            // Two roof slabs lying on the gable's upper faces, plus a ridge cap
            float slabLength = diamond + 0.5f;
            Pieces.Prim(root, PrimitiveType.Cube, "RoofRight", new Vector3(width * 0.26f, eave + width * 0.26f, 0f), new Vector3(slabLength, 0.14f, depth + 0.7f), m["Roof"], Col.None, new Vector3(0f, 0f, -45f));
            Pieces.Prim(root, PrimitiveType.Cube, "RoofLeft", new Vector3(-width * 0.26f, eave + width * 0.26f, 0f), new Vector3(slabLength, 0.14f, depth + 0.7f), m["Roof"], Col.None, new Vector3(0f, 0f, 45f));
            Pieces.Prim(root, PrimitiveType.Cube, "RidgeCap", new Vector3(0f, eave + width * 0.5f + 0.06f, 0f), new Vector3(0.3f, 0.3f, depth + 0.75f), m["WoodDark"], Col.None, new Vector3(0f, 0f, 45f));
            Pieces.Prim(root, PrimitiveType.Cube, "Chimney", new Vector3(1.35f, eave + 1.55f, -0.9f), new Vector3(0.6f, 1.7f, 0.6f), m["Stone"], Col.None);
            ChimneySmoke(root, new Vector3(1.35f, eave + 2.5f, -0.9f));

            // Front: door, step, windows, round attic window
            float front = depth * 0.5f;
            Pieces.Prim(root, PrimitiveType.Cube, "Step", new Vector3(0f, floor + 0.03f, front + 0.45f), new Vector3(1.7f, 0.1f, 0.8f), m["Stone"], Col.Box);
            Pieces.Prim(root, PrimitiveType.Cube, "DoorFrame", new Vector3(0f, floor + 0.95f, front + 0.02f), new Vector3(1.25f, 1.95f, 0.1f), m["WoodDark"]);
            Pieces.Prim(root, PrimitiveType.Cube, "Door", new Vector3(0f, floor + 0.9f, front + 0.06f), new Vector3(1.0f, 1.8f, 0.1f), m["Wood"]);
            for (int side = -1; side <= 1; side += 2)
            {
                Pieces.Prim(root, PrimitiveType.Cube, "FrontFrame" + side, new Vector3(side * 1.65f, 1.6f, front + 0.02f), new Vector3(0.95f, 0.95f, 0.08f), m["WoodDark"]);
                WindowGlow(Pieces.Prim(root, PrimitiveType.Cube, "FrontWindow" + side, new Vector3(side * 1.65f, 1.6f, front + 0.06f), new Vector3(0.78f, 0.78f, 0.08f), m["Window"]), dusk);
                Pieces.Prim(root, PrimitiveType.Cube, "SideFrame" + side, new Vector3(side * (width * 0.5f + 0.02f), 1.6f, 0f), new Vector3(0.08f, 0.95f, 0.95f), m["WoodDark"]);
                WindowGlow(Pieces.Prim(root, PrimitiveType.Cube, "SideWindow" + side, new Vector3(side * (width * 0.5f + 0.06f), 1.6f, 0f), new Vector3(0.08f, 0.78f, 0.78f), m["Window"]), dusk);
                Pieces.Prim(root, PrimitiveType.Cube, "FlowerBox" + side, new Vector3(side * 1.65f, 0.95f, front + 0.18f), new Vector3(1.0f, 0.2f, 0.3f), m["Wood"]);
                Pieces.Prim(root, PrimitiveType.Sphere, "BoxBloomA" + side, new Vector3(side * 1.65f - 0.25f, 1.12f, front + 0.18f), Vector3.one * 0.22f, m[side < 0 ? "FlowerPink" : "FlowerYellow"]);
                Pieces.Prim(root, PrimitiveType.Sphere, "BoxBloomB" + side, new Vector3(side * 1.65f + 0.25f, 1.12f, front + 0.18f), Vector3.one * 0.22f, m[side < 0 ? "FlowerWhite" : "FlowerLilac"]);
            }
            WindowGlow(Pieces.Prim(root, PrimitiveType.Cylinder, "AtticWindow", new Vector3(0f, eave + 1.0f, front + 0.02f), new Vector3(0.6f, 0.04f, 0.6f), m["Window"], Col.None, new Vector3(90f, 0f, 0f)), dusk);

            // Workshop yard: cauldron, table with bottles, herb crates, barrels and a sign
            Transform yard = Pieces.Group("Yard", root, new Vector3(0f, 0f, front + 1.4f));
            Pieces.Prim(yard, PrimitiveType.Sphere, "Cauldron", new Vector3(-2.2f, 0.5f, 0.3f), new Vector3(0.95f, 0.75f, 0.95f), m["Iron"], Col.Box);
            Pieces.Prim(yard, PrimitiveType.Cylinder, "CauldronBrew", new Vector3(-2.2f, 0.84f, 0.3f), new Vector3(0.62f, 0.012f, 0.62f), m["PotionGreen"]);
            Pieces.Prim(yard, PrimitiveType.Cylinder, "CauldronLogA", new Vector3(-2.2f, 0.06f, -0.15f), new Vector3(0.12f, 0.45f, 0.12f), m["WoodDark"], Col.None, new Vector3(0f, 0f, 90f));
            Pieces.Prim(yard, PrimitiveType.Cylinder, "CauldronLogB", new Vector3(-2.2f, 0.06f, 0.75f), new Vector3(0.12f, 0.45f, 0.12f), m["WoodDark"], Col.None, new Vector3(0f, 0f, 90f));

            Pieces.Prim(yard, PrimitiveType.Cube, "TableTop", new Vector3(2.1f, 0.85f, 0.2f), new Vector3(1.7f, 0.1f, 0.8f), m["Wood"], Col.Box);
            for (int lx = -1; lx <= 1; lx += 2)
                for (int lz = -1; lz <= 1; lz += 2)
                    Pieces.Prim(yard, PrimitiveType.Cylinder, "TableLeg", new Vector3(2.1f + lx * 0.72f, 0.4f, 0.2f + lz * 0.3f), new Vector3(0.09f, 0.4f, 0.09f), m["WoodDark"]);
            string[] potions = { "PotionGreen", "PotionPurple", "PotionBlue" };
            for (int i = 0; i < potions.Length; i++)
            {
                Vector3 p = new Vector3(1.65f + i * 0.45f, 1.0f, 0.2f);
                Pieces.Prim(yard, PrimitiveType.Sphere, "Bottle" + i, p, new Vector3(0.22f, 0.22f, 0.22f), m[potions[i]]);
                Pieces.Prim(yard, PrimitiveType.Cylinder, "Neck" + i, p + new Vector3(0f, 0.17f, 0f), new Vector3(0.07f, 0.09f, 0.07f), m[potions[i]]);
            }

            Pieces.Prim(yard, PrimitiveType.Cylinder, "BarrelA", new Vector3(3.6f, 0.45f, -2.4f), new Vector3(0.75f, 0.45f, 0.75f), m["Wood"], Col.Auto);
            Pieces.Prim(yard, PrimitiveType.Cylinder, "BarrelB", new Vector3(4.4f, 0.45f, -2.1f), new Vector3(0.7f, 0.45f, 0.7f), m["WoodDark"], Col.Auto);
            Pieces.Prim(yard, PrimitiveType.Cube, "HerbCrate", new Vector3(-3.4f, 0.25f, -1.8f), new Vector3(0.9f, 0.5f, 0.6f), m["Wood"], Col.Box);
            Pieces.Prim(yard, PrimitiveType.Sphere, "Herbs", new Vector3(-3.4f, 0.55f, -1.8f), new Vector3(0.8f, 0.3f, 0.5f), m["LeafB"]);

            Pieces.Prim(yard, PrimitiveType.Cylinder, "SignPost", new Vector3(-0.4f, 0.8f, 1.8f), new Vector3(0.09f, 0.8f, 0.09f), m["WoodDark"], Col.Auto);
            Pieces.Prim(yard, PrimitiveType.Cube, "SignBoard", new Vector3(-0.4f, 1.45f, 1.8f), new Vector3(0.95f, 0.55f, 0.07f), m["Straw"]);
            Pieces.Prim(yard, PrimitiveType.Sphere, "SignFlask", new Vector3(-0.4f, 1.4f, 1.85f), new Vector3(0.2f, 0.2f, 0.06f), m["PotionBlue"]);
            Pieces.Prim(yard, PrimitiveType.Cylinder, "SignFlaskNeck", new Vector3(-0.4f, 1.55f, 1.85f), new Vector3(0.07f, 0.07f, 0.05f), m["PotionBlue"], Col.None, new Vector3(90f, 0f, 0f));

            Transform point = Pieces.Group("WorkshopInteraction", root, new Vector3(0f, 0f, front + 1.5f));
            WorldArt.Cottage(root,m);
            return new CottageParts { Root = root, InteractionPoint = point };
        }
    }
}
