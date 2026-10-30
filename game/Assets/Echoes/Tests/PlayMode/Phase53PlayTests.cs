using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Echoes.Tests
{
    public class Phase53PlayTests : ValleyPlayTestBase
    {
#if UNITY_EDITOR
        const string Scene="Assets/Echoes/Art/Phase53/Scenes/Valley.unity";
        Phase53Pilot p;
        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;FindSceneObjects();p=Object.FindFirstObjectByType<Phase53Pilot>();Assert.IsNotNull(p);Assert.IsFalse(p.BeforeMode);
        }
        Vector3 Ground(float x,float z)
        {
            var at=new Vector3(x,0,z);at.y=p.Terrain.SampleHeight(at)+p.Terrain.transform.position.y+.08f;return at;
        }
        Vector2 Input(Vector3 world)
        {
            Vector3 f=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized,r=Vector3.ProjectOnPlane(Camera.main.transform.right,Vector3.up).normalized;
            return new Vector2(Vector3.Dot(world,r),Vector3.Dot(world,f)).normalized;
        }
        IEnumerator Walk(Vector3 goal,float timeout=4f)
        {
            float time=0;
            while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(goal.x,goal.z))>.18f && time<timeout)
            {player.InputOverride=Input(Vector3.ProjectOnPlane(goal-player.transform.position,Vector3.up));time+=Time.deltaTime;yield return null;}
            player.InputOverride=Vector2.zero;yield return new WaitForSeconds(.15f);
        }
        [UnityTest] public IEnumerator RealControllerVisitsAllEightViewDirectionsWithoutMirroring()
        {
            yield return Load();var hero=p.Hero;var keys=new[]{"N","NE","E","SE","S","SW","W","NW"};
            for(int i=0;i<8;i++)
            {
                player.Teleport(Ground(4,-12));Object.FindFirstObjectByType<FixedFollowCamera25D>().Snap();
                float a=i*45*Mathf.Deg2Rad;player.InputOverride=new Vector2(Mathf.Sin(a),Mathf.Cos(a));yield return new WaitForSeconds(.65f);
                Assert.AreEqual(keys[i],hero.Direction);Assert.AreEqual(keys[i],hero.ShownDirection);Assert.IsFalse(hero.Mirrored);Assert.IsTrue(hero.Running);
            }
            player.InputOverride=Vector2.zero;
        }
        [UnityTest] public IEnumerator EightDirectionBoundaryHasSixDegreeHysteresis()
        {
            yield return Load();player.InputOverride=Vector2.zero;var view=Camera.main;float yaw=Object.FindFirstObjectByType<FixedFollowCamera25D>().Yaw;
            foreach(var test in new[]{(0f,"N"),(26f,"N"),(31f,"NE"),(24f,"NE"),(15f,"N")})
            {player.transform.rotation=Quaternion.Euler(0,yaw+test.Item1,0);p.Hero.Step(.01f,view);Assert.AreEqual(test.Item2,p.Hero.Direction);}
        }
        [UnityTest] public IEnumerator WalkCycleReachesBothContactsAndStopsOnTheSameGroundRoot()
        {
            yield return Load();player.Teleport(Ground(4,-12));var frames=new HashSet<int>();
            player.InputOverride=Input(Vector3.left);float began=Time.time;
            while(Time.time-began<1.8f){frames.Add(p.Hero.FrameIndex);yield return null;}
            Assert.IsTrue(frames.Contains(0)&&frames.Contains(4));Assert.GreaterOrEqual(frames.Count,7);
            player.InputOverride=Vector2.zero;yield return new WaitForSeconds(.3f);Assert.IsFalse(p.Hero.Running);
            Assert.AreEqual(Vector3.zero,p.Hero.transform.localPosition);Assert.AreEqual(Vector3.zero,p.Mira.transform.localPosition);
        }
        [UnityTest] public IEnumerator MiraTurnsTowardTheActualPlayerWhileHerDialogueIsOpen()
        {
            yield return Load();yield return StandNextTo(npc);yield return null;
            Assert.IsTrue(detector.TryInteract());yield return null;Assert.IsTrue(dialogue.IsOpen);Assert.AreEqual("Mira",dialogue.Speaker);
            var observed=new HashSet<string>();
            foreach(float yaw in new[]{0f,90f,180f,270f})
            {
                Vector3 delta=Quaternion.Euler(0,yaw,0)*Vector3.forward*2;
                player.Teleport(npc.transform.position+delta+Vector3.up*.05f);yield return null;yield return null;
                observed.Add(p.Mira.Direction);
            }
            Assert.GreaterOrEqual(observed.Count,4);dialogue.Close();
        }
        [UnityTest] public IEnumerator PreservedDockIsWalkableOutAndBackAndItsEndAndSidesStopEntryToWater()
        {
            yield return Load();player.Teleport(Ground(-21.1f,-14));yield return Walk(new Vector3(-26.05f,0,-14));
            Assert.Less(player.transform.position.x,-25.8f);Assert.Greater(player.transform.position.y,-.15f,"real plank collision supports the feet");
            player.InputOverride=Input(Vector3.left);yield return new WaitForSeconds(1);player.InputOverride=Vector2.zero;
            Assert.Greater(player.transform.position.x,-26.65f);Assert.IsFalse(Phase53Pond.IsDeep(player.transform.position));
            yield return Walk(new Vector3(-24.2f,0,-14));player.InputOverride=Input(Vector3.forward);yield return new WaitForSeconds(1);player.InputOverride=Vector2.zero;
            Assert.Less(Mathf.Abs(player.transform.position.z+14),.63f);Assert.IsFalse(Phase53Pond.IsDeep(player.transform.position));
            yield return Walk(new Vector3(-24.2f,0,-14));yield return Walk(new Vector3(-21.1f,0,-14));Assert.Greater(player.transform.position.x,-21.5f);
        }
        [UnityTest] public IEnumerator DeepWaterIsBlockedFromThreeUnbridgedBanks()
        {
            yield return Load();
            foreach(var at in new[]{Ground(-28,-21.5f),Ground(-35.5f,-14),Ground(-28,-6.5f)})
            {
                foreach(bool before in new[]{true,false})
                {
                    p.Apply(before);player.Teleport(Ground(at.x,at.z));player.InputOverride=Input(Phase53Pond.Centre-at);yield return new WaitForSeconds(2.2f);player.InputOverride=Vector2.zero;
                    Assert.AreEqual(before,Phase53Pond.InWater(player.transform.position),(before?"B5 permits the old water walk; control case":"Phase53 must stop before the real water contour")+player.transform.position);
                }
            }
        }
        [UnityTest] public IEnumerator EveryResonanceStageSurvivesSaveAndContinueWithCorrectPondArt()
        {
            for(int stage=0;stage<=3;stage++)
            {
                GameLaunch.Mode=LaunchMode.New;yield return Load();
                // Reach valid saved states through real quests. A forced Resonance value alone is correctly rejected by SaveCodec.
                if(stage==1)yield return RestoreTheStream();
                if(stage==2)yield return RestoreTheGarden();
                if(stage==3)yield return RestoreTheWindmill();
                yield return null;var pond=Object.FindFirstObjectByType<Phase53Pond>();pond.Refresh();Assert.AreEqual(stage>0,pond.RestoredShown);Assert.AreSame(stage>0?pond.Restored:pond.Dormant,pond.Surface.sharedMaterial);
                Assert.IsTrue(host.SaveNow(),"Valid quest-derived S"+stage+" must save");GameLaunch.Mode=LaunchMode.Continue;yield return Load();Assert.IsTrue(host.LoadedFromSave,host.StartupNotice);Assert.AreEqual((ResonanceStage)stage,Session.Resonance);
                pond=Object.FindFirstObjectByType<Phase53Pond>();Assert.AreEqual(stage>0,pond.RestoredShown);Assert.AreSame(stage>0?pond.Restored:pond.Dormant,pond.Surface.sharedMaterial);
            }
        }
        [UnityTest] public IEnumerator RealStreamQuestAndContinueKeepTheNewPresentationAndRewardOnlyOnce()
        {
            yield return Load();yield return RestoreTheStream();Assert.AreEqual(ResonanceStage.AwakeningStream,Session.Resonance);
            int coins=Session.Coins;Assert.IsTrue(host.SaveNow());GameLaunch.Mode=LaunchMode.Continue;yield return Load();
            Assert.AreEqual(coins,Session.Coins);Assert.AreEqual(StreamQuestStage.Completed,Session.StreamQuest);Assert.IsTrue(Object.FindFirstObjectByType<Phase53Pond>().RestoredShown);
        }
        [UnityTest] public IEnumerator WorkshopCutawayRevealsBodyAtDefaultAndClosestZoom_RenderDiagnostic()
        {
            yield return Load();var follow=Object.FindFirstObjectByType<FixedFollowCamera25D>();
            MethodInfo measure=typeof(Phase52B5PlayTests).GetMethod("VisibleShare",BindingFlags.Static|BindingFlags.NonPublic);Assert.IsNotNull(measure);
            foreach(float zoom in new[]{14f,10f})
            {
                player.Teleport(Ground(27.2f,11.8f));player.transform.rotation=Quaternion.Euler(0,225,0);follow.SetZoom(zoom);follow.Snap();
                p.Apply(true);for(int i=0;i<4;i++)yield return null;float before=(float)measure.Invoke(null,new object[]{p.HeroCard,player.transform.position});
                p.Apply(false);for(int i=0;i<4;i++)yield return null;float after=(float)measure.Invoke(null,new object[]{p.HeroCard,player.transform.position});
                Assert.Less(before,.12f);Assert.Greater(after,.55f,"body pixels, silhouette disabled; actual Player evidence is measured separately");
            }
        }
        [UnityTest] public IEnumerator NewPondLeavesTheB5SpringGardenTreesAndSinglePlayerIntact()
        {
            yield return Load();Assert.AreEqual(1,Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<StagePainting25D>());Assert.IsNotNull(GameObject.Find("B5_PaintedTrees"));Assert.IsTrue(resonanceView.GardenDormantShown);
            Assert.IsFalse(Object.FindFirstObjectByType<Phase52B5Pilot>().BeforeMode);Assert.IsTrue(p.NewPond.activeSelf);
            foreach(var actor in Object.FindObjectsByType<NpcVisual2D5D>(FindObjectsSortMode.None))Assert.AreEqual(Vector3.zero,actor.transform.localPosition,"All map NPCs keep their original ground roots");
        }
#endif
    }
}
