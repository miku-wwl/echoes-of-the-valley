using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>Opt-in real Windows framebuffer evidence. Automated controller input; no teleports within the recorded movement route.</summary>
    public sealed class Phase53Tour : MonoBehaviour
    {
        [Serializable] sealed class Sample { public float t,x,y,z,frameMs,phase; public string section,direction; public int frame; public long batches,drawCalls,memoryBytes; public bool deep; }
        [Serializable] sealed class Captured { public string file; public float t; }
        [Serializable] sealed class Visibility { public string presentation; public float zoom,visibleShare; public int referencePixels; }
        [Serializable] sealed class Record
        {
            public string input="AUTOMATED PlayerController.InputOverride, real collision, gravity, detector and dialogue; not human play";
            public string presentation,camera="yaw 45 / pitch 48 / zoom 14 (10-17) / FOV 35",videoCapture="None";
            public bool teleportsDuringRoute,pass,benchmark;
            public int width,height;
            public float routeSeconds,travelledMetres;
            public List<Sample> samples=new List<Sample>(); public List<Captured> capturedFrames=new List<Captured>();
            public List<string> stills=new List<string>(),failures=new List<string>(); public List<Visibility> visibility=new List<Visibility>();
        }
        PlayerController player; FixedFollowCamera25D follow; Phase53Pilot pilot; Camera view;
        Record record; string folder,section="setup"; bool recording,frames,finished; float began,launched; Vector3 last;
        ProfilerRecorder batches,draws;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Begin()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-echoes-p53-tour")<0)return;
            var go=new GameObject("AutomatedPhase53Evidence");DontDestroyOnLoad(go);go.AddComponent<Phase53Tour>();
        }
        void Start(){launched=Time.realtimeSinceStartup;StartCoroutine(Run());}
        void OnDestroy(){batches.Dispose();draws.Dispose();}
        void Update(){if(!finished && Time.realtimeSinceStartup-launched>480){Debug.LogError("[Echoes][P53 TOUR] TIMEOUT");Application.Quit(1);}}
        void LateUpdate()
        {
            if(!recording||player==null)return;
            Vector3 p=player.transform.position;record.travelledMetres+=Vector3.ProjectOnPlane(p-last,Vector3.up).magnitude;last=p;
            record.samples.Add(new Sample{t=Time.realtimeSinceStartup-began,x=p.x,y=p.y,z=p.z,frameMs=Time.unscaledDeltaTime*1000,section=section,direction=pilot.Hero.ShownDirection,frame=pilot.Hero.FrameIndex,phase=pilot.Hero.RunPhase,deep=Phase53Pond.IsDeep(p),batches=batches.Valid?batches.LastValue:-1,drawCalls=draws.Valid?draws.LastValue:-1,memoryBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()});
        }
        IEnumerator Capture()
        {
            Directory.CreateDirectory(Path.Combine(folder,"frames"));
            while(recording)
            {
                yield return new WaitForEndOfFrame();if(!recording)break;
                float timestamp=Time.realtimeSinceStartup-began;var tex=ScreenCapture.CaptureScreenshotAsTexture();string name="frame-"+record.capturedFrames.Count.ToString("D5")+".jpg";
                File.WriteAllBytes(Path.Combine(folder,"frames",name),tex.EncodeToJPG(85));Destroy(tex);
                record.capturedFrames.Add(new Captured{file=name,t=timestamp});
            }
        }
        IEnumerator Bind()
        {
            while((player=FindFirstObjectByType<PlayerController>())==null)yield return null;
            yield return null;yield return null;pilot=FindFirstObjectByType<Phase53Pilot>();follow=FindFirstObjectByType<FixedFollowCamera25D>();view=Camera.main;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        }
        IEnumerator Run()
        {
            folder=Arg("-echoes-tour-output");if(string.IsNullOrEmpty(folder))throw new IOException("Fresh explicit tour folder required");
            Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"tour.json")))throw new IOException("Preserve existing tour");
            yield return null;var menu=FindFirstObjectByType<MainMenuController>();if(menu!=null)menu.PlayButton.onClick.Invoke();yield return Bind();
            record=new Record{width=Screen.width,height=Screen.height,presentation=pilot.BeforeMode?"B5 presentation in same Phase53 build":"Phase53 high-angle 8D character and concave painted pond",benchmark=Array.IndexOf(Environment.GetCommandLineArgs(),"-echoes-p53-benchmark")>=0};
            batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            yield return Place(6.6f,-2.2f);yield return new WaitForSecondsRealtime(2);
            if(record.benchmark)yield return Benchmark();
            else
            {
                frames=Array.IndexOf(Environment.GetCommandLineArgs(),"-echoes-p53-record-frames")>=0;
                if(frames)record.videoCapture="Actual ScreenCapture.CaptureScreenshotAsTexture at WaitForEndOfFrame; timestamped JPEG frames; no interpolation, concepts, desktop capture or audio";
                StartRecord();if(frames)StartCoroutine(Capture());yield return Route();StopRecord();
                yield return Stills();
            }
            record.pass=record.failures.Count==0;
            File.WriteAllText(Path.Combine(folder,"tour.json"),JsonUtility.ToJson(record,true));
            File.WriteAllText(Path.Combine(folder,"done.flag"),record.pass?"PASS":"FAIL");
            Debug.Log("[Echoes][P53 TOUR] "+(record.pass?"PASS":"FAIL")+" routeSeconds="+record.routeSeconds+" metres="+record.travelledMetres+" failures="+string.Join(" | ",record.failures));
            finished=true;yield return new WaitForSecondsRealtime(.5f);Application.Quit(record.pass?0:1);
        }
        void StartRecord(){began=Time.realtimeSinceStartup;last=player.transform.position;recording=true;}
        void StopRecord(){player.InputOverride=Vector2.zero;record.routeSeconds=Time.realtimeSinceStartup-began;recording=false;}
        IEnumerator Benchmark()
        {
            StartRecord();section="static-plaza";yield return new WaitForSecondsRealtime(8);
            section="running-plaza";
            for(int lap=0;lap<3;lap++)for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;player.InputOverride=new Vector2(Mathf.Sin(a),Mathf.Cos(a));yield return new WaitForSecondsRealtime(.7f);}
            player.InputOverride=Vector2.zero;recording=false;
            yield return Place(-20.4f,-15);yield return new WaitForSecondsRealtime(2);
            section="static-pond";last=player.transform.position;recording=true;yield return new WaitForSecondsRealtime(8);StopRecord();
        }
        IEnumerator Route()
        {
            section="eight-directions";
            for(int i=0;i<8;i++)
            {
                float a=i*45*Mathf.Deg2Rad;player.InputOverride=new Vector2(Mathf.Sin(a),Mathf.Cos(a));yield return new WaitForSecondsRealtime(.9f);
                yield return Shot("moving-direction-"+pilot.Hero.ShownDirection);player.InputOverride=Vector2.zero;yield return new WaitForSecondsRealtime(.25f);
            }
            section="continuous-turns";
            for(int i=0;i<16;i++){float a=i*45*Mathf.Deg2Rad;player.InputOverride=new Vector2(Mathf.Sin(a),Mathf.Cos(a));yield return new WaitForSecondsRealtime(.45f);}
            player.InputOverride=Vector2.zero;
            section="Mira-interaction";yield return Walk(new Vector2(12.9f,3.6f));yield return new WaitForSecondsRealtime(.4f);
            var detector=FindFirstObjectByType<InteractionDetector>();var npc=FindFirstObjectByType<NpcInteractable>();
            if(!ReferenceEquals(detector.Current,npc)||!detector.TryInteract())record.failures.Add("Mira not offered through real interaction detector");
            var dialogue=FindFirstObjectByType<DialogueBox>();yield return new WaitForSecondsRealtime(.6f);yield return Shot("Mira-talking-to-player");
            while(dialogue.IsOpen){yield return new WaitForSecondsRealtime(.4f);dialogue.Advance();}
            section="workshop";yield return Walk(new Vector2(16.5f,5));yield return Walk(new Vector2(20.6f,7.2f));yield return Shot("workshop-front");
            foreach(var q in new[]{new Vector2(21.6f,3.4f),new Vector2(26.6f,2),new Vector2(29.4f,5.4f),new Vector2(28.8f,10.4f),new Vector2(26.6f,12.6f)})yield return Walk(q);
            section="workshop-cutaway";yield return new WaitForSecondsRealtime(2);yield return Shot("workshop-behind-in-recorded-route");
            section="walk-to-pond";
            foreach(var q in new[]{new Vector2(23.4f,13.6f),new Vector2(20.4f,12.4f),new Vector2(18,14.5f),new Vector2(9,14),new Vector2(-2,13),new Vector2(-11,11),new Vector2(-17,4),new Vector2(-19,-3),new Vector2(-18,-10),new Vector2(-21.1f,-14)})yield return Walk(q);
            section="pond-approach";yield return Shot("pond-S0-approach-played");yield return new WaitForSecondsRealtime(2);
            section="original-dock-out";yield return Walk(new Vector2(-26.05f,-14));yield return Shot("pond-S0-original-dock-end-played");yield return new WaitForSecondsRealtime(2);
            section="original-dock-return";yield return Walk(new Vector2(-21.1f,-14));yield return new WaitForSecondsRealtime(2);
            if(record.samples.Select(s=>s.direction).Distinct().Count()<8)record.failures.Add("Real movement did not cover eight distinct displayed directions");
            if(record.samples.Where(s=>s.section.StartsWith("original-dock")).Any(s=>s.y<-.25f||s.deep))record.failures.Add("Recorded dock route entered deep water or lacked plank support");
        }
        IEnumerator Walk(Vector2 goal)
        {
            float beganWalk=Time.realtimeSinceStartup;float initial=Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),goal);
            while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),goal)>.32f)
            {
                if(Time.realtimeSinceStartup-beganWalk>initial/3+6){record.failures.Add(section+" blocked toward "+goal+" at "+player.transform.position);break;}
                Vector3 delta=new Vector3(goal.x-player.transform.position.x,0,goal.y-player.transform.position.z).normalized;
                player.InputOverride=Input(delta);yield return null;
            }
            player.InputOverride=Vector2.zero;yield return new WaitForSecondsRealtime(.12f);
        }
        Vector2 Input(Vector3 delta)=>new Vector2(Vector3.Dot(delta,Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized),Vector3.Dot(delta,Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized));
        IEnumerator Place(float x,float z,float zoom=14)
        {
            var w=new Vector3(x,0,z);w.y=pilot.Terrain.SampleHeight(w)+pilot.Terrain.transform.position.y+.08f;
            // The original dock is above the carved basin; evidence setup lands on its real planks.
            if(!pilot.BeforeMode&&Phase53Pond.OnDock(w)&&x< -22.2f)w.y=.08f;
            player.Teleport(w);player.InputOverride=Vector2.zero;follow.SetZoom(zoom);follow.Snap();yield return new WaitForSecondsRealtime(.45f);follow.Snap();yield return null;
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));record.stills.Add(name);yield return new WaitForSecondsRealtime(.15f);
        }
        IEnumerator Stills()
        {
            section="post-route-stills-teleports-allowed";
            foreach(float zoom in new[]{14f,10f})foreach(bool before in new[]{true,false})
            {
                pilot.Apply(before);yield return Place(27.2f,11.8f,zoom);player.transform.rotation=Quaternion.Euler(0,225,0);yield return new WaitForSecondsRealtime(.25f);
                yield return Shot("workshop-"+(before?"B5":"Phase53")+"-zoom-"+zoom);yield return VisibilityMeasure(before,zoom);
            }
            pilot.Apply(false);
            foreach(var spot in new[]{("plaza",6.6f,-2.2f),("pond",-21.1f,-14f),("spring",-18.4f,12.6f),("garden",10.2f,-14.2f),("willow",12.6f,8.6f)})
            {
                foreach(bool before in new[]{true,false}){pilot.Apply(before);yield return Place(spot.Item2,spot.Item3);yield return Shot("comparison-"+(before?"B5-":"Phase53-")+spot.Item1);}
            }
            pilot.Apply(false);
            var mira=FindFirstObjectByType<NpcInteractable>();var dialog=FindFirstObjectByType<DialogueBox>();
            yield return Place(mira.Position.x+1.4f,mira.Position.z+1.4f);player.transform.rotation=Quaternion.Euler(0,225,0);yield return new WaitForSecondsRealtime(.3f);
            var detector=FindFirstObjectByType<InteractionDetector>();
            if(ReferenceEquals(detector.Current,mira)&&detector.TryInteract())
            {
                var facings=new HashSet<string>();
                for(int i=0;i<8;i++)
                {
                    float a=i*45*Mathf.Deg2Rad;Vector3 delta=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized*Mathf.Cos(a)+Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized*Mathf.Sin(a);
                    yield return Place(mira.Position.x+delta.x*2.1f,mira.Position.z+delta.z*2.1f);player.transform.rotation=Quaternion.LookRotation(-delta);yield return new WaitForSecondsRealtime(.25f);
                    facings.Add(pilot.Mira.Direction);yield return Shot("Mira-map-facing-"+pilot.Mira.Direction+"-AUTOMATED");
                }
                if(facings.Count!=8)record.failures.Add("Mira real Player facing coverage "+facings.Count+"/8");
                while(dialog.IsOpen)dialog.Advance();
            }
            else record.failures.Add("Mira direction stills could not open the actual NPC dialogue");
            for(int stage=0;stage<=3;stage++)
            {
                // Visual comparison setup only, explicitly named SET-BY-SCRIPT. Keep quest flags consistent with SaveCodec;
                // the separate whole-game smoke and PlayMode stage test reach these stages through real quest interactions.
                var s=GameSession.Current;s.RestoreCore(s.Coins,s.Inventory.Snapshot(),s.Chicken,s.ChickenRemaining,GameSession.ExplorationComplete,stage>=1?StreamQuestStage.Completed:StreamQuestStage.NotStarted,stage>=2?QuestStage.Completed:QuestStage.NotStarted,stage>=3?QuestStage.Completed:QuestStage.NotStarted,false,(ResonanceStage)stage,s.NodeTimers().ToDictionary(k=>k.Key,k=>k.Value));
                yield return new WaitForSecondsRealtime(5);yield return Place(-21.1f,-14);yield return Shot("pond-S"+stage+"-SET-BY-SCRIPT-approach");yield return Place(-25.8f,-14,17);yield return Shot("pond-S"+stage+"-SET-BY-SCRIPT-dock");
            }
            var host=GameSessionHost.Instance;if(!host.SaveNow())record.failures.Add("Save failed");GameLaunch.Mode=LaunchMode.Continue;
            yield return SceneManager.LoadSceneAsync(SceneNames.Valley);yield return Bind();
            if(GameSession.Current.Resonance!=ResonanceStage.RestoredValley||!GameSessionHost.Instance.LoadedFromSave)record.failures.Add("S3 Continue failed");
            yield return Place(-21.1f,-14);yield return Shot("pond-S3-SET-BY-SCRIPT-Save-Continue");
        }
        IEnumerator VisibilityMeasure(bool before,float zoom)
        {
            Renderer hero=pilot.HeroCard;Material material=hero.sharedMaterial;Color tint=material.GetColor("_OccludedTint");material.SetColor("_OccludedTint",Color.clear);
            var quad=hero.GetComponent<Sprite25DQuad>();Vector4 rect=quad.Rect;Vector3 foot=player.transform.position;
            Vector3 a=view.WorldToScreenPoint(foot+view.transform.right*rect.x+view.transform.up*rect.y),b=view.WorldToScreenPoint(foot+view.transform.right*(rect.x+rect.z)+view.transform.up*(rect.y+rect.w));
            var box=new RectInt(Mathf.FloorToInt(a.x)-3,Mathf.FloorToInt(a.y)-3,Mathf.CeilToInt(b.x-a.x)+6,Mathf.CeilToInt(b.y-a.y)+6);
            Color32[] on=null,off=null,refOn=null,refOff=null;yield return Pixels(p=>on=p);hero.enabled=false;yield return Pixels(p=>off=p);hero.enabled=true;
            var others=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r!=hero).ToArray();foreach(var r in others)r.enabled=false;
            yield return Pixels(p=>refOn=p);hero.enabled=false;yield return Pixels(p=>refOff=p);hero.enabled=true;foreach(var r in others)r.enabled=true;material.SetColor("_OccludedTint",tint);
            int reference=Changed(refOn,refOff,box);float visible=reference>50?Mathf.Clamp01(Changed(on,off,box)/(float)reference):-1;
            record.visibility.Add(new Visibility{presentation=before?"B5":"Phase53",zoom=zoom,visibleShare=visible,referencePixels=reference});
            if(!before&&visible<.60f)record.failures.Add("Workshop body visibility below 60% at zoom "+zoom+": "+visible);
            Debug.Log("[Echoes][P53 TOUR] WORKSHOP "+(before?"B5":"Phase53")+" zoom="+zoom+" BODY_VISIBLE="+visible+" pixels="+reference+" silhouette excluded");
        }
        IEnumerator Pixels(Action<Color32[]> store){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();store(t.GetPixels32());Destroy(t);}
        int Changed(Color32[] a,Color32[] b,RectInt box)
        {
            int count=0;for(int y=Mathf.Max(0,box.yMin);y<Mathf.Min(Screen.height,box.yMax);y++)for(int x=Mathf.Max(0,box.xMin);x<Mathf.Min(Screen.width,box.xMax);x++)
            {int i=y*Screen.width+x;Color32 p=a[i],q=b[i];if(Mathf.Max(Mathf.Abs(p.r-q.r),Mathf.Abs(p.g-q.g),Mathf.Abs(p.b-q.b))>64)count++;}return count;
        }
        static string Arg(string name){string[] a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);return i>=0&&i+1<a.Length?a[i+1]:null;}
    }
}
