using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Echoes.Tests
{
    public class Phase52MotionPlayTests : ValleyPlayTestBase
    {
#if UNITY_EDITOR
        GameObject AddSkin()
        {
            player.Animator.gameObject.SetActive(false);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Art/Phase52/CaretakerSkin.prefab");return Object.Instantiate(prefab,player.transform);
        }

        [UnityTest] public IEnumerator ActualMovement_DrivesSkin_AndPauseStopsItsPhase()
        {
            var skin=AddSkin();var motion=skin.GetComponent<SkinnedCaretakerMotion>();Vector3 start=player.transform.position;player.InputOverride=Vector2.up;
            yield return new WaitForSeconds(.7f);Assert.Greater(Vector3.Distance(start,player.transform.position),1);Assert.Greater(motion.WalkBlend,.8f);Assert.AreEqual("Walk",motion.Motion);
            pause.SetPaused(true);yield return null;float phase=motion.WalkPhase;yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(phase,motion.WalkPhase);pause.Resume();
        }

        [UnityTest] public IEnumerator TurnClipsPlayOnAnUpperBodyLayer_AndLocomotionStaysOnTheBaseLayer()
        {
            var skin=AddSkin();var animation=skin.GetComponent<Animation>();yield return null;
            Assert.AreEqual(1,animation["TurnLeft"].layer);Assert.AreEqual(1,animation["TurnRight"].layer);Assert.AreEqual(0,animation["Walk"].layer);Assert.AreEqual(0,animation["Idle"].layer);
        }

        [UnityTest] public IEnumerator FoliageFadeDriver_PublishesThePlayerAsTheShaderFocus_AndClearsItOnDisable()
        {
            var driver=new GameObject("FoliageFadeDriverTest").AddComponent<FoliageFadeDriver>();yield return null;yield return null;
            Vector4 focus=Shader.GetGlobalVector("_EchoesFocus");Assert.AreEqual(1f,focus.w);
            Assert.Less(Vector3.Distance(new Vector3(focus.x,focus.y,focus.z),player.transform.position+Vector3.up*1.05f),.05f);
            Object.Destroy(driver.gameObject);yield return null;yield return null;Assert.AreEqual(0f,Shader.GetGlobalVector("_EchoesFocus").w);
        }

        [UnityTest] public IEnumerator TeleportDoesNotBecomeWalkingEvidence()
        {
            var skin=AddSkin();var motion=skin.GetComponent<SkinnedCaretakerMotion>();yield return null;yield return null;
            float before=motion.TravelledMetres;float phase=motion.WalkPhase;player.Teleport(player.transform.position+Vector3.right*5);yield return null;yield return null;
            Assert.That(motion.TravelledMetres-before,Is.LessThan(.05f));Assert.That(Mathf.Abs(motion.WalkPhase-phase),Is.LessThan(.02f));
        }
#endif
    }
}
