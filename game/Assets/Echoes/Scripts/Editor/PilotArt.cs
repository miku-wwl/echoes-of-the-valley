using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>Blender-authored original meshes, baked by Unity. Only the windmill neighbourhood is the pilot.</summary>
    internal static class PilotArt
    {
        const string Folder = AssetPaths.Root + "/Art/Pilot";
        [Serializable] sealed class Export { public Part[] parts; }
        [Serializable] sealed class Part
        {
            public string name, rig, material;
            public float[] vertices, normals, uv;
            public int[] triangles;
        }
        static Export data;
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        static Vector3 Pivot(string rig)
        {
            if(rig.Contains("_")) rig=rig.Substring(rig.IndexOf('_')+1);
            switch (rig)
            {
                case "Head": return new Vector3(0, 1.52f, 0);
                case "ArmL": return new Vector3(-.22f, 1.31f, 0);
                case "ArmR": return new Vector3(.22f, 1.31f, 0);
                case "LegL": return new Vector3(-.105f, .83f, 0);
                case "LegR": return new Vector3(.105f, .83f, 0);
                case "Eye-1": return new Vector3(-.043f, 1.53f, .089f);
                case "Eye1": return new Vector3(.043f, 1.53f, .089f);
                case "Ponytail": return new Vector3(0, 1.57f, -.11f);
                case "ScarfTail": return new Vector3(.03f, 1.37f, -.073f);
                default: return Vector3.zero;
            }
        }

        public static void Bake()
        {
            string json = File.ReadAllText(AssetPaths.Root + "/Art/PilotSource/pilot-meshes.json");
            data = JsonUtility.FromJson<Export>(json);
            if (data?.parts == null || data.parts.Length == 0) throw new InvalidDataException("No Blender pilot meshes; run tools/art/build_pilot.py.");
            AssetPaths.EnsureFolder(Folder);
            meshes.Clear(); materials.Clear();
            foreach (Part part in data.parts)
            {
                string path = Folder + "/" + part.name + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
                mesh.Clear(); mesh.name = part.name;
                mesh.indexFormat = part.vertices.Length / 3 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                Vector3[] vertices = new Vector3[part.vertices.Length / 3];
                Vector3[] normals = new Vector3[vertices.Length];
                Vector2[] uv = new Vector2[vertices.Length];
                Vector3 pivot = Pivot(part.rig);
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new Vector3(part.vertices[i*3], part.vertices[i*3+1], part.vertices[i*3+2]) - pivot;
                    normals[i] = new Vector3(part.normals[i*3], part.normals[i*3+1], part.normals[i*3+2]);
                    uv[i] = new Vector2(part.uv[i*2], part.uv[i*2+1]);
                }
                mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = part.triangles;
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                EditorUtility.SetDirty(mesh); meshes[part.name] = mesh;
                if (!materials.ContainsKey(part.material)) materials[part.material] = Surface(part.material);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Echoes] Original Blender pilot baked: " + data.parts.Length + " mesh/material groups.");
        }

        static Material Surface(string name)
        {
            string hex = "C0BEB0";
            switch (name)
            {
                case "ClothTeal": hex="397A79"; break;
                case "ClothCream": hex="E8D9C0"; break;
                case "ClothSage": hex="6F8C75"; break;
                case "ClothOchre": hex="AA8258"; break;
                case "ClothSlate": hex="637989"; break;
                case "HairGolden": hex="987451"; break;
                case "HairDark": hex="483B34"; break;
                case "HairSilver": hex="A7A49A"; break;
                case "Straw": hex="C1AA78"; break;
                case "Feather": hex="E5DCC9"; break;
                case "FeatherShade": hex="B4B7AA"; break;
                case "Coral": hex="C86E5C"; break;
                case "Leather": hex="563D34"; break;
                case "LeatherLight": hex="96684D"; break;
                case "Hair": hex="59382C"; break;
                case "Skin": hex="E5B99D"; break;
                case "EyeWhite": hex="E5DCCF"; break;
                case "Iris": hex="345A53"; break;
                case "Trousers": hex="414C50"; break;
                case "Plaster": hex="D4C9AF"; break;
                case "Timber": hex="76543F"; break;
                case "TimberDark": hex="443D34"; break;
                case "TimberLight": hex="B58B63"; break;
                case "Bark": hex="625849"; break;
                case "StoneDark": hex="7C827C"; break;
                case "Roof": hex="566B6B"; break;
                case "RoofDark": hex="354B50"; break;
                case "RoofLight": hex="798783"; break;
                case "RoofWarm0": hex="AF6D55"; break;
                case "RoofWarm1": hex="9F624E"; break;
                case "RoofWarm2": hex="BB7D62"; break;
                case "Glass": hex="41636A"; break;
                case "Iron": hex="373B3C"; break;
                case "Brass": hex="C5A769"; break;
                case "Clay": hex="9D6E55"; break;
                case "ClayLight": hex="C39877"; break;
                case "Soil": hex="625647"; break;
                case "Path": hex="BFA788"; break;
                case "Leaf0": hex="506F56"; break;
                case "Leaf1": hex="83966A"; break;
                case "Leaf2": hex="A5B47D"; break;
                case "Petal0": hex="D3A3A2"; break;
                case "Petal1": hex="E5DCC5"; break;
                case "Petal2": hex="9D9BB8"; break;
            }
            ColorUtility.TryParseHtmlString("#"+hex, out Color color);
            string path = Folder + "/Art_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",color);
            bool metal = name=="Iron" || name=="Brass";
            float smooth = metal ? .42f : name.StartsWith("Leather") ? .29f : name=="Glass" ? .55f : .10f;
            material.SetFloat("_Smoothness",smooth); material.SetFloat("_Metallic",metal ? .65f : 0);
            material.enableInstancing = true;
            Texture2D texture = Texture(name);
            material.SetTexture("_BaseMap",texture);
            if(name!="Skin" && name!="EyeWhite" && name!="Iris")
            {
                material.SetTexture("_BumpMap",Normal(name,texture));
                material.SetFloat("_BumpScale",name.StartsWith("Cloth")?.20f:.32f);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D Texture(string name)
        {
            const int size=256;
            string path=Folder+"/Surface_"+name+".asset";
            Texture2D texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture==null) { texture=new Texture2D(size,size,TextureFormat.RGB24,true,false);AssetDatabase.CreateAsset(texture,path); }
            Color[] pixels=new Color[size*size];
            bool wood=name.StartsWith("Timber") || name=="Bark" || name=="Hair";
            bool cloth=name.StartsWith("Cloth") || name=="Coral" || name=="Trousers";
            bool skin=name=="Skin" || name=="EyeWhite" || name=="Iris";
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                float u=x/(float)size, v=y/(float)size;
                float broad=Mathf.PerlinNoise(u*9+17,v*9+31);
                float fine=Mathf.PerlinNoise(u*83+11,v*83+29);
                float value=.86f+(broad-.5f)*.16f+(fine-.5f)*.10f;
                if (wood) value=.76f+.12f*Mathf.Sin(u*190+Mathf.Sin(v*25)*2)+.11f*broad+.04f*fine;
                if (cloth) value=.89f+.035f*Mathf.Sin(x*Mathf.PI*.5f)*Mathf.Sin(y*Mathf.PI*.5f)+.035f*(fine-.5f);
                if (skin) value=.96f+.02f*(fine-.5f);
                pixels[y*size+x]=new Color(value,value*.995f,value*.982f);
            }
            texture.SetPixels(pixels);texture.Apply(true,false);texture.wrapMode=TextureWrapMode.Repeat;texture.anisoLevel=4;
            EditorUtility.SetDirty(texture);return texture;
        }

        static Texture2D Normal(string name,Texture2D height)
        {
            int size=height.width;string path=Folder+"/Normal_"+name+".asset";
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(normal==null){normal=new Texture2D(size,size,TextureFormat.RGB24,true,true);AssetDatabase.CreateAsset(normal,path);}
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=height.GetPixel((x+1)%size,y).r-height.GetPixel((x+size-1)%size,y).r;
                float dy=height.GetPixel(x,(y+1)%size).r-height.GetPixel(x,(y+size-1)%size).r;
                Vector3 n=new Vector3(-dx*2,-dy*2,1).normalized;
                pixels[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f);
            }
            normal.SetPixels(pixels);normal.Apply(true,false);normal.wrapMode=TextureWrapMode.Repeat;
            EditorUtility.SetDirty(normal);return normal;
        }

        static void Ensure() { if (data==null || meshes.Count==0) Bake(); }
        static void AddParts(string rig, Transform parent)
        {
            foreach (Part p in data.parts)
            {
                if (p.rig!=rig) continue;
                GameObject go=new GameObject(p.name,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(parent,false);
                go.GetComponent<MeshFilter>().sharedMesh=meshes[p.name];
                go.GetComponent<MeshRenderer>().sharedMaterial=materials[p.material];
            }
        }

        public static GameObject Hero(MaterialLibrary m) => Human("Hero", "", m);
        public static GameObject Human(string name,string profile,MaterialLibrary m)
        {
            Ensure();
            GameObject hero=new GameObject(name);
            Transform body=Pieces.Group("Body",hero.transform,Vector3.zero);
            Dictionary<string,Transform> nodes=new Dictionary<string,Transform>{{"Body",body}};
            foreach (string tag in new[]{"Head","ArmL","ArmR","LegL","LegR","Ponytail","ScarfTail","Eye-1","Eye1"})
            {
                Transform parent=tag.StartsWith("Leg") ? hero.transform : (tag.StartsWith("Eye") || tag=="Ponytail") ? nodes["Head"] : body;
                Vector3 offset=(parent==body || parent==hero.transform) ? Vector3.zero : Pivot("Head");
                nodes[tag]=Pieces.Group(tag,parent,Pivot(tag)-offset);
            }
            foreach (var pair in nodes) AddParts((profile==""?"":profile+"_")+pair.Key,pair.Value);
            CharacterAnimator animator=hero.AddComponent<CharacterAnimator>();
            Wire.Set(animator,"body",body);Wire.Set(animator,"head",nodes["Head"]);
            Wire.Set(animator,"leftArm",nodes["ArmL"]);Wire.Set(animator,"rightArm",nodes["ArmR"]);
            Wire.Set(animator,"leftLeg",nodes["LegL"]);Wire.Set(animator,"rightLeg",nodes["LegR"]);
            Wire.Set(animator,"eyes",new[]{nodes["Eye-1"],nodes["Eye1"]});
            Wire.Set(animator,"tails",new[]{nodes["Ponytail"],nodes["ScarfTail"]});
            if(profile!="") return hero;
            // Retain the stage-lit wearable lantern without a new gameplay state.
            Transform lantern=Pieces.Group("Lantern",body,new Vector3(-.215f,.91f,0));
            Pieces.Prim(lantern,PrimitiveType.Cube,"Frame",new Vector3(0,-.07f,0),new Vector3(.075f,.115f,.075f),m["Iron"]);
            GameObject glass=Pieces.Prim(lantern,PrimitiveType.Cube,"Glass",new Vector3(0,-.07f,.04f),new Vector3(.047f,.075f,.015f),m["LanternLamp"]);
            StageGlow glow=glass.AddComponent<StageGlow>();Wire.Set(glow,"target",glass.GetComponent<Renderer>());
            Wire.Set(glow,"color",new Color(1,.80f,.42f));Wire.Set(glow,"flicker",.12f);
            return hero;
        }

        public static void Tower(Transform root)
        {
            Ensure();AddParts("Static",root);
            // Keep original physics envelopes; visible geometry has been replaced.
            HiddenCollider(root,"Foundation",new Vector3(0,.35f,0),new Vector3(5.6f,.35f,5.6f));
            HiddenCollider(root,"TowerLower",new Vector3(0,2.4f,0),new Vector3(4.4f,1.7f,4.4f));
            HiddenCollider(root,"TowerUpper",new Vector3(0,5.6f,0),new Vector3(3.7f,1.5f,3.7f));
        }
        static void HiddenCollider(Transform root,string name,Vector3 position,Vector3 scale)
        {
            var go=Pieces.Prim(root,PrimitiveType.Cylinder,name,position,scale,materials["Stone"],Col.Mesh);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
        }
        public static Transform Prop(string type,Transform parent,Vector3 position,float yaw=0,float scale=1)
        {
            Ensure();Transform root=Pieces.Group(type,parent,position,yaw);root.localScale=Vector3.one*scale;AddParts(type,root);return root;
        }
        public static Material Timber => materials["Timber"];
        public static Material Iron => materials["Iron"];
        public static Material Material(string name) { Ensure();if(!materials.ContainsKey(name))materials[name]=Surface(name);return materials[name]; }
        public static void AddModel(string rig,Transform parent) { Ensure();AddParts(rig,parent); }

        public static GameObject Tree(string name,float height,MaterialLibrary m,string leafMain,string leafTop)
        {
            GameObject tree=new GameObject(name);Transform root=tree.transform;
            Transform crown=Prop("Tree",root,Vector3.zero);
            crown.localScale=new Vector3(height>2?.78f:1, height/1.7f, height>2?.80f:1);
            var collider=tree.AddComponent<CapsuleCollider>();collider.center=new Vector3(0,height*.5f,0);collider.height=height;collider.radius=.17f;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name.EndsWith("Leaf0"))renderer.sharedMaterial=m[leafMain];
                if(renderer.name.EndsWith("Leaf1") || renderer.name.EndsWith("Leaf2"))renderer.sharedMaterial=m[leafTop];
            }
            return root.gameObject;
        }

        public static void ApplyWorldMaterials(MaterialLibrary m)
        {
            // Preserve the authoritative MaterialLibrary identities/GUIDs; share tactile surfaces.
            string[,] pairs={{"Stone","Stone"},{"StoneDark","StoneDark"},{"Wood","Timber"},{"WoodDark","TimberDark"},{"Plaster","Plaster"},{"Trunk","Bark"},{"SailCloth","ClothCream"},{"Straw","Straw"},{"SoilRich","Soil"},{"Boots","Leather"}};
            for(int i=0;i<pairs.GetLength(0);i++)
            {
                var from=Material(pairs[i,1]);var target=m[pairs[i,0]];
                target.CopyPropertiesFromMaterial(from);target.enableInstancing=true;EditorUtility.SetDirty(target);
            }
            string[] leaves={"LeafA","LeafB","GrassBlade","GrassBladeLight","GardenGreen","GardenGreenLight"};
            foreach(string name in leaves)
            {
                var target=m[name];var source=Material(name.EndsWith("Light") || name=="LeafB" ? "Leaf2":"Leaf0");
                target.CopyPropertiesFromMaterial(source);target.enableInstancing=true;EditorUtility.SetDirty(target);
            }
            foreach(string name in new[]{"Roof","Rock","StoneMoss","SoilDry","OreRock"})
            {
                var target=m[name];target.SetTexture("_BaseMap",Material(name=="Roof"?"Roof":"Stone").GetTexture("_BaseMap"));EditorUtility.SetDirty(target);
            }
        }

        public static void Dress(Transform parent,Terrain terrain,Vector3 center)
        {
            Transform group=Pieces.Group("WindmillPilot",parent,Vector3.zero);
            Vector3 Ground(float x,float z) { Vector3 p=center+new Vector3(x,0,z);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;return p; }
            // Deliberate service yard on the east side leaves Alder and the south approach clear.
            Prop("Barrel",group,Ground(3.5f,-1.3f),15);
            Prop("Barrel",group,Ground(4.1f,-.5f),-25,.85f);
            Prop("Crate",group,Ground(3.5f,-2.3f),12);
            Prop("Crate",group,Ground(3.5f,-2.3f)+Vector3.up*.65f,-4,.8f);
            WorldArt.Rope(group,Ground(4.3f,-2.3f)+Vector3.up*.025f);
            foreach (float side in new[]{-1f,1f})
            {
                Transform pot=Prop("Planter",group,Ground(side*2.5f,-3),0,1.1f);
                Prop("Flowers",pot,new Vector3(0,.40f,0),side*28,.8f);
            }
            Prop("Tree",group,Ground(-7,-1),-18,1.3f);
            Prop("Tree",group,Ground(7,2),64,1.5f);
            Prop("Tree",group,Ground(-10,8),140,1.8f);
            System.Random rng=new System.Random(5101);
            for(int i=0;i<95;i++)
            {
                float z=-13+(float)rng.NextDouble()*16;
                float side=i%2==0?-1:1;
                float x=side*(3.5f+(float)rng.NextDouble()*3.3f);
                Vector3 p=Ground(x,z);
                if(Vector3.Distance(p,center)<3.5f || Vector2.Distance(new Vector2(p.x,p.z),ValleyLayout.WindmillRestorer)<2.2f)continue;
                Prop(i%3==0?"Flowers":"Broadleaf",group,p,(float)rng.NextDouble()*360,.8f+(float)rng.NextDouble()*.6f);
            }
            Pieces.MakeStatic(group.gameObject);
            Path(group,terrain,center);
        }

        static void Path(Transform group,Terrain terrain,Vector3 center)
        {
            // Native terrain blending avoids a hard decal edge and leaves collision untouched.
            const string path=Folder+"/WindmillTrail.terrainlayer";
            TerrainLayer layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if(layer==null){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,path);}
            Texture2D gray=Texture("Path");
            string texturePath=Folder+"/TrailEarth.asset";
            Texture2D color=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(color==null){color=new Texture2D(256,256,TextureFormat.RGB24,true,false);AssetDatabase.CreateAsset(color,texturePath);}
            Color[] pixels=gray.GetPixels();
            for(int i=0;i<pixels.Length;i++)pixels[i]*=new Color(.88f,.77f,.62f,1);
            color.SetPixels(pixels);color.Apply(true,false);color.wrapMode=TextureWrapMode.Repeat;color.anisoLevel=4;EditorUtility.SetDirty(color);
            layer.diffuseTexture=color;layer.normalMapTexture=Normal("Path",gray);layer.normalScale=.22f;
            layer.tileSize=new Vector2(2.5f,2.5f);layer.smoothness=.02f;EditorUtility.SetDirty(layer);
            TerrainData td=terrain.terrainData;
            var layers=new List<TerrainLayer>(td.terrainLayers);
            int index=layers.IndexOf(layer);
            if(index<0){index=layers.Count;layers.Add(layer);}
            string grassPath=Folder+"/PilotMeadow.terrainlayer";
            var meadow=AssetDatabase.LoadAssetAtPath<TerrainLayer>(grassPath);
            if(meadow==null){meadow=new TerrainLayer();AssetDatabase.CreateAsset(meadow,grassPath);}
            string grassTexturePath=Folder+"/MeadowEarth.asset";
            var grassTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(grassTexturePath);
            if(grassTexture==null){grassTexture=new Texture2D(256,256,TextureFormat.RGB24,true,false);AssetDatabase.CreateAsset(grassTexture,grassTexturePath);}
            Color[] grassPixels=gray.GetPixels();
            for(int i=0;i<grassPixels.Length;i++)grassPixels[i]*=new Color(.58f,.67f,.48f,1);
            grassTexture.SetPixels(grassPixels);grassTexture.Apply(true,false);grassTexture.wrapMode=TextureWrapMode.Repeat;grassTexture.anisoLevel=4;EditorUtility.SetDirty(grassTexture);
            meadow.diffuseTexture=grassTexture;meadow.tileSize=new Vector2(4.6f,4.6f);meadow.smoothness=0;EditorUtility.SetDirty(meadow);
            int grassIndex=layers.IndexOf(meadow);
            if(grassIndex<0){grassIndex=layers.Count;layers.Add(meadow);}
            float[,,] old=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
            int oldCount=old.GetLength(2);
            td.terrainLayers=layers.ToArray();
            var weights=new float[td.alphamapHeight,td.alphamapWidth,layers.Count];
            for(int z=0;z<td.alphamapHeight;z++)for(int x=0;x<td.alphamapWidth;x++)
            {
                float wx=terrain.transform.position.x+(x+.5f)/td.alphamapWidth*td.size.x;
                float wz=terrain.transform.position.z+(z+.5f)/td.alphamapHeight*td.size.z;
                float localZ=wz-center.z;
                float cx=center.x-1.7f+Mathf.SmoothStep(0,1,Mathf.InverseLerp(-16,-3,localZ))*1.7f;
                float wobble=(Mathf.PerlinNoise(wx*.8f,wz*.8f)-.5f)*.4f;
                float width=1.35f+wobble;
                float mask=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width,width+.8f,Mathf.Abs(wx-cx))))
                    *Mathf.SmoothStep(0,1,Mathf.InverseLerp(-18,-15,localZ))
                    *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-1,2,localZ)));
                float grass=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(13,21,Vector2.Distance(new Vector2(wx,wz),new Vector2(center.x,center.z))));
                for(int i=0;i<oldCount;i++)weights[z,x,i]=old[z,x,i]*(1-mask)*(1-grass);
                weights[z,x,grassIndex]+=grass*(1-mask);
                weights[z,x,index]+=mask;
            }
            td.SetAlphamaps(0,0,weights);EditorUtility.SetDirty(td);
            AssetDatabase.DeleteAsset(Folder+"/WindmillTrail.asset"); // obsolete pilot-only mesh, never user content
        }
    }
}
