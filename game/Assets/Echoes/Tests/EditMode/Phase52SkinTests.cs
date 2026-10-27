using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Echoes.EditorTools;
using Object=UnityEngine.Object;

namespace Echoes.Tests
{
    public class Phase52SkinTests
    {
        const string Folder=Phase52HeroBuilder.Folder;
        [OneTimeSetUp] public void RegeneratePilotAfterEarlierGeneratorFixtures()=>Phase52PilotBuilder.Generate();
        static GameObject Make()=>Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Phase52HeroBuilder.PrefabPath));

        [Test] public void Skin_HasNormalizedWeightsAndBlendedJointInfluences()
        {
            var root=Make();try
            {
                var skin=root.GetComponent<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
                int boneCount=skin.bones.Length;Assert.GreaterOrEqual(boneCount,40,"Anatomical plus hair, scarf, coat and skirt secondary bones.");Assert.AreEqual(boneCount,mesh.bindposes.Length);Assert.AreEqual(mesh.vertexCount,mesh.boneWeights.Length);
                foreach(string required in new[]{"Root","Pelvis","Spine","Chest","Neck","Head","ClavicleL","ClavicleR","UpperArmL","UpperArmR","ForearmL","ForearmR","HandL","HandR","ThighL","ThighR","CalfL","CalfR","FootL","FootR"})Assert.IsTrue(skin.bones.Any(b=>b.name==required),required);
                int blended=0;
                foreach(var w in mesh.boneWeights)
                {
                    Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.00001));
                    foreach(int n in new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3})Assert.That(n,Is.InRange(0,boneCount-1));
                    if(w.weight1>.08f)blended++;
                }
                Assert.Greater(blended,500,"Weight continuity at joints, not only rigid per-part bone attachment.");
                Assert.That(mesh.bounds.size.y,Is.InRange(1.6f,1.9f));
            }finally{Object.DestroyImmediate(root);}
        }

        [Test] public void FourAuthoredClips_ResolveTheirBonePathsAndContainChangingKneeCurves()
        {
            var root=Make();try
            {
                foreach(string name in new[]{"Idle","Walk","TurnLeft","TurnRight"})
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Animations/"+name+".anim");Assert.IsNotNull(clip);Assert.IsTrue(clip.legacy);
                    var bindings=AnimationUtility.GetCurveBindings(clip);Assert.Greater(bindings.Length,100);
                    foreach(var binding in bindings)Assert.IsNotNull(root.transform.Find(binding.path),binding.path);
                    if(name=="Walk")
                    {
                        var curve=AnimationUtility.GetEditorCurve(clip,bindings.First(b=>b.path.EndsWith("CalfR")&&b.propertyName.Contains("Rotation")&&b.propertyName.EndsWith("x")));Assert.Greater(curve.keys.Max(k=>k.value)-curve.keys.Min(k=>k.value),.1f);
                    }
                }
            }finally{Object.DestroyImmediate(root);}
        }

        [Test] public void WalkSample_BendsKneeAndActuallyDeformsTheNativeSkin()
        {
            var root=Make();var a=new Mesh();var b=new Mesh();try
            {
                var skin=root.GetComponent<SkinnedMeshRenderer>();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Animations/Walk.anim");
                clip.SampleAnimation(root,0);skin.BakeMesh(a);clip.SampleAnimation(root,clip.length*.16f);skin.BakeMesh(b);
                var av=a.vertices;var bv=b.vertices;float change=0;for(int i=0;i<av.Length;i++)change+=(av[i]-bv[i]).magnitude;
                Assert.Greater(change/av.Length,.015f,"Valid curves must deform vertices, not just exist.");
                Transform thigh=skin.bones.Single(t=>t.name=="ThighR"),knee=skin.bones.Single(t=>t.name=="CalfR"),ankle=skin.bones.Single(t=>t.name=="FootR");
                float bend=Vector3.Angle(knee.position-thigh.position,ankle.position-knee.position);Assert.That(bend,Is.InRange(20,125));
                float floor=bv.Min(v=>v.y);
                Debug.Log("[P52 Skin sample] actualVertexFloor="+floor+" conservativeBoundsFloor="+b.bounds.min.y+" kneeBend="+bend+" averageVertexChange="+change/av.Length);
                if(floor<-.065f)
                {
                    int index=System.Array.FindIndex(bv,v=>v.y==floor);var w=skin.sharedMesh.boneWeights[index];
                    Debug.Log("[P52 Skin floor] vertex="+index+" floor="+floor+" bounds="+b.bounds.min.y+" rest="+skin.sharedMesh.vertices[index]+" weights="+skin.bones[w.boneIndex0].name+":"+w.weight0+","+skin.bones[w.boneIndex1].name+":"+w.weight1+","+skin.bones[w.boneIndex2].name+":"+w.weight2);
                    foreach(var bone in skin.bones.Where(t=>t.name.Contains("Calf")||t.name.Contains("Foot")||t.name.Contains("Thigh")))Debug.Log("[P52 bone] "+bone.name+" position="+bone.position.ToString("F5")+" local="+bone.localPosition.ToString("F5")+" rotation="+bone.rotation.ToString("F5"));
                }
                Assert.Greater(floor,-.065f,"Sampled skin vertices should not fold below the sole plane.");
            }finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);Object.DestroyImmediate(root);}
        }

        [Test] public void PilotScene_PreservesPlayerCameraColliderAndPublicInteractionCounts()
        {
            EditorSceneManager.OpenScene("Assets/Echoes/Scenes/Valley.unity");
            var p=Object.FindFirstObjectByType<PlayerController>();var c=p.GetComponent<CharacterController>();var camera=Object.FindFirstObjectByType<ThirdPersonCamera>();
            var contract=new[]{p.MoveSpeed,c.height,c.radius,c.stepOffset,c.slopeLimit,c.skinWidth,camera.Distance,camera.MinPitch,camera.MaxPitch};
            int nodes=Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).Length;
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);p=Object.FindFirstObjectByType<PlayerController>();c=p.GetComponent<CharacterController>();camera=Object.FindFirstObjectByType<ThirdPersonCamera>();
            CollectionAssert.AreEqual(contract,new[]{p.MoveSpeed,c.height,c.radius,c.stepOffset,c.slopeLimit,c.skinWidth,camera.Distance,camera.MinPitch,camera.MaxPitch});
            Assert.AreEqual(nodes,Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).Length);Assert.AreEqual(15,nodes);
            Assert.AreSame(p.transform,camera.Target);Assert.IsNotNull(p.Animator);Assert.IsNotNull(Object.FindFirstObjectByType<Phase52Pilot>());
        }

        [Test] public void LocalTerrainPaint_KeepsEveryOriginalHeight()
        {
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);var pilot=Object.FindFirstObjectByType<Phase52Pilot>();
            var a=pilot.BaselineTerrain;var b=pilot.PilotTerrain;Assert.AreNotSame(a,b);Assert.AreEqual(a.heightmapResolution,b.heightmapResolution);Assert.AreEqual(a.size,b.size);
            var ah=a.GetHeights(0,0,a.heightmapResolution,a.heightmapResolution);var bh=b.GetHeights(0,0,b.heightmapResolution,b.heightmapResolution);
            for(int z=0;z<a.heightmapResolution;z++)for(int x=0;x<a.heightmapResolution;x++)Assert.AreEqual(ah[z,x],bh[z,x]);
        }

        [Test] public void ThreeTreeSilhouettes_UseCutoutWithTransparentBorderAndValidTexture()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PilotLeaves.mat");Assert.IsTrue(material.shader.isSupported);Assert.IsTrue(material.IsKeywordEnabled("_ALPHATEST_ON"));
            var texture=(Texture2D)material.GetTexture("_BaseMap");Assert.IsNotNull(texture);
            for(int x=0;x<texture.width;x++){Assert.AreEqual(0,texture.GetPixel(x,0).a);Assert.AreEqual(0,texture.GetPixel(x,texture.height-1).a);}
            var meshes=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Tree_"+i+".asset")).ToArray();Assert.IsTrue(meshes.All(m=>m!=null&&m.vertexCount>50));
            Assert.Greater(meshes[1].bounds.size.y-meshes[0].bounds.size.y,.5f);
        }

        [Test] public void Portrait_IsCaretakerInformationAndNeverMiraSpeakerArt()
        {
            EditorSceneManager.OpenScene(Phase52PilotBuilder.Valley);var pilot=Object.FindFirstObjectByType<Phase52Pilot>();
            Assert.IsTrue(pilot.Portrait.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("Caretaker"));
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/CaretakerPortrait.png");Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(pilot.Portrait.transform.IsChildOf(Object.FindFirstObjectByType<InventoryPanel>().Panel.transform));
        }
    }
}
