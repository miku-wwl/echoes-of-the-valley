using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>Creates or updates every material used by the generated content (URP Lit, flat colors, one shader per kind).</summary>
    internal sealed class MaterialLibrary
    {
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Shader lit;

        public Material this[string name]
        {
            get
            {
                if (!materials.TryGetValue(name, out Material material))
                    throw new KeyNotFoundException("Unknown material '" + name + "'");
                return material;
            }
        }

        public MaterialLibrary()
        {
            AssetPaths.EnsureFolder(AssetPaths.Materials);
            lit = FindShader("Universal Render Pipeline/Lit");

            // Stone, wood, plaster
            Lit("Stone", 0.72f, 0.70f, 0.66f);
            Lit("StoneDark", 0.50f, 0.50f, 0.54f);
            Lit("StoneMoss", 0.56f, 0.64f, 0.50f);
            Lit("Mud", 0.43f, 0.34f, 0.26f);
            Lit("Wood", 0.58f, 0.40f, 0.26f);
            Lit("WoodDark", 0.40f, 0.28f, 0.20f);
            Lit("Plaster", 0.96f, 0.91f, 0.80f);
            Lit("Roof", 0.76f, 0.37f, 0.31f);
            Lit("Straw", 0.93f, 0.80f, 0.46f);
            Lit("Iron", 0.24f, 0.25f, 0.29f, 0.35f);
            Lit("Window", 0.98f, 0.88f, 0.55f, 0.4f);

            // Nature
            Lit("Trunk", 0.52f, 0.37f, 0.27f);
            Lit("LeafA", 0.40f, 0.70f, 0.40f);
            Lit("LeafB", 0.60f, 0.80f, 0.42f);
            Lit("Blossom", 0.98f, 0.72f, 0.80f);
            Lit("BlossomLight", 1.00f, 0.86f, 0.90f);
            Lit("Rock", 0.63f, 0.63f, 0.61f);
            Lit("Stem", 0.31f, 0.56f, 0.29f);
            Lit("FlowerPink", 0.98f, 0.52f, 0.66f);
            Lit("FlowerYellow", 1.00f, 0.85f, 0.30f);
            Lit("FlowerWhite", 0.98f, 0.97f, 0.94f);
            Lit("FlowerLilac", 0.72f, 0.60f, 0.95f);
            Lit("Water", 0.36f, 0.70f, 0.88f, 0.9f, shiny: true);
            Lit("LilyPad", 0.34f, 0.62f, 0.34f, 0.3f);
            Lit("GroundMenu", 0.46f, 0.66f, 0.38f);
            Lit("HillMenu", 0.40f, 0.60f, 0.42f);

            // Characters
            Lit("Skin", 0.99f, 0.87f, 0.76f);
            Lit("Blush", 1.00f, 0.66f, 0.66f);
            Lit("Eye", 0.12f, 0.10f, 0.14f, 0.5f);
            Lit("Boots", 0.40f, 0.28f, 0.22f);
            Lit("TunicHero", 0.34f, 0.64f, 0.80f);
            Lit("HairHero", 0.52f, 0.28f, 0.17f);
            Lit("Scarf", 0.90f, 0.38f, 0.33f);
            Lit("TunicMira", 0.56f, 0.76f, 0.46f);
            Lit("HairMira", 0.84f, 0.64f, 0.36f);
            Lit("Cream", 0.98f, 0.94f, 0.84f);

            // Glowing details (emission keyword stays enabled so the variant ships in the player)
            Glow("PotionGreen", new Color(0.45f, 0.85f, 0.55f), new Color(0.10f, 0.45f, 0.18f));
            Glow("PotionPurple", new Color(0.66f, 0.46f, 0.86f), new Color(0.25f, 0.10f, 0.40f));
            Glow("PotionBlue", new Color(0.40f, 0.66f, 0.96f), new Color(0.08f, 0.22f, 0.45f));
            Glow("Crystal", new Color(0.50f, 0.62f, 0.72f), Color.black, 0.7f);

            // Terrain and skies
            Material terrain = Create("TerrainLit", FindShader("Universal Render Pipeline/Terrain/Lit"));
            EditorUtility.SetDirty(terrain);
            Sky("SkyValley", new Color(0.46f, 0.54f, 0.64f), 1.05f, 0.9f);
            Sky("SkyMenu", new Color(0.50f, 0.50f, 0.50f), 1.0f, 2.4f);

            AssetDatabase.SaveAssets();
        }

        static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Shader not found: " + name + " (is URP installed?)");
            return shader;
        }

        Material Create(string name, Shader shader)
        {
            string path = AssetPaths.Materials + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            materials[name] = material;
            return material;
        }

        void Lit(string name, float r, float g, float b, float smoothness = 0.08f, bool shiny = false)
        {
            Material material = Create(name, lit);
            material.SetColor("_BaseColor", new Color(r, g, b));
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
            SetReflective(material, shiny);
            EditorUtility.SetDirty(material);
        }

        /// <summary>Stylized look and cheaper shading: matte materials skip specular highlights and environment reflections.</summary>
        static void SetReflective(Material material, bool shiny)
        {
            material.SetFloat("_SpecularHighlights", shiny ? 1f : 0f);
            material.SetFloat("_EnvironmentReflections", shiny ? 1f : 0f);
            if (shiny)
            {
                material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
            else
            {
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
        }

        void Glow(string name, Color color, Color emission, float smoothness = 0.5f)
        {
            Material material = Create(name, lit);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            SetReflective(material, false);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
        }

        void Sky(string name, Color tint, float exposure, float thickness)
        {
            Material material = Create(name, FindShader("Skybox/Procedural"));
            material.SetColor("_SkyTint", tint);
            material.SetColor("_GroundColor", new Color(0.42f, 0.44f, 0.40f));
            material.SetFloat("_Exposure", exposure);
            material.SetFloat("_AtmosphereThickness", thickness);
            material.SetFloat("_SunSize", 0.05f);
            material.SetFloat("_SunSizeConvergence", 5f);
            EditorUtility.SetDirty(material);
        }
    }
}
