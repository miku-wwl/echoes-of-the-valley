using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>Bakes original Blender vertices, weights, bind transforms and actions into Unity-owned native assets.</summary>
    public static class Phase52HeroBuilder
    {
        public const string Folder = "Assets/Echoes/Art/Phase52";
        public const string PrefabPath = Folder + "/CaretakerSkin.prefab";
        [Serializable] sealed class Export { public Bone[] bones; public Part[] parts; public Clip[] clips; public Surface[] materials; public float strideMetres; }
        [Serializable] sealed class Bone { public string name, parent; public float[] position, rotation; }
        [Serializable] sealed class Part { public string name, material; public float[] vertices, normals, uv, weights; public int[] triangles, boneIndices; }
        [Serializable] sealed class Surface { public string name, color; }
        [Serializable] sealed class Clip { public string name; public float length; public Track[] tracks; }
        [Serializable] sealed class Track { public string bone; public Key[] keys; }
        [Serializable] sealed class Key { public float time; public float[] values; }

        public static void Bake()
        {
            AssetPaths.EnsureFolder(Folder);
            AssetPaths.EnsureFolder(Folder + "/Animations");
            Export data = JsonUtility.FromJson<Export>(File.ReadAllText(Folder + "/Source/hero-skin.json"));
            if (data.bones == null || data.bones.Length < 20) throw new InvalidDataException("Missing anatomical skeleton.");
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
                    node.localRotation = new Quaternion(b.rotation[0], b.rotation[1], b.rotation[2], b.rotation[3]);
                    bones[b.name] = node;
                }
                Transform[] ordered = data.bones.Select(b => bones[b.name]).ToArray();
                var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var weights = new List<BoneWeight>();
                string[] names = data.parts.Select(p => p.material).Distinct().ToArray();
                var triangles = names.ToDictionary(n => n, n => new List<int>());
                foreach (Part p in data.parts)
                {
                    int offset = vertices.Count;
                    for (int i = 0; i < p.vertices.Length / 3; i++)
                    {
                        vertices.Add(new Vector3(p.vertices[i * 3], p.vertices[i * 3 + 1], p.vertices[i * 3 + 2]));
                        normals.Add(new Vector3(p.normals[i * 3], p.normals[i * 3 + 1], p.normals[i * 3 + 2]));
                        uv.Add(new Vector2(p.uv[i * 2], p.uv[i * 2 + 1]));
                        weights.Add(new BoneWeight { boneIndex0=p.boneIndices[i*4], boneIndex1=p.boneIndices[i*4+1], boneIndex2=p.boneIndices[i*4+2], boneIndex3=p.boneIndices[i*4+3], weight0=p.weights[i*4], weight1=p.weights[i*4+1], weight2=p.weights[i*4+2], weight3=p.weights[i*4+3] });
                    }
                    triangles[p.material].AddRange(p.triangles.Select(i => i + offset));
                }
                string meshPath = Folder + "/CaretakerWeighted.asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
                mesh.Clear(); mesh.name = "Caretaker_OriginalWeightedSurface";
                mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.boneWeights = weights.ToArray();
                mesh.bindposes = ordered.Select(b => b.worldToLocalMatrix * root.transform.localToWorldMatrix).ToArray();
                mesh.subMeshCount = names.Length;
                for (int i=0;i<names.Length;i++) mesh.SetTriangles(triangles[names[i]],i);
                mesh.RecalculateBounds(); mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh);
                var renderer = root.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh; renderer.bones = ordered; renderer.rootBone = bones["Root"];
                renderer.localBounds = new Bounds(new Vector3(0,.88f,0),new Vector3(1.6f,2.0f,1.4f));
                renderer.quality = SkinQuality.Bone4; renderer.updateWhenOffscreen = false;
                renderer.sharedMaterials = names.Select(n => Material(data.materials.Single(s=>s.name==n))).ToArray();
                Animation animation = root.AddComponent<Animation>(); animation.playAutomatically=false; animation.cullingType=AnimationCullingType.AlwaysAnimate;
                foreach (Clip c in data.clips)
                {
                    AnimationClip clip = BakeClip(c, bones, root.transform);
                    animation.AddClip(clip,c.name);
                    if (c.name=="Idle") animation.clip=clip;
                }
                var motion=root.AddComponent<SkinnedCaretakerMotion>(); Wire.Set(motion,"strideMetres",data.strideMetres);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Echoes][P52] native skin baked: bones="+ordered.Length+" vertices="+vertices.Count+" triangles="+triangles.Sum(t=>t.Value.Count/3)+" clips="+data.clips.Length);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static AnimationClip BakeClip(Clip data, Dictionary<string,Transform> bones, Transform root)
        {
            string path=Folder+"/Animations/"+data.name+".anim";
            AnimationClip clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.ClearCurves();clip.name=data.name;clip.legacy=true;clip.frameRate=30;clip.wrapMode=WrapMode.Loop;
            foreach(Track track in data.tracks)
            {
                string binding=AnimationUtility.CalculateTransformPath(bones[track.bone],root);
                string[] properties={"localPosition.x","localPosition.y","localPosition.z","localRotation.x","localRotation.y","localRotation.z","localRotation.w"};
                for(int p=0;p<properties.Length;p++)
                {
                    var keys=track.keys.Select(k=>new Keyframe(k.time,k.values[p])).ToArray();var curve=new AnimationCurve(keys);
                    for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                    clip.SetCurve(binding,typeof(Transform),properties[p],curve);
                }
            }
            clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);return clip;
        }

        static Material Material(Surface surface)
        {
            string path=Folder+"/Hero_"+surface.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            ColorUtility.TryParseHtmlString("#"+surface.color,out Color color);
            material.SetColor("_BaseColor",color);
            material.SetFloat("_Smoothness",surface.name=="Gold"?.45f:surface.name.StartsWith("Leather")?.24f:.13f);
            material.SetFloat("_Metallic",surface.name=="Gold"?.5f:0);
            material.SetColor("_EmissionColor",color*(surface.name=="Face"||surface.name=="Skin"?.065f:.018f));material.EnableKeyword("_EMISSION");
            material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }
    }
}
