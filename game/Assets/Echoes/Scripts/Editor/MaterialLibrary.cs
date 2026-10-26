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
            // (the Window glow material is defined with the other glowing details below)

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
            Lit("GrassBlade", 0.40f, 0.72f, 0.32f);
            Lit("GrassBladeLight", 0.60f, 0.83f, 0.38f);
            Lit("Reed", 0.46f, 0.66f, 0.30f);
            Lit("Water", 0.42f, 0.78f, 0.95f, 0.9f, shiny: true);
            Lit("LilyPad", 0.34f, 0.62f, 0.34f, 0.3f);
            Lit("GroundMenu", 0.46f, 0.66f, 0.38f);
            Lit("HillMenu", 0.40f, 0.60f, 0.42f);

            // Characters
            Lit("Skin", 0.99f, 0.87f, 0.76f);
            Lit("Blush", 1.00f, 0.66f, 0.66f);
            Lit("Eye", 0.12f, 0.10f, 0.14f, 0.5f);
            Lit("Boots", 0.40f, 0.28f, 0.22f);
            Lit("TunicHero", 0.20f, 0.62f, 0.64f);
            Lit("HairHero", 0.52f, 0.28f, 0.17f);
            Lit("HeroCape", 0.99f, 0.88f, 0.62f);
            Lit("Trousers", 0.60f, 0.46f, 0.34f);
            Lit("MouthCoral", 0.92f, 0.45f, 0.45f);
            Lit("Brow", 0.32f, 0.21f, 0.16f);
            Lit("Highlight", 1.00f, 1.00f, 1.00f);
            Lit("Scarf", 0.90f, 0.38f, 0.33f);
            Lit("TunicMira", 0.56f, 0.76f, 0.46f);
            Lit("HairMira", 0.84f, 0.64f, 0.36f);
            Lit("Cream", 0.98f, 0.94f, 0.84f);

            // Gameplay: gathering nodes, the hen, the murky water of the silent valley
            Lit("WaterDull", 0.52f, 0.56f, 0.44f, 0.25f);
            Lit("HerbLeaf", 0.20f, 0.74f, 0.64f);
            Lit("HerbDark", 0.14f, 0.42f, 0.38f);
            Lit("BlossomCoral", 1.00f, 0.54f, 0.36f);
            Lit("OreRock", 0.50f, 0.52f, 0.58f);
            Lit("ChickenWhite", 0.98f, 0.97f, 0.94f);
            Lit("ChickenComb", 0.90f, 0.26f, 0.26f);
            Lit("ChickenBeak", 1.00f, 0.70f, 0.20f);
            Lit("EggShell", 1.00f, 0.96f, 0.88f, 0.35f);
            Glow("BlossomGlow", new Color(1f, 0.80f, 0.42f), new Color(0.55f, 0.30f, 0.05f));
            Glow("OreCrystal", new Color(0.55f, 0.86f, 1f), new Color(0.10f, 0.42f, 0.58f), 0.7f);
            Glow("IconHungry", new Color(1f, 0.58f, 0.26f), new Color(0.45f, 0.18f, 0.02f));
            Glow("IconStar", new Color(1f, 0.92f, 0.45f), new Color(0.65f, 0.50f, 0.08f));

            // Phase 3: the garden (withered, then blooming), the windmill, the village's lanterns and the two new villagers
            Lit("SoilDry", 0.64f, 0.57f, 0.46f);
            Lit("SoilRich", 0.33f, 0.24f, 0.18f);
            Lit("Withered", 0.52f, 0.46f, 0.34f);
            Lit("WitheredLeaf", 0.60f, 0.57f, 0.38f);
            Lit("GardenGreen", 0.34f, 0.74f, 0.32f);
            Lit("GardenGreenLight", 0.58f, 0.86f, 0.40f);
            Lit("FlowerOrange", 1.00f, 0.60f, 0.20f);
            Lit("FlowerRed", 0.93f, 0.28f, 0.32f);
            Lit("FlowerBlue", 0.42f, 0.60f, 0.97f);
            Lit("SailCloth", 0.97f, 0.94f, 0.86f);
            Lit("BrokenWood", 0.36f, 0.29f, 0.23f);
            Lit("TunicKeeper", 0.97f, 0.66f, 0.44f);
            Lit("HairKeeper", 0.36f, 0.23f, 0.20f);
            Lit("TunicRestorer", 0.42f, 0.54f, 0.76f);
            Lit("HairRestorer", 0.82f, 0.82f, 0.84f);
            Lit("ScarfRestorer", 0.78f, 0.34f, 0.22f);
            Glow("LanternGlow", new Color(1f, 0.86f, 0.50f), new Color(1.0f, 0.62f, 0.18f), 0.4f);
            Glow("BraceGlow", new Color(0.60f, 0.88f, 1f), new Color(0.14f, 0.55f, 0.78f), 0.7f);

            // Glowing details (emission keyword stays enabled so the variant ships in the player)
            Glow("PotionGreen", new Color(0.45f, 0.85f, 0.55f), new Color(0.10f, 0.45f, 0.18f));
            Glow("PotionPurple", new Color(0.66f, 0.46f, 0.86f), new Color(0.25f, 0.10f, 0.40f));
            Glow("PotionBlue", new Color(0.40f, 0.66f, 0.96f), new Color(0.08f, 0.22f, 0.45f));
            Glow("Crystal", new Color(0.50f, 0.62f, 0.72f), Color.black, 0.7f);

            // Phase 4: windows that light up with the valley, clouds, water that flows and particles that glow
            Glow("Window", new Color(0.98f, 0.88f, 0.55f), Color.black, 0.4f);   // StageGlow drives its emission
            Glow("LanternLamp", new Color(1f, 0.90f, 0.62f), new Color(0.9f, 0.55f, 0.15f), 0.3f);
            Material cloud = Create("Cloud", FindShader("Universal Render Pipeline/Unlit"));
            cloud.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(cloud);

            AssetPaths.EnsureFolder(AssetPaths.Textures);
            Texture2D streaks = TextureKit.WaterStreaks(AssetPaths.Textures + "/WaterFlow.asset");
            foreach (string waterName in new[] { "Water", "WaterDull" })
            {
                materials[waterName].SetTexture("_BaseMap", streaks);
                materials[waterName].SetTextureScale("_BaseMap", Vector2.one);
                EditorUtility.SetDirty(materials[waterName]);
            }
            Texture2D dot = TextureKit.GlowDot(AssetPaths.Textures + "/GlowDot.asset");
            Particle("ParticleGlow", dot, additive: true);
            Particle("ParticleSoft", dot, additive: false);

            // Terrain and skies
            Material terrain = Create("TerrainLit", FindShader("Universal Render Pipeline/Terrain/Lit"));
            EditorUtility.SetDirty(terrain);
            // The silent valley's sky is hazy and muted but never dark; ValleyLighting moves it towards a golden hour.
            GradientSky("SkyValley", new Color(0.45f, 0.57f, 0.76f), new Color(0.80f, 0.85f, 0.89f), new Color(0.44f, 0.50f, 0.40f), 0.95f);
            GradientSky("SkyMenu", new Color(0.20f, 0.17f, 0.42f), new Color(1.00f, 0.62f, 0.42f), new Color(0.24f, 0.20f, 0.22f), 1.0f);

            AssetDatabase.SaveAssets();
        }

        /// <summary>URP particle material from the pipeline's own particle shader: soft round dots, additive (light) or alpha-blended (smoke, pollen).</summary>
        void Particle(string name, Texture2D dot, bool additive)
        {
            Material material = Create(name, FindShader("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", dot);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);                     // transparent
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", 5f);                    // SrcAlpha
            material.SetFloat("_DstBlend", additive ? 1f : 10f);   // One, or OneMinusSrcAlpha
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
        }

        void GradientSky(string name, Color zenith, Color horizon, Color ground, float exposure)
        {
            Material material = Create(name, FindShader("Echoes/GradientSky"));
            material.SetColor("_ZenithColor", zenith);
            material.SetColor("_HorizonColor", horizon);
            material.SetColor("_GroundColor", ground);
            material.SetColor("_SunColor", new Color(1f, 0.92f, 0.75f));
            material.SetVector("_SunDir", new Vector4(0.45f, 0.62f, 0.64f, 0f));
            material.SetFloat("_SunSize", 0.9985f);
            material.SetFloat("_Exposure", exposure);
            material.SetFloat("_Falloff", 0.7f);
            EditorUtility.SetDirty(material);
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

    }
}
