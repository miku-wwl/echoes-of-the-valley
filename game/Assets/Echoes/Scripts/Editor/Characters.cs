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

        public static GameObject Hero(MaterialLibrary m) => PilotArt.Hero(m);

        // ---- Mira, the herbalist ----------------------------------------------------------------------------------------------

        public static GameObject Mira(MaterialLibrary m) => PilotArt.Human("Villager","Mira",m);

        // ---- Wren, the gardener -------------------------------------------------------------------------------------------------

        public static GameObject Wren(MaterialLibrary m) => PilotArt.Human("GardenKeeper","Wren",m);

        // ---- Alder, the windmill restorer ---------------------------------------------------------------------------------------

        public static GameObject Alder(MaterialLibrary m) => PilotArt.Human("WindmillRestorer","Alder",m);
    }
}
