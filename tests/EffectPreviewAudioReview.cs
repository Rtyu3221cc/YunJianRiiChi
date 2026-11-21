using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class EffectPreviewAudioReview {
        const string Key="EffectPreviewAudioReview";
        static EffectPreviewAudioReview(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<EffectPreviewAudioCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;string root=Environment.GetEnvironmentVariable("YUNJIAN_EFFECT_PREVIEW_ROOT");CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=root+"/roles";WardrobePackService.RootOverride=root+"/wardrobes";WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.ResetForReview();CharacterPackService.ResetForReview();}
        public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="EffectPreviewR35";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class EffectPreviewAudioCapture:MonoBehaviour {
        [Serializable] class Case {public string id,category;public bool inTable,effectsEnabled,previewStarted,cueObserved,sourcePlaying;public float peak,length,firstCueAt,lastCueAt;public int cycles;public bool exited,stoppedOnExit;public List<string> cues=new List<string>();}
        [Serializable] class Report {public string mode;public bool androidDeviceTested;public List<Case> cases=new List<Case>();}
        Report report=new Report();string output,mode,savedPresets;bool hadPresets;RiichiClient client;LobbyController lobby;GameAudio audio;TableEffects effects;AudioSource source;
        void RestorePresets(){if(hadPresets)PlayerPrefs.SetString("riichi.wardrobe.presets.v3",savedPresets);else PlayerPrefs.DeleteKey("riichi.wardrobe.presets.v3");PlayerPrefs.Save();}
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Private).GetValue(owner);
        IEnumerator Start(){
            output=Environment.GetEnvironmentVariable("YUNJIAN_EFFECT_PREVIEW_ROOT");mode=Environment.GetEnvironmentVariable("YUNJIAN_EFFECT_PREVIEW_MODE")??"after";report.mode=mode;
            hadPresets=PlayerPrefs.HasKey("riichi.wardrobe.presets.v3");savedPresets=PlayerPrefs.GetString("riichi.wardrobe.presets.v3","");
            client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("r35-local-only");lobby=client.Lobby;audio=GetComponent<GameAudio>();effects=GetComponent<TableEffects>();source=Field<AudioSource>(audio,"effects");GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();
            CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.Load();AppPreferences.Volume[0]=AppPreferences.Volume[2]=1;AppPreferences.Volume[1]=0;AppPreferences.Muted[0]=AppPreferences.Muted[2]=false;AppPreferences.SetVolume(2,1);AudioListener.volume=1;AudioListener.pause=false;
            var task=Run();while(true){object next;try{if(!task.MoveNext())break;next=task.Current;}catch(Exception error){RestorePresets();File.WriteAllText(output+"/"+mode+".failed.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);yield break;}yield return next;}
            RestorePresets();File.WriteAllText(output+"/"+mode+".json",JsonUtility.ToJson(report,true));Debug.Log("EFFECT PREVIEW R35 "+mode+" PASS");EditorApplication.Exit(0);
        }
        IEnumerator Run(){
            foreach(var id in mode=="before"?new[]{"ron_chilin"}:new[]{"ron_chilin","effect_liqi_chilin","riichi-fire"}){
                var item=WardrobeCatalog.Find(id);if(!WardrobePackService.Installed(id))throw new Exception("Test pack missing: "+id);
                lobby.PreviewWardrobe(item.category);typeof(LobbyController).GetMethod("EquipDressItem",Private).Invoke(lobby,new object[]{item.category,id});yield return null;
                int before=audio.PlayedCues.Count;float began=Time.unscaledTime;int startBursts=effects.SpawnedWinEffects;var result=new Case{id=id,category=item.category,length=CharacterVoices.Clip("yiji","act_ron")?.length??0};report.cases.Add(result);
                var clip=Resources.Load<AudioClip>("Majsoul/Audio/"+item.cue);result.length=clip?.length??0;
                lobby.PreviewDressTable(item.category);
                while(!lobby.PreviewDressTableReady){if(Time.unscaledTime-began>20)throw new Exception("Preview not ready "+id);yield return null;}
                began=Time.unscaledTime;var data=new float[2048];float seconds=mode=="before"?8:22;
                while(Time.unscaledTime-began<seconds){
                    var cues=audio.PlayedCues.Skip(before).Where(c=>c==item.cue).ToArray();
                    if(cues.Length>result.cues.Count){float now=Time.unscaledTime-began;if(result.cues.Count==0)result.firstCueAt=now;result.lastCueAt=now;result.cues=cues.ToList();}
                    if(cues.Length>0){result.sourcePlaying|=source.isPlaying;source.GetOutputData(data,0);foreach(float value in data)result.peak=Mathf.Max(result.peak,Mathf.Abs(value));}
                    result.cycles=Field<int>(lobby,"dressTableCycle");yield return null;
                }
                result.previewStarted=item.category=="winEffect"?effects.SpawnedWinEffects>startBursts:GetComponent<RiichiDiscardVisual>().PlayedCount>0;
                result.inTable=Field<bool>(audio,"inTable");result.effectsEnabled=Field<bool>(audio,"effectsEnabled");result.cueObserved=result.cues.Count>0;
                lobby.PreviewCloseDressTable();yield return new WaitForSecondsRealtime(.4f);result.exited=!client.DressPreviewing&&client.State==null;result.stoppedOnExit=!source.isPlaying&&!Field<bool>(audio,"inTable");
                if(mode=="before"){if(!result.previewStarted||result.inTable||result.cueObserved)throw new Exception("Before-state did not reproduce expected missing preview audio");}
                else if(!result.previewStarted||!result.inTable||!result.effectsEnabled||!result.cueObserved||!result.sourcePlaying||result.peak<.00001f||result.cues.Count<2||!result.exited||!result.stoppedOnExit)throw new Exception("Preview playback failed "+id+" / "+JsonUtility.ToJson(result));
                yield return null;
            }
        }
    }
}
