using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.EditorTools
{
    public static class Phase52PilotPreview
    {
        public static void Capture()
        {
            ShaderUtil.allowAsyncCompilation=false;
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);
            var pilot=Object.FindFirstObjectByType<Phase52Pilot>();
            var player=Object.FindFirstObjectByType<PlayerController>();
            var camera=Camera.main;var p=new Vector3(4.7f,0,24f);p.y=pilot.Terrain.SampleHeight(p)+pilot.Terrain.transform.position.y+.02f;
            player.transform.position=p;player.transform.rotation=Quaternion.Euler(0,180,0);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/visual-qa/phase-5.2/unity-preview"));Directory.CreateDirectory(folder);
            foreach(string pose in new[]{"Idle","Walk"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Phase52HeroBuilder.Folder+"/Animations/"+pose+".anim");
                clip.SampleAnimation(pilot.SkinnedVisual,pose=="Walk"?.16f*clip.length:0);
                Shot(pose+"-front",p+new Vector3(0,1.05f,-2.35f),p+Vector3.up*.93f);
                Shot(pose+"-side",p+new Vector3(2.4f,1.05f,0),p+Vector3.up*.93f);
            }
            Shot("route-windmill",new Vector3(3,p.y+3.6f,14),new Vector3(6,p.y+4.6f,32));
            Debug.Log("[Echoes][P52] editor diagnostic previews only: "+folder);
            void Shot(string name,Vector3 from,Vector3 target)
            {
                camera.transform.SetPositionAndRotation(from,Quaternion.LookRotation(target-from));
                var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;
                camera.Render();camera.Render();RenderTexture previous=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
                Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
