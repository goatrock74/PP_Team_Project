using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Creates a new, editable scene once. It never overwrites user-painted scenes.
public static class CozyBeachMapBuilder
{
 const string Folder="Assets/PJH/CozyBeach", ScenePath=Folder+"/CozyBeach.unity";
 const string BeachSource="Assets/WaterTiles/Tiles/WaterTile/beach_tileset.png";
 const int Width=72,Height=64;
 static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
 static readonly Dictionary<string,Tile> tiles=new Dictionary<string,Tile>();
 static readonly List<Candidate> candidates=new List<Candidate>();
 sealed class Candidate {public string name;public bool[] land;}
 // Script reloads must never repaint a user-edited scene.
 static void FixSolidTiles(){
 if(!File.Exists(ScenePath)||File.Exists(Folder+"/SolidTilesFixed.txt")||EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.IsValid()||scene.isDirty)return;
 try{
 ImportSheet("FlatLand","Assets/_Sprite/Tiles/Summer/Basic Tiles 1.png",new[]{Def("SandPlain",336,112,16,16)});
 ImportSheet("FlatWater","Assets/WaterTiles/Tiles/WaterTile/Water_Tileset.png",new[]{Def("WaterPlain",48,48,16,16)});
 foreach(var name in new[]{"SandPlain","WaterPlain"}){var t=AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/Tiles/"+name+".asset");t.sprite=sprites[name];t.transform=Matrix4x4.identity;EditorUtility.SetDirty(t);}
 foreach(var m in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Tilemap>()))m.RefreshAllTiles();
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ExportOverview();File.WriteAllText(Folder+"/SolidTilesFixed.txt","Native 16x16 solid ground and water tiles.\n");Debug.Log("[CozyBeach] Native 16x16 ground tiles fixed.");
 }catch(Exception e){Debug.LogException(e);}
 }
 static void RefineOnce(){
 if(!File.Exists(ScenePath)||File.Exists(Folder+"/LayoutRefined.txt")||EditorApplication.isPlayingOrWillChangePlaymode)return;
 var s=SceneManager.GetSceneByPath(ScenePath);if(!s.IsValid()||s.isDirty)return;
 try{
 ImportSheet("Beach",BeachSource,BeachRects());Analyze();
 foreach(var key in new[]{"SandPlain","WaterPlain"}){
 var t=AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/Tiles/"+key+".asset");t.sprite=sprites[key];t.transform=Matrix4x4.Scale(new Vector3(16,16,1));EditorUtility.SetDirty(t);tiles[key]=t;
 }
 foreach(var t in AssetDatabase.FindAssets("t:Tile",new[]{Folder+"/Tiles"}).Select(g=>AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(g))))if(t!=null)tiles[t.name]=t;
 var maps=s.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Tilemap>()).ToArray();
 var sea=maps.First(m=>m.name=="01_SeaAndSand");sea.RefreshAllTiles();maps.First(m=>m.name=="02_GrassBorder").RefreshAllTiles();
 var deepMat=Palette("DeepWater",new Color32(103,202,197,255),new Color32(103,202,197,255),new Color32(65,158,177,255));
 var deep=Map(sea.transform.parent,"01b_DeepWater",1,deepMat);
 for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)if(x>Coast(y)+4)deep.SetTile(new Vector3Int(x,y),Choose(x-6,y,false));
 var pier=maps.First(m=>m.name=="05_WoodenPiers");pier.ClearAllTiles();
 var cells=new HashSet<Vector3Int>();
 AddPierCells(cells,32,36,25,4);AddPierCells(cells,53,29,4,14);AddPierCells(cells,31,14,22,3);AddPierCells(cells,50,10,3,11);
 foreach(var p in cells){int sx=!cells.Contains(p+Vector3Int.left)?18:!cells.Contains(p+Vector3Int.right)?22:20;int sy=!cells.Contains(p+Vector3Int.up)?9:!cells.Contains(p+Vector3Int.down)?11:10;Stamp(pier,$"Dock_{sx}_{sy}",p.x,p.y);}
 EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();ExportOverview();File.WriteAllText(Folder+"/LayoutRefined.txt","Flat sand and water, deep water band, connected piers.\n");Debug.Log("[CozyBeach] Refined coast and connected piers.");
 }catch(Exception e){Debug.LogException(e);}
 }
 static void AddPierCells(HashSet<Vector3Int> cells,int x,int y,int w,int h){for(int dy=0;dy<h;dy++)for(int dx=0;dx<w;dx++)cells.Add(new Vector3Int(x+dx,y+dy));}
 static void CreateOnce(){
 if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(ScenePath))return;
 try{Build();}catch(Exception ex){Debug.LogException(ex);}
 }
 [MenuItem("PJH/Cozy Beach/Open Created Map")]
 public static void Open(){
 if(!File.Exists(ScenePath)){Build();return;}
 var s=SceneManager.GetSceneByPath(ScenePath);
 if(!s.IsValid())s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
 SceneManager.SetActiveScene(s);FrameMap();
 }
 static void Build(){
 if(File.Exists(ScenePath))return;
 Directory.CreateDirectory(Folder+"/Tiles");Directory.CreateDirectory(Folder+"/Sprites");AssetDatabase.Refresh();
 ImportSheet("Beach",BeachSource,BeachRects());
 ImportSheet("Village","Assets/_Sprite/Tiles/Summer/Basic Tiles 1.png",new[]{
 Def("TreeGreen",39,15,33,45),Def("TreeLime",183,15,33,45),
 Def("BushGreen",81,15,46,45),Def("BushLime",225,15,46,45),
 Def("RockA",64,128,16,16),Def("RockB",96,128,16,16),
 Def("Flowers",192,144,16,16)});
 ImportSheet("House","Assets/_Sprite/Tiles/Exterior house 2.png",new[]{Def("RedRoof",336,16,160,112),Def("Door",145,36,30,30)});
 Analyze();
 var sandMat=Palette("SandAndWater",new Color32(247,220,151,255),new Color32(214,184,122,255),new Color32(103,202,197,255));
 var grassMat=Palette("GrassAndSand",new Color32(131,190,73,255),new Color32(103,158,62,255),new Color32(247,220,151,255));
 var propsMat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(propsMat,Folder+"/Props.mat");
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
 var root=new GameObject("CozyBeach_Map").transform;
 var grid=new GameObject("Grid",typeof(Grid));grid.transform.SetParent(root,false);
 var sea=Map(grid.transform,"01_SeaAndSand",0,sandMat);
 var grass=Map(grid.transform,"02_GrassBorder",1,grassMat);
 var decor=Map(grid.transform,"03_ShellsAndStones",3,propsMat);
 var waves=Map(grid.transform,"04_AnimatedWater",2,propsMat);
 var pier=Map(grid.transform,"05_WoodenPiers",5,propsMat);
 var trees=Map(grid.transform,"06_TreesAndBushes",8,propsMat);
 var hut=Map(grid.transform,"07_FishingHut",10,propsMat);
 var random=new System.Random(92126);
 for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){
 sea.SetTile(new Vector3Int(x,y),Choose(x,y,false));
 if(Grass(x+0.5f,y+0.5f)>-2)grass.SetTile(new Vector3Int(x,y),Choose(x,y,true));
 if(x>Coast(y)+4&&x<Width-2&&random.NextDouble()<0.037){
 int n=random.Next(1,13),first=166+(n-1)*4;
 var a=AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_{n:00}_{first}-{first+3}.asset");
 if(a!=null)waves.SetTile(new Vector3Int(x,y),a);
 }}
 for(int y=1;y<Height-3;y+=3){
 if(y>=29&&y<=35)continue;
 for(int x=0;x<9;x+=3)Stamp(trees,random.Next(3)==0?"TreeLime":"TreeGreen",x+random.Next(2),y);
 if(y%2==0)Stamp(trees,"BushGreen",10,y);
 }
 for(int x=10;x<31;x+=3)Stamp(trees,x%2==0?"TreeGreen":"TreeLime",x,60);
 for(int x=9;x<26;x+=4)Stamp(trees,"TreeGreen",x,0);
 foreach(var p in new[]{new Vector2Int(13,51),new Vector2Int(17,55),new Vector2Int(10,44),new Vector2Int(14,9),new Vector2Int(19,5)})Stamp(trees,"TreeLime",p.x,p.y);
 foreach(var p in new[]{new Vector2Int(19,45),new Vector2Int(26,53),new Vector2Int(18,15),new Vector2Int(26,7)})Stamp(trees,"Palm",p.x,p.y);
 for(int i=0;i<85;i++){
 int x=random.Next(12,49),y=random.Next(4,59);
 if(Coast(y)-x<2||Grass(x,y)>-1||(y>26&&y<35)||(x>25&&x<41&&y>38&&y<50))continue;
 string[] names={"ShellA","ShellB","Starfish","Pebble","SandMark1","SandMark2"};
 Stamp(decor,names[random.Next(names.Length)],x,y);
 }
 for(int i=0;i<24;i++){int x=random.Next(9,25),y=random.Next(3,60);if(Grass(x,y)>1)Stamp(decor,i%2==0?"Flowers":"RockA",x,y);}
 Pier(pier,39,36,17,4);Pier(pier,53,29,4,14);Pier(pier,31,14,22,3);Pier(pier,50,10,3,11);
 Stamp(hut,"RedRoof",27,41);Stamp(hut,"Door",31,41);
 Stamp(decor,"Crate",26,40);Stamp(decor,"Crate",37,41);Stamp(decor,"Crate",54,38);
 var area=new GameObject("SeaFishingArea",typeof(PolygonCollider2D));area.transform.SetParent(root,false);
 int waterLayer=LayerMask.NameToLayer("Water");if(waterLayer>=0)area.layer=waterLayer;
 var poly=area.GetComponent<PolygonCollider2D>();poly.isTrigger=true;
 var outline=new List<Vector2>{new Vector2(Width,0),new Vector2(Width,Height)};
 for(int y=Height;y>=0;y--)outline.Add(new Vector2(Coast(y)+1,y));poly.points=outline.ToArray();
 var type=TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(t=>t.Name=="FishingWaterArea");
 if(type!=null){var c=area.AddComponent(type);var so=new SerializedObject(c);var p=so.FindProperty("category");if(p!=null){p.enumValueIndex=0;so.ApplyModifiedPropertiesWithoutUndo();}}
 var camObj=new GameObject("BeachOverviewCamera",typeof(Camera));camObj.transform.SetParent(root,false);camObj.transform.position=new Vector3(36,32,-10);
 var camera=camObj.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=33;camera.backgroundColor=new Color32(62,133,140,255);camera.clearFlags=CameraClearFlags.SolidColor;camera.tag="MainCamera";
 if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save CozyBeach.");
 AssetDatabase.SaveAssets();Export(camera);FrameMap();
 Debug.Log("[CozyBeach] Created editable scene: "+ScenePath);
 }
 static float Coast(float y){return 40+4.3f*Mathf.Sin(y*0.105f)+2.1f*Mathf.Sin(y*0.235f+1.2f);}
 static float Grass(float x,float y){float edge=13+2.5f*Mathf.Sin(y*0.15f);if(y>54)edge+=Mathf.Min(16,(y-54)*2.3f);if(y<8)edge+=(8-y)*1.3f;if(y>28&&y<35)edge=-3;return edge-x;}
 static Tile Choose(int x,int y,bool grass){
 var mask=new bool[64];int count=0;
 for(int sy=0;sy<8;sy++)for(int sx=0;sx<8;sx++){
 float px=x+(sx+0.5f)/8,py=y+1-(sy+0.5f)/8;
 bool inside=(grass?Grass(px,py):Coast(py)-px)>0;mask[sy*8+sx]=inside;if(inside)count++;
 }
 if(count==64)return Tile("SandPlain");if(count==0)return Tile("WaterPlain");
 Candidate best=null;int score=int.MaxValue;
 foreach(var c in candidates){int error=0;for(int i=0;i<64;i++)if(c.land[i]!=mask[i])error++;if(error<score){score=error;best=c;}}
 return Tile(best.name);
 }
 static void Analyze(){
 var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(BeachSource));
 for(int y=1;y<=7;y++)for(int x=1;x<=7;x++){
 var c=new Candidate{name=$"Coast_{x}_{y}",land=new bool[64]};bool valid=true;
 for(int sy=0;sy<8;sy++)for(int sx=0;sx<8;sx++){
 var color=image.GetPixel(x*16+sx*2+1,image.height-1-(y*16+sy*2+1));if(color.a<0.9f)valid=false;c.land[sy*8+sx]=color.b<=color.r*1.08f;
 }if(valid)candidates.Add(c);
 }UnityEngine.Object.DestroyImmediate(image);
 }
 static IEnumerable<SpriteMetaData> BeachRects(){
 for(int y=1;y<=7;y++)for(int x=1;x<=7;x++)yield return Def($"Coast_{x}_{y}",x*16,y*16,16,16);
 yield return Def("SandPlain",135,145,1,1);yield return Def("WaterPlain",70,38,1,1);
 yield return Def("ShellA",208,240,16,16);yield return Def("ShellB",224,240,16,16);yield return Def("Starfish",160,240,16,16);yield return Def("Pebble",192,240,16,16);
 yield return Def("SandMark1",176,144,16,16);yield return Def("SandMark2",192,144,16,16);
 yield return Def("Palm",112,208,48,64);yield return Def("Crate",160,208,32,32);
 for(int y=9;y<=11;y++)for(int x=18;x<=22;x++)yield return Def($"Dock_{x}_{y}",x*16,y*16,16,16);
 }
 static SpriteMetaData Def(string n,int x,int y,int w,int h){return new SpriteMetaData{name=n,rect=new Rect(x,y,w,h),alignment=(int)SpriteAlignment.BottomLeft,pivot=Vector2.zero};}
 static void ImportSheet(string name,string source,IEnumerable<SpriteMetaData> rects){
 string path=Folder+"/Sprites/"+name+".png";if(!File.Exists(path))File.Copy(source,path);
 AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var defs=rects.ToArray();
 for(int i=0;i<defs.Length;i++){var d=defs[i];d.rect=new Rect(d.rect.x,texture.height-d.rect.y-d.rect.height,d.rect.width,d.rect.height);defs[i]=d;}
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsToUnits=16;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
 #pragma warning disable 618
 importer.spritesheet=defs;
 #pragma warning restore 618
 importer.SaveAndReimport();foreach(var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())sprites[s.name]=s;
 }
 static Material Palette(string name,Color land,Color wet,Color water){var m=new Material(Shader.Find("PJH/CozyBeachPalette"));m.SetColor("_Land",land);m.SetColor("_Wet",wet);m.SetColor("_Water",water);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
 static Tile Tile(string name){if(tiles.TryGetValue(name,out var t))return t;t=ScriptableObject.CreateInstance<Tile>();t.name=name;t.sprite=sprites[name];t.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;AssetDatabase.CreateAsset(t,Folder+"/Tiles/"+name+".asset");tiles[name]=t;return t;}
 static Tilemap Map(Transform parent,string name,int order,Material mat){var o=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));o.transform.SetParent(parent,false);var m=o.GetComponent<Tilemap>();m.tileAnchor=Vector3.zero;m.animationFrameRate=1;var r=o.GetComponent<TilemapRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;return m;}
 static void Stamp(Tilemap m,string n,int x,int y){m.SetTile(new Vector3Int(x,y),Tile(n));}
 static void Pier(Tilemap m,int x,int y,int w,int h){for(int dy=0;dy<h;dy++)for(int dx=0;dx<w;dx++){int sx=dx==0?18:dx==w-1?22:19+dx%3;int sy=dy==0?11:dy==h-1?9:10;Stamp(m,$"Dock_{sx}_{sy}",x+dx,y+dy);}}
 static void FrameMap(){if(SceneView.lastActiveSceneView==null)return;SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.LookAt(new Vector3(36,32,0),Quaternion.identity,37,true,true);}
 [MenuItem("PJH/Cozy Beach/Export Overview")]
 public static void ExportOverview(){var s=SceneManager.GetSceneByPath(ScenePath);if(!s.IsValid())return;var c=s.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).FirstOrDefault();if(c!=null)Export(c);}
 static void Export(Camera c){var rt=new RenderTexture(1152,1056,24);rt.filterMode=FilterMode.Point;var old=RenderTexture.active;c.targetTexture=rt;c.aspect=1152f/1056;c.Render();RenderTexture.active=rt;var t=new Texture2D(1152,1056,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1152,1056),0,0);t.Apply();File.WriteAllBytes(Folder+"/CozyBeach_Overview.png",t.EncodeToPNG());c.targetTexture=null;c.ResetAspect();RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
}
