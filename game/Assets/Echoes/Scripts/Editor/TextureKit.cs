using UnityEditor;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Small tileable textures made from code: the ground layers of the terrain, the flowing-water streaks and the soft round
    /// particle glow. They are written into the same asset files on every run (stable GUIDs) and are tiny: 128 x 128 at most.
    /// </summary>
    internal static class TextureKit
    {
        /// <summary>Wrapping value noise: octaves of a random lattice whose edges meet, so the texture repeats without seams.</summary>
        sealed class NoiseField
        {
            readonly float[][,] lattices;
            readonly int[] cellsX;
            readonly int[] cellsY;

            public NoiseField(int seed, int cellsX0, int cellsY0, int octaves)
            {
                System.Random random = new System.Random(seed);
                lattices = new float[octaves][,];
                cellsX = new int[octaves];
                cellsY = new int[octaves];
                for (int o = 0; o < octaves; o++)
                {
                    cellsX[o] = cellsX0 << o;
                    cellsY[o] = cellsY0 << o;
                    lattices[o] = new float[cellsX[o], cellsY[o]];
                    for (int x = 0; x < cellsX[o]; x++)
                        for (int y = 0; y < cellsY[o]; y++)
                            lattices[o][x, y] = (float)random.NextDouble();
                }
            }

            /// <summary>0..1, for u and v in [0, 1).</summary>
            public float Sample(float u, float v)
            {
                float sum = 0f;
                float weight = 1f;
                float total = 0f;
                for (int o = 0; o < lattices.Length; o++)
                {
                    float x = u * cellsX[o];
                    float y = v * cellsY[o];
                    int x0 = Mathf.FloorToInt(x);
                    int y0 = Mathf.FloorToInt(y);
                    float fx = x - x0;
                    float fy = y - y0;
                    fx = fx * fx * (3f - 2f * fx);
                    fy = fy * fy * (3f - 2f * fy);
                    int x1 = (x0 + 1) % cellsX[o];
                    int y1 = (y0 + 1) % cellsY[o];
                    x0 %= cellsX[o];
                    y0 %= cellsY[o];
                    float value = Mathf.Lerp(Mathf.Lerp(lattices[o][x0, y0], lattices[o][x1, y0], fx),
                        Mathf.Lerp(lattices[o][x0, y1], lattices[o][x1, y1], fx), fy);
                    sum += value * weight;
                    total += weight;
                    weight *= 0.5f;
                }
                return sum / total;
            }
        }

        static Texture2D LoadOrCreate(string path, int width, int height, TextureFormat format)
        {
            AssetPaths.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(width, height, format, true, false);
                AssetDatabase.CreateAsset(texture, path);
            }
            texture.Reinitialize(width, height, format, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        static void Finish(Texture2D texture, Color[] pixels, int anisotropy = 1)
        {
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            texture.anisoLevel = anisotropy;
            EditorUtility.SetDirty(texture);
        }

        /// <summary>
        /// A ground tile: the base colour with soft blotches of lighter and darker tone and a sprinkle of specks (flower dots
        /// or pebbles). No alpha channel on purpose: URP's terrain shader locks smoothness to 1 when the diffuse has alpha.
        /// </summary>
        public static Texture2D Ground(string path, Color baseColor, float variation, int cells, int seed, Color speckle, float speckleChance)
        {
            const int size = 128;
            Texture2D texture = LoadOrCreate(path, size, size, TextureFormat.RGB24);
            NoiseField blotches = new NoiseField(seed, cells, cells, 3);
            System.Random random = new System.Random(seed * 31 + 7);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = blotches.Sample(x / (float)size, y / (float)size);
                    float tone = 1f + (n - 0.5f) * 2f * variation;
                    Color c = new Color(baseColor.r * tone, baseColor.g * tone * (1f + (n - 0.5f) * 0.1f), baseColor.b * tone, 1f);
                    if (speckleChance > 0f && random.NextDouble() < speckleChance)
                        c = Color.Lerp(c, speckle, 0.75f);
                    pixels[y * size + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            }
            Finish(texture, pixels, 4);
            return texture;
        }

        /// <summary>Light and dark streaks that are long along v (the direction a stream runs): scrolling them reads as flowing water.</summary>
        public static Texture2D WaterStreaks(string path)
        {
            const int size = 128;
            Texture2D texture = LoadOrCreate(path, size, size, TextureFormat.RGB24);
            NoiseField broad = new NoiseField(11, 14, 2, 2);
            NoiseField fine = new NoiseField(23, 30, 5, 2);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float a = broad.Sample(u, v);
                    float b = fine.Sample(u, v);
                    float value = 0.80f + 0.16f * (a - 0.5f) * 2f + 0.10f * (b - 0.5f) * 2f;
                    if (b > 0.64f) value += (b - 0.64f) * 1.6f;   // thin bright glints
                    value = Mathf.Clamp01(value);
                    pixels[y * size + x] = new Color(value * 0.94f, value * 0.98f, value, 1f);
                }
            }
            Finish(texture, pixels, 2);
            return texture;
        }

        /// <summary>A soft round dot, white with a smooth alpha falloff, for glowing particles.</summary>
        public static Texture2D GlowDot(string path)
        {
            const int size = 64;
            Texture2D texture = LoadOrCreate(path, size, size, TextureFormat.RGBA32);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }
    }
}
