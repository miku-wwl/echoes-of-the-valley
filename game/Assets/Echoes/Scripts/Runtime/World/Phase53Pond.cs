using UnityEngine;

namespace Echoes
{
    /// <summary>One persistent ResonanceStage drives the pond. No extra save state; registered paintings switch conservatively to avoid ghosting.</summary>
    public sealed class Phase53Pond : MonoBehaviour
    {
        public Renderer Surface;
        public Renderer Lilies;
        public Material Dormant;
        public Material Restored;
        public Terrain Terrain;
        public bool RestoredShown { get; private set; }
        public static readonly Vector3 Centre = new Vector3(-28f, 0f, -14f);
        public const float Width = 12.8f;
        public static float Depth => Width * (2f / 3f) / Mathf.Sin(48f * Mathf.Deg2Rad);
        public static readonly Vector3 Right = new Vector3(0.70710678f, 0f, -0.70710678f);
        public static readonly Vector3 Forward = new Vector3(0.70710678f, 0f, 0.70710678f);
        // Inner water contour follows the actual painted bank, not an ellipse or the old cylinder.
        public static readonly Vector2[] Inner = {
            new Vector2(.315f,.16f), new Vector2(.44f,.145f), new Vector2(.59f,.235f), new Vector2(.675f,.31f),
            new Vector2(.80f,.385f), new Vector2(.885f,.445f), new Vector2(.92f,.56f), new Vector2(.88f,.755f),
            new Vector2(.74f,.805f), new Vector2(.56f,.775f), new Vector2(.37f,.68f), new Vector2(.185f,.605f),
            new Vector2(.14f,.465f), new Vector2(.19f,.305f)
        };
        public static Vector3 Point(float u, float topV) => Centre + Right * ((u - .5f) * Width) + Forward * ((.5f - topV) * Depth);
        public static Vector2 ImagePoint(Vector3 world)
        {
            Vector3 d = world - Centre;
            return new Vector2(.5f + Vector3.Dot(d, Right) / Width, .5f - Vector3.Dot(d, Forward) / Depth);
        }
        public static bool InWater(Vector3 world)
        {
            Vector2 p = ImagePoint(world); bool inside = false;
            for (int i = 0, j = Inner.Length - 1; i < Inner.Length; j = i++)
                if ((Inner[i].y > p.y) != (Inner[j].y > p.y) && p.x < (Inner[j].x - Inner[i].x) * (p.y - Inner[i].y) / (Inner[j].y - Inner[i].y) + Inner[i].x)
                    inside = !inside;
            return inside;
        }
        public static bool OnDock(Vector3 p) => p.x >= -26.65f && p.x <= -21.75f && Mathf.Abs(p.z + 14f) < .63f;
        public static bool IsDeep(Vector3 p) => InWater(p) && !OnDock(p);
        public static float EdgeDistance(Vector3 world)
        {
            float best = float.MaxValue;
            for (int i = 0; i < Inner.Length; i++)
            {
                Vector3 a = Point(Inner[i].x, Inner[i].y), b = Point(Inner[(i + 1) % Inner.Length].x, Inner[(i + 1) % Inner.Length].y);
                Vector3 ab = b - a; float k = Mathf.Clamp01(Vector3.Dot(world - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector3.ProjectOnPlane(world - a - ab * k, Vector3.up).magnitude);
            }
            return best;
        }

        void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (Surface == null) return;
            int stage = GameSession.Current != null ? (int)GameSession.Current.Resonance : 0;
            RestoredShown = stage >= 1;
            Surface.sharedMaterial = RestoredShown ? Restored : Dormant;
            var block = new MaterialPropertyBlock();
            float k = stage == 1 ? .82f : stage == 2 ? .92f : 1f;
            block.SetColor("_BaseColor", new Color(k, k, k, 1f));
            Surface.SetPropertyBlock(block);
            if (Lilies != null)
            {
                block.SetColor("_BaseColor", stage == 0 ? new Color(.54f,.50f,.38f,1f) : new Color(k,k,k,1f));
                Lilies.SetPropertyBlock(block);
            }
        }
    }
}
