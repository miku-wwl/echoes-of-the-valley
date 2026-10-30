using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>Unity serializes every generated scene, material, mesh and metadata. Only the new Phase53 pilot is edited.</summary>
    public static class Phase53Builder
    {
        public const string Folder = "Assets/Echoes/Art/Phase53";
        public const string Production = Folder + "/Production";
        public const string Valley = Folder + "/Scenes/Valley.unity";
        public const string Menu = Folder + "/Scenes/MainMenu.unity";
        public const string Version = "0.5.3-pilot.1";
#pragma warning disable 0649
        [Serializable] class Rect { public int x, y, w, h; }
        [Serializable] class Direction { public string key; public float[] pivot; public Rect[] idle, run; }
        [Serializable] class Frames { public int[] atlas; public float pixelsPerMeter, strideMeters, idleFps; public Direction[] directions; }
#pragma warning restore 0649

        [MenuItem("Echoes/Phase 5.3/Generate character and pond pilot")]
        public static void Generate()
        {
            AssetPaths.EnsureFolder(Folder + "/Scenes");
            AssetPaths.EnsureFolder(Folder + "/Materials");
            AssetPaths.EnsureFolder(Folder + "/Meshes");
            foreach (string path in Directory.GetFiles(Production, "*.png")) Import(path.Replace('\\', '/'));
            var scene = EditorSceneManager.OpenScene(Phase52B5Builder.Valley, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, Valley, true)) throw new IOException("Could not copy the preserved B5 scene");
            scene = EditorSceneManager.OpenScene(Valley, OpenSceneMode.Single);
            var b5 = UnityEngine.Object.FindFirstObjectByType<Phase52B5Pilot>();
            var b4 = UnityEngine.Object.FindFirstObjectByType<Phase52B4Pilot>();
            var p = b5.gameObject.AddComponent<Phase53Pilot>();
            p.Hero = b4.Hero; p.HeroCard = b4.HeroCard;
            p.BeforeHeroSheet = b4.B4Sheet; p.BeforeHeroMaterial = b4.B4HeroMaterial;
            p.HeroSheet = Sheet(false);
            p.HeroMaterial = Clone("Caretaker8D", p.BeforeHeroMaterial, p.HeroSheet.atlas);
            p.Mira = UnityEngine.Object.FindObjectsByType<NpcVisual2D5D>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(n => n.transform.parent.name == "Mira");
            p.MiraCard = p.Mira.GetComponentInChildren<Sprite25DQuad>().Renderer;
            p.BeforeMiraSheet = p.Mira.Sheet; p.BeforeMiraMaterial = p.MiraCard.sharedMaterial;
            p.MiraSheet = Sheet(true);
            p.MiraMaterial = Clone("Mira8D", p.BeforeMiraMaterial, p.MiraSheet.atlas);
            p.Workshop = UnityEngine.Object.FindObjectsByType<StaticProp25D>(FindObjectsSortMode.None).First(s => s.name.Contains("Cottage"));
            p.BeforeWorkshop = p.Workshop.GetComponent<Renderer>().sharedMaterial;
            p.AfterWorkshop = Clone("WorkshopLocalCutaway", p.BeforeWorkshop, p.BeforeWorkshop.mainTexture);
            p.AfterWorkshop.SetFloat("_FadeRadius", .17f);EditorUtility.SetDirty(p.AfterWorkshop);
            p.Terrain = b5.Terrain; p.BeforeTerrain = b5.B5Terrain;
            var data = UnityEngine.Object.Instantiate(p.BeforeTerrain);
            data.name = "Phase53Terrain";
            Save(data, Folder + "/Phase53Terrain.asset");
            p.AfterTerrain = AssetDatabase.LoadAssetAtPath<TerrainData>(Folder + "/Phase53Terrain.asset");
            Carve(p.AfterTerrain, p.Terrain.transform.position);
            p.NewPond = Pond(p);
            // These are hidden only in this copied scene. ResonanceView keeps its legacy pond reference for existing contracts.
            p.Hidden = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.name == "Pond" || (r.name.StartsWith("LilyPad") && r.transform.parent.name == "Water")).ToArray();
            p.OldColliders = p.Hidden.SelectMany(r => r.GetComponents<Collider>()).Where(c => c.enabled).ToArray();
            var dock = GameObject.Find("Dock").transform;
            var dockColliders = new List<Collider>();
            foreach (Transform plank in dock)
                if (plank.name.StartsWith("Plank") && plank.GetComponent<Collider>() == null) dockColliders.Add(plank.gameObject.AddComponent<BoxCollider>());
            p.DockColliders = dockColliders.ToArray();
            var minimap = UnityEngine.Object.FindFirstObjectByType<MinimapController>(FindObjectsInactive.Include);
            p.Minimap = minimap.Map; p.BeforeMap = b5.B5Minimap;
            p.Apply(false);
            BakeMap(p, b4, b5);
            p.AfterMap = AssetDatabase.LoadAssetAtPath<Texture2D>(Production + "/MinimapBase.png");
            p.Apply(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene(Phase52B5Builder.Menu, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, Menu, true)) throw new IOException("Could not copy B5 menu");
            scene=EditorSceneManager.OpenScene(Menu,OpenSceneMode.Single);new GameObject("Phase53SourceDisclosure").AddComponent<Phase53MenuCredits>();EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Echoes][P53] GENERATED: 8 own hero directions, 2 contact sources -> 8 derived frames; Mira 8D; Wren/Alder unchanged PARTIAL; concave pond + original 9-plank dock made solid; local workshop fade. Owner approval PENDING.");
        }

        static void Import(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var t = (TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType = TextureImporterType.Default; t.alphaIsTransparency = true; t.sRGBTexture = true;
            t.mipmapEnabled = true; t.mipMapsPreserveCoverage = true; t.alphaTestReferenceValue = .5f;
            t.wrapMode = path.Contains("Tile") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4; t.maxTextureSize = 4096;
            t.npotScale = TextureImporterNPOTScale.None; t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.SaveAndReimport();
        }
        static SpriteSheet25D Sheet(bool mira)
        {
            Frames f = JsonUtility.FromJson<Frames>(File.ReadAllText(Production + "/CharacterFrames.json"));
            var s = ScriptableObject.CreateInstance<SpriteSheet25D>();
            s.name = mira ? "Mira8D" : "Caretaker8D";
            s.atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Production + "/" + s.name + ".png");
            s.pixelsPerMeter = f.pixelsPerMeter; s.strideMeters = f.strideMeters; s.idleFps = f.idleFps;
            int height = mira ? 384 : f.atlas[1];
            Func<Rect, SpriteSheet25D.Frame> frame = r => new SpriteSheet25D.Frame { uv = new Vector4((float)r.x / f.atlas[0], (float)(height - r.y - r.h) / height, (float)r.w / f.atlas[0], (float)r.h / height), sizePx = new Vector2(r.w, r.h) };
            s.directions = f.directions.Select(d => new SpriteSheet25D.Direction { key = d.key, pivotPx = new Vector2(d.pivot[0], d.pivot[1]), idle = d.idle.Select(frame).ToArray(), run = (mira ? d.idle : d.run).Select(frame).ToArray() }).ToArray();
            string path = Production + "/" + s.name + ".asset"; Save(s, path);
            return AssetDatabase.LoadAssetAtPath<SpriteSheet25D>(path);
        }
        static Material Clone(string name, Material basis, Texture texture)
        {
            var m = new Material(basis) { name = name }; m.mainTexture = texture;
            string path = Folder + "/Materials/" + name + ".mat"; Save(m, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        static Material Paint(string name, string texture)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Production + "/" + texture));
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .3f); m.SetFloat("_Cull", 0);
            m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450;
            string path = Folder + "/Materials/" + name + ".mat"; Save(m, path); return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        static void Save(UnityEngine.Object value, string path)
        {
            var old = AssetDatabase.LoadMainAssetAtPath(path);
            if (old == null) AssetDatabase.CreateAsset(value, path);
            else { EditorUtility.CopySerialized(value, old); EditorUtility.SetDirty(old); UnityEngine.Object.DestroyImmediate(value); }
        }

        static void Carve(TerrainData data, Vector3 origin)
        {
            int n = data.heightmapResolution; float[,] h = data.GetHeights(0, 0, n, n);
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                Vector3 w = origin + new Vector3((float)x / (n - 1) * data.size.x, 0f, (float)z / (n - 1) * data.size.z);
                float radius = Vector2.Distance(new Vector2(w.x, w.z), new Vector2(-28, -14));
                if (radius > 9) continue;
                float height = h[z, x] * data.size.y + origin.y;
                // Undo the old circular carve locally, then carve the actual irregular inner water. Stream inlet stays open.
                height = Mathf.Min(0f,height + 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(3, 8, radius)));
                Vector2 uv=Phase53Pond.ImagePoint(w);
                // The painting is a ground surface, not an overlay drawn through Terrain. Keep the entire footprint below
                // its shore and a 0.65 m safety band below its water; otherwise heightmap triangles/LOD pierce the painting.
                if(uv.x>=-.035f&&uv.x<=1.035f&&uv.y>=-.035f&&uv.y<=1.035f)height=Mathf.Min(height,-.065f);
                float edge=Phase53Pond.EdgeDistance(w);
                if (Phase53Pond.InWater(w)) height = -.43f - .52f * Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.85f));
                else if(edge<.65f)height=Mathf.Min(height,Mathf.Lerp(-.43f,-.065f,Mathf.SmoothStep(0,1,edge/.65f)));
                if (Mathf.Abs(w.x + 28) < 1.3f && w.z > -12) height = Mathf.Min(height, -.85f);
                h[z, x] = Mathf.Clamp01((height - origin.y) / data.size.y);
            }
            data.SetHeights(0, 0, h); EditorUtility.SetDirty(data);
        }
        static GameObject Pond(Phase53Pilot p)
        {
            var root = new GameObject("Phase53ConcavePond");
            var surface = new GameObject("IrregularShoreAndWater", typeof(MeshFilter), typeof(MeshRenderer)); surface.transform.SetParent(root.transform);
            const int nx = 96, ny = 64;
            var verts = new Vector3[(nx + 1) * (ny + 1)]; var uv = new Vector2[verts.Length]; var triangles = new List<int>();
            for (int y = 0; y <= ny; y++) for (int x = 0; x <= nx; x++)
            {
                int i = y * (nx + 1) + x; Vector3 w = Phase53Pond.Point((float)x / nx, (float)y / ny);
                w.y = Phase53Pond.InWater(w) ? -.285f : .035f;
                verts[i] = w; uv[i] = new Vector2((float)x / nx, 1f - (float)y / ny);
                if (x < nx && y < ny) { int j = i + nx + 1; triangles.AddRange(new[] { i, i + 1, j, i + 1, j + 1, j }); }
            }
            var mesh = new Mesh { name = "IrregularPondBasinSurface" }; mesh.vertices = verts; mesh.uv = uv; mesh.triangles = triangles.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Save(mesh, Folder + "/Meshes/PondSurface.asset"); surface.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Meshes/PondSurface.asset");
            var renderer = surface.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off;
            var pond = root.AddComponent<Phase53Pond>(); pond.Surface = renderer; pond.Terrain = p.Terrain;
            pond.Dormant = Paint("PondS0", "Pond_S0_dormant_45deg.png"); pond.Restored = Paint("PondS3", "Pond_S3_restored_45deg.png"); renderer.sharedMaterial = pond.Dormant;
            var lily = new GameObject("SixPaintedLilyClusters",typeof(MeshFilter),typeof(MeshRenderer)); lily.transform.SetParent(root.transform);
            var lv = new List<Vector3>(); var lu = new List<Vector2>(); var lt = new List<int>();
            Vector2[] spots = {new Vector2(.40f,.35f),new Vector2(.55f,.40f),new Vector2(.68f,.49f),new Vector2(.69f,.65f),new Vector2(.40f,.59f),new Vector2(.31f,.46f)};
            for(int i=0;i<6;i++)
            {
                Vector3 c=Phase53Pond.Point(spots[i].x,spots[i].y); c.y=-.255f;
                float size=.7f+.09f*(i%3), u=(i%3)/3f, textureV=1f-(i/3+1)/2f;
                lv.AddRange(new[]{c-Phase53Pond.Right*size/2-Phase53Pond.Forward*size/2,c+Phase53Pond.Right*size/2-Phase53Pond.Forward*size/2,c-Phase53Pond.Right*size/2+Phase53Pond.Forward*size/2,c+Phase53Pond.Right*size/2+Phase53Pond.Forward*size/2});
                lu.AddRange(new[]{new Vector2(u,textureV),new Vector2(u+1f/3,textureV),new Vector2(u,textureV+.5f),new Vector2(u+1f/3,textureV+.5f)}); int o=i*4; lt.AddRange(new[]{o,o+2,o+1,o+2,o+3,o+1});
            }
            var lm=new Mesh{name="SixLilyClusters"};lm.SetVertices(lv);lm.SetUVs(0,lu);lm.SetTriangles(lt,0);lm.RecalculateNormals();lm.RecalculateBounds();Save(lm,Folder+"/Meshes/Lilies.asset");
            lily.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Meshes/Lilies.asset");pond.Lilies=lily.GetComponent<MeshRenderer>();pond.Lilies.sharedMaterial=Paint("LilyClusters","LilyClusters.png");pond.Lilies.shadowCastingMode=ShadowCastingMode.Off;
            // One static non-convex solid collider, assembled from a fine grid, excludes the original dock corridor.
            var blockers = new GameObject("DeepWaterBoundary", typeof(MeshCollider)); blockers.transform.SetParent(root.transform);
            var v = new List<Vector3>(); var t = new List<int>();
            for (float z = -21.5f; z <= -6.5f; z += .3f) for (float x = -35.5f; x <= -20.5f; x += .3f)
            {
                Vector3 w = new Vector3(x, 0, z); if (Phase53Pond.OnDock(w) || (!Phase53Pond.InWater(w) && Phase53Pond.EdgeDistance(w) > .20f)) continue;
                Box(v, t, w + Vector3.down * .3f, new Vector3(.302f, 2.4f, .302f));
            }
            var solid = new Mesh { name = "IrregularDeepWaterSolid" }; solid.indexFormat = IndexFormat.UInt32; solid.SetVertices(v); solid.SetTriangles(t, 0); solid.RecalculateBounds();
            Save(solid, Folder + "/Meshes/DeepWaterBoundary.asset"); blockers.GetComponent<MeshCollider>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Meshes/DeepWaterBoundary.asset");
            foreach (float side in new[] {-1f,1f})
            {
                var guard=new GameObject("DockWaterEdge"+(side<0?"South":"North"));guard.transform.SetParent(root.transform);guard.transform.position=new Vector3(-24.5f,.25f,-14f+side*.82f);
                guard.AddComponent<BoxCollider>().size=new Vector3(4.5f,1.5f,.12f);
            }
            var end=new GameObject("DockEndWaterBoundary");end.transform.SetParent(root.transform);end.transform.position=new Vector3(-26.70f,.25f,-14f);end.AddComponent<BoxCollider>().size=new Vector3(.12f,1.5f,1.7f);
            return root;
        }
        static void Box(List<Vector3> v, List<int> t, Vector3 c, Vector3 size)
        {
            int o = v.Count; Vector3 h = size * .5f;
            foreach (var q in new[] { new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,-1,1), new Vector3(-1,-1,1), new Vector3(-1,1,-1), new Vector3(1,1,-1), new Vector3(1,1,1), new Vector3(-1,1,1) }) v.Add(c + Vector3.Scale(q, h));
            foreach (int q in new[] {0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7}) t.Add(o + q);
        }
        static void BakeMap(Phase53Pilot p, Phase52B4Pilot b4, Phase52B5Pilot b5)
        {
            string[] cards={"Echoes/StaticProp25D","Echoes/StageProp25D","Echoes/PaintedCard25D","Echoes/Foliage25D","Echoes/Sprite25D","Echoes/ContactShadow"};
            var off = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && (r is ParticleSystemRenderer || r.GetComponent<Sprite25DQuad>()!=null || r.sharedMaterial!=null && cards.Contains(r.sharedMaterial.shader.name))).ToList();
            var clouds=GameObject.Find("Clouds");if(clouds!=null)off.AddRange(clouds.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!off.Contains(r)));
            bool fog=RenderSettings.fog,async=ShaderUtil.allowAsyncCompilation;RenderSettings.fog=false;ShaderUtil.allowAsyncCompilation=false;
            var restored = b4.HiddenInB4.Where(r => r != null && !r.enabled && !p.Hidden.Contains(r) && !off.Contains(r) && !(r is ParticleSystemRenderer)).Distinct().ToArray();
            var shadows = b4.ShadowOnlyInB4.Concat(b5.ShadowOnlyInB5).Where(r => r != null).Distinct().ToDictionary(r => r, r => r.shadowCastingMode);
            foreach (var r in off) r.enabled = false; foreach (var r in restored) r.enabled = true; foreach (var r in shadows.Keys) r.shadowCastingMode = ShadowCastingMode.On;
            var go = new GameObject("Phase53MapBake"); var cam = go.AddComponent<Camera>(); Vector4 b = Phase52B4Builder.MapBounds;
            cam.orthographic = true; cam.orthographicSize = b.w / 2; cam.aspect = b.z / b.w; cam.transform.SetPositionAndRotation(new Vector3(b.x+b.z/2,120,b.y+b.w/2),Quaternion.Euler(90,0,0)); cam.farClipPlane = 260;cam.nearClipPlane=1;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.22f,.16f);
            var rt = RenderTexture.GetTemporary(1024,1024,24); cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1024,1024,TextureFormat.RGB24,false); image.ReadPixels(new UnityEngine.Rect(0,0,1024,1024),0,0); image.Apply(); RenderTexture.active = null; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(go);
            Color32[] colours=image.GetPixels32();for(int i=0;i<colours.Length;i++){Color c=colours[i];float lum=c.r*.3f+c.g*.59f+c.b*.11f;c=Color.Lerp(new Color(lum,lum,lum),c,1.2f);colours[i]=new Color(c.r*1.10f+.03f,c.g*1.06f+.02f,c.b*.96f,1);}image.SetPixels32(colours);image.Apply();
            File.WriteAllBytes(Production + "/MinimapBase.png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            foreach (var r in off) r.enabled = true; foreach (var r in restored) r.enabled = false; foreach (var pair in shadows) pair.Key.shadowCastingMode = pair.Value;
            RenderSettings.fog=fog;ShaderUtil.allowAsyncCompilation=async;
            Import(Production + "/MinimapBase.png");
        }
        public static void BuildWindowsPilot()
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args,"-echoesBuildPath"); string output = index >= 0 ? args[index+1] : null;
            if (string.IsNullOrEmpty(output) || Directory.Exists(output)) throw new IOException("Fresh explicit build output required");
            Directory.CreateDirectory(output); string old = PlayerSettings.bundleVersion;
            try { PlayerSettings.bundleVersion = Version; var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] {Menu,Valley},locationPathName=Path.Combine(output,"EchoesOfTheValley.exe"),target=BuildTarget.StandaloneWindows64,targetGroup=BuildTargetGroup.Standalone,options=BuildOptions.None }); var s=report.summary; Debug.Log("[Echoes][P53] BUILD "+s.result+" id="+s.guid+" errors="+s.totalErrors+" warnings="+s.totalWarnings+" bytes="+s.totalSize); if(s.result!=BuildResult.Succeeded || s.totalWarnings!=0) throw new Exception("Phase53 build did not meet the zero-warning gate"); }
            finally { PlayerSettings.bundleVersion = old; }
        }
    }
}
