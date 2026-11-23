using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class NovContentReview {
  const string Key="NovContentReview";
  static NovContentReview(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<NovContentCapture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-characters";WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-wardrobes";}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="NovemberR10";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class NovContentCapture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report{public int checks;public List<string> passed=new List<string>();}
  readonly Report report=new Report();readonly List<string> chapters=new List<string>();RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects effects;string output;System.Diagnostics.Process encoder;RenderTexture rt;Texture2D pixels;int frame;
  void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
  IEnumerator Wait(int n=8){for(int i=0;i<n;i++)yield return null;}
  IEnumerator Shot(string name){yield return Wait();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  void StartFilm(){rt=new RenderTexture(1600,900,0,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=@"C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/november-raw.mp4\" --width 1600 --height 900 --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});}
  IEnumerator Film(string label,float seconds,Action<int> action=null){chapters.Add(frame+" "+label);for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){action?.Invoke(i);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);var before=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);RenderTexture.active=before;var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;}}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("november-local");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();CharacterPackService.RespectPreviewAvailability=false;WardrobePackService.RespectPreviewAvailability=false;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();AudioListener.volume=0;
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var t=stack.Peek();if(!t.MoveNext()){stack.Pop();continue;}next=t.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(encoder!=null&&!encoder.HasExited)encoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;Check(encoder.ExitCode==0,"video encoded");File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));File.WriteAllLines(output+"/chapters.txt",chapters);EditorApplication.Exit(0);
  }
  IEnumerator Run(){var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1600,900),"November source resources"});yield return Wait(20);StartFilm();
   foreach(string key in (Environment.GetCommandLineArgs().Contains("-novMaskOnly")?Array.Empty<string>():new[]{"lingxing","fulilian","feilun","xiutaerke","youbeier"})){
    var role=CharacterRoster.Find(key);Check(role!=null,"new role "+key);Check(CharacterVoiceCatalog.Lines(key).All(x=>x.available),"all declared voice page clips "+key);
    foreach(var skin in role.skins){CharacterRoster.Use(key,skin.id);lobby.DesignPreviewClock=1000;lobby.Home();lobby.DesignPreviewClock=1002;yield return Wait(30);yield return Shot("skin-"+skin.id);yield return Film(role.name+" · "+skin.name,skin.dynamic?3:1,i=>lobby.DesignPreviewClock=1002+i/20f);}
   }
   var load=new WardrobeLoadout{stick="liqi_26fll",cloth="tablecloth_26fll",tileBack="mjp_26fll",headFrame="headframe_26fll",winEffect="ron_26fll",riichiEffect="effect_liqi_26fll"};
   var view=DressPreviewStates.Base("november-static",load,"fulilian",40012801,"芙莉莲");for(int s=0;s<4;s++){view.players[s].wardrobe=load;view.players[s].riichi=1;}client.ShowValidationFixture(view);yield return Wait(20);
   for(int s=0;s<4;s++){var stick=table.StickTarget(s);Check(stick!=null&&stick.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&!WardrobeModelVisual.IsShadow(r)),"new staff visible seat "+s);}
   yield return Shot("new-static-table");yield return Film("联动桌布、牌背、头像框与菲伦的法杖",2);
   effects.ValidationFreeze=true;effects.ValidationSequentialClock=true;
   for(int seat=0;seat<(Environment.GetCommandLineArgs().Contains("-novMaskOnly")?0:4);seat++){
    var before=DressPreviewStates.BeforeRiichi("november-riichi-"+seat,load,"fulilian",40012801,"芙莉莲",seat);client.ShowValidationFixture(before);client.ValidationTableOnly=true;effects.ValidationFreeze=true;effects.ValidationSequentialClock=true;effects.ValidationTime=0;effects.Accept(before);yield return Wait(8);var next=DressPreviewStates.Riichi(before.audioRoom,load,"fulilian",40012801,"芙莉莲",seat);client.ValidationApplyState(next);effects.Accept(next);typeof(TableEffects).GetMethod("FinishCall",Private).Invoke(effects,null);
    bool saw=false,checkedLayers=false;yield return Film("普通攻击魔法 · "+DressPreviewStates.SeatName(seat),4,i=>{effects.ValidationTime=i/20f;var fx=GetComponent<RiichiDiscardVisual>().Latest;if(fx!=null){saw|=fx.ParticleCount>0;if(!checkedLayers){Check(fx.LayerCount==131,"all riichi particle layers seat "+seat);checkedLayers=true;}}});Check(saw,"riichi effect particles seat "+seat);yield return Shot("new-riichi-"+seat);
   }
   for(int seat=0;seat<(Environment.GetCommandLineArgs().Contains("-novMaskOnly")?1:4);seat++){
    var next=DressPreviewStates.Tsumo("november-win-"+seat,load,"fulilian",40012801,"芙莉莲",seat);client.ShowValidationFixture(next);client.ValidationTableOnly=true;effects.ValidationFreeze=true;effects.ValidationTime=0;yield return Wait(8);var detail=next.result.details[0];var sound=new PublicSound{id=81+seat,kind="tsumo",seat=seat,from=seat,tile=detail.tile,winSource="tsumo"};var win=new GameObject("November source win").AddComponent<WinTileEffect>();win.transform.SetParent(table.transform,false);win.WinningEvent=sound;win.Initialize(table.WinningPoint(sound),table.SeatAngle(seat),0,71,"Majsoul/NativeReview/NovR10/ron_26fll-playback");win.BindWinningTile(table.WinningTarget(sound));Check(win.LayerCount==54,"all ron particle layers seat "+seat);var backgrounds=win.GetComponentsInChildren<ParticleSystem>(true).Where(p=>p.name=="mask"||p.transform.parent.name=="light").ToArray();Check(backgrounds.Length==3&&backgrounds.All(p=>p.main.startSize.constant>=3),"source environment covers viewport seat "+seat);bool saw=false;if(Environment.GetCommandLineArgs().Contains("-novMaskOnly")){
     var source=WardrobePackService.Asset<TextAsset>("Majsoul/NativeReview/NovR10/ron_26fll-source");Debug.Log("SOURCE FOUND "+(source!=null));win.Sample(2.5f);yield return Shot("mask-on");var mask=win.GetComponentsInChildren<ParticleSystem>(true).First(p=>p.name=="mask");var main=mask.main;var renderer=mask.GetComponent<ParticleSystemRenderer>();Debug.Log("MASK SIZE "+main.startSize.constant+" pos="+mask.transform.position+" bounds="+renderer.bounds);renderer.enabled=false;yield return Shot("mask-off");renderer.enabled=true;
     var layers=win.GetComponentsInChildren<ParticleSystemRenderer>(true);var states=layers.Select(r=>r.enabled).ToArray();
     File.WriteAllLines(output+"/layers.txt",layers.Select((r,index)=>index+" "+r.name+" "+r.sharedMaterial.name+" dst="+r.sharedMaterial.GetFloat("_DstBlend")+" family="+r.sharedMaterial.GetFloat("_SourceFamily")+" "+r.bounds));
     for(int group=0;group<4;group++){
      for(int part=0;part<layers.Length;part++)layers[part].enabled=states[part]&&part/14==group;
      yield return Shot("layers-"+group);
     }
     for(int part=0;part<layers.Length;part++)layers[part].enabled=states[part];
    }
    yield return Film("变出花田的魔法 · "+DressPreviewStates.SeatName(seat),6,i=>{win.Sample(i/20f);saw|=win.ParticleCount>0;});Check(saw,"win effect particles seat "+seat);yield return Shot("new-win-"+seat);Destroy(win.gameObject);
   }
  }
 }
}
