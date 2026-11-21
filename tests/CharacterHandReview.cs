using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class CharacterHandReview {
        const string Key="CharacterHandReview";
        public static readonly List<string> Errors=new List<string>();
        static CharacterHandReview(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterHandCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;Application.logMessageReceived+=(message,stack,type)=>{if((type==LogType.Error||type==LogType.Exception)&&!(stack??"").Contains("UnityEditor.Search"))Errors.Add(message);};CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_HAND_REPO")+"/build/character-voice-r34/roles";CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_HAND_REPO")+"/build/character-hands-r37/wardrobes";WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.ResetForReview();}
        public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="CharacterHandsR37";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterHandCapture:MonoBehaviour {
        [Serializable] class Case {public string role,kind,motion,screenshot;public int seat,renderers;public float movement,nearestTile;}
        [Serializable] class Report {public int checks,styles,variants,clips,roles;public bool androidDeviceTested;public List<Case> cases=new List<Case>();public List<string> passed=new List<string>();}
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        RiichiClient client;TableEffects effects;TableRenderer table;Report report=new Report();string output,presets;bool hadPresets;
        void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
        void Restore(){if(hadPresets)PlayerPrefs.SetString("riichi.wardrobe.presets.v3",presets);else PlayerPrefs.DeleteKey("riichi.wardrobe.presets.v3");PlayerPrefs.Save();}
        IEnumerator Start(){
            output=Environment.GetEnvironmentVariable("YUNJIAN_HAND_OUTPUT")??Environment.GetEnvironmentVariable("YUNJIAN_HAND_REPO")+"/build/character-hands-r37";hadPresets=PlayerPrefs.HasKey("riichi.wardrobe.presets.v3");presets=PlayerPrefs.GetString("riichi.wardrobe.presets.v3","");
            client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("character-hands-local-review");GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();effects.SuppressShouts=true;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AudioListener.volume=0;
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var task=stack.Peek();if(!task.MoveNext()){stack.Pop();continue;}next=task.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){Restore();File.WriteAllText(output+"/runtime.failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            Restore();if(CharacterHandReview.Errors.Count>0){File.WriteAllText(output+"/runtime.failed.txt",string.Join("\n",CharacterHandReview.Errors));EditorApplication.Exit(1);yield break;}File.WriteAllText(output+"/runtime-report.json",JsonUtility.ToJson(report,true));Debug.Log("CHARACTER HAND REVIEW PASS "+report.checks+" checks / "+report.clips+" clips");EditorApplication.Exit(0);
        }
        static Vector3[] Vertices(GameObject root){var result=new List<Vector3>();var baked=new Mesh();foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){if(!skin.enabled)continue;skin.BakeMesh(baked,false);result.AddRange(baked.vertices.Select(p=>skin.transform.position+skin.transform.rotation*p));}UnityEngine.Object.Destroy(baked);return result.ToArray();}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Character hand review"});for(int i=0;i<8;i++)yield return null;}
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());Destroy(image);}
        IEnumerator Run(){
            var asuna=CharacterHands.All.First(h=>h.key=="hand_yasina");
            foreach(var path in asuna.variants){var prefab=Resources.Load<GameObject>(path);foreach(var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled)){
                Check(skin.sharedMesh.tangents.Length==skin.sharedMesh.vertexCount,"Asuna source tangent frame "+path);
                foreach(var material in skin.sharedMaterials){Check(material.GetFloat("_SourceBRDF")==1,"Asuna source BRDF enabled "+material.name);foreach(string texture in new[]{"_BumpMap","_ORMMap","_RampMap"})Check(material.GetTexture(texture)!=null,"Asuna source texture "+material.name+"/"+texture);Check(material.GetColor("_HandLightColor").r>1.5f,"Asuna original table light intensity "+material.name);}
            }}
            foreach(var texture in asuna.variants.SelectMany(path=>Resources.Load<GameObject>(path).GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials)).SelectMany(m=>new[]{m.mainTexture,m.GetTexture("_BumpMap"),m.GetTexture("_ORMMap"),m.GetTexture("_RampMap"),m.GetTexture("_MatCapTex")}).Where(t=>t!=null).Distinct()){
                Check(((Texture2D)texture).mipmapCount==1,"Asuna source mip count "+texture.name);if(texture.height==4)Check(texture.filterMode==FilterMode.Point&&texture.wrapMode==TextureWrapMode.Clamp,"source ramp bands stay separate "+texture.name);
            }
            var normal=WardrobeCatalog.Find("hand-human");var orange=WardrobeCatalog.Find("hand-cat-orange");
            Check(WardrobeCatalog.Items("hand").Length==2,"hand selection retains the existing two equipment choices");
            foreach(var role in CharacterRoster.All){var item=WardrobeRuntime.ResolveHand(normal,role.key);Check(WardrobeRuntime.ResolveHand(orange,role.key).id==orange.id,"equipped orange paw overrides "+role.key);var binding=CharacterHands.All.FirstOrDefault(h=>h.characters.Contains(role.id));if(binding!=null){Check(item.nativeHand==binding.key,"default character binding "+role.key);report.roles++;}else Check(item.id==(role.key=="wangcilang"?"hand-dog":role.sex==2?"hand-man":"hand-human"),"generic/dog fallback "+role.key);}
            foreach(var hand in CharacterHands.All){report.styles++;var item=new WardrobeItem{nativeHand=hand.key};for(int physical=0;physical<hand.variants.Length;physical++){report.variants++;foreach(string action in new[]{"Dapai","LiQiPai","LiZhi","Fulu","Zimo"}){
                var data=CharacterHands.Clip(item,physical,action);Check(data.frames>1&&data.tilePoints.Length==data.frames*3&&data.tileRotations.Length==data.frames*4,"motion anchors "+hand.key+"/"+physical+"/"+action);
                var go=new GameObject("rig check");go.transform.position=new Vector3(100,100,100);var rig=go.AddComponent<CharacterHandVisual>();rig.Initialize(data);var a=Vertices(go);Check(a.Length>100,"visible skinned geometry "+hand.key+"/"+physical);rig.SampleFrame((data.frames-1)*.65f);var b=Vertices(go);Check(a.Length==b.Length&&a.Zip(b,(x,y)=>Vector3.Distance(x,y)).Max()>.0001f,"sampled bones deform mesh "+hand.key+"/"+physical+"/"+action);Check(go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.name=="YunJian/CharacterHand"&&m.mainTexture!=null)),"portable source textures "+hand.key+"/"+physical);Destroy(go);report.clips++;
            }}yield return null;}
            yield return Size(1800,810);
            foreach(string role in CharacterHands.All.Select(h=>CharacterRoster.All.FirstOrDefault(r=>h.characters.Contains(r.id))?.key).Where(r=>r!=null)){
                foreach(int seat in role=="yasina"?new[]{0,1,2,3}:new[]{0})foreach(string kind in role=="yasina"?new[]{"discard","riichi","riichi-stick","pon","tsumo","nuki"}:new[]{"riichi","tsumo"}){
                    var choice=new WardrobeLoadout{hand="hand-human"};var skin=CharacterRoster.Find(role).defaultSkin;var view=kind=="tsumo"?DressPreviewStates.Tsumo("hand-review",choice,role,skin,"手型检查",seat):DressPreviewStates.Riichi("hand-review",choice,role,skin,"手型检查",seat);view.players[seat].character=role;view.players[seat].skin=skin;
                    if(kind=="pon"){view.players[seat].melds=new[]{"p777-"};view.players[seat].count=10;if(seat==0)view.hand=view.hand.Take(10).ToArray();}
                    if(kind=="nuki")view.players[seat].nuki=1;
                    client.ShowValidationFixture(view);effects.SuppressShouts=true;yield return null;effects.ValidationSequentialClock=false;effects.ValidationPlay(seat,kind=="riichi"?"discard":kind,kind=="riichi-stick"?null:kind=="pon"?"p7":kind=="nuki"?"z4":kind=="tsumo"?DressPreviewStates.WinTile:DressPreviewStates.RiichiDiscard,kind=="riichi");typeof(TableEffects).GetMethod("FinishCall",Private).Invoke(effects,null);effects.ValidationTime=0;yield return null;
                    var rig=GetComponentsInChildren<CharacterHandVisual>().FirstOrDefault();if(rig==null)rig=UnityEngine.Object.FindObjectsByType<CharacterHandVisual>(FindObjectsSortMode.None).FirstOrDefault();Check(rig!=null,"actual table uses native character hand "+role+"/"+seat+"/"+kind);var a=Vertices(rig.gameObject);
                    float sample=Mathf.Min(effects.ValidationPlayLength*.6f,effects.ValidationHandoff*.85f);effects.ValidationTime=sample;yield return null;yield return null;var b=Vertices(rig.gameObject);var result=new Case{role=role,seat=seat,kind=kind,motion=effects.ValidationMotion,renderers=rig.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r=>r.enabled),movement=a.Zip(b,(x,y)=>Vector3.Distance(x,y)).Max()};Check(result.movement>.001f,"actual table bone motion "+role+"/"+seat+"/"+kind);if(effects.ValidationAnimatedTile!=null)result.nearestTile=b.Min(p=>Vector3.Distance(p,effects.ValidationAnimatedTile.position));report.cases.Add(result);
                    if(role=="yasina"||seat==0&&kind=="riichi"){result.screenshot=role+"-"+seat+"-"+kind;yield return Shot(result.screenshot);}
                    if(role=="yasina"&&kind=="discard"){
                        var materials=rig.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
                        foreach(var material in materials)material.SetFloat("_SourceBRDF",0);yield return Shot("yasina-"+seat+"-base-only");foreach(var material in materials)material.SetFloat("_SourceBRDF",1);
                    }
                    effects.Reset();yield return null;
                }
            }
            // The wardrobe's existing default-hand preview must use the same rig.
            for(int seat=0;seat<3;seat++){
                var view=DressPreviewStates.Riichi("hand-review-three",new WardrobeLoadout{hand="hand-human"},"yasina",CharacterRoster.Find("yasina").defaultSkin,"三麻手型",seat);view.n=3;view.turn=(seat+1)%3;view.players=view.players.Take(3).ToArray();view.players[seat].character="yasina";client.ShowValidationFixture(view);yield return null;effects.SuppressShouts=true;effects.ValidationPlay(seat,"discard",DressPreviewStates.RiichiDiscard,true);effects.ValidationTime=.25f;yield return null;
                int physical=NativeReviewLayout.Physical(table.RelativeSeat(seat),3);Check(effects.ValidationMotion.Contains("hand_yasina_"+(physical+1)),"three-player hand uses physical direction "+seat);yield return Shot("yasina-three-player-"+seat);effects.Reset();yield return null;
            }
            client.BeginLobbyValidation("character-hand-preview");CharacterRoster.Use("yasina",CharacterRoster.Find("yasina").defaultSkin);client.Lobby.PreviewWardrobe("hand");CharacterRoster.Dorm=CharacterRoster.Find("yasina");client.Lobby.ValidationPress("dress-eye");for(int i=0;i<12;i++)yield return null;Check(UnityEngine.Object.FindObjectsByType<CharacterHandVisual>(FindObjectsSortMode.None).Any(r=>r.name=="Character default hand preview"),"wardrobe default-hand preview uses the character rig");yield return Shot("yasina-wardrobe-hand");
        }
    }
}
