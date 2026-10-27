using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>Bakes the original Blender-authored caretaker (vertices, weights, rest joints, clips) into Unity-owned native assets.</summary>
    public static class Phase52HeroBuilder
    {
        public const string Folder = "Assets/Echoes/Art/Phase52";
        public const string PrefabPath = Folder + "/CaretakerSkin.prefab";
        public const string ShaderName = "Echoes/ToonCharacter";
        [Serializable] sealed class Export { public float strideMetres, cycleSeconds, speed; public Bone[] bones; public string[] materials; public Sub[] submeshes; public Clip[] clips; }
        [Serializable] sealed class Bone { public string name, parent; public float[] position; }
        [Serializable] sealed class Sub { public string material; public float[] vertices, normals, uv, colors, outline, weights; public int[] triangles, boneIndices; }
        [Serializable] sealed class Clip { public string name; public float length, fps; public int layer; public Track[] tracks; }
        [Serializable] sealed class Track { public string bone; public float[] rotation, position; }

        /// <summary>Per-material toon response. Colours are sRGB hex; shade tints are multiplied into the albedo in shadow.</summary>
        sealed class Look
        {
            public string Shade = "AE9AC8", Outline = "211310", Rim = "FFF0D6";
            public float RimStrength = .30f, Highlight, HighlightPower = 40, OutlineWidth = 1.6f, Ao = .85f, OutlineMix = .45f, Ambient = 1.10f, Fill = .20f;
            public bool AlphaClip;
        }

        static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
        {
            ["Skin"] = new Look { Shade = "F4C1B6", Outline = "5A2E26", RimStrength = .34f, Ao = .55f, OutlineWidth = 1.3f, OutlineMix = .30f, Fill = .55f },
            ["Hair"] = new Look { Shade = "A9809C", Outline = "241510", RimStrength = .46f, Highlight = .22f, HighlightPower = 26, OutlineWidth = 1.5f, Ao = .90f, Fill = .38f },
            ["Teal"] = new Look { Shade = "86A6C8", Outline = "0B2A2B", RimStrength = .32f, OutlineWidth = 1.6f },
            ["Cream"] = new Look { Shade = "CDB7DA", Outline = "5B3E2C", RimStrength = .28f, OutlineWidth = 1.5f, Ao = .90f },
            ["Burgundy"] = new Look { Shade = "B58BB4", Outline = "2D0B16", RimStrength = .30f, AlphaClip = true, OutlineWidth = 1.4f },
            ["Leather"] = new Look { Shade = "A58590", Outline = "1A0F0B", Highlight = .30f, HighlightPower = 22, OutlineWidth = 1.5f },
            ["Tights"] = new Look { Shade = "9A869E", Outline = "140C0B", RimStrength = .38f, OutlineWidth = 1.4f },
            ["Trinket"] = new Look { Shade = "B49EA6", Outline = "1F1409", Highlight = .85f, HighlightPower = 70, OutlineWidth = 1.0f, Ao = .70f },
        };

        public static readonly string[] ObsoleteFirstPilotMaterials = { "Ear", "EyeWhite", "Face", "Glass", "Glint", "Gold", "HairLight", "Iris", "IrisLight", "Lash", "LeatherEdge", "Legging", "Lips", "Pupil", "TealLight" };

        public static void Bake()
        {
            AssetPaths.EnsureFolder(Folder);
            AssetPaths.EnsureFolder(Folder + "/Animations");
            AssetPaths.EnsureFolder(Folder + "/Textures");
            Export data = JsonUtility.FromJson<Export>(File.ReadAllText(Folder + "/Source/hero-skin.json"));
            if (data.bones == null || data.bones.Length < 40) throw new InvalidDataException("Missing anatomical skeleton.");
            foreach (string stale in ObsoleteFirstPilotMaterials) AssetDatabase.DeleteAsset(Folder + "/Hero_" + stale + ".mat"); // flat-colour first-pilot materials, kept in git history
            GameObject root = new GameObject("CaretakerSkin");
            var bones = new Dictionary<string, Transform>();
            try
            {
                Transform armature = new GameObject("Armature").transform;
                armature.SetParent(root.transform, false);
                foreach (Bone b in data.bones)
                {
                    Transform node = new GameObject(b.name).transform;
                    node.SetParent(string.IsNullOrEmpty(b.parent) ? armature : bones[b.parent], false);
                    node.localPosition = new Vector3(b.position[0], b.position[1], b.position[2]);
                    node.localRotation = Quaternion.identity;
                    bones[b.name] = node;
                }
                Transform[] ordered = data.bones.Select(b => bones[b.name]).ToArray();
                var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var outline = new List<Vector2>(); var weights = new List<BoneWeight>();
                var triangles = new List<int>[data.submeshes.Length];
                for (int s = 0; s < data.submeshes.Length; s++)
                {
                    Sub sub = data.submeshes[s];
                    int offset = vertices.Count, count = sub.vertices.Length / 3;
                    for (int i = 0; i < count; i++)
                    {
                        vertices.Add(new Vector3(sub.vertices[i * 3], sub.vertices[i * 3 + 1], sub.vertices[i * 3 + 2]));
                        normals.Add(new Vector3(sub.normals[i * 3], sub.normals[i * 3 + 1], sub.normals[i * 3 + 2]));
                        uv.Add(new Vector2(sub.uv[i * 2], sub.uv[i * 2 + 1]));
                        colors.Add(new Color(sub.colors[i * 4], sub.colors[i * 4 + 1], sub.colors[i * 4 + 2], sub.colors[i * 4 + 3]));
                        outline.Add(new Vector2(sub.outline[i], 0));
                        weights.Add(new BoneWeight { boneIndex0 = sub.boneIndices[i * 4], boneIndex1 = sub.boneIndices[i * 4 + 1], boneIndex2 = sub.boneIndices[i * 4 + 2], boneIndex3 = sub.boneIndices[i * 4 + 3], weight0 = sub.weights[i * 4], weight1 = sub.weights[i * 4 + 1], weight2 = sub.weights[i * 4 + 2], weight3 = sub.weights[i * 4 + 3] });
                    }
                    triangles[s] = sub.triangles.Select(t => t + offset).ToList();
                }
                string meshPath = Folder + "/CaretakerWeighted.asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
                mesh.Clear(); mesh.name = "Caretaker_OriginalWeightedSurface";
                mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetUVs(2, outline);
                mesh.boneWeights = weights.ToArray();
                mesh.bindposes = ordered.Select(b => b.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray();
                mesh.subMeshCount = data.submeshes.Length;
                for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                var renderer = root.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh; renderer.bones = ordered; renderer.rootBone = bones["Root"];
                renderer.localBounds = new Bounds(new Vector3(0, .85f, -.05f), new Vector3(1.4f, 1.95f, 1.2f));
                renderer.quality = SkinQuality.Bone4; renderer.updateWhenOffscreen = false;
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                renderer.sharedMaterials = data.submeshes.Select(s => Material(s.material)).ToArray();
                Animation animation = root.AddComponent<Animation>(); animation.playAutomatically = false; animation.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (Clip c in data.clips)
                {
                    AnimationClip clip = BakeClip(c, bones, root.transform);
                    animation.AddClip(clip, c.name);
                    if (c.name == "Idle") animation.clip = clip;
                }
                var motion = root.AddComponent<SkinnedCaretakerMotion>(); Wire.Set(motion, "strideMetres", data.strideMetres);
                root.AddComponent<CaretakerFace>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Echoes][P52B2] native skin baked: bones=" + ordered.Length + " vertices=" + vertices.Count + " triangles=" + triangles.Sum(t => t.Count / 3) + " materials=" + data.submeshes.Length + " clips=" + data.clips.Length + " stride=" + data.strideMetres);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static AnimationClip BakeClip(Clip data, Dictionary<string, Transform> bones, Transform root)
        {
            string path = Folder + "/Animations/" + data.name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves(); clip.name = data.name; clip.legacy = true; clip.frameRate = data.fps; clip.wrapMode = WrapMode.Loop;
            foreach (Track track in data.tracks)
            {
                string binding = AnimationUtility.CalculateTransformPath(bones[track.bone], root);
                string[] rot = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
                int keys = track.rotation.Length / 5;
                for (int p = 0; p < 4; p++)
                    clip.SetCurve(binding, typeof(Transform), rot[p], Linear(Enumerable.Range(0, keys).Select(k => new Keyframe(track.rotation[k * 5], track.rotation[k * 5 + 1 + p])).ToArray()));
                if (track.position != null && track.position.Length > 0)
                {
                    string[] pos = { "localPosition.x", "localPosition.y", "localPosition.z" };
                    int pk = track.position.Length / 4;
                    for (int p = 0; p < 3; p++)
                        clip.SetCurve(binding, typeof(Transform), pos[p], Linear(Enumerable.Range(0, pk).Select(k => new Keyframe(track.position[k * 4], track.position[k * 4 + 1 + p])).ToArray()));
                }
            }
            clip.EnsureQuaternionContinuity(); EditorUtility.SetDirty(clip); return clip;
        }

        static AnimationCurve Linear(Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
            return curve;
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }

        static Material Material(string name)
        {
            string path = Folder + "/Hero_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(ShaderName);
            if (shader == null) throw new FileNotFoundException("Missing shader " + ShaderName);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            Look look = Looks[name];
            string texturePath = Folder + "/Textures/" + name + ".png";
            ConfigureTexture(texturePath, look.AlphaClip || name == "Hair");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            string blinkPath = Folder + "/Textures/" + name + "_Blink.png";
            if (File.Exists(blinkPath)) { ConfigureTexture(blinkPath, false); material.SetTexture("_BlinkMap", AssetDatabase.LoadAssetAtPath<Texture2D>(blinkPath)); }
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ShadeColor", Hex(look.Shade));
            material.SetColor("_OutlineColor", Hex(look.Outline));
            material.SetColor("_RimColor", Hex(look.Rim));
            material.SetFloat("_RimStrength", look.RimStrength);
            material.SetFloat("_HighlightStrength", look.Highlight);
            material.SetFloat("_HighlightPower", look.HighlightPower);
            material.SetFloat("_OutlineWidth", look.OutlineWidth);
            material.SetFloat("_OutlineTint", look.OutlineMix);
            material.SetFloat("_AoStrength", look.Ao);
            material.SetFloat("_Ambient", look.Ambient);
            material.SetFloat("_CameraFill", look.Fill);
            material.SetFloat("_Cull", 2);
            material.SetFloat("_AlphaClip", look.AlphaClip ? 1 : 0);
            if (look.AlphaClip) material.EnableKeyword("_ALPHATEST_ON"); else material.DisableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cutoff", .5f);
            material.enableInstancing = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Painted sRGB colour textures with mipmaps; textures that carry shape in alpha preserve coverage in their mips.</summary>
        public static void ConfigureTexture(string path, bool cutout)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Missing painted texture " + path + "; run tools/art/phase52b2/textures.py");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.anisoLevel = 8;
            importer.filterMode = FilterMode.Trilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = cutout;
            importer.mipMapsPreserveCoverage = cutout; importer.alphaTestReferenceValue = .5f;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.maxTextureSize = 2048; importer.crunchedCompression = false;
            importer.SaveAndReimport();
        }
    }
}
