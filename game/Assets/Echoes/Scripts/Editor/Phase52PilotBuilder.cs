using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    public static class Phase52PilotBuilder
    {
        public const string SceneFolder=Phase52HeroBuilder.Folder+"/Scenes";
        public const string Valley=SceneFolder+"/Valley.unity";
        public const string Menu=SceneFolder+"/MainMenu.unity";

        [MenuItem("Echoes/Phase 5.2/Generate isolated pilot")]
        public static void Generate()
        {
            Phase52HeroBuilder.Bake();AssetPaths.EnsureFolder(SceneFolder);
            var scene=EditorSceneManager.OpenScene(AssetPaths.ValleyScene,OpenSceneMode.Single);
            if(!EditorSceneManager.SaveScene(scene,Valley,true))throw new IOException("Could not copy source Valley.");
            scene=EditorSceneManager.OpenScene(Valley,OpenSceneMode.Single);
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var host=new GameObject("Phase52PilotOnly").AddComponent<Phase52Pilot>();
            host.OriginalVisual=player.Animator.gameObject;host.Terrain=Terrain.activeTerrain;host.BaselineTerrain=host.Terrain.terrainData;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Phase52HeroBuilder.PrefabPath),player.transform);
            visual.name="SkinnedCaretaker_Pilot";host.SkinnedVisual=visual;
            host.OriginalVisual.SetActive(false);
            Phase52EnvironmentBuilder.Build(host);
            host.Portrait=Portrait();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene(AssetPaths.MainMenuScene,OpenSceneMode.Single);
            if(!EditorSceneManager.SaveScene(scene,Menu,true))throw new IOException("Could not copy source menu.");
            scene=EditorSceneManager.OpenScene(Menu,OpenSceneMode.Single);
            UnityEngine.Object.FindFirstObjectByType<MainMenuController>().gameObject.AddComponent<Phase52PilotRuntime>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Echoes][P52] isolated pilot generated; default build scenes and original scenes/prefabs untouched.");
        }

        static GameObject Portrait()
        {
            const string path=Phase52HeroBuilder.Folder+"/CaretakerPortrait.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null)throw new FileNotFoundException("Copy supplied original 01 portrait before generating the pilot.");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var panel=UnityEngine.Object.FindFirstObjectByType<InventoryPanel>().Panel.transform;
            var frame=UiKit.NewRect("CaretakerInformation_PilotPortrait",panel).Place(UiKit.Center,UiKit.Center,new Vector2(-590,0),new Vector2(320,630));
            frame.gameObject.AddComponent<Image>().color=new Color(.07f,.13f,.12f,.97f);
            UiKit.Label(frame,"CaretakerLabel","Caretaker",29,UiKit.Gold,TextAnchor.MiddleCenter).rectTransform.Place(UiKit.TopCenter,UiKit.TopCenter,new Vector2(0,-12),new Vector2(300,44));
            var image=UiKit.NewRect("OriginalCaretakerPortrait",frame).gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);image.preserveAspect=true;image.raycastTarget=false;image.rectTransform.Place(UiKit.Center,UiKit.Center,new Vector2(0,-4),new Vector2(300,530));
            UiKit.Label(frame,"PilotPortraitNote","Original AI illustration · pilot",17,UiKit.Cream,TextAnchor.MiddleCenter).rectTransform.Place(UiKit.BottomCenter,UiKit.BottomCenter,new Vector2(0,10),new Vector2(300,30));
            return frame.gameObject;
        }

        public static void BuildWindowsPilot()
        {
            string output=Argument("-echoesBuildPath");
            if(string.IsNullOrEmpty(output) || Directory.Exists(output))throw new IOException("Use a fresh explicit -echoesBuildPath; existing builds are preserved.");
            Directory.CreateDirectory(output);string version=PlayerSettings.bundleVersion;
            try
            {
                PlayerSettings.bundleVersion="0.5.2-pilot.1";
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Menu,Valley},locationPathName=Path.Combine(output,"EchoesOfTheValley.exe"),target=BuildTarget.StandaloneWindows64,targetGroup=BuildTargetGroup.Standalone,options=BuildOptions.None});
                var s=report.summary;
                Debug.Log("[Echoes][P52] PILOT_BUILD "+s.result+" id="+s.guid+" errors="+s.totalErrors+" warnings="+s.totalWarnings+" bytes="+s.totalSize+"; owner approval PENDING, not a final release candidate.");
                if(s.result!=BuildResult.Succeeded)throw new Exception("Pilot build failed.");
            }
            finally{PlayerSettings.bundleVersion=version;}
        }

        static string Argument(string name){string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
    }
}
