using System.Linq;
using Echoes.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;

namespace Echoes.Tests
{
    public class Phase51ArtTests
    {
        [Test]
        public void ClosedSleevesAndTrousers_FaceOutward_NotJustConsistentlyInward()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Echoes/Art/Pilot"}))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                if(!mesh.name.EndsWith("_Trousers") && !mesh.name.EndsWith("ArmL_ClothCream") && !mesh.name.EndsWith("ArmR_ClothCream"))continue;
                var v=mesh.vertices;var t=mesh.triangles;double volume=0;
                for(int i=0;i<t.Length;i+=3)volume+=Vector3.Dot(v[t[i]],Vector3.Cross(v[t[i+1]],v[t[i+2]]))/6.0;
                Assert.Greater(volume,0,mesh.name+": a closed garment must have positive outward volume");
            }
        }

        [Test]
        public void WorldHumans_PreserveCollidersAndUseDistinctOriginalMeshProfiles()
        {
            string[,] roles={{"Villager","Mira"},{"GardenKeeper","Wren"},{"WindmillRestorer","Alder"}};
            for(int i=0;i<3;i++)
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/"+roles[i,0]+".prefab");
                Assert.IsTrue(root.GetComponent<CharacterAnimator>().HasLimbs);
                Assert.AreEqual(1.6f,root.GetComponent<CapsuleCollider>().height);
                Assert.IsTrue(root.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh.name.StartsWith(roles[i,1]+"_")));
            }
        }

        [Test]
        public void Clover_HasOriginalBirdGeometryInsideTheExistingBodyAndStateIndicators()
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/Hen.prefab");
            var body=root.transform.Find("Body");Assert.IsNotNull(body);
            var meshes=body.GetComponentsInChildren<MeshFilter>();Assert.GreaterOrEqual(meshes.Length,5);
            Assert.IsTrue(meshes.All(f=>f.sharedMesh.name.StartsWith("Clover_")));
            foreach(string name in new[]{"Egg","HungryIcon","ProducingIcon"})Assert.IsNotNull(root.transform.Find(name));
        }

        [Test]
        public void SatchelSprites_AreBoundInStableCatalogOrder()
        {
            EditorSceneManager.OpenScene("Assets/Echoes/Scenes/Valley.unity");
            var panel=Object.FindFirstObjectByType<InventoryPanel>();
            var sprites=new SerializedObject(panel).FindProperty("itemSprites");
            Assert.AreEqual(ItemCatalog.All.Count,sprites.arraySize);
            for(int i=0;i<sprites.arraySize;i++)Assert.AreEqual("Icon_"+ItemCatalog.All[i].Id,sprites.GetArrayElementAtIndex(i).objectReferenceValue.name);
            Assert.AreEqual(8,panel.RowCount);
        }

        [Test]
        public void GeneratedMeshes_MatchNativeUnityFaceWinding()
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            float nativeSign;
            try
            {
                var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
                var v=mesh.vertices;var n=mesh.normals;var t=mesh.triangles;
                nativeSign=Mathf.Sign(Vector3.Dot(Vector3.Cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]]),n[t[0]]));
            }
            finally { Object.DestroyImmediate(cube); }
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Echoes/Art/Pilot"}))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                var v=mesh.vertices;var n=mesh.normals;var t=mesh.triangles;
                for(int i=0;i<t.Length;i+=3)
                {
                    Vector3 cross=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);
                    if(cross.sqrMagnitude<1e-14f)continue;
                    Vector3 normal=n[t[i]]+n[t[i+1]]+n[t[i+2]];
                    float agreement=Vector3.Dot(cross.normalized,normal.normalized)*nativeSign;
                    Assert.GreaterOrEqual(agreement,-.001f,mesh.name+": triangle "+i/3+" faces opposite its outward normals");
                }
            }
        }

        [Test]
        public void RebuildingVolume_KeepsGuidAndOnlyThreeReferencedOverrides()
        {
            const string path="Assets/Echoes/Art/Settings/Phase51TestVolume.asset";
            string guid=null;
            try
            {
                for(int run=0;run<3;run++)
                {
                    var volume=SceneKit.CreateVolume("TestVolume",path,.2f,1,.2f,0);
                    Object.DestroyImmediate(volume.gameObject);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    string current=AssetDatabase.AssetPathToGUID(path);
                    if(guid!=null)Assert.AreEqual(guid,current,"preserve references");
                    guid=current;
                    var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                    var all=AssetDatabase.LoadAllAssetsAtPath(path);
                    Assert.AreEqual(4,all.Length,"profile plus exactly three overrides, including hidden assets");
                    Assert.AreEqual(3,profile.components.Count);
                    foreach(var component in profile.components)Assert.Contains(component,all);
                }
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void HeroMesh_IsAdultHeightWithNeckAndSevenHeadProportion_AndOriginalAssets()
        {
            var hero=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Echoes/Prefabs/Hero.prefab");
            Assert.IsNotNull(hero);Assert.IsTrue(hero.GetComponent<CharacterAnimator>().HasLimbs);
            var head=hero.transform.Find("Body/Head");Assert.IsNotNull(head);
            var face=head.GetComponentsInChildren<MeshFilter>().Single(f=>f.sharedMesh.name=="Head_Skin");
            Assert.That(face.sharedMesh.bounds.size.y,Is.InRange(.23f,.27f));
            Bounds bodyBounds=new Bounds(Vector3.zero,Vector3.zero);
            foreach(var renderer in hero.GetComponentsInChildren<Renderer>())bodyBounds.Encapsulate(renderer.bounds);
            Assert.That(bodyBounds.size.y,Is.InRange(1.60f,1.75f));
            foreach(var mesh in hero.GetComponentsInChildren<MeshFilter>())
            {
                Assert.IsNotNull(mesh.sharedMesh);
                if(mesh.name=="Glass" || mesh.name=="Frame")continue;
                StringAssert.StartsWith("Assets/Echoes/Art/Pilot/",AssetDatabase.GetAssetPath(mesh.sharedMesh));
                Assert.AreEqual(mesh.sharedMesh.vertexCount,mesh.sharedMesh.normals.Length);
                Assert.AreEqual(mesh.sharedMesh.vertexCount,mesh.sharedMesh.uv.Length);
                Assert.IsTrue(mesh.sharedMesh.bounds.size.sqrMagnitude>0);
            }
            foreach(var renderer in hero.GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                {
                    Assert.IsNotNull(material);Assert.IsTrue(material.shader.isSupported);
                    StringAssert.StartsWith("Universal Render Pipeline/",material.shader.name);
                }
        }
    }
}
