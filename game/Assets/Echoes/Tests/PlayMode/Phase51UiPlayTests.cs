using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;

namespace Echoes.Tests
{
    public class Phase51UiPlayTests : ValleyPlayTestBase
    {
        [UnityTest]
        public IEnumerator VillagerFeet_AreAtTheTerrainSurface()
        {
            yield return null;
            foreach(var actor in Object.FindObjectsByType<NpcIdle>(FindObjectsSortMode.None))
            {
                float foot=float.PositiveInfinity;
                foreach(var mesh in actor.GetComponentsInChildren<MeshFilter>())
                    if(mesh.name.Contains("LegL") || mesh.name.Contains("LegR"))foot=Mathf.Min(foot,mesh.GetComponent<Renderer>().bounds.min.y);
                float ground=Terrain.activeTerrain.SampleHeight(actor.transform.position)+Terrain.activeTerrain.transform.position.y;
                Assert.That(foot-ground,Is.InRange(-.02f,.04f),actor.name+" sole clearance");
            }
        }

        [UnityTest]
        public IEnumerator ConsumingAnEarlierStack_MovesTheEggWithItsOwnIcon()
        {
            Assert.AreEqual(Outcome.Ok,Session.Inventory.TryAdd(ItemIds.Herb,1));
            Assert.AreEqual(Outcome.Ok,Session.Inventory.TryAdd(ItemIds.Egg,1));
            inventoryPanel.Open();yield return null;
            Assert.AreEqual(Outcome.Ok,Session.Inventory.TryRemove(ItemIds.Herb,1));
            yield return null;
            Assert.AreEqual("Hen's Egg",inventoryPanel.RowName(0));
            Assert.AreEqual("Icon_egg",inventoryPanel.Panel.transform.Find("Icon0").GetComponent<Image>().sprite.name);
            Assert.AreEqual(1,Session.Inventory.Count(ItemIds.Egg));
            inventoryPanel.Close();
        }
    }
}
