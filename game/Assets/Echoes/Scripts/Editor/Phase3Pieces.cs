using UnityEngine;

namespace Echoes.EditorTools
{
    internal struct GardenParts
    {
        public Transform Root;
        public GameObject Dormant;   // withered stalks, dry soil, bare vines: shown until stage 2
        public GameObject Bloom;     // flowers, bushes, rich soil: popped up one by one when the garden wakes
    }

    internal struct WindmillParts
    {
        public Transform Root;
        public Transform SailsPivot;   // WindmillRotator turns this
        public GameObject[] Damaged;   // broken sails and fallen beams: shown until stage 3
        public GameObject[] Repaired;  // whole sails and glowing ore braces
    }

    /// <summary>Primitive-built Phase 3 props: the village garden, the old windmill and the hen yard's decorations.</summary>
    internal static class Phase3Pieces
    {
        const float GardenHalfX = 4f;
        const float GardenHalfZ = 3f;
        const float SailLength = 5.2f;

        static readonly string[] Petals = { "FlowerPink", "FlowerYellow", "FlowerOrange", "FlowerLilac", "FlowerRed", "FlowerWhite", "FlowerBlue" };

        // ---- Garden ------------------------------------------------------------------------------------------------------

        /// <summary>
        /// An 8 x 6 m fenced garden with a gate on its west side and four raised beds. The frames, the fence, the arch and the
        /// scarecrow always stand; everything that changes lives under the Dormant and Bloom roots, one of which is shown.
        /// </summary>
        public static GardenParts Garden(Transform parent, Vector3 position, MaterialLibrary m)
        {
            Transform root = Pieces.Group("Garden", parent, position);
            System.Random random = new System.Random(303);

            // Fence with a gate gap on the west side (the side the path arrives from)
            Transform fence = Pieces.Group("GardenFence", root, Vector3.zero);
            FenceRun(fence, new Vector3(-GardenHalfX, 0f, GardenHalfZ), new Vector3(GardenHalfX, 0f, GardenHalfZ), m);
            FenceRun(fence, new Vector3(-GardenHalfX, 0f, -GardenHalfZ), new Vector3(GardenHalfX, 0f, -GardenHalfZ), m);
            FenceRun(fence, new Vector3(GardenHalfX, 0f, -GardenHalfZ), new Vector3(GardenHalfX, 0f, GardenHalfZ), m);
            FenceRun(fence, new Vector3(-GardenHalfX, 0f, -GardenHalfZ), new Vector3(-GardenHalfX, 0f, -1f), m);
            FenceRun(fence, new Vector3(-GardenHalfX, 0f, 1f), new Vector3(-GardenHalfX, 0f, GardenHalfZ), m);

            // The arch over the gate
            Transform arch = Pieces.Group("GateArch", root, new Vector3(-GardenHalfX, 0f, 0f));
            Pieces.Prim(arch, PrimitiveType.Cylinder, "ArchPostL", new Vector3(0f, 1.1f, -1.15f), new Vector3(0.11f, 1.1f, 0.11f), m["WoodDark"]);
            Pieces.Prim(arch, PrimitiveType.Cylinder, "ArchPostR", new Vector3(0f, 1.1f, 1.15f), new Vector3(0.11f, 1.1f, 0.11f), m["WoodDark"]);
            Pieces.Prim(arch, PrimitiveType.Cube, "ArchBeam", new Vector3(0f, 2.25f, 0f), new Vector3(0.12f, 0.12f, 2.5f), m["Wood"]);

            // A scarecrow where the paths cross
            Transform scarecrow = Pieces.Group("Scarecrow", root, Vector3.zero);
            Pieces.Prim(scarecrow, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.95f, 0f), new Vector3(0.05f, 0.95f, 0.05f), m["WoodDark"]);
            Pieces.Prim(scarecrow, PrimitiveType.Cube, "Arms", new Vector3(0f, 1.38f, 0f), new Vector3(1.4f, 0.08f, 0.08f), m["Wood"]);
            Pieces.Prim(scarecrow, PrimitiveType.Cylinder, "Coat", new Vector3(0f, 1.15f, 0f), new Vector3(0.46f, 0.36f, 0.3f), m["TunicHero"]);
            Pieces.Prim(scarecrow, PrimitiveType.Cylinder, "Sleeves", new Vector3(0f, 1.38f, 0f), new Vector3(0.18f, 0.5f, 0.18f), m["TunicHero"], Col.None, new Vector3(0f, 0f, 90f));
            Pieces.Prim(scarecrow, PrimitiveType.Sphere, "StrawL", new Vector3(-0.78f, 1.38f, 0f), new Vector3(0.18f, 0.12f, 0.12f), m["Straw"]);
            Pieces.Prim(scarecrow, PrimitiveType.Sphere, "StrawR", new Vector3(0.78f, 1.38f, 0f), new Vector3(0.18f, 0.12f, 0.12f), m["Straw"]);
            Pieces.Prim(scarecrow, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.82f, 0f), Vector3.one * 0.34f, m["Straw"]);
            Pieces.Prim(scarecrow, PrimitiveType.Sphere, "EyeL", new Vector3(-0.07f, 1.85f, 0.15f), Vector3.one * 0.045f, m["Eye"]);
            Pieces.Prim(scarecrow, PrimitiveType.Sphere, "EyeR", new Vector3(0.07f, 1.85f, 0.15f), Vector3.one * 0.045f, m["Eye"]);
            Pieces.Prim(scarecrow, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.99f, 0f), new Vector3(0.66f, 0.012f, 0.66f), m["Roof"]);
            Pieces.Prim(scarecrow, PrimitiveType.Sphere, "HatDome", new Vector3(0f, 2.04f, 0f), new Vector3(0.34f, 0.22f, 0.34f), m["Roof"]);

            GameObject dormant = new GameObject("Dormant");
            dormant.transform.SetParent(root, false);
            GameObject bloom = new GameObject("Bloom");
            bloom.transform.SetParent(root, false);

            Vector2[] beds = { new Vector2(-2f, -1.5f), new Vector2(2f, -1.5f), new Vector2(-2f, 1.5f), new Vector2(2f, 1.5f) };
            for (int i = 0; i < beds.Length; i++)
            {
                Vector3 center = new Vector3(beds[i].x, 0f, beds[i].y);

                // Always: the wooden frame of the bed
                Transform frame = Pieces.Group("BedFrame" + i, root, center);
                Pieces.Prim(frame, PrimitiveType.Cube, "North", new Vector3(0f, 0.14f, 0.85f), new Vector3(3.0f, 0.28f, 0.1f), m["WoodDark"]);
                Pieces.Prim(frame, PrimitiveType.Cube, "South", new Vector3(0f, 0.14f, -0.85f), new Vector3(3.0f, 0.28f, 0.1f), m["WoodDark"]);
                Pieces.Prim(frame, PrimitiveType.Cube, "East", new Vector3(1.45f, 0.14f, 0f), new Vector3(0.1f, 0.28f, 1.8f), m["WoodDark"]);
                Pieces.Prim(frame, PrimitiveType.Cube, "West", new Vector3(-1.45f, 0.14f, 0f), new Vector3(0.1f, 0.28f, 1.8f), m["WoodDark"]);

                // Dormant: pale cracked soil and drooping stalks
                Pieces.Prim(dormant.transform, PrimitiveType.Cube, "DrySoil" + i, center + new Vector3(0f, 0.14f, 0f), new Vector3(2.8f, 0.2f, 1.6f), m["SoilDry"]);
                for (int k = 0; k < 7; k++)
                {
                    float height = 0.25f + (float)random.NextDouble() * 0.3f;
                    Vector3 foot = center + new Vector3(Range(random, -1.2f, 1.2f), 0.22f, Range(random, -0.65f, 0.65f));
                    Pieces.Prim(dormant.transform, PrimitiveType.Cylinder, "Stalk" + i + "_" + k, foot + new Vector3(0f, height * 0.5f, 0f),
                        new Vector3(0.035f, height * 0.5f, 0.035f), m["Withered"], Col.None, new Vector3(Range(random, -25f, 25f), 0f, Range(random, -25f, 25f)));
                    if (k % 3 == 0)
                        Pieces.Prim(dormant.transform, PrimitiveType.Sphere, "Droop" + i + "_" + k, foot + new Vector3(0.08f, height * 0.85f, 0f),
                            new Vector3(0.2f, 0.05f, 0.13f), m["WitheredLeaf"], Col.None, new Vector3(0f, Range(random, 0f, 180f), 25f));
                }

                // Bloom: rich soil, a flower grid and two leafy bushes
                Pieces.Prim(bloom.transform, PrimitiveType.Cube, "RichSoil" + i, center + new Vector3(0f, 0.17f, 0f), new Vector3(2.8f, 0.26f, 1.6f), m["SoilRich"]);
                for (int gx = 0; gx < 3; gx++)
                {
                    for (int gz = 0; gz < 2; gz++)
                    {
                        Vector3 foot = center + new Vector3(-0.9f + gx * 0.9f + Range(random, -0.15f, 0.15f), 0.3f, -0.4f + gz * 0.8f + Range(random, -0.12f, 0.12f));
                        string petals = Petals[(i * 3 + gx + gz * 2) % Petals.Length];
                        Flower(bloom.transform, "Flower" + i + "_" + gx + gz, foot, 0.45f + (float)random.NextDouble() * 0.4f, m, petals);
                    }
                }
                Bush(bloom.transform, "BushA" + i, center + new Vector3(-1.2f, 0.28f, 0.55f), 0.62f, m, random);
                Bush(bloom.transform, "BushB" + i, center + new Vector3(1.2f, 0.28f, -0.55f), 0.55f, m, random);
            }

            // The gate arch: bare vines when dormant, a curtain of blossoms in bloom
            Vector3 archBase = new Vector3(-GardenHalfX, 0f, 0f);
            for (int k = 0; k < 6; k++)
                Pieces.Prim(dormant.transform, PrimitiveType.Sphere, "Vine" + k, archBase + new Vector3(0.06f, 0.9f + k * 0.26f, k % 2 == 0 ? -1.15f : 1.15f),
                    new Vector3(0.14f, 0.2f, 0.14f), m["Withered"]);
            // Blossoms climb both posts (three each) and run along the beam (four), always touching the wood they grow on
            int blossom = 0;
            for (int post = 0; post < 2; post++)
                for (int k = 0; k < 3; k++)
                    Pieces.Prim(bloom.transform, PrimitiveType.Sphere, "ArchBlossom" + blossom, archBase + new Vector3(0.1f, 0.7f + k * 0.6f, post == 0 ? -1.15f : 1.15f),
                        Vector3.one * 0.2f, m[Petals[blossom++ % Petals.Length]]);
            for (int k = 0; k < 4; k++)
                Pieces.Prim(bloom.transform, PrimitiveType.Sphere, "ArchBlossom" + blossom, archBase + new Vector3(0.1f, 2.27f, -0.75f + k * 0.5f),
                    Vector3.one * 0.2f, m[Petals[blossom++ % Petals.Length]]);

            // Hedges outside the fence
            Vector3[] hedges =
            {
                new Vector3(-4.7f, 0f, 3.7f), new Vector3(4.7f, 0f, 3.7f), new Vector3(-4.7f, 0f, -3.7f), new Vector3(4.7f, 0f, -3.7f),
                new Vector3(0f, 0f, 3.9f), new Vector3(0f, 0f, -3.9f), new Vector3(4.9f, 0f, 0f)
            };
            for (int i = 0; i < hedges.Length; i++)
            {
                Pieces.Prim(dormant.transform, PrimitiveType.Sphere, "DryHedge" + i, hedges[i] + new Vector3(0f, 0.32f, 0f), new Vector3(0.95f, 0.64f, 0.95f), m["WitheredLeaf"]);
                Bush(bloom.transform, "Hedge" + i, hedges[i] + new Vector3(0f, 0.4f, 0f), 1.0f, m, random);
            }

            bloom.SetActive(false);   // the scene starts in the silent valley
            return new GardenParts { Root = root, Dormant = dormant, Bloom = bloom };
        }

        static void FenceRun(Transform parent, Vector3 from, Vector3 to, MaterialLibrary m)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            Vector3 direction = delta / length;
            Quaternion rotation = Quaternion.LookRotation(direction);

            int segments = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            for (int i = 0; i <= segments; i++)
                Pieces.Prim(parent, PrimitiveType.Cylinder, "Post", from + direction * (length * i / segments) + Vector3.up * 0.45f,
                    new Vector3(0.12f, 0.45f, 0.12f), m["WoodDark"]);
            Pieces.Prim(parent, PrimitiveType.Cube, "RailLow", from + delta * 0.5f + Vector3.up * 0.3f, new Vector3(0.05f, 0.07f, length), m["Wood"], Col.None, rotation.eulerAngles);
            Pieces.Prim(parent, PrimitiveType.Cube, "RailHigh", from + delta * 0.5f + Vector3.up * 0.7f, new Vector3(0.05f, 0.07f, length), m["Wood"], Col.None, rotation.eulerAngles);

            GameObject block = new GameObject("Collider");
            block.transform.SetParent(parent, false);
            block.transform.localPosition = from + delta * 0.5f + Vector3.up * 0.5f;
            block.transform.localRotation = rotation;
            block.AddComponent<BoxCollider>().size = new Vector3(0.3f, 1f, length + 0.3f);
        }

        /// <summary>A flower whose origin is its foot, so scaling it from zero makes it grow upwards.</summary>
        static void Flower(Transform parent, string name, Vector3 foot, float height, MaterialLibrary m, string petals)
        {
            Transform flower = Pieces.Group(name, parent, foot);
            Pieces.Prim(flower, PrimitiveType.Cylinder, "Stem", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.04f, height * 0.5f, 0.04f), m["Stem"]);
            Pieces.Prim(flower, PrimitiveType.Sphere, "Bloom", new Vector3(0f, height + 0.03f, 0f), new Vector3(0.3f, 0.18f, 0.3f), m[petals]);
            Pieces.Prim(flower, PrimitiveType.Sphere, "Heart", new Vector3(0f, height + 0.1f, 0f), Vector3.one * 0.11f, m["BlossomGlow"]);
        }

        /// <summary>A leafy bush dotted with blossoms; origin at its centre.</summary>
        static void Bush(Transform parent, string name, Vector3 center, float size, MaterialLibrary m, System.Random random)
        {
            Transform bush = Pieces.Group(name, parent, center);
            Pieces.Prim(bush, PrimitiveType.Sphere, "Leaves", Vector3.zero, new Vector3(size, size * 0.8f, size), m["GardenGreen"]);
            Pieces.Prim(bush, PrimitiveType.Sphere, "LeavesTop", new Vector3(0.05f * size, 0.22f * size, 0f), new Vector3(size * 0.6f, size * 0.45f, size * 0.6f), m["GardenGreenLight"]);
            for (int k = 0; k < 4; k++)
            {
                float angle = (k * 90f + (float)random.NextDouble() * 40f) * Mathf.Deg2Rad;
                Vector3 at = new Vector3(Mathf.Cos(angle) * size * 0.42f, size * (0.05f + 0.12f * (k % 2)), Mathf.Sin(angle) * size * 0.42f);
                Pieces.Prim(bush, PrimitiveType.Sphere, "Blossom" + k, at, Vector3.one * (0.13f * Mathf.Max(0.8f, size)), m[Petals[(k + (int)(size * 10f)) % Petals.Length]]);
            }
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // ---- Windmill ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// A stone-and-plaster tower with a domed cap, built facing local +Z. The sails (SailsPivot) come in two sets that are
        /// toggled, a broken one with snapped arms and fallen beams, and a whole one with glowing ore braces around its foot.
        /// </summary>
        public static WindmillParts Windmill(Transform parent, Vector3 position, float yaw, MaterialLibrary m)
        {
            Transform root = Pieces.Group("Windmill", parent, position, yaw);

            Pieces.Prim(root, PrimitiveType.Cylinder, "Foundation", new Vector3(0f, 0.35f, 0f), new Vector3(5.6f, 0.35f, 5.6f), m["Stone"], Col.Mesh);
            Pieces.Prim(root, PrimitiveType.Cylinder, "TowerLower", new Vector3(0f, 2.4f, 0f), new Vector3(4.4f, 1.7f, 4.4f), m["Plaster"], Col.Mesh);
            Pieces.Prim(root, PrimitiveType.Cylinder, "TowerUpper", new Vector3(0f, 5.6f, 0f), new Vector3(3.7f, 1.5f, 3.7f), m["Plaster"], Col.Mesh);
            Pieces.Prim(root, PrimitiveType.Cylinder, "Band", new Vector3(0f, 4.1f, 0f), new Vector3(4.0f, 0.07f, 4.0f), m["WoodDark"]);
            Pieces.Prim(root, PrimitiveType.Cube, "DoorFrame", new Vector3(0f, 1.6f, 2.2f), new Vector3(1.35f, 1.95f, 0.14f), m["WoodDark"]);
            Pieces.Prim(root, PrimitiveType.Cube, "Door", new Vector3(0f, 1.55f, 2.25f), new Vector3(1.05f, 1.8f, 0.12f), m["Wood"]);
            Landmarks.WindowGlow(Pieces.Prim(root, PrimitiveType.Cube, "WindowFront", new Vector3(0f, 4.9f, 1.88f), new Vector3(0.7f, 0.9f, 0.12f), m["Window"]), false);
            Landmarks.WindowGlow(Pieces.Prim(root, PrimitiveType.Cube, "WindowLeft", new Vector3(-1.88f, 4.9f, 0f), new Vector3(0.12f, 0.9f, 0.7f), m["Window"]), false);
            Landmarks.WindowGlow(Pieces.Prim(root, PrimitiveType.Cube, "WindowRight", new Vector3(1.88f, 4.9f, 0f), new Vector3(0.12f, 0.9f, 0.7f), m["Window"]), false);
            Pieces.Prim(root, PrimitiveType.Sphere, "Cap", new Vector3(0f, 7.2f, 0f), new Vector3(4.0f, 2.6f, 4.0f), m["Roof"]);
            Pieces.Prim(root, PrimitiveType.Sphere, "CapTip", new Vector3(0f, 8.55f, 0f), Vector3.one * 0.32f, m["WoodDark"]);
            Pieces.Prim(root, PrimitiveType.Cylinder, "Axle", new Vector3(0f, 7.2f, 2.0f), new Vector3(0.32f, 0.5f, 0.32f), m["WoodDark"], Col.None, new Vector3(90f, 0f, 0f));

            Transform pivot = Pieces.Group("SailsPivot", root, new Vector3(0f, 7.2f, 2.6f));
            Pieces.Prim(pivot, PrimitiveType.Sphere, "Hub", Vector3.zero, Vector3.one * 0.6f, m["Wood"]);

            // The whole sails: four full arms with cloth
            Transform intact = Pieces.Group("IntactSails", pivot, Vector3.zero);
            for (int i = 0; i < 4; i++) Arm(intact, "Arm" + i, i * 90f, SailLength, 1f, true, m);

            // The broken sails: one half-torn, one snapped, one bare, one stump
            Transform broken = Pieces.Group("BrokenSails", pivot, Vector3.zero);
            Arm(broken, "Arm0", 0f, SailLength, 0.5f, true, m);
            Transform snapped = Arm(broken, "Arm1", 90f, 2.1f, 0f, false, m);
            Pieces.Prim(snapped, PrimitiveType.Cube, "Splinter", new Vector3(0.45f, 2.9f, 0.04f), new Vector3(0.2f, 1.5f, 0.1f), m["BrokenWood"], Col.None, new Vector3(0f, 0f, -38f));
            Arm(broken, "Arm2", 180f, SailLength, 0f, false, m);
            Arm(broken, "Arm3", 270f, 0.9f, 0f, false, m);

            // The broken supports: fallen beams, a leaning prop and splinters
            GameObject fallen = new GameObject("SupportsBroken");
            fallen.transform.SetParent(root, false);
            Pieces.Prim(fallen.transform, PrimitiveType.Cube, "FallenBeamA", new Vector3(3.5f, 0.22f, 1.6f), new Vector3(0.3f, 0.3f, 3.2f), m["BrokenWood"], Col.None, new Vector3(0f, 35f, 6f));
            Pieces.Prim(fallen.transform, PrimitiveType.Cube, "FallenBeamB", new Vector3(-3.7f, 0.2f, 0.8f), new Vector3(0.3f, 0.3f, 2.6f), m["BrokenWood"], Col.None, new Vector3(0f, -28f, -5f));
            Pieces.Prim(fallen.transform, PrimitiveType.Cube, "LeaningProp", new Vector3(2.5f, 1.05f, -2.1f), new Vector3(0.3f, 2.1f, 0.3f), m["BrokenWood"], Col.None, new Vector3(-20f, 25f, 0f));
            for (int i = 0; i < 4; i++)
                Pieces.Prim(fallen.transform, PrimitiveType.Cube, "Splinter" + i, new Vector3(-2.8f + i * 1.9f, 0.1f, -3.2f + (i % 2) * 0.9f), new Vector3(0.14f, 0.1f, 0.5f), m["BrokenWood"], Col.None, new Vector3(0f, i * 53f, 4f));

            // The mended supports: four glowing ore braces leaning against the tower (the door faces +Z, so they sit on the diagonals)
            GameObject braces = new GameObject("Braces");
            braces.transform.SetParent(root, false);
            for (int i = 0; i < 4; i++)
            {
                Transform at = Pieces.Group("Brace" + i, braces.transform, new Vector3(0f, 0f, 0f), 45f + i * 90f);
                Pieces.Prim(at, PrimitiveType.Cube, "Strut", new Vector3(0f, 2.1f, 2.25f), new Vector3(0.3f, 3.0f, 0.3f), m["BraceGlow"], Col.None, new Vector3(-20f, 0f, 0f));
                Pieces.Prim(at, PrimitiveType.Cube, "Foot", new Vector3(0f, 0.85f, 2.6f), new Vector3(0.6f, 0.3f, 0.6f), m["WoodDark"]);
            }

            intact.gameObject.SetActive(false);   // the scene starts with the windmill broken
            braces.SetActive(false);
            return new WindmillParts
            {
                Root = root,
                SailsPivot = pivot,
                Damaged = new[] { broken.gameObject, fallen },
                Repaired = new[] { intact.gameObject, braces }
            };
        }

        /// <summary>One sail: a spar from the hub with an optional cloth lattice covering a fraction of its length.</summary>
        static Transform Arm(Transform parent, string name, float angle, float length, float latticeFraction, bool lattice, MaterialLibrary m)
        {
            Transform arm = Pieces.Group(name, parent, Vector3.zero);
            arm.localRotation = Quaternion.Euler(0f, 0f, angle);
            Pieces.Prim(arm, PrimitiveType.Cube, "Spar", new Vector3(0f, 0.15f + length * 0.5f, 0f), new Vector3(0.22f, length, 0.14f), m["WoodDark"]);
            if (lattice && latticeFraction > 0f)
            {
                float span = (length - 0.7f) * latticeFraction;
                Pieces.Prim(arm, PrimitiveType.Cube, "Lattice", new Vector3(0.55f, 0.9f + span * 0.5f, 0.02f), new Vector3(0.95f, span, 0.05f), m["SailCloth"]);
                for (int k = 0; k < 3; k++)
                    Pieces.Prim(arm, PrimitiveType.Cube, "Bar" + k, new Vector3(0.55f, 0.9f + span * (k + 0.5f) / 3f, 0.05f), new Vector3(0.95f, 0.06f, 0.07f), m["Wood"]);
            }
            return arm;
        }

        // ---- Hen yard ---------------------------------------------------------------------------------------------------

        /// <summary>Flower pots on the fence corners and two strings of bunting: shown from stage 2, popped up one by one.</summary>
        public static GameObject YardLife(Transform parent, Vector3 henYardPosition, MaterialLibrary m)
        {
            GameObject root = new GameObject("HenYardLife");
            root.transform.SetParent(parent, false);
            root.transform.position = henYardPosition;
            const float half = 2.3f;
            System.Random random = new System.Random(55);

            Vector2[] corners = { new Vector2(-half, -half), new Vector2(half, -half), new Vector2(-half, half), new Vector2(half, half) };
            for (int i = 0; i < corners.Length; i++)
            {
                Transform pot = Pieces.Group("Pot" + i, root.transform, new Vector3(corners[i].x * 1.12f, 0f, corners[i].y * 1.12f));
                Pieces.Prim(pot, PrimitiveType.Cylinder, "Pot", new Vector3(0f, 0.17f, 0f), new Vector3(0.42f, 0.17f, 0.42f), m["Roof"]);
                Pieces.Prim(pot, PrimitiveType.Cylinder, "Soil", new Vector3(0f, 0.33f, 0f), new Vector3(0.36f, 0.02f, 0.36f), m["SoilRich"]);
                for (int k = 0; k < 3; k++)
                    Flower(pot, "Flower" + k, new Vector3(-0.1f + k * 0.1f, 0.34f, (k % 2) * 0.08f), 0.3f + 0.08f * k, m, Petals[(i + k) % Petals.Length]);
            }

            // Bunting along the south and west sides, hung between the corner posts
            for (int side = 0; side < 2; side++)
            {
                Transform bunting = Pieces.Group("Bunting" + side, root.transform, Vector3.zero);
                Vector3 a = side == 0 ? new Vector3(-half, 1.2f, -half) : new Vector3(-half, 1.2f, half);
                Vector3 b = side == 0 ? new Vector3(half, 1.2f, -half) : new Vector3(-half, 1.2f, -half);
                Vector3 direction = (b - a).normalized;
                Pieces.Prim(bunting, PrimitiveType.Cube, "String", (a + b) * 0.5f, new Vector3(0.02f, 0.02f, (b - a).magnitude), m["WoodDark"], Col.None,
                    Quaternion.LookRotation(direction).eulerAngles);
                for (int k = 0; k < 7; k++)
                {
                    float t = (k + 0.5f) / 7f;
                    Vector3 at = Vector3.Lerp(a, b, t) + Vector3.down * (0.15f + 0.12f * Mathf.Sin(t * Mathf.PI));
                    Pieces.Prim(bunting, PrimitiveType.Cube, "Flag" + k, at, new Vector3(0.22f, 0.3f, 0.02f), m[Petals[(k + side * 3) % Petals.Length]], Col.None,
                        new Vector3(0f, Quaternion.LookRotation(direction).eulerAngles.y + 90f, 8f * (k % 2 == 0 ? 1f : -1f)));
                }
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f + 20f) * Mathf.Deg2Rad;
                Vector3 foot = new Vector3(Mathf.Cos(angle) * 1.5f, 0f, Mathf.Sin(angle) * 1.5f) + new Vector3(Range(random, -0.1f, 0.1f), 0f, Range(random, -0.1f, 0.1f));
                Flower(root.transform, "YardFlower" + i, foot, 0.22f + (float)random.NextDouble() * 0.1f, m, Petals[i % Petals.Length]);
            }
            root.SetActive(false);   // shown from stage 2
            return root;
        }

        // ---- Particles -----------------------------------------------------------------------------------------------------

        /// <summary>Pastel specks drifting upwards over a box of the given size; plays for as long as it is switched on.</summary>
        public static ParticleSystem Motes(Transform parent, string name, Vector3 localPosition, Vector3 boxSize, float rate, int max,
            Color colorA, Color colorB, float size, bool glow = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = -0.01f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(SceneKit.FadeInOut(Color.white, 1f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = glow ? SceneKit.GlowMaterial() : SceneKit.ParticleMaterial();
            return system;
        }
    }
}
