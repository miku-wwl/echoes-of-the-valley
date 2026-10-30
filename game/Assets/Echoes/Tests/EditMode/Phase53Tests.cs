using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Echoes.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Echoes.Tests
{
    public class Phase53Tests
    {
        static Phase53Pilot Open()
        {
            EditorSceneManager.OpenScene(Phase53Builder.Valley);
            var p=Object.FindFirstObjectByType<Phase53Pilot>();Assert.IsNotNull(p);return p;
        }
        [TestCase("Caretaker_8dir_idle_source.png","0bf418e1879d065da4a8e88b812374a654565af0c772b84684391068d6626a64")]
        [TestCase("Caretaker_8dir_walk_keyposes_source.png","d04bbe7bb127c400774faa11b2ed7148b11df20db2b3d1ec4516cfd975fe3fb9")]
        [TestCase("Mira_8dir_idle_source.png","2716a12b2aa6afac8bd1793dbba8e8bee5f96a34754ee9973c5d096a56b1a86c")]
        [TestCase("LilyPads_6clusters_45deg_source.png","67002bdd004d94e63b4a37829b7c41daee2c70e5289e78f1a5297ce8db99338a")]
        [TestCase("Pond_S0_dormant_45deg_source.png","41fa69fbc322ce2bd12086afb23bc16b3611d384b05b912057c2ee707589af8a")]
        [TestCase("Pond_S3_restored_45deg_source.png","0c5a76ca52116c04acf32ae6605c40bcc611224ec5b587c9e459924d7d677a1d")]
        [TestCase("PondWater_muted_albedo_TILING_CANDIDATE.png","8e4d5f110bbafb0b2bdfd6a2b14cd16aa963f1f9699f0528e6978e0adf65dd9b")]
        public void OwnerSourceBytesAndPngAlphaRemainIntact(string name,string expected)
        {
            byte[] bytes=File.ReadAllBytes(Phase53Builder.Folder+"/Source/"+name);
            using(var sha=SHA256.Create())Assert.AreEqual(expected,BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant());
            var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);Assert.IsTrue(tex.LoadImage(bytes));var px=tex.GetPixels32();Assert.IsTrue(px.Any(p=>p.a>200));
            if(!name.Contains("TILING"))Assert.IsTrue(px.Any(p=>p.a==0));Object.DestroyImmediate(tex);
        }
        [TestCase(0,"N")][TestCase(45,"NE")][TestCase(90,"E")][TestCase(135,"SE")]
        [TestCase(180,"S")][TestCase(-135,"SW")][TestCase(-90,"W")][TestCase(-45,"NW")]
        public void EightViewHeadingsHaveTheirOwnSector(float angle,string expected)=>Assert.AreEqual(expected,CharacterVisual2D5D.Sector8(angle));

        [Test] public void HeroAndMiraHaveEightIndependentDrawingsAndGroundPivots()
        {
            var p=Open();
            foreach(var s in new[]{p.HeroSheet,p.MiraSheet})
            {
                Assert.AreEqual(8,s.directions.Length);Assert.AreEqual(8,s.directions.Select(d=>d.key).Distinct().Count());
                Assert.AreEqual(8,s.directions.Select(d=>d.idle[0].uv).Distinct().Count());
                foreach(var d in s.directions){Assert.IsTrue(string.IsNullOrEmpty(d.mirrorOf));Assert.That(d.pivotPx.x,Is.EqualTo(160));Assert.That(d.pivotPx.y,Is.InRange(18,30));Assert.Greater(d.idle.Length,0);}
            }
        }
        [Test] public void HeroCycleUsesTwoDisclosedContactsAndDistinctDerivedFrames()
        {
            var p=Open();var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(Phase53Builder.Production+"/Caretaker8D.png"));
            foreach(var d in p.HeroSheet.directions)
            {
                Assert.AreEqual(8,d.run.Length);var a=d.run[0];var b=d.run[4];
                var x=tex.GetPixels(Mathf.RoundToInt(a.uv.x*tex.width),Mathf.RoundToInt(a.uv.y*tex.height),320,384);
                var y=tex.GetPixels(Mathf.RoundToInt(b.uv.x*tex.width),Mathf.RoundToInt(b.uv.y*tex.height),320,384);
                Assert.Greater(x.Zip(y,(u,v)=>Vector4.Distance(u,v)).Count(e=>e>.08f),800,d.key+" has two different contact drawings, not duplicated idle frames");
            }
            Assert.IsTrue(File.ReadAllText(Phase53Builder.Production+"/CharacterFrames.json").Contains("originalWalkDrawings"));Object.DestroyImmediate(tex);
        }
        [Test] public void EarlierScenesStayFreeOfPhase53AndWrenAlderKeepTheirIdentity()
        {
            EditorSceneManager.OpenScene(Phase52B5Builder.Valley);Assert.IsNull(Object.FindFirstObjectByType<Phase53Pilot>());
            var p=Open();
            foreach(var n in Object.FindObjectsByType<NpcVisual2D5D>(FindObjectsSortMode.None).Where(n=>n.transform.parent.name!="Mira"))
            { CollectionAssert.Contains(new[]{"Wren","Alder"},n.transform.parent.name);Assert.AreEqual(1,n.Sheet.directions.Length,"Missing new art stays explicitly unchanged"); }
            Assert.AreEqual(1,Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3,Object.FindObjectsByType<NpcVisual2D5D>(FindObjectsSortMode.None).Length);
        }
        [Test] public void OldGreyDiscAndSixGreenCylindersAreHiddenOnlyInTheNewPresentation()
        {
            var p=Open();Assert.AreEqual(7,p.Hidden.Length);Assert.IsTrue(p.Hidden.All(r=>!r.enabled));
            Assert.IsTrue(p.Hidden.Single(r=>r.name=="Pond")!=null);Assert.AreEqual(6,p.Hidden.Count(r=>r.name.StartsWith("LilyPad")));
            Assert.IsNotNull(p.NewPond.GetComponentInChildren<Phase53Pond>().Lilies);
        }
        [Test] public void BasinHasIrregularContourRealDepthAndSolidDeepWaterBoundary()
        {
            var p=Open();Assert.AreEqual(14,Phase53Pond.Inner.Length);
            Assert.Less(p.AfterTerrain.GetInterpolatedHeight(( -28-p.Terrain.transform.position.x)/p.AfterTerrain.size.x,(-14-p.Terrain.transform.position.z)/p.AfterTerrain.size.z)+p.Terrain.transform.position.y,-.6f);
            var collider=p.NewPond.GetComponentInChildren<MeshCollider>();Assert.IsNotNull(collider);Assert.IsFalse(collider.isTrigger);Assert.Greater(collider.sharedMesh.triangles.Length,300);
            var mesh=p.NewPond.GetComponentInChildren<MeshFilter>().sharedMesh;Assert.Greater(mesh.vertices.Max(v=>v.y)-mesh.vertices.Min(v=>v.y),.25f);
        }
        [Test] public void OriginalDockKeepsNinePlanksAndItsTransformButNowHasSupport()
        {
            var p=Open();var dock=GameObject.Find("Dock").transform;
            Assert.AreEqual(-22.4f,dock.position.x,.001f);Assert.AreEqual(-14,dock.position.z,.001f);
            Assert.AreEqual(9,dock.Cast<Transform>().Count(t=>t.name.StartsWith("Plank")));Assert.AreEqual(9,p.DockColliders.Length);Assert.IsTrue(p.DockColliders.All(c=>c.enabled));
        }
        [Test] public void PaintedPondSurfaceHasClearanceAboveEveryTerrainSample()
        {
            var p=Open();var mesh=p.NewPond.transform.Find("IrregularShoreAndWater").GetComponent<MeshFilter>().sharedMesh;
            foreach(Vector3 v in mesh.vertices)
                Assert.Less(p.Terrain.SampleHeight(v)+p.Terrain.transform.position.y,v.y-.025f,"Terrain must not pierce the painted water/shore at "+v);
        }
        [Test] public void WorkshopOpeningIsLocalAndKeepsTheOriginalDepthShader()
        {
            var p=Open();Assert.IsTrue(p.Workshop.FadesAroundPlayer);Assert.AreEqual(p.BeforeWorkshop.shader,p.AfterWorkshop.shader);
            Assert.AreNotSame(p.BeforeWorkshop,p.AfterWorkshop);Assert.AreEqual(2450,p.AfterWorkshop.renderQueue);
            p.Apply(true);Assert.IsFalse(p.Workshop.FadesAroundPlayer);Assert.AreSame(p.BeforeWorkshop,p.Workshop.GetComponent<Renderer>().sharedMaterial);
            p.Apply(false);
        }
        [Test] public void BeforeSwitchRestoresB5TerrainHeroPondAndRealMinimap()
        {
            var p=Open();p.Apply(true);Assert.AreSame(p.BeforeTerrain,p.Terrain.terrainData);Assert.AreSame(p.BeforeHeroSheet,p.Hero.Sheet);
            Assert.IsTrue(p.Hidden.All(r=>r.enabled));Assert.IsFalse(p.NewPond.activeSelf);Assert.IsTrue(p.DockColliders.All(c=>!c.enabled));Assert.AreSame(p.BeforeMap,p.Minimap.texture);
            p.Apply(false);Assert.AreSame(p.AfterTerrain,p.Terrain.terrainData);Assert.AreSame(p.HeroSheet,p.Hero.Sheet);Assert.AreSame(p.AfterMap,p.Minimap.texture);
        }
        [Test] public void ProductionAlphaHasNoFrameBorderClippingAndKeepsMipCoverage()
        {
            foreach(string name in new[]{"Caretaker8D","Mira8D"})
            {
                string path=Phase53Builder.Production+"/"+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);Assert.IsTrue(imp.mipmapEnabled);Assert.IsTrue(imp.mipMapsPreserveCoverage);Assert.AreEqual(TextureWrapMode.Clamp,imp.wrapMode);
                var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(path));var px=tex.GetPixels32();
                for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(x%320<2||x%320>317||y%384<2||y%384>381)Assert.Less(px[y*tex.width+x].a,20,name+" sprite not cut at its cell edge");
                Object.DestroyImmediate(tex);
            }
        }
        [Test] public void OptionalRepairedWaterTileHasMatchingWrapEdges()
        {
            var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(Phase53Builder.Production+"/PondWaterTile.png"));var p=t.GetPixels32();
            for(int y=0;y<t.height;y++)Assert.AreEqual(p[y*t.width],p[y*t.width+t.width-1]);
            for(int x=0;x<t.width;x++)Assert.AreEqual(p[x],p[(t.height-1)*t.width+x]);Object.DestroyImmediate(t);
        }
        [Test] public void RegisteredPondMasksShareTheirShorelineWithoutCrossfadeGhosts()
        {
            var a=new Texture2D(2,2);var b=new Texture2D(2,2);a.LoadImage(File.ReadAllBytes(Phase53Builder.Production+"/Pond_S0_dormant_45deg.png"));b.LoadImage(File.ReadAllBytes(Phase53Builder.Production+"/Pond_S3_restored_45deg.png"));
            var x=a.GetPixels32();var y=b.GetPixels32();int both=0,either=0;
            for(int i=0;i<x.Length;i++){if(x[i].a>128||y[i].a>128)either++;if(x[i].a>128&&y[i].a>128)both++;}
            Assert.Greater(both/(float)either,.975f);Object.DestroyImmediate(a);Object.DestroyImmediate(b);
        }
        [Test] public void RealMinimapBakeExcludesOverheadCloudsAndKeepsLandmarksVisible()
        {
            var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(Phase53Builder.Production+"/MinimapBase.png"));var px=t.GetPixels32();
            Assert.Less(px.Count(c=>c.r>240&&c.g>240&&c.b>240)/(float)px.Length,.02f,"Opaque white clouds must never cover the minimap");
            Assert.Greater(px.Select(c=>((int)c.r<<16)|((int)c.g<<8)|c.b).Distinct().Count(),1000);Object.DestroyImmediate(t);
        }
        [Test] public void StreamInletNeverCutsAHoleThroughTheInteriorPondWater()
        {
            var w=new Vector3(-28,0,-11);Assert.IsTrue(Phase53Pond.InWater(w));var uv=Phase53Pond.ImagePoint(w);
            foreach(string name in new[]{"Pond_S0_dormant_45deg","Pond_S3_restored_45deg"})
            {
                var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(Phase53Builder.Production+"/"+name+".png"));
                Assert.Greater(t.GetPixel(Mathf.RoundToInt(uv.x*(t.width-1)),Mathf.RoundToInt((1-uv.y)*(t.height-1))).a,.8f,"Inlet removes the bank only; pond water covers the old rectangular stream end");Object.DestroyImmediate(t);
            }
        }
    }
}
