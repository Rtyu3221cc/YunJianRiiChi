#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace YunJian.Riichi {
 // 1012-2 review: APK tile toon lighting, effect_shadow, sun_shine, caption band,
 // native name/title sizes and the centred UI_ChiPengHu button row.
 public static class ReviewValidation1012b {
  static void Check(bool v,string s){if(!v)throw new Exception(s);}
  static bool Near(Color a,Color b)=>Mathf.Abs(a.r-b.r)<.002f&&Mathf.Abs(a.g-b.g)<.002f&&Mathf.Abs(a.b-b.b)<.002f;
  static RoomView Load(string folder,string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));
  static Rect Bounds(Transform root,Camera camera){Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);foreach(var c in root.GetComponentsInChildren<BoxCollider>(true))for(int i=0;i<8;i++){var offset=Vector3.Scale(c.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=camera.WorldToScreenPoint(c.transform.TransformPoint(c.center+offset));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
  static void Size(int w,int h){var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{new Vector2(w,h),"1012-2 table look"});game.Repaint();}
  static RoomView Reaction(string folder){
   var v=Load(folder,"preview-3");v.audioEvents=new PublicSound[0];v.waitDetails=new WaitDetail[0];v.discardHints=new DiscardHint[0];
   int from=Enumerable.Range(1,v.n-1).First(i=>v.players[i].river.Length>0);var tile=v.players[from].river.Last().tile;
   v.phase="reaction";v.turn=from;v.last=new LastDiscard{from=from,tile=tile};v.pending=new PendingView{from=from,tile=tile,kind="discard",waiting=new[]{v.you}};
   v.options=new[]{new Choice{id="minkan:"+tile[0]+new string(tile[1],3)+tile[1]+"-",kind="minkan",meld=tile[0]+new string(tile[1],3)+tile[1]+"-"},new Choice{id="pass",kind="pass"}};
   return v;
  }
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();
   var light=(Vector3)Shader.GetGlobalVector("_MjLightDir");
   Check(Vector3.Distance(light,TableLook.LightDirection)<.0001f&&Mathf.Abs(light.y-.8937f)<.002f&&Mathf.Abs(light.z-.4484f)<.002f&&Mathf.Abs(light.x)<.0001f,"scene_mj light direction not converted: "+light);
   var sun=UnityEngine.Object.FindFirstObjectByType<SunShine>();Check(sun!=null&&sun.Particles.Length==3,"APK sun_shine emitters missing");
   Check(sun.Particles.All(p=>p.main.loop&&p.main.scalingMode==ParticleSystemScalingMode.Hierarchy),"sun_shine modules not looping/scaled");
   foreach(var res in new[]{new Vector2(2760,1256),new Vector2(936,435),new Vector2(1280,720)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;
    client.ShowValidationFixture(Reaction(folder));for(int i=0;i<4;i++)yield return null;
    var state=GameObject.Find("PublicTableState").transform;var renderers=state.GetComponentsInChildren<MeshRenderer>(true);
    var faces=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.shader.name=="YunJian/OriginalTileFace").Distinct().ToArray();
    var bodies=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.shader.name=="YunJian/OriginalTileBody").Distinct().ToArray();
    Check(faces.Length>0&&faces.All(m=>Mathf.Abs(m.GetFloat("_SplitThreshold")-.08f)<.0001f&&Near(m.GetColor("_ColorUnlight"),new Color(.8113208f,.8113208f,.8113208f))),"Face toon parameters differ from mjp_face");
    Check(bodies.Length==1&&bodies[0].GetTexture("_TileTex")!=null&&Mathf.Abs(bodies[0].GetTextureScale("_TileTex").x-.1f)<.0001f&&Mathf.Abs(bodies[0].GetTextureOffset("_TileTex").x-.9f)<.0001f,"Body is not the mjp_mid back/face-column mix");
    var outline=renderers.First(r=>r.name=="Original tile outline").sharedMaterial;Check(Near(outline.color,new Color(.165f,.192f,.204f)),"Outline is not the Catfood/Outline _Tint");
    var shadows=renderers.Where(r=>r.name=="Contact shadow").ToArray();var foot=TableLook.ShadowFootprint;
    Check(shadows.Length>30&&shadows.All(r=>r.sharedMaterial.shader.name=="YunJian/TileShadow"&&r.sharedMaterial.mainTexture!=null&&r.sharedMaterial.mainTexture.name=="shadow"),"Tiles lack the original effect_shadow");
    Check(Mathf.Abs(foot.x-1.4071f)<.002f&&Mathf.Abs(foot.y-1.3978f)<.002f,"Shadow footprint differs from code.js .53 x PAIMODEL: "+foot);
    Check(sun.Particles.Sum(p=>p.particleCount)>5,"sun_shine emits nothing (prewarm)");
    var view=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,true);var rects=client.ActionRects(view.Width,view.Height);float u=Mathf.Min(view.Width/1920f,view.Height/1080f);
    Check(rects.Length==2&&Mathf.Abs(rects[1].center.x-(view.Width/2+355*u))<.01f&&Mathf.Abs(rects[0].center.x-(view.Width/2+(355-268.8f)*u))<.01f,"Action buttons not at UI_ChiPengHu slots");
    Check(Mathf.Abs(rects[0].width-268.8f*u)<.01f&&Mathf.Abs(rects[0].yMax-(view.Height/2+(321.56f+.2f*130.2f)*u))<.05f,"Action button size/pivot differs");
    Check(Resources.Load<Texture2D>("Majsoul/TableLook/Sprites/notitle").width==338&&Resources.Load<Texture2D>("Majsoul/TableLook/Sprites/bg_type")!=null,"Native title/caption art missing");
    while(!client.OptionsReady)yield return null;yield return new WaitForSecondsRealtime(.2f);
    // The HUD previews were rendered outside OnGUI: an opaque red back, transparent margin.
    var back=(RenderTexture)table.DoraTexture(null);var prev=RenderTexture.active;RenderTexture.active=back;var im=new Texture2D(back.width,back.height,TextureFormat.RGBA32,false);im.ReadPixels(new Rect(0,0,back.width,back.height),0,0);im.Apply();RenderTexture.active=prev;
    var centre=im.GetPixel(back.width/2,back.height/2);var corner=im.GetPixel(2,2);UnityEngine.Object.Destroy(im);Check(centre.r>.6f&&centre.g<.3f&&centre.a>.99f&&corner.a<.01f,"Dora back preview not rendered: "+centre+" "+corner);
    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-1012b-table-"+(int)res.x+".png"));yield return new WaitForSecondsRealtime(.12f);
   }
   var v2=Load(folder,"preview-4");v2.audioEvents=new PublicSound[0];client.ShowValidationFixture(v2);for(int i=0;i<4;i++)yield return null;
   ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-1012b-four-1280.png"));yield return new WaitForSecondsRealtime(.12f);
   // Own North matches the projected size of own meld tiles (1012-2 follow-up).
   Size(2760,1256);for(int i=0;i<8;i++)yield return null;
   // Every seat's North is no larger on screen than that seat's meld tiles (left seat sits nearest the camera).
   var north=Reaction(folder);foreach(var p in north.players)p.nuki=2;north.players[north.you].melds=new[]{"p777-","s1111"};north.hand=north.hand.Take(north.hand.Length-6).ToArray();client.ShowValidationFixture(north);for(int i=0;i<4;i++)yield return null;
   for(int seat=0;seat<north.n;seat++){float reference=table.ValidationMeldFaceWidth(seat);for(int n=1;n<=2;n++){var tile=table.NorthTarget(seat,n);float face=table.ValidationFaceWidth(tile);Debug.Log("NORTH SIZE seat "+seat+"/"+n+" northFace="+face+" meldFace="+reference+" scale="+tile.localScale.x);Check(tile.localScale.x<=1.0001f&&face<=reference*1.03f&&(tile.localScale.x>.999f||face>=reference*.97f),"North face differs from its seat's meld faces: seat "+seat+" north="+face+" meld="+reference);}}
   ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-1012b-north-size-2760.png"));yield return new WaitForSecondsRealtime(.12f);
   // Whole-model blue: selecting a hand tile tints every visible copy, shell included.
   var kind=north.hand[0];north.players[1].river=north.players[1].river.Take(3).Concat(new[]{new RiverTile{tile=kind}}).ToArray();client.ShowValidationFixture(north);yield return null;table.SelectHand(0);yield return null;yield return null;
   var matched=GameObject.Find("PublicTableState").GetComponentsInChildren<Transform>(true).Where(t=>t.Find("Original tile")!=null&&OriginalTiles.IsMatchedModel(t)).ToArray();Check(matched.Length>0,"Selecting a tile matched no visible copy");
   foreach(var m in matched){var r=m.Find("Original tile").GetComponent<MeshRenderer>();var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Check(block.GetFloat("_Match")>.5f&&r.sharedMaterials.All(s=>s.HasProperty("_Match")),"Matched tile has an untinted material");}
   ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-1012b-match-north-2760.png"));yield return new WaitForSecondsRealtime(.12f);table.SelectHand(-1);
   // Majsoul default tap effect: petals and flash over the HUD, then cleaned up.
   client.Tap.Clear();client.Tap.Play(new Vector2(Screen.width*.62f,Screen.height*.55f));client.Tap.Play(new Vector2(Screen.width*.2f,Screen.height*.85f));
   yield return new WaitForSecondsRealtime(.08f);Check(client.Tap.ActiveCount==2&&client.Tap.ParticleCount>=20,"Tap sakura burst missing: "+client.Tap.ParticleCount);
   yield return new WaitForSecondsRealtime(.17f);yield return new WaitForEndOfFrame();
   // The drawn petals (pink, blue above green, unlike the cloth) must surround each tap point.
   var frame=ScreenCapture.CaptureScreenshotAsTexture();
   foreach(var at in new[]{new Vector2(Screen.width*.62f,Screen.height*.55f),new Vector2(Screen.width*.2f,Screen.height*.85f)}){
    int count=0;float sx=0,sy=0;int r=(int)(Screen.height*.12f);
    for(int y=Mathf.Max(0,(int)at.y-r);y<Mathf.Min(frame.height,(int)at.y+r);y+=2)for(int x=Mathf.Max(0,(int)at.x-r);x<Mathf.Min(frame.width,(int)at.x+r);x+=2){var c=frame.GetPixel(x,y);if(c.r>.75f&&c.b>.62f&&c.b>c.g+.05f){count++;sx+=x;sy+=y;}}
    Check(count>8,"No sakura petals drawn near tap "+at+" ("+count+")");var centre=new Vector2(sx/count,sy/count);Debug.Log("TAP PIXELS at "+at+" count="+count+" centre="+centre);
    Check(Vector2.Distance(centre,at)<Screen.height*.06f,"Sakura drawn away from the tap: tap="+at+" petals="+centre);
   }
   UnityEngine.Object.Destroy(frame);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-1012b-tap-2760.png"));yield return new WaitForSecondsRealtime(.12f);
   yield return new WaitForSecondsRealtime(ScreenTapEffect.Lifetime);Check(client.Tap.ActiveCount==0,"Tap effects leaked");
   Debug.Log("RIICHI REVIEW 1012B PASS: scene_mj toon light direction, mjp_face/mjp_mid parameters, Catfood outline tint, effect_shadow footprint, three looping sun_shine emitters, centred UI_ChiPengHu buttons at three sizes, native caption band and notitle art, own North at own meld size, whole-model matching blue, effect_click_default tap sakura.");
  }
 }
}
#endif
