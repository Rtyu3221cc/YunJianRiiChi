using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YunJian.Riichi {
 // DesktopMgr.ShowYiManEffect: source cameras/curves/art, then real result
 // tiles and the winning player's L0 illustration replace the authoring data.
 public sealed class YakumanCinematic:IDisposable {
  [Serializable] public sealed class Plan {public int id,priority;public string name,key;public float soundAt;}
  [Serializable] sealed class Catalog {public Plan[] plans;}
  static Catalog catalog;
  static Plan[] Plans=>(catalog??(catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Majsoul/NativeReview/YakumanR39/catalog").text))).plans;
  const string Prefix="Majsoul/NativeReview/YakumanR39/";
  const int Layer=27;
  // ShowYiManEffect waits .15 s before the two-second table launch begins.
  public const float SceneAt=2.15f,Duration=SceneAt+8;
  static string Canonical(string name){switch(name){case "緑一色":return "绿一色";case "清老頭":return "清老头";case "国士無双":return "国士无双";case "国士無双十三面":return "国士无双十三面";case "四槓子":return "四杠子";case "九蓮宝燈":return "九莲宝灯";case "純正九蓮宝燈":return "纯正九莲宝灯";case "四暗刻単騎":return "四暗刻单骑";default:return name;}}
  public static Plan Resolve(WinDetail detail){if(detail?.yaku==null||!detail.yaku.Any(y=>y?.yakuman>0)&&!(detail.score?.damanguan>0))return null;return Plans.Where(p=>detail.yaku.Any(y=>y!=null&&Canonical(y.name)==p.name&&(y.yakuman>0||detail.score?.damanguan>0))).OrderBy(p=>p.priority).FirstOrDefault();}
  public static WinDetail Select(RoomView view){return(view?.result?.details??Array.Empty<WinDetail>()).Where(d=>Resolve(d)!=null).OrderBy(d=>d.seat==view.you?-1:(d.seat-view.you+view.n)%view.n).FirstOrDefault();}
  public static string[] HandTiles(WinDetail detail){
   var tiles=(detail.tiles??Array.Empty<string>()).ToList();var melds=detail.melds??Array.Empty<string>();int expected=13-melds.Length*3;
   // Engine results omit the winning tile; accept older complete-hand fixtures.
   if(tiles.Count==expected+1){int at=tiles.LastIndexOf(detail.tile);if(at>=0)tiles.RemoveAt(at);}
   foreach(string meld in melds)if(!string.IsNullOrEmpty(meld))foreach(char value in meld.Skip(1).Where(char.IsDigit))tiles.Add(meld[0].ToString()+value);
   tiles.Add(detail.tile);return tiles.ToArray();
  }
  GameObject scene,bankRoot,ghost,burstRoot,trailRoot;Transform authored,row;Camera camera;RenderTexture image;OriginalTiles tiles;
  NativeEffectMotion[] motions;ParticleSystem[] particles;NativeParticleEffect burst,trail;
  readonly Dictionary<ParticleSystem,bool> activation=new Dictionary<ParticleSystem,bool>();
  readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
  readonly List<Transform> rowSlots=new List<Transform>();
  TableRenderer table;GameAudio audio;PublicSound winning;Plan plan;float previous=-1,age=-1;
  bool preloadPlayed,soundPlayed,launchCaptured;Vector3 launchPoint;Quaternion launchRotation;
  public string SourceKey{get;private set;}
  public string[] ActualTiles{get;private set;}=Array.Empty<string>();
  public bool Fullscreen=>scene!=null&&age>=SceneAt&&age<Duration;
  public Texture Image=>image;
  public void Play(TableRenderer owner,WinDetail detail,PublicSound action){
   table=owner;audio=owner.GetComponent<GameAudio>();winning=action;plan=Resolve(detail);ActualTiles=HandTiles(detail);
   bool wide=ActualTiles.Length>14||plan.key=="fan_sigangzi";SourceKey=plan.key+(wide?"18":"");var prefab=Resources.Load<GameObject>(Prefix+SourceKey);if(prefab==null)throw new InvalidOperationException("Missing native yakuman scene "+SourceKey);
   scene=UnityEngine.Object.Instantiate(prefab);scene.name="Native yakuman "+SourceKey;scene.transform.position=new Vector3(50,0,0);authored=scene.transform.GetChild(0);
   foreach(var node in scene.GetComponentsInChildren<Transform>(true))node.gameObject.layer=Layer;
   camera=authored.Find("camera/dapai_camera_gensui").GetComponent<Camera>();foreach(var c in scene.GetComponentsInChildren<Camera>(true))c.enabled=false;camera.cullingMask=1<<Layer;camera.allowHDR=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
   var background=authored.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="dapai_beijing_amin");if(background!=null)background.localScale=new Vector3(1.01f,1,1);
   var afterimage=authored.Find("renwu_anim/canying");if(afterimage!=null)afterimage.gameObject.SetActive(false);
   var player=owner.Client.State.players[detail.seat];var skin=CharacterRoster.Skin(player.character,player.skin);
   foreach(var raw in scene.GetComponentsInChildren<RawImage>(true)){
    var rt=raw.rectTransform;var basis=CharacterFraming.NativeLocal(400101,"L0",0,new Rect(rt.anchoredPosition,rt.sizeDelta),out _);var placed=CharacterFraming.NativeLocal(skin.id,"L0",0,basis,out bool flip);rt.anchoredPosition=placed.position;rt.sizeDelta=placed.size;rt.localScale=new Vector3(flip?-1:1,1,1);raw.texture=CharacterFraming.SceneTexture(skin.id,"L0");
   }
   var view=owner.Client.State;tiles=new OriginalTiles(WardrobeRuntime.Item(view,view.you,"tileFace"),WardrobeRuntime.Item(view,view.you,"tileBack"));bankRoot=new GameObject("Yakuman tile material bank");bankRoot.SetActive(false);
   row=authored.Find("dapai_zhengpai_anim");foreach(Transform child in row){child.gameObject.SetActive(false);if(child.GetComponent<MeshFilter>()!=null&&child.GetComponent<MeshRenderer>()!=null)rowSlots.Add(child);}if(ActualTiles.Length>rowSlots.Count)throw new InvalidOperationException("Native yakuman hand capacity "+ActualTiles.Length);
   var outlinePrefab=Resources.Load<GameObject>(Prefix+"maque_outline");
   for(int i=0;i<rowSlots.Count;i++){rowSlots[i].gameObject.SetActive(i<ActualTiles.Length);if(i<ActualTiles.Length){BindFace(rowSlots[i].GetComponent<MeshRenderer>(),ActualTiles[i]);if(outlinePrefab!=null){var edge=UnityEngine.Object.Instantiate(outlinePrefab,rowSlots[i],false).transform.GetChild(0);edge.localPosition=Vector3.zero;edge.localRotation=Quaternion.identity;edge.localScale=Vector3.one*1.06f;foreach(var t in edge.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;}}}
   foreach(var renderer in scene.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("maque",StringComparison.Ordinal)&&r.name!="maque_outline"&&!rowSlots.Contains(r.transform)))BindFace(renderer,detail.tile);
   motions=scene.GetComponentsInChildren<NativeEffectMotion>(true);particles=scene.GetComponentsInChildren<ParticleSystem>(true);for(int i=0;i<particles.Length;i++){particles[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);particles[i].useAutoRandomSeed=false;particles[i].randomSeed=(uint)(371+i);}
   var target=table.WinningTarget(winning);launchPoint=target!=null?target.position:table.WinningPoint(winning);launchRotation=target!=null?target.rotation:Quaternion.identity;
   if(target!=null){ghost=UnityEngine.Object.Instantiate(target.gameObject);ghost.name="Actual yakuman winning tile";ghost.transform.SetPositionAndRotation(launchPoint,launchRotation);ghost.SetActive(false);foreach(var r in ghost.GetComponentsInChildren<MeshRenderer>(true)){if(r.name=="Contact shadow"){r.gameObject.SetActive(false);continue;}r.enabled=true;r.forceRenderingOff=false;}foreach(var c in ghost.GetComponentsInChildren<Collider>())UnityEngine.Object.Destroy(c);}
   burstRoot=new GameObject("Native yakuman prelude burst");burstRoot.transform.SetParent(table.transform,false);burstRoot.transform.position=target!=null?TileCenter(target):launchPoint;burstRoot.transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;burst=burstRoot.AddComponent<NativeParticleEffect>();
   // The prefab root keeps its authoring-scene offset (.19 source units along Z);
   // ShowYiManEffect places the root itself on the winning tile.
   burst.Initialize("YakumanR39/effect_baodian",normalizeSurfaceOrigin:true);
   trailRoot=new GameObject("Native yakuman prelude trail");trailRoot.transform.SetParent(table.transform,false);trailRoot.transform.position=launchPoint;trailRoot.transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;trail=trailRoot.AddComponent<NativeParticleEffect>();trail.Initialize("YakumanR39/effect_tuowei");
   scene.SetActive(false);Sample(-1);
  }
  void BindFace(MeshRenderer renderer,string face){
   if(renderer==null)return;var model=tiles.CreateTable(bankRoot.transform,face,Vector3.zero).Find("Original tile").GetComponent<MeshRenderer>();var m=model.sharedMaterials;
   m[0].shader=Resources.Load<Shader>("Majsoul/OriginalTileBody");m[1].shader=Resources.Load<Shader>("Majsoul/OriginalTileFace");
   // This prefab retains APK coordinates; table models alone use the Y half-turn.
   // Keep the source light local to these materials instead of the table global.
   var light=TableLook.LightDirection;foreach(var material in m)material.SetVector("_MjLightDir",new Vector4(-light.x,light.y,-light.z,0));
   // setPai3DFace replaces body/face only; the outline remains a separate pass.
   m[1].renderQueue=2003;
   renderer.sharedMaterials=renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount==3?new[]{m[0],m[1],m[0]}:m;
  }
  // APK tile meshes are centred on their pivot; table tiles pivot on the cloth.
  // The burst's horizontal ripple rings (quan/quan01/guang) sit on that pivot
  // plane, so mounting them at the table pivot buried them in the cloth depth.
  static Vector3 TileCenter(Transform target){
   var renderers=target.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name!="Contact shadow"&&r.gameObject.activeInHierarchy).ToArray();if(renderers.Length==0)return target.position;
   var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);return bounds.center;
  }
  void HideTarget(){var target=table.WinningTarget(winning);if(target==null)return;foreach(var r in target.GetComponentsInChildren<Renderer>(true)){if(!hidden.ContainsKey(r))hidden[r]=r.forceRenderingOff;r.forceRenderingOff=true;}}
  public void Sample(float next){
   age=next;if(scene==null)return;
   if(next>=0&&!preloadPlayed){preloadPlayed=true;audio?.Cue("yiman_preload");}
   if(next>=plan.soundAt&&!soundPlayed){soundPlayed=true;audio?.Cue(plan.key);}
   bool prelude=next>=.15f&&next<SceneAt;
   if(prelude&&!launchCaptured){launchCaptured=true;var target=table.WinningTarget(winning);if(target!=null){launchPoint=target.position;launchRotation=target.rotation;}burstRoot.transform.position=target!=null?TileCenter(target):launchPoint;trailRoot.transform.position=launchPoint;}
   burstRoot.SetActive(prelude);trailRoot.SetActive(prelude);burst.Sample(prelude?next-.15f:-1);trail.Sample(prelude?next-.15f-25f/60:-1);
   if(ghost!=null){ghost.SetActive(prelude);if(prelude){HideTarget();float t=next-.15f;float back=t/2-1;float settle=.02f*(1+2.70158f*back*back*back+1.70158f*back*back);float flight=Mathf.Clamp01((t-1.4f)/.6f);float ease=flight>=1?1:flight<=0?0:Mathf.Pow(2,10*(flight-1));float lift=t<1.4f?settle:Mathf.Lerp(.021603988f,.653f,ease);var shake=t<.84f?Quaternion.Euler(Mathf.Sin(t*63)*4,Mathf.Cos(t*71)*4,Mathf.Sin(t*81)*4):Quaternion.identity;float turn=Mathf.Clamp01((t-.85f)/.5f);float angle=Mathf.Sin(turn*Mathf.PI*.5f)*(winning.kind=="ron"?-89:-90);ghost.transform.SetPositionAndRotation(launchPoint+Vector3.up*(lift*TableTileGeometry.SourceUnits),launchRotation*Quaternion.Euler(angle,0,0)*shake);trailRoot.transform.position=ghost.transform.position;}}
   scene.SetActive(Fullscreen);if(!Fullscreen)return;
   float time=next-SceneAt;if(previous<0||time<previous){foreach(var p in particles)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);activation.Clear();previous=0;foreach(var motion in motions)motion.Sample(0);}
   while(previous<time-.000001f){float at=Mathf.Min(time,previous+1f/60);foreach(var motion in motions)motion.Sample(at);foreach(var p in particles){bool active=p.gameObject.activeInHierarchy;bool was=activation.TryGetValue(p,out var value)&&value;if(active)p.Simulate(at-previous,false,!was,false);else p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);activation[p]=active;}previous=at;}
   foreach(var motion in motions)motion.Sample(time);for(int i=0;i<rowSlots.Count;i++)rowSlots[i].gameObject.SetActive(i<ActualTiles.Length);
  }
  public void Render(){
   if(!Fullscreen)return;int w=Screen.width,h=Screen.height;if(w<=0||h<=0)return;
   if(image==null||image.width!=w||image.height!=h){if(image!=null){camera.targetTexture=null;image.Release();UnityEngine.Object.Destroy(image);}image=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32){name="Native yakuman cinematic",antiAliasing=2};image.Create();camera.targetTexture=image;}
   camera.aspect=Mathf.Clamp(w/(float)h,16f/9,23f/9);foreach(var canvas in scene.GetComponentsInChildren<Canvas>(true))canvas.worldCamera=camera;Canvas.ForceUpdateCanvases();var prior=RenderTexture.active;try{camera.Render();}finally{RenderTexture.active=prior;}
  }
  public void Draw(){if(!Fullscreen||image==null||Event.current.type!=EventType.Repaint)return;var prior=GUI.matrix;GUI.matrix=Matrix4x4.identity;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),image,ScaleMode.StretchToFill,false);GUI.matrix=prior;}
  public void Dispose(){
   foreach(var entry in hidden)if(entry.Key!=null)entry.Key.forceRenderingOff=entry.Value;hidden.Clear();
   foreach(var go in new[]{scene,ghost,bankRoot,burstRoot,trailRoot})if(go!=null){go.SetActive(false);UnityEngine.Object.Destroy(go);}
   if(image!=null){camera.targetTexture=null;image.Release();UnityEngine.Object.Destroy(image);}tiles?.Dispose();scene=null;
  }
 }
}
