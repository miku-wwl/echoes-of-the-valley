using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>Original rural detailing. All added dressing is visual-only; original collision and interaction points remain.</summary>
    internal static class WorldArt
    {
        static GameObject Beam(Transform p,string n,Vector3 at,Vector3 size,Material m,Vector3 euler=default) => Pieces.Prim(p,PrimitiveType.Cube,n,at,size,m,Col.None,euler);

        public static void Rope(Transform parent,Vector3 position)
        {
            for(int ring=0;ring<3;ring++)
            {
                var go=new GameObject("CoiledWorkRope",typeof(LineRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position+Vector3.up*(ring*.018f);
                var line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=40;line.widthMultiplier=.018f;line.generateLightingData=true;
                line.sharedMaterial=PilotArt.Material("TimberLight");line.shadowCastingMode=ShadowCastingMode.Off;
                for(int i=0;i<40;i++){float a=i*Mathf.PI/20;line.SetPosition(i,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.13f+ring*.025f));}
            }
        }

        public static void TreeLine(Transform parent,Terrain terrain)
        {
            Transform root=Pieces.Group("DistantTreeLine",parent,Vector3.zero);
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12;float radius=55+3*Mathf.Sin(i*2.31f);
                Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
                var tree=PilotArt.Prop("Tree",root,p,i*137,1.05f+.35f*Mathf.Sin(i*1.7f));
                foreach(var r in tree.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            }
            Pieces.MakeStatic(root.gameObject);
        }

        public static void Cottage(Transform root,MaterialLibrary m)
        {
            Transform details=Pieces.Group("CraftedCottageDetails",root,Vector3.zero);
            for(int side=-1;side<=1;side+=2)
            {
                for(int row=0;row<12;row++)for(int col=0;col<10;col++)
                {
                    float x=side*(.10f+row*.235f),z=-2.19f+col*.46f+(row%2)*.08f;
                    Beam(details,"OverlappingShingle",new Vector3(x,5.42f-Mathf.Abs(x)-row*.01f,z),new Vector3(.37f,.10f,.445f),PilotArt.Material("RoofWarm"+((row+col*7)%3)),new Vector3(0,0,-side*45));
                }
                for(int z=-1;z<=1;z+=2)
                    Beam(details,"CornerPost",new Vector3(side*2.52f,1.42f,z*2.035f),new Vector3(.15f,2.42f,.15f),m["WoodDark"]);
                Beam(details,"GableBrace",new Vector3(side*1.15f,3.70f,2.04f),new Vector3(.12f,3.16f,.12f),m["WoodDark"],new Vector3(0,0,side*45));
                Beam(details,"WallRail",new Vector3(0,2.63f,side*2.04f),new Vector3(5.1f,.14f,.11f),m["WoodDark"]);
                Beam(details,"WindowMullionV",new Vector3(side*1.65f,1.6f,2.135f),new Vector3(.04f,.78f,.025f),m["Wood"]);
                Beam(details,"WindowMullionH",new Vector3(side*1.65f,1.6f,2.14f),new Vector3(.78f,.045f,.025f),m["Wood"]);
                foreach(string prefix in new[]{"BoxBloomA","BoxBloomB"})
                {
                    Transform old=root.Find(prefix+side);if(old!=null)Object.DestroyImmediate(old.gameObject);
                }
                PilotArt.Prop("Flowers",details,new Vector3(side*1.65f,1.02f,2.25f),side*16,.8f);
                Transform herbs=PilotArt.Prop("Broadleaf",details,new Vector3(side*2.08f,2.43f,2.18f),0,.9f);
                herbs.localRotation=Quaternion.Euler(180,side*25,0);
                Beam(details,"HerbTie",new Vector3(side*2.08f,2.53f,2.18f),new Vector3(.018f,.22f,.018f),m["Straw"]);
            }
            Transform ridge=root.Find("RidgeCap");if(ridge!=null){var p=ridge.localPosition;p.y=5.34f;ridge.localPosition=p;}
            for(int i=0;i<7;i++)Beam(details,"OakDoorPlank",new Vector3(-.43f+i*.142f,1.1f,2.12f),new Vector3(.133f,1.72f,.03f),m["Wood"]);
            Beam(details,"DoorLatch",new Vector3(.32f,1.13f,2.17f),new Vector3(.05f,.13f,.05f),m["Iron"]);
            for(int i=0;i<10;i++)for(int side=-1;side<=1;side+=2)
            {
                float x=-2.25f+i*.5f;if(side==1 && Mathf.Abs(x)<.8f)continue;
                Beam(details,"FieldstoneFooting",new Vector3(x,.28f,side*2.035f),new Vector3(.47f,.21f,.16f),m[i%3==0?"StoneDark":"Stone"]);
            }
            for(int level=0;level<2;level++)
            {
                Beam(details,"AlchemyShelf",new Vector3(-2.68f,1.0f+level*.55f,.25f),new Vector3(.38f,.07f,1.9f),m["Wood"]);
                for(int i=0;i<4;i++)
                {
                    var pot=PilotArt.Prop("Planter",details,new Vector3(-2.72f,1.04f+level*.55f,-.45f+i*.45f),0,.43f);
                    PilotArt.Prop("Broadleaf",pot,new Vector3(0,.39f,0),i*35,.7f);
                }
            }
            Transform yard=root.Find("Yard");
            foreach(string name in new[]{"BarrelA","BarrelB"})
            {
                Transform old=yard.Find(name);
                if(old==null)continue;
                var renderer=old.GetComponent<MeshRenderer>();if(renderer!=null)Object.DestroyImmediate(renderer);
                var p=old.localPosition;p.y=0;PilotArt.Prop("Barrel",yard,p,name=="BarrelA"?8:-20,1.15f);
            }
            Transform crate=yard.Find("HerbCrate");
            if(crate!=null)
            {
                Object.DestroyImmediate(crate.GetComponent<MeshRenderer>());
                var p=crate.localPosition;p.y=0;var visual=PilotArt.Prop("Crate",yard,p);visual.localScale=new Vector3(1.34f,.81f,.98f);
                PilotArt.Prop("Broadleaf",yard,p+Vector3.up*.5f,20,1.4f);
                Transform old=yard.Find("Herbs");if(old!=null)Object.DestroyImmediate(old.gameObject);
            }
        }

        public static void Resources(GameObject root,bool flowers,MaterialLibrary m)
        {
            var ready=root.transform.Find("Ready");var depleted=root.transform.Find("Depleted");
            foreach(Transform child in new List<Transform>(Children(ready)))Object.DestroyImmediate(child.gameObject);
            foreach(Transform child in new List<Transform>(Children(depleted)))Object.DestroyImmediate(child.gameObject);
            var at=PilotArt.Prop(flowers?"Flowers":"Broadleaf",ready,Vector3.zero,0,flowers?1.5f:1.8f);
            if(flowers)foreach(var renderer in at.GetComponentsInChildren<MeshRenderer>())if(renderer.name.Contains("Petal"))renderer.sharedMaterial=m["BlossomCoral"];
            if(!flowers)foreach(var renderer in at.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=m[renderer.name.EndsWith("Leaf0")?"HerbDark":"HerbLeaf"];
            PilotArt.Prop("Broadleaf",depleted,Vector3.zero,27,.45f);
            Pieces.NoShadows(root);
        }
        static IEnumerable<Transform> Children(Transform root){for(int i=0;i<root.childCount;i++)yield return root.GetChild(i);}

        static Mesh rock;
        public static Mesh Rock()
        {
            if(rock!=null)return rock;
            const string path=AssetPaths.Root+"/Art/Pilot/RoughStone.asset";
            rock=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(rock==null){rock=new Mesh();AssetDatabase.CreateAsset(rock,path);}
            var source=new List<Vector3>();var indices=new List<int>();
            for(int row=0;row<5;row++)for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*.2f;float radius=new[]{.19f,.43f,.50f,.40f,.16f}[row]*(1+.14f*Mathf.Sin(i*3.13f+row*4.77f));
                source.Add(new Vector3(Mathf.Cos(a)*radius,row*.25f-.5f,Mathf.Sin(a)*radius));
            }
            for(int row=0;row<4;row++)for(int i=0;i<10;i++)
            {
                int a=row*10+i,b=row*10+(i+1)%10;
                indices.AddRange(new[]{a,a+10,b+10,a,b+10,b});
            }
            source.Add(new Vector3(0,-.5f,0));source.Add(new Vector3(0,.5f,0));
            for(int i=0;i<10;i++)indices.AddRange(new[]{50,i,(i+1)%10,51,40+(i+1)%10,40+i});
            var v=new Vector3[indices.Count];var uv=new Vector2[v.Length];var t=new int[v.Length];
            for(int i=0;i<v.Length;i++){v[i]=source[indices[i]];uv[i]=new Vector2(v[i].x,v[i].y+v[i].z);t[i]=i;}
            rock.Clear();rock.vertices=v;rock.uv=uv;rock.triangles=t;rock.RecalculateNormals();rock.RecalculateTangents();rock.RecalculateBounds();EditorUtility.SetDirty(rock);return rock;
        }

        public static void Banks(Transform parent,Terrain terrain,MaterialLibrary m)
        {
            Transform root=Pieces.Group("StreamBankDetails",parent,Vector3.zero);
            for(int i=2;i<ValleyLayout.Stream.Count-2;i+=2)for(int side=-1;side<=1;side+=2)
            {
                Vector2 at=ValleyLayout.Stream[i];Vector2 d=(ValleyLayout.Stream[i+1]-ValleyLayout.Stream[i-1]).normalized;
                Vector2 p=at+new Vector2(-d.y,d.x)*side*(1.8f+.18f*Mathf.Sin(i));
                Vector3 foot=new Vector3(p.x,0,p.y);foot.y=terrain.SampleHeight(foot)+terrain.transform.position.y;
                var stone=new GameObject("BankPebble",typeof(MeshFilter),typeof(MeshRenderer));stone.transform.SetParent(root,false);stone.transform.position=foot;
                stone.transform.localScale=new Vector3(.32f,.20f,.28f);stone.GetComponent<MeshFilter>().sharedMesh=Rock();stone.GetComponent<MeshRenderer>().sharedMaterial=m["StoneMoss"];
                if(i%4==0)PilotArt.Prop("Broadleaf",root,foot+new Vector3(.25f,0,.05f),i*33,.8f);
            }
            Pieces.MakeStatic(root.gameObject);
        }

        public static Sprite Icon(string id)
        {
            const int size=64;string path=AssetPaths.Root+"/Art/Pilot/Icon_"+id+".asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null){texture=new Texture2D(size,size,TextureFormat.RGBA32,false,false);AssetDatabase.CreateAsset(texture,path);}
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size-.5f,v=(y+.5f)/size-.5f;bool ink=false;
                switch(id)
                {
                    case ItemIds.Herb:float a=(u+v)*.7071f,b=(v-u)*.7071f;ink=a*a/.18f+b*b/.045f<1 && Mathf.Abs(b)>.012f;break;
                    case ItemIds.Flower:for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;ink|=Vector2.Distance(new Vector2(u,v),new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.23f)<.17f;}ink|=u*u+v*v<.02f;break;
                    case ItemIds.Ore:ink=Mathf.Abs(u)/.38f+Mathf.Abs(v)/.46f<1 && Mathf.Abs(u-v*.27f)>.018f;break;
                    case ItemIds.Egg:ink=u*u/Mathf.Pow(.29f-v*.08f,2)+(v+.015f)*(v+.015f)/.18f<1;break;
                    case ItemIds.ChickenFeed:ink=(Mathf.Abs(u)<.31f && v>-.40f && v<.22f)||(Mathf.Abs(u)<.20f && v>.24f && v<.39f);ink&=!(Mathf.Abs(u)<.012f && v<.08f && v>-.22f);break;
                    case ItemIds.SpringTonic:ink=(Mathf.Abs(u)<.27f && v>-.40f && v<.10f)||(Mathf.Abs(u)<.10f && v>.06f && v<.37f)||(Mathf.Abs(u)<.15f && v>.34f && v<.43f);ink&=Mathf.Abs(v+.07f)>.018f;break;
                    case ItemIds.WindmillBraces:ink=(Mathf.Abs(u+v*.45f-.16f)<.055f || Mathf.Abs(u+v*.45f+.16f)<.055f)&&Mathf.Abs(v)<.43f;ink|=Mathf.Abs(u)<.33f && (Mathf.Abs(v-.24f)<.04f || Mathf.Abs(v+.24f)<.04f);break;
                }
                pixels[y*size+x]=ink?Color.white:Color.clear;
            }
            texture.SetPixels(pixels);texture.Apply(false,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;EditorUtility.SetDirty(texture);
            Sprite sprite=null;foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite s)sprite=s;
            if(sprite==null){sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),64);sprite.name="Icon_"+id;AssetDatabase.AddObjectToAsset(sprite,texture);}
            return sprite;
        }
        public static Sprite[] Icons(){var list=new List<Sprite>();foreach(var item in ItemCatalog.All)list.Add(Icon(item.Id));return list.ToArray();}
    }
}
