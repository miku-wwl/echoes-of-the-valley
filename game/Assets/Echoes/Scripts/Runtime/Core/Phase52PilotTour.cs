using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Echoes
{
    /// <summary>Opt-in actual-controller walking evidence. Automated analog input; no teleport during the measured route.</summary>
    public sealed class Phase52PilotTour : MonoBehaviour
    {
        [Serializable] sealed class Sample { public float t,x,y,z,speed,phase,walkBlend,yaw,frameMs; }
        [Serializable] sealed class CapturedFrame { public string file; public float t; }
        [Serializable] sealed class Record
        {
            public string input="AUTOMATED PlayerController.InputOverride; original controller, gravity/collision and camera remain active";
            public bool teleportsDuringMovement=false;
            public float movementSeconds,travelledMetres;
            public int visitedWaypoints,width,height;
            public bool pass;
            public List<Sample> samples=new List<Sample>();
            public string videoCapture="None";
            public List<CapturedFrame> capturedFrames=new List<CapturedFrame>();
        }
        PlayerController player;
        ThirdPersonCamera cameraRig;
        Camera tourCamera;
        string folder;
        bool finished;
        float launched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-echoes-pilot-tour")<0)return;
            var root=new GameObject("AutomatedPhase52MovementEvidence");DontDestroyOnLoad(root);root.AddComponent<Phase52PilotTour>();
        }
        void Start(){launched=Time.realtimeSinceStartup;StartCoroutine(Run());}
        void Update()
        {
            if(!finished&&Time.realtimeSinceStartup-launched>120){Debug.LogError("[Echoes][P52 TOUR] TIMEOUT; no movement acceptance.");Application.Quit(1);}
        }
        IEnumerator Run()
        {
            folder=Argument("-echoes-tour-output");if(string.IsNullOrEmpty(folder))throw new IOException("Explicit fresh tour output required.");
            Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"tour.json")))throw new IOException("Tour output already exists.");
            yield return null;
            var menu=FindFirstObjectByType<MainMenuController>();if(menu!=null)menu.PlayButton.onClick.Invoke();
            while((player=FindFirstObjectByType<PlayerController>())==null)yield return null;
            tourCamera=Camera.main;cameraRig=tourCamera.GetComponent<ThirdPersonCamera>();QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            var pilot=FindFirstObjectByType<Phase52Pilot>();
            Vector3 start=new Vector3(10.4f,0,-11.0f);start.y=pilot.Terrain.SampleHeight(start)+pilot.Terrain.transform.position.y+.02f;
            player.Teleport(start);cameraRig.Snap();
            yield return new WaitForSecondsRealtime(.5f);
            FindFirstObjectByType<HudController>().HideBanner();
            File.WriteAllText(Path.Combine(folder,"ready.flag"),"Actual Windows player ready; movement begins after five seconds.\n");
            Debug.Log("[Echoes][P52 TOUR] READY; AUTOMATED_INPUT; 36s route, no teleports during movement.");
            yield return new WaitForSecondsRealtime(5f);
            Vector2[] points={new Vector2(9.5f,-9.5f),new Vector2(7,-6),new Vector2(9,1),new Vector2(15,4),new Vector2(18,6),new Vector2(15,4),new Vector2(9,1),new Vector2(8,8),new Vector2(0,9),new Vector2(1.5f,19),new Vector2(4.5f,26),new Vector2(1.5f,19),new Vector2(0,9),new Vector2(8,8),new Vector2(9,1),new Vector2(15,4),new Vector2(18,6),new Vector2(15,4),new Vector2(9,1)};
            int next=0;float began=Time.realtimeSinceStartup;Vector3 last=player.transform.position;var record=new Record{width=Screen.width,height=Screen.height};
            bool captureFrames=Array.IndexOf(Environment.GetCommandLineArgs(),"-echoes-pilot-record-frames")>=0;
            float nextFrame=0;
            if(captureFrames){Directory.CreateDirectory(Path.Combine(folder,"frames"));record.videoCapture="Actual ScreenCapture.CaptureScreenshotAsTexture at WaitForEndOfFrame; original monotonic timestamps; no desktop capture or concept compositing";}
            var motion=player.GetComponentInChildren<SkinnedCaretakerMotion>();
            while(Time.realtimeSinceStartup-began<36f)
            {
                Vector2 current=new Vector2(player.transform.position.x,player.transform.position.z);Vector2 delta=points[next]-current;
                if(delta.magnitude<.65f){next=(next+1)%points.Length;record.visitedWaypoints++;delta=points[next]-current;}
                Vector3 wanted=new Vector3(delta.x,0,delta.y).normalized;
                float age=Time.realtimeSinceStartup-began;
                float viewOffset=age<9?0:age<17?80:age<24?170:0;
                float desiredYaw=Mathf.Atan2(wanted.x,wanted.z)*Mathf.Rad2Deg+viewOffset;
                float look=Mathf.Clamp(Mathf.DeltaAngle(cameraRig.Yaw,desiredYaw),-2.5f,2.5f);
                cameraRig.ApplyLook(new Vector2(look/.12f,0));
                Vector3 forward=Vector3.ProjectOnPlane(tourCamera.transform.forward,Vector3.up).normalized,right=Vector3.ProjectOnPlane(tourCamera.transform.right,Vector3.up).normalized;
                player.InputOverride=new Vector2(Vector3.Dot(wanted,right),Vector3.Dot(wanted,forward));
                yield return null;
                Vector3 position=player.transform.position;float distance=Vector3.ProjectOnPlane(position-last,Vector3.up).magnitude;
                record.travelledMetres+=distance;last=position;
                record.samples.Add(new Sample{t=Time.realtimeSinceStartup-began,x=position.x,y=position.y,z=position.z,speed=distance/Mathf.Max(.0001f,Time.deltaTime),phase=motion!=null?motion.WalkPhase:0,walkBlend=motion!=null?motion.WalkBlend:0,yaw=player.transform.eulerAngles.y,frameMs=Time.unscaledDeltaTime*1000});
                if(captureFrames&&Time.realtimeSinceStartup-began>=nextFrame)
                {
                    yield return new WaitForEndOfFrame();
                    float capturedAt=Time.realtimeSinceStartup-began;
                    Texture2D frame=ScreenCapture.CaptureScreenshotAsTexture();
                    string filename="frame-"+record.capturedFrames.Count.ToString("D5")+".jpg";
                    File.WriteAllBytes(Path.Combine(folder,"frames",filename),frame.EncodeToJPG(94));Destroy(frame);
                    record.capturedFrames.Add(new CapturedFrame{file=filename,t=capturedAt});nextFrame=capturedAt+1f/30f;
                }
            }
            player.InputOverride=Vector2.zero;record.movementSeconds=Time.realtimeSinceStartup-began;
            record.pass=record.travelledMetres>60&&record.visitedWaypoints>=8;
            File.WriteAllText(Path.Combine(folder,"tour.json"),JsonUtility.ToJson(record,true));
            Debug.Log("[Echoes][P52 TOUR] "+(record.pass?"PASS":"FAIL")+" distance="+record.travelledMetres+"m visited="+record.visitedWaypoints+" seconds="+record.movementSeconds+"; automated, human QA PENDING.");
            yield return new WaitForSecondsRealtime(.5f);
            // Teleport only AFTER the recorded continuous route, for matched diagnostic stills.
            Vector3 foot=new Vector3(4.7f,0,24f);foot.y=pilot.Terrain.SampleHeight(foot)+pilot.Terrain.transform.position.y+.1f;
            player.Teleport(foot);player.transform.rotation=Quaternion.Euler(0,180,0);yield return new WaitForSecondsRealtime(.4f);
            cameraRig.enabled=false;FindFirstObjectByType<HudController>().HideBanner();
            yield return Shot("hero-front",foot+new Vector3(0,1.10f,-2.6f),foot+Vector3.up*.92f);
            yield return Shot("hero-side",foot+new Vector3(2.6f,1.10f,0),foot+Vector3.up*.92f);
            yield return Shot("hero-rear",foot+new Vector3(0,1.10f,2.6f),foot+Vector3.up*.92f);
            finished=true;File.WriteAllText(Path.Combine(folder,"done.flag"),record.pass?"PASS":"FAIL");yield return new WaitForSecondsRealtime(1f);Application.Quit(record.pass?0:1);
        }
        IEnumerator Shot(string name,Vector3 at,Vector3 target)
        {
            tourCamera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(target-at));yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.2f);
        }
        static string Argument(string name){string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
    }
}
