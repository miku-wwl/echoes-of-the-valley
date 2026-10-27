using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.EditorTools
{
    /// <summary>Only the selected garden/workshop/north-path corridor. Does not change navigation or restoration groups.</summary>
    internal static class Phase52EnvironmentBuilder
    {
        const string Folder=Phase52HeroBuilder.Folder;
        static readonly Vector2[][] Routes={new[]{new Vector2(11,-13),new Vector2(9.5f,-9.5f),new Vector2(7,-6),new Vector2(9,1),new Vector2(15,4),new Vector2(21,8)},new[]{new Vector2(9,1),new Vector2(8,8),new Vector2(0,9),new Vector2(1.5f,19),new Vector2(4.5f,28.5f)}};
        internal static float Distance(Vector2 p) => Routes.Min(line=>ValleyLayout.DistanceToPolyline(p,line));
        static bool InPilot(Vector3 p) => Distance(new Vector2(p.x,p.z))<8 || Vector2.Distance(new Vector2(p.x,p.z),ValleyLayout.Windmill)<12;

        public static void Build(Phase52Pilot pilot)
        {
            Transform root=new GameObject("Phase52_RouteOnly").transform;pilot.Route=root.gameObject;
            Terrain terrain=pilot.Terrain;
            Mesh[] trees={TreeMesh(0),TreeMesh(1),TreeMesh(2)};
            Material leaves=Leaves();
            Material bark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Echoes/Art/Pilot/Art_Bark.mat");
            var old=new List<Renderer>();int index=0;
            // Preserve original trunk capsules; replace visible models at the same roots.
            foreach(var tree in Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.z).ToArray())
            {
                if(!tree.name.StartsWith("Tree_") || !InPilot(tree.transform.position))continue;
                Renderer[] renderers=tree.GetComponentsInChildren<Renderer>();old.AddRange(renderers);
                AddTree(tree.transform.position,tree.transform.eulerAngles.y,index++);
            }
            foreach(var tree in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).ToArray())
            {
                if(!tree.name.StartsWith("Tree") || tree.GetComponent<Renderer>()!=null || tree.GetComponent<Collider>()!=null || !InPilot(tree.position))continue;
                if(tree.GetComponentInParent<Collider>()!=null)continue;
                var renderers=tree.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)continue;
                old.AddRange(renderers);AddTree(tree.position,tree.eulerAngles.y,index++);
            }
            pilot.ReplacedTreeRenderers=old.Distinct().ToArray();
            foreach(var renderer in pilot.ReplacedTreeRenderers)renderer.enabled=false;
            Vector2[] framing={new Vector2(-4.8f,18),new Vector2(8.5f,20),new Vector2(12.5f,10),new Vector2(2,11.5f),new Vector2(13,-6)};
            foreach(Vector2 p in framing)
                if(!ValleyLayout.NearGameplaySpot(p,1.7f))AddTree(Ground(p),index*47,index++);
            Mesh plants=PlantMesh();var rng=new System.Random(5202);
            foreach(Vector2[] route in Routes)
                for(int segment=0;segment<route.Length-1;segment++)
                {
                    Vector2 a=route[segment],b=route[segment+1],normal=new Vector2(-(b-a).y,(b-a).x).normalized;
                    int count=Mathf.CeilToInt(Vector2.Distance(a,b)*1.8f);
                    for(int i=0;i<count;i++)
                    {
                        float t=(i+.3f)/(count+.1f);float sign=i%2==0?-1:1;
                        Vector2 p=Vector2.Lerp(a,b,t)+normal*sign*(2.0f+(float)rng.NextDouble()*1.5f);
                        if(ValleyLayout.NearGameplaySpot(p,1.6f) || Vector2.Distance(p,ValleyLayout.Cottage)<4.2f || Vector2.Distance(p,ValleyLayout.Garden)<4.2f)continue;
                        var go=new GameObject("RouteFern",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.position=Ground(p);go.transform.localScale=Vector3.one*(.65f+(float)rng.NextDouble()*.55f);go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
                        go.GetComponent<MeshFilter>().sharedMesh=plants;go.GetComponent<MeshRenderer>().sharedMaterial=leaves;
                    }
                }
            pilot.PilotTerrain=PaintTerrain(pilot.BaselineTerrain,terrain.transform.position);
            terrain.terrainData=pilot.PilotTerrain;terrain.GetComponent<TerrainCollider>().terrainData=pilot.PilotTerrain;
            Debug.Log("[Echoes][P52] bounded route: "+index+" tree models, "+pilot.ReplacedTreeRenderers.Length+" old renderers swapped; original colliders retained.");

            Vector3 Ground(Vector2 p){Vector3 v=new Vector3(p.x,0,p.y);v.y=terrain.SampleHeight(v)+terrain.transform.position.y;return v;}
            void AddTree(Vector3 position,float yaw,int n)
            {
                var go=new GameObject(new[]{"Oak","Aspen","Willow"}[n%3]+"_Pilot",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw+17*n,0);go.transform.localScale=Vector3.one*(.88f+(n%4)*.065f);
                go.GetComponent<MeshFilter>().sharedMesh=trees[n%3];var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{bark,leaves};
                var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,new Renderer[]{renderer})});lod.RecalculateBounds();
            }
        }

        static Material Leaves()
        {
            const int size=512;string texPath=Folder+"/PaintedLeafClusters.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if(texture==null){texture=new Texture2D(size,size,TextureFormat.RGBA32,true,false);AssetDatabase.CreateAsset(texture,texPath);}
            var pixels=new Color[size*size];var random=new System.Random(5203);
            // Original pointed leaves, connected into an asymmetric brush-painted canopy cluster.
            for(int leaf=0;leaf<220;leaf++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble());
                float cx=256+Mathf.Cos(a)*r*174,cy=248+Mathf.Sin(a)*r*181;
                float angle=a*.6f+(float)random.NextDouble()*2,rx=17+(float)random.NextDouble()*18,ry=6+(float)random.NextDouble()*8;
                Color color=Color.Lerp(new Color(.22f,.34f,.17f),new Color(.58f,.69f,.32f),cy/512f*.60f+(float)random.NextDouble()*.35f);
                int radius=Mathf.CeilToInt(rx+ry);
                for(int y=Mathf.Max(0,(int)cy-radius);y<Mathf.Min(size,(int)cy+radius);y++)for(int x=Mathf.Max(0,(int)cx-radius);x<Mathf.Min(size,(int)cx+radius);x++)
                {
                    float dx=x-cx,dy=y-cy,u=(dx*Mathf.Cos(angle)+dy*Mathf.Sin(angle))/rx,v=(-dx*Mathf.Sin(angle)+dy*Mathf.Cos(angle))/ry;
                    float shape=1-Mathf.Abs(u);if(shape<=0 || Mathf.Abs(v)>Mathf.Sqrt(shape)*(1-.18f*Mathf.Abs(u)))continue;
                    Color c=color*(.87f+.13f*(v+1)*.5f);c.a=1;pixels[y*size+x]=c;
                }
            }
            texture.SetPixels(pixels);texture.Apply(true,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;texture.anisoLevel=2;EditorUtility.SetDirty(texture);
            string path=Folder+"/PilotLeaves.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.45f);material.SetFloat("_Cull",0);material.SetFloat("_Smoothness",.05f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;material.SetOverrideTag("RenderType","TransparentCutout");material.enableInstancing=true;material.SetTexture("_EmissionMap",texture);material.SetColor("_EmissionColor",new Color(.055f,.055f,.055f));material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);return material;
        }

        static Mesh TreeMesh(int kind)
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var bark=new List<int>();var leaf=new List<int>();
            float height=kind==1?6.4f:kind==2?4.7f:5.0f;
            Tube(new[]{Vector3.zero,new Vector3(.11f,1.1f,0),new Vector3(-.09f,2.2f,.10f),new Vector3(.1f,height-.4f,.04f)},new[]{.22f,.16f,.12f,.035f});
            int branches=kind==1?11:15;
            for(int i=0;i<branches;i++)
            {
                float a=i*2.39996f;float y=kind==1?2.2f+i*.29f:2.1f+(i%5)*.42f;
                float width=kind==1?1.0f:(kind==2?2.0f:1.75f);float radial=width*(.55f+.45f*Mathf.Sin(i*1.71f)*Mathf.Sin(i*1.71f));
                Vector3 start=new Vector3(0,y,0),end=new Vector3(Mathf.Cos(a)*radial,y+.7f,Mathf.Sin(a)*radial);
                if(kind==2)end.y-=.35f;
                Tube(new[]{start,Vector3.Lerp(start,end,.55f)+Vector3.up*.23f,end},new[]{.075f,.045f,.008f});
                float size=kind==1?.93f:1.25f;
                for(int face=0;face<3;face++)Card(end,new Vector3(Mathf.Cos(a+face*Mathf.PI/3),.18f,Mathf.Sin(a+face*Mathf.PI/3)).normalized*size,Vector3.up*(kind==2?1.05f:.89f)*size,leaf);
            }
            return Save("Tree_"+kind,v,n,uv,new[]{bark,leaf});
            void Tube(Vector3[] points,float[] radii)
            {
                int offset=v.Count,sides=9;
                for(int j=0;j<points.Length;j++)
                {
                    Vector3 direction=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(0,j-1)]).normalized;
                    Vector3 x=Vector3.Cross(direction,Vector3.forward).normalized;if(x.sqrMagnitude<.1f)x=Vector3.right;Vector3 z=Vector3.Cross(direction,x).normalized;
                    for(int k=0;k<sides;k++){float a=k*Mathf.PI*2/sides;Vector3 normal=x*Mathf.Cos(a)+z*Mathf.Sin(a);v.Add(points[j]+normal*radii[j]);n.Add(normal);uv.Add(new Vector2(k/(float)sides,points[j].y));}
                }
                for(int j=0;j<points.Length-1;j++)for(int k=0;k<sides;k++){int a=offset+j*sides+k,b=offset+j*sides+(k+1)%sides;bark.AddRange(new[]{a,b,b+sides,a,b+sides,a+sides});}
            }
            void Card(Vector3 center,Vector3 right,Vector3 up,List<int> indices)
            {
                int o=v.Count;v.AddRange(new[]{center-right-up,center+right-up,center+right+up,center-right+up});
                Vector3 normal=(Vector3.up*.8f+(new Vector3(center.x,0,center.z)).normalized*.3f).normalized;
                n.AddRange(Enumerable.Repeat(normal,4));uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});indices.AddRange(new[]{o,o+1,o+2,o,o+2,o+3});
            }
        }

        static Mesh PlantMesh()
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI/2.5f;Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.36f,up=new Vector3(Mathf.Sin(a)*.15f,.42f,Mathf.Cos(a)*.15f);int o=v.Count;
                v.AddRange(new[]{-side,side,side+up,-side+up});n.AddRange(Enumerable.Repeat(Vector3.up,4));uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});t.AddRange(new[]{o,o+1,o+2,o,o+2,o+3});
            }
            return Save("RouteFern",v,n,uv,new[]{t});
        }

        static Mesh Save(string name,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<int>[] triangles)
        {
            string path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.name=name;mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.subMeshCount=triangles.Length;
            for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }

        static TerrainData PaintTerrain(TerrainData original,Vector3 origin)
        {
            string path=Folder+"/PilotTerrain.asset";
            TerrainData data=Object.Instantiate(original);data.name="Phase52_OriginalHeights_LocalRoutePaint";
            var existing=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if(existing==null)AssetDatabase.CreateAsset(data,path);else{EditorUtility.CopySerialized(data,existing);Object.DestroyImmediate(data);data=existing;}
            var layers=data.terrainLayers.ToList();int meadow=layers.Count;layers.Add(Layer("RouteMeadow",new Color(.43f,.53f,.30f),4.4f));int dirt=layers.Count;layers.Add(Layer("RouteDirt",new Color(.66f,.56f,.40f),2.8f));
            float[,,] old=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);data.terrainLayers=layers.ToArray();var weights=new float[data.alphamapHeight,data.alphamapWidth,layers.Count];
            for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++)
            {
                float wx=origin.x+x/(float)(data.alphamapWidth-1)*data.size.x,wz=origin.z+z/(float)(data.alphamapHeight-1)*data.size.z;float d=Distance(new Vector2(wx,wz));
                float mask=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,8,d));float noise=Mathf.PerlinNoise(wx*1.1f+30,wz*1.1f+17);float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.0f+noise*.35f,2.5f+noise*.45f,d));
                for(int i=0;i<old.GetLength(2);i++)weights[z,x,i]=old[z,x,i]*(1-mask);weights[z,x,meadow]=mask*(1-road);weights[z,x,dirt]=mask*road;
            }
            data.SetAlphamaps(0,0,weights);EditorUtility.SetDirty(data);return data;
        }
        static TerrainLayer Layer(string name,Color color,float tile)
        {
            string p=Folder+"/"+name+".terrainlayer";var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);if(layer==null){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,p);}
            string t=Folder+"/"+name+"Color.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(t);if(texture==null){texture=new Texture2D(256,256,TextureFormat.RGB24,true,false);AssetDatabase.CreateAsset(texture,t);}
            Color[] pixels=new Color[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++){float broad=Mathf.PerlinNoise(x*.027f+7,y*.027f+19),fine=Mathf.PerlinNoise(x*.32f,y*.32f);pixels[y*256+x]=color*(.88f+broad*.16f+(fine-.5f)*.08f);}
            texture.SetPixels(pixels);texture.Apply(true,false);texture.wrapMode=TextureWrapMode.Repeat;EditorUtility.SetDirty(texture);layer.diffuseTexture=texture;layer.tileSize=Vector2.one*tile;layer.smoothness=0;EditorUtility.SetDirty(layer);return layer;
        }
    }
}
