using System.Collections.Generic;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>Pure layout math for the Valley: where things stand, and the terrain height and paint functions derived from it.</summary>
    internal static class ValleyLayout
    {
        public const float TerrainSize = 120f;
        public const float TerrainMaxHeight = 24f;
        public const float TerrainBaseOffset = 3f;   // terrain object sits 3 m below zero so carved areas fit
        public const float WaterLevel = -0.3f;
        public const float PlayableHalfSize = 50f;
        public const float PondRadius = 6.1f;
        public const float PlazaRadius = 9f;
        public const float StreamWidth = 2.9f;
        public const float CottageYaw = -90f;         // the cottage door (local +Z) faces world -X

        public static readonly Vector2 Spawn = new Vector2(0f, -32f);
        public static readonly Vector2 Heart = Vector2.zero;
        public static readonly Vector2 Cottage = new Vector2(25f, 8f);
        public static readonly Vector2 Villager = new Vector2(14.2f, 5.4f);
        public static readonly Vector2 Spring = new Vector2(-21f, 16f);
        public static readonly Vector2 Pond = new Vector2(-28f, -14f);

        public static readonly Vector2 HenYard = new Vector2(22f, -5f);

        // Phase 3: the village garden beside the hen yard (south-east of the Heart) and the old windmill on the northern rise.
        public static readonly Vector2 Garden = new Vector2(16f, -14f);
        public static readonly Vector2 Windmill = new Vector2(6f, 32f);
        public static readonly Vector2 GardenKeeper = Garden + new Vector2(-5.4f, -1.4f);        // by the garden's west gate
        public static readonly Vector2 WindmillRestorer = Windmill + new Vector2(-3.8f, -3.6f);   // at the windmill's foot
        public const float WindmillYaw = 180f;                                                    // its front (local +Z) faces the Heart

        // Renewable gathering spots, spread so exploring the valley is how the quest gets done.
        public static readonly Vector2[] HerbSpots =
        {
            new Vector2(-19.5f, 8.5f), new Vector2(-20.5f, 0.5f), new Vector2(-19f, -8f),
            new Vector2(-6f, -19f), new Vector2(8f, -17f), new Vector2(-12f, -26f)
        };

        public static readonly Vector2[] FlowerSpots =
        {
            new Vector2(13f, -3.5f), new Vector2(18f, 1f), new Vector2(10f, 10.5f), new Vector2(3f, 14f), new Vector2(-9f, -13f)
        };

        public static readonly Vector2[] OreSpots =
        {
            new Vector2(35f, 18f), new Vector2(37f, -8f), new Vector2(-38f, 22f), new Vector2(-33f, -30f)
        };

        static readonly Vector2[] PathSouth ={ new Vector2(0f, -32f), new Vector2(0f, -21f), new Vector2(0f, -9f) };
        static readonly Vector2[] PathEast = { new Vector2(9f, 1f), new Vector2(15f, 4f), new Vector2(21f, 8f) };
        static readonly Vector2[] PathWest = { new Vector2(-8f, 5f), new Vector2(-14f, 11f), new Vector2(-19f, 15f) };
        static readonly Vector2[] PathNorth = { new Vector2(0f, 9f), new Vector2(1.5f, 19f), new Vector2(4.5f, 28.5f) };
        static readonly Vector2[] PathGarden = { new Vector2(7f, -6f), new Vector2(9.5f, -9.5f), new Vector2(11f, -13f) };
        static readonly Vector2[][] Paths = { PathSouth, PathEast, PathWest, PathNorth, PathGarden };

        public static readonly List<Vector2> Stream = Resample(new[]
        {
            new Vector2(-22.6f, 14.2f), new Vector2(-24.5f, 10f), new Vector2(-27f, 5f),
            new Vector2(-27.5f, -1f), new Vector2(-28.5f, -7f), new Vector2(-28f, -12f)
        }, 8);

        public static float Smooth(float from, float to, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));

        /// <summary>1 inside "full", fading to 0 at "fade".</summary>
        public static float Mask(float distance, float full, float fade) => 1f - Smooth(full, fade, distance);

        public static float Height(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float d = p.magnitude;

            float wall = Smooth(36f, 62f, d) * 17f;
            float calm = Smooth(8f, 30f, d);
            float hills = Mathf.Pow(Mathf.PerlinNoise(x * 0.045f + 31.7f, z * 0.045f + 11.3f), 1.6f) * 5.5f * calm;
            float rolls = Mathf.PerlinNoise(x * 0.12f + 5.1f, z * 0.12f + 72.9f) * 0.7f * calm;
            float natural = wall + hills + rolls;

            float flat = Mask(d, 15f, 25f);
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, Spawn), 8f, 16f));
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, Cottage), 10f, 18f));
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, Spring), 7f, 14f));
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, Pond), 12f, 20f));
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, HenYard), 5f, 11f));
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, Garden), 8f, 15f));
            flat = Mathf.Max(flat, Mask(Vector2.Distance(p, Windmill), 9f, 18f));
            flat = Mathf.Max(flat, Mask(DistanceToPaths(p), 3f, 8f));
            flat = Mathf.Max(flat, Mask(DistanceToPolyline(p, Stream), 5f, 11f));

            float height = Mathf.Lerp(natural, 0f, flat);

            float streamCut = 0.8f * (1f - Smooth(0.5f, 2.2f, DistanceToPolyline(p, Stream)));
            float pondCut = 1.0f * (1f - Smooth(3f, 8f, Vector2.Distance(p, Pond)));
            return height - Mathf.Max(streamCut, pondCut);
        }

        /// <summary>Terrain layer weights: 0 grass, 1 meadow, 2 path/sand, 3 stone. They always sum to 1.</summary>
        public static void Weights(float x, float z, float[] w)
        {
            Vector2 p = new Vector2(x, z);
            float meadow = Smooth(0.35f, 0.65f, Mathf.PerlinNoise(x * 0.06f + 9.1f, z * 0.06f + 3.7f));
            w[0] = 1f - meadow;
            w[1] = meadow;
            w[2] = 0f;
            w[3] = 0f;

            float dx = Height(x + 0.8f, z) - Height(x - 0.8f, z);
            float dz = Height(x, z + 0.8f) - Height(x, z - 0.8f);
            float slope = Mathf.Sqrt(dx * dx + dz * dz) / 1.6f;
            Blend(w, 3, Smooth(0.85f, 1.3f, slope));

            Blend(w, 2, Mask(DistanceToPaths(p), 1.1f, 2.0f));
            Blend(w, 2, 0.9f * Mask(DistanceToPolyline(p, Stream), 2.0f, 3.4f));
            Blend(w, 2, 0.9f * Mask(Vector2.Distance(p, Pond), PondRadius + 0.5f, PondRadius + 2.4f));
            Blend(w, 3, Mask(p.magnitude, PlazaRadius - 1f, PlazaRadius + 0.8f));
            Blend(w, 2, 0.9f * Mask(Vector2.Distance(p, Windmill), 3.6f, 5.6f));   // trodden earth around the windmill's foot
        }

        static void Blend(float[] w, int layer, float amount)
        {
            for (int i = 0; i < w.Length; i++) w[i] *= 1f - amount;
            w[layer] += amount;
        }

        public static float DistanceToPaths(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (Vector2[] path in Paths)
                best = Mathf.Min(best, DistanceToPolyline(p, path));
            return best;
        }

        public static float DistanceToPolyline(Vector2 p, IList<Vector2> points)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Count - 1; i++)
                best = Mathf.Min(best, DistanceToSegment(p, points[i], points[i + 1]));
            return best;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>True when trees, rocks and flowers may stand here without blocking paths, buildings or water.</summary>
        public static bool IsFreeForScenery(Vector2 p, float extra = 0f)
        {
            if (Mathf.Abs(p.x) > PlayableHalfSize - 3f || Mathf.Abs(p.y) > PlayableHalfSize - 3f) return false;
            if (p.magnitude < PlazaRadius + 5f + extra) return false;
            if (Vector2.Distance(p, Spawn) < 7f + extra) return false;
            if (Vector2.Distance(p, Cottage) < 9f + extra) return false;
            if (Vector2.Distance(p, Villager) < 3f + extra) return false;
            if (Vector2.Distance(p, Spring) < 6f + extra) return false;
            if (Vector2.Distance(p, Pond) < PondRadius + 3f + extra) return false;
            if (DistanceToPaths(p) < 3f + extra) return false;
            if (NearGameplaySpot(p, 3f + extra)) return false;
            return DistanceToPolyline(p, Stream) >= 3.5f + extra;
        }

        /// <summary>True near a resource node or the hen yard, so decoration never hides or blocks gameplay objects.</summary>
        public static bool NearGameplaySpot(Vector2 p, float radius)
        {
            if (Vector2.Distance(p, HenYard) < radius + 3f) return true;
            if (Vector2.Distance(p, Garden) < radius + 6f) return true;
            if (Vector2.Distance(p, Windmill) < radius + 8f) return true;
            if (Vector2.Distance(p, GardenKeeper) < radius) return true;
            if (Vector2.Distance(p, WindmillRestorer) < radius) return true;
            foreach (Vector2 spot in HerbSpots) if (Vector2.Distance(p, spot) < radius) return true;
            foreach (Vector2 spot in FlowerSpots) if (Vector2.Distance(p, spot) < radius) return true;
            foreach (Vector2 spot in OreSpots) if (Vector2.Distance(p, spot) < radius) return true;
            return false;
        }

        /// <summary>Catmull-Rom resampling of a control polyline.</summary>
        public static List<Vector2> Resample(Vector2[] control, int subdivisions)
        {
            List<Vector2> result = new List<Vector2>();
            for (int i = 0; i < control.Length - 1; i++)
            {
                Vector2 p0 = control[Mathf.Max(i - 1, 0)];
                Vector2 p1 = control[i];
                Vector2 p2 = control[i + 1];
                Vector2 p3 = control[Mathf.Min(i + 2, control.Length - 1)];
                for (int s = 0; s < subdivisions; s++)
                {
                    float t = s / (float)subdivisions;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }
            result.Add(control[control.Length - 1]);
            return result;
        }
    }
}
