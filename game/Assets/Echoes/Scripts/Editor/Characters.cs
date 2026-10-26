using System.Collections.Generic;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// The four original characters, built from primitives around one small rig (head, two arms, two legs, a face that blinks):
    /// the caretaker the player controls (a teal tunic, a cream cape, a trailing scarf, a satchel with a sprig and a lantern that
    /// brightens as the valley wakes), Mira the herbalist, Wren the gardener and Alder the windmill restorer. Each has its own
    /// silhouette and colours. CharacterAnimator moves the parts, so no skinning or clips are needed.
    /// </summary>
    internal static class Characters
    {
        sealed class Rig
        {
            public GameObject Root;
            public Transform Body, Head, ArmL, ArmR, LegL, LegR;
            public readonly List<Transform> Eyes = new List<Transform>();
            public readonly List<Transform> Tails = new List<Transform>();
        }

        static GameObject P(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler = default) =>
            Pieces.Prim(parent, type, name, position, scale, material, Col.None, euler);

        static Rig Begin(string name, MaterialLibrary m, string torso, string sleeve, string legs, string boots, string hands, float bulk)
        {
            Rig r = new Rig { Root = new GameObject(name) };
            r.Body = Pieces.Group("Body", r.Root.transform, Vector3.zero);
            P(r.Body, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.64f, 0f), new Vector3(0.52f * bulk, 0.34f, 0.44f * bulk), m[torso]);
            r.Head = Pieces.Group("Head", r.Body, new Vector3(0f, 1.22f, 0f));
            P(r.Head, PrimitiveType.Sphere, "Skull", Vector3.zero, Vector3.one * 0.50f, m["Skin"]);
            Face(r, m);
            r.ArmL = Arm(r, m, -1, sleeve, hands, bulk);
            r.ArmR = Arm(r, m, 1, sleeve, hands, bulk);
            r.LegL = Leg(r, m, -1, legs, boots);
            r.LegR = Leg(r, m, 1, legs, boots);
            return r;
        }

        /// <summary>Eyes with a glint, brows, a small mouth and blushing cheeks. The eyes are kept so they can blink.</summary>
        static void Face(Rig r, MaterialLibrary m)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                string tag = side < 0 ? "L" : "R";
                GameObject eye = P(r.Head, PrimitiveType.Sphere, "Eye" + tag, new Vector3(side * 0.095f, 0f, 0.215f), new Vector3(0.058f, 0.08f, 0.04f), m["Eye"]);
                r.Eyes.Add(eye.transform);
                P(r.Head, PrimitiveType.Sphere, "Glint" + tag, new Vector3(side * 0.095f + 0.012f, 0.026f, 0.236f), Vector3.one * 0.018f, m["Highlight"]);
                P(r.Head, PrimitiveType.Cube, "Brow" + tag, new Vector3(side * 0.095f, 0.072f, 0.222f), new Vector3(0.075f, 0.014f, 0.016f), m["Brow"], new Vector3(0f, 0f, side * -8f));
                P(r.Head, PrimitiveType.Sphere, "Cheek" + tag, new Vector3(side * 0.15f, -0.06f, 0.18f), new Vector3(0.07f, 0.04f, 0.03f), m["Blush"]);
            }
            P(r.Head, PrimitiveType.Sphere, "Mouth", new Vector3(0f, -0.085f, 0.225f), new Vector3(0.06f, 0.022f, 0.02f), m["MouthCoral"]);
        }

        static Transform Arm(Rig r, MaterialLibrary m, int side, string sleeve, string hands, float bulk)
        {
            Transform arm = Pieces.Group(side < 0 ? "ArmL" : "ArmR", r.Body, new Vector3(side * 0.31f * bulk, 0.92f, 0f));
            P(arm, PrimitiveType.Capsule, "Sleeve", new Vector3(0f, -0.17f, 0f), new Vector3(0.14f, 0.19f, 0.14f), m[sleeve]);
            P(arm, PrimitiveType.Sphere, "Hand", new Vector3(0f, -0.38f, 0f), Vector3.one * 0.115f, m[hands]);
            return arm;
        }

        static Transform Leg(Rig r, MaterialLibrary m, int side, string legs, string boots)
        {
            Transform leg = Pieces.Group(side < 0 ? "LegL" : "LegR", r.Root.transform, new Vector3(side * 0.12f, 0.36f, 0f));
            P(leg, PrimitiveType.Capsule, "Leg", new Vector3(0f, -0.13f, 0f), new Vector3(0.14f, 0.15f, 0.14f), m[legs]);
            P(leg, PrimitiveType.Sphere, "Boot", new Vector3(0f, -0.27f, 0.035f), new Vector3(0.17f, 0.12f, 0.27f), m[boots]);
            return leg;
        }

        static GameObject Finish(Rig r)
        {
            CharacterAnimator animator = r.Root.AddComponent<CharacterAnimator>();
            Wire.Set(animator, "body", r.Body);
            Wire.Set(animator, "head", r.Head);
            Wire.Set(animator, "leftArm", r.ArmL);
            Wire.Set(animator, "rightArm", r.ArmR);
            Wire.Set(animator, "leftLeg", r.LegL);
            Wire.Set(animator, "rightLeg", r.LegR);
            Wire.Set(animator, "eyes", r.Eyes.ToArray());
            Wire.Set(animator, "tails", r.Tails.ToArray());
            return r.Root;
        }

        /// <summary>A group hanging from a pivot, so CharacterAnimator can swing it behind the body.</summary>
        static Transform Hanging(Rig r, Transform parent, string name, Vector3 pivot)
        {
            Transform t = Pieces.Group(name, parent, pivot);
            r.Tails.Add(t);
            return t;
        }

        // ---- The caretaker (the player) -------------------------------------------------------------------------------------

        public static GameObject Hero(MaterialLibrary m)
        {
            Rig r = Begin("Hero", m, "TunicHero", "TunicHero", "Trousers", "Boots", "Skin", 1f);

            // Soft brown hair with a fringe, a ponytail that swings and a leaf hairpin
            P(r.Head, PrimitiveType.Sphere, "Hair", new Vector3(0f, 0.05f, -0.025f), new Vector3(0.545f, 0.50f, 0.545f), m["HairHero"]);
            for (int k = -1; k <= 1; k++)
                P(r.Head, PrimitiveType.Sphere, "Fringe" + k, new Vector3(k * 0.10f, 0.185f, 0.16f), new Vector3(0.17f, 0.12f, 0.13f), m["HairHero"]);
            Transform pony = Hanging(r, r.Head, "Ponytail", new Vector3(0f, 0.08f, -0.25f));
            P(pony, PrimitiveType.Sphere, "Tie", Vector3.zero, Vector3.one * 0.07f, m["Scarf"]);
            P(pony, PrimitiveType.Sphere, "Tuft", new Vector3(0f, -0.13f, -0.03f), new Vector3(0.14f, 0.27f, 0.14f), m["HairHero"]);
            P(r.Head, PrimitiveType.Sphere, "LeafA", new Vector3(0.20f, 0.20f, 0.10f), new Vector3(0.11f, 0.045f, 0.06f), m["GardenGreen"], new Vector3(0f, 0f, 35f));
            P(r.Head, PrimitiveType.Sphere, "LeafB", new Vector3(0.25f, 0.17f, 0.08f), new Vector3(0.09f, 0.04f, 0.05f), m["GardenGreenLight"], new Vector3(0f, 0f, -15f));

            // A cream cape and a warm scarf with two tails, both trailing when the caretaker walks
            Transform cape = Hanging(r, r.Body, "Cape", new Vector3(0f, 1.0f, -0.2f));
            P(cape, PrimitiveType.Sphere, "Cloth", new Vector3(0f, -0.21f, -0.03f), new Vector3(0.60f, 0.52f, 0.12f), m["HeroCape"]);
            P(r.Body, PrimitiveType.Cylinder, "Scarf", new Vector3(0f, 0.99f, 0f), new Vector3(0.40f, 0.045f, 0.40f), m["Scarf"]);
            Transform tail = Hanging(r, r.Body, "ScarfTail", new Vector3(0.12f, 0.97f, -0.17f));
            P(tail, PrimitiveType.Cube, "Cloth", new Vector3(0f, -0.17f, 0f), new Vector3(0.09f, 0.36f, 0.025f), m["Scarf"]);

            // Belt, a satchel with a sprig on the right hip, and the lantern on the left that brightens with the valley
            P(r.Body, PrimitiveType.Cylinder, "Belt", new Vector3(0f, 0.44f, 0f), new Vector3(0.55f, 0.025f, 0.47f), m["WoodDark"]);
            P(r.Body, PrimitiveType.Cube, "Satchel", new Vector3(0.32f, 0.38f, -0.03f), new Vector3(0.20f, 0.23f, 0.13f), m["Wood"]);
            P(r.Body, PrimitiveType.Cube, "SatchelFlap", new Vector3(0.32f, 0.47f, -0.03f), new Vector3(0.21f, 0.07f, 0.14f), m["WoodDark"]);
            P(r.Body, PrimitiveType.Sphere, "SprigA", new Vector3(0.30f, 0.56f, -0.03f), new Vector3(0.10f, 0.05f, 0.07f), m["GardenGreen"], new Vector3(0f, 0f, 50f));
            P(r.Body, PrimitiveType.Sphere, "SprigB", new Vector3(0.35f, 0.58f, -0.03f), new Vector3(0.09f, 0.045f, 0.06f), m["GardenGreenLight"], new Vector3(0f, 0f, -30f));
            Transform lantern = Hanging(r, r.Body, "Lantern", new Vector3(-0.32f, 0.44f, 0.04f));
            P(lantern, PrimitiveType.Cube, "Chain", new Vector3(0f, -0.06f, 0f), new Vector3(0.012f, 0.12f, 0.012f), m["Iron"]);
            P(lantern, PrimitiveType.Cylinder, "Cap", new Vector3(0f, -0.13f, 0f), new Vector3(0.10f, 0.015f, 0.10f), m["Iron"]);
            GameObject glass = P(lantern, PrimitiveType.Sphere, "Glass", new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.18f, 0.16f), m["LanternLamp"]);
            P(lantern, PrimitiveType.Cylinder, "Base", new Vector3(0f, -0.30f, 0f), new Vector3(0.09f, 0.015f, 0.09f), m["Iron"]);
            StageGlow glow = glass.AddComponent<StageGlow>();
            Wire.Set(glow, "target", glass.GetComponent<Renderer>());
            Wire.Set(glow, "color", new Color(1f, 0.80f, 0.42f));
            Wire.Set(glow, "flicker", 0.12f);

            return Finish(r);
        }

        // ---- Mira, the herbalist ----------------------------------------------------------------------------------------------

        public static GameObject Mira(MaterialLibrary m)
        {
            Rig r = Begin("Villager", m, "TunicMira", "Cream", "Trousers", "Boots", "Skin", 1f);

            // A sage-green dress with a cream apron
            P(r.Body, PrimitiveType.Cylinder, "Skirt", new Vector3(0f, 0.36f, 0f), new Vector3(0.66f, 0.20f, 0.66f), m["TunicMira"]);
            P(r.Body, PrimitiveType.Cylinder, "SkirtHem", new Vector3(0f, 0.17f, 0f), new Vector3(0.72f, 0.04f, 0.72f), m["Cream"]);
            P(r.Body, PrimitiveType.Cube, "Apron", new Vector3(0f, 0.50f, 0.225f), new Vector3(0.30f, 0.40f, 0.03f), m["Cream"]);
            P(r.Body, PrimitiveType.Cube, "ApronBib", new Vector3(0f, 0.78f, 0.215f), new Vector3(0.20f, 0.16f, 0.025f), m["Cream"]);

            // Golden-brown hair with a long braid that swings
            P(r.Head, PrimitiveType.Sphere, "Hair", new Vector3(0f, 0.06f, -0.03f), new Vector3(0.555f, 0.50f, 0.555f), m["HairMira"]);
            P(r.Head, PrimitiveType.Sphere, "HairBack", new Vector3(0f, -0.15f, -0.17f), new Vector3(0.40f, 0.46f, 0.26f), m["HairMira"]);
            for (int k = -1; k <= 1; k++)
                P(r.Head, PrimitiveType.Sphere, "Fringe" + k, new Vector3(k * 0.11f, 0.17f, 0.165f), new Vector3(0.16f, 0.11f, 0.12f), m["HairMira"]);
            Transform braid = Hanging(r, r.Head, "Braid", new Vector3(0.04f, -0.2f, -0.21f));
            for (int i = 0; i < 4; i++)
                P(braid, PrimitiveType.Sphere, "Link" + i, new Vector3(0f, -0.07f - i * 0.10f, -0.02f), new Vector3(0.11f - 0.012f * i, 0.14f, 0.11f - 0.012f * i), m["HairMira"]);
            P(braid, PrimitiveType.Sphere, "Ribbon", new Vector3(0f, -0.46f, -0.02f), new Vector3(0.09f, 0.05f, 0.09f), m["FlowerPink"]);

            // A straw hat with a pink flower band
            P(r.Head, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 0.25f, 0f), new Vector3(0.74f, 0.012f, 0.74f), m["Straw"]);
            P(r.Head, PrimitiveType.Sphere, "HatDome", new Vector3(0f, 0.30f, 0f), new Vector3(0.37f, 0.24f, 0.37f), m["Straw"]);
            P(r.Head, PrimitiveType.Cylinder, "HatBand", new Vector3(0f, 0.265f, 0f), new Vector3(0.385f, 0.02f, 0.385f), m["FlowerPink"]);
            for (int k = 0; k < 3; k++)
                P(r.Head, PrimitiveType.Sphere, "HatBloom" + k, new Vector3(-0.12f + k * 0.12f, 0.285f, 0.185f - Mathf.Abs(k - 1) * 0.03f), Vector3.one * 0.075f, m[k == 1 ? "FlowerYellow" : "FlowerWhite"]);

            // A basket of herbs on the right arm
            P(r.ArmR, PrimitiveType.Cylinder, "Basket", new Vector3(0.02f, -0.45f, 0.07f), new Vector3(0.24f, 0.075f, 0.24f), m["Wood"]);
            P(r.ArmR, PrimitiveType.Sphere, "Herbs", new Vector3(0.02f, -0.37f, 0.07f), new Vector3(0.22f, 0.10f, 0.22f), m["HerbLeaf"]);
            P(r.ArmR, PrimitiveType.Sphere, "BasketBloom", new Vector3(0.06f, -0.34f, 0.10f), Vector3.one * 0.06f, m["BlossomCoral"]);
            return Finish(r);
        }

        // ---- Wren, the gardener -------------------------------------------------------------------------------------------------

        public static GameObject Wren(MaterialLibrary m)
        {
            Rig r = Begin("GardenKeeper", m, "TunicKeeper", "Cream", "Trousers", "Boots", "GardenGreen", 1f);

            // Apricot overalls over a cream blouse
            P(r.Body, PrimitiveType.Cylinder, "Skirt", new Vector3(0f, 0.36f, 0f), new Vector3(0.62f, 0.20f, 0.62f), m["TunicKeeper"]);
            P(r.Body, PrimitiveType.Cube, "Bib", new Vector3(0f, 0.74f, 0.22f), new Vector3(0.28f, 0.22f, 0.03f), m["TunicKeeper"]);
            P(r.Body, PrimitiveType.Cube, "Pocket", new Vector3(0f, 0.56f, 0.235f), new Vector3(0.15f, 0.10f, 0.02f), m["Straw"]);
            for (int side = -1; side <= 1; side += 2)
                P(r.Body, PrimitiveType.Cube, "Strap" + side, new Vector3(side * 0.13f, 0.93f, 0.08f), new Vector3(0.05f, 0.30f, 0.05f), m["TunicKeeper"], new Vector3(25f, 0f, 0f));

            // Short dark curls
            P(r.Head, PrimitiveType.Sphere, "Hair", new Vector3(0f, 0.04f, -0.02f), new Vector3(0.54f, 0.49f, 0.54f), m["HairKeeper"]);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f + 20f) * Mathf.Deg2Rad;
                P(r.Head, PrimitiveType.Sphere, "Curl" + i, new Vector3(Mathf.Cos(angle) * 0.25f, 0.02f + (i % 2) * 0.06f, Mathf.Sin(angle) * 0.25f - 0.03f),
                    Vector3.one * 0.14f, m["HairKeeper"]);
            }

            // A wide straw hat with a sunflower
            P(r.Head, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 0.25f, 0f), new Vector3(0.84f, 0.012f, 0.84f), m["Straw"]);
            P(r.Head, PrimitiveType.Sphere, "HatDome", new Vector3(0f, 0.30f, 0f), new Vector3(0.38f, 0.26f, 0.38f), m["Straw"]);
            P(r.Head, PrimitiveType.Cylinder, "HatBand", new Vector3(0f, 0.265f, 0f), new Vector3(0.395f, 0.02f, 0.395f), m["TunicKeeper"]);
            P(r.Head, PrimitiveType.Sphere, "Sunflower", new Vector3(0.17f, 0.30f, 0.20f), new Vector3(0.16f, 0.16f, 0.03f), m["FlowerYellow"], new Vector3(0f, -25f, 0f));
            P(r.Head, PrimitiveType.Sphere, "SunflowerCore", new Vector3(0.175f, 0.30f, 0.215f), Vector3.one * 0.075f, m["WoodDark"]);

            // A trowel in the right hand
            Transform trowel = Pieces.Group("Trowel", r.ArmR, new Vector3(0f, -0.40f, 0.07f));
            P(trowel, PrimitiveType.Cylinder, "Handle", new Vector3(0f, 0f, 0.05f), new Vector3(0.03f, 0.07f, 0.03f), m["Wood"], new Vector3(90f, 0f, 0f));
            P(trowel, PrimitiveType.Cube, "Blade", new Vector3(0f, 0f, 0.17f), new Vector3(0.055f, 0.012f, 0.13f), m["Iron"]);
            return Finish(r);
        }

        // ---- Alder, the windmill restorer ---------------------------------------------------------------------------------------

        public static GameObject Alder(MaterialLibrary m)
        {
            Rig r = Begin("WindmillRestorer", m, "TunicRestorer", "TunicRestorer", "Trousers", "Boots", "Skin", 1.12f);
            r.Root.transform.localScale = Vector3.one * 1.06f;   // a little broader and taller than the others

            // A long slate-blue work coat, a rust scarf and a tool belt with a hammer
            P(r.Body, PrimitiveType.Cylinder, "Coat", new Vector3(0f, 0.38f, 0f), new Vector3(0.70f, 0.24f, 0.68f), m["TunicRestorer"]);
            P(r.Body, PrimitiveType.Cylinder, "Scarf", new Vector3(0f, 0.99f, 0f), new Vector3(0.43f, 0.05f, 0.43f), m["ScarfRestorer"]);
            Transform tail = Hanging(r, r.Body, "ScarfTail", new Vector3(-0.13f, 0.97f, 0.16f));
            P(tail, PrimitiveType.Cube, "Cloth", new Vector3(0f, -0.15f, 0f), new Vector3(0.09f, 0.30f, 0.025f), m["ScarfRestorer"]);
            P(r.Body, PrimitiveType.Cylinder, "Belt", new Vector3(0f, 0.48f, 0f), new Vector3(0.62f, 0.03f, 0.52f), m["WoodDark"]);
            Transform hammer = Pieces.Group("Hammer", r.Body, new Vector3(0.33f, 0.46f, 0.08f));
            P(hammer, PrimitiveType.Cylinder, "Handle", new Vector3(0f, -0.09f, 0f), new Vector3(0.028f, 0.09f, 0.028f), m["Wood"]);
            P(hammer, PrimitiveType.Cube, "Head", new Vector3(0f, -0.19f, 0f), new Vector3(0.10f, 0.055f, 0.055f), m["Iron"]);

            // White hair at the sides, bushy brows, a moustache and a beard
            for (int side = -1; side <= 1; side += 2)
            {
                P(r.Head, PrimitiveType.Sphere, "Tuft" + side, new Vector3(side * 0.20f, 0.04f, -0.03f), new Vector3(0.15f, 0.19f, 0.17f), m["HairRestorer"]);
                P(r.Head, PrimitiveType.Cube, "BushyBrow" + side, new Vector3(side * 0.095f, 0.085f, 0.225f), new Vector3(0.10f, 0.03f, 0.022f), m["HairRestorer"], new Vector3(0f, 0f, side * -10f));
                P(r.Head, PrimitiveType.Sphere, "Mustache" + side, new Vector3(side * 0.055f, -0.062f, 0.225f), new Vector3(0.11f, 0.05f, 0.05f), m["HairRestorer"], new Vector3(0f, 0f, side * 14f));
            }
            P(r.Head, PrimitiveType.Sphere, "Beard", new Vector3(0f, -0.17f, 0.14f), new Vector3(0.27f, 0.23f, 0.21f), m["HairRestorer"]);

            // A flat rust-red cap
            P(r.Head, PrimitiveType.Sphere, "CapDome", new Vector3(0f, 0.20f, 0f), new Vector3(0.545f, 0.22f, 0.545f), m["ScarfRestorer"]);
            P(r.Head, PrimitiveType.Cube, "CapBill", new Vector3(0f, 0.15f, 0.25f), new Vector3(0.27f, 0.02f, 0.17f), m["ScarfRestorer"], new Vector3(-12f, 0f, 0f));

            // A wooden mallet in the right hand
            Transform mallet = Pieces.Group("Mallet", r.ArmR, new Vector3(0f, -0.40f, 0.06f));
            P(mallet, PrimitiveType.Cylinder, "Handle", new Vector3(0f, 0f, 0.09f), new Vector3(0.03f, 0.10f, 0.03f), m["Wood"], new Vector3(90f, 0f, 0f));
            P(mallet, PrimitiveType.Cylinder, "Head", new Vector3(0f, 0f, 0.22f), new Vector3(0.09f, 0.07f, 0.09f), m["WoodDark"], new Vector3(0f, 0f, 90f));
            return Finish(r);
        }
    }
}
