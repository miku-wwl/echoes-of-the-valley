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

        [UnityTest] public IEnumerator TeleportDoesNotBecomeWalkingEvidence()
        {
            var skin=AddSkin();var motion=skin.GetComponent<SkinnedCaretakerMotion>();yield return null;yield return null;
            float before=motion.TravelledMetres;float phase=motion.WalkPhase;player.Teleport(player.transform.position+Vector3.right*5);yield return null;yield return null;
            Assert.That(motion.TravelledMetres-before,Is.LessThan(.05f));Assert.That(Mathf.Abs(motion.WalkPhase-phase),Is.LessThan(.02f));
        }
#endif
    }
}
