#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi {
 public static class OpponentHandPreview {
  static IEnumerator Shot(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
  public static IEnumerator Run(RiichiClient client,string folder){
   Directory.CreateDirectory(folder);var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,780),"Opponent hand layout preview"});window.Repaint();for(int i=0;i<8;i++)yield return null;
   foreach(int n in new[]{3,4}){
    var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.GetFullPath("../build/unity-smoke/preview-"+n+".json")));view.audioEvents=new PublicSound[0];view.options=new Choice[0];view.audioRoom="opponent-hand-preview";view.handSeq=0;
    foreach(var player in view.players){player.melds=new[]{"z3333-","p555+5","s1111","z555="};player.count=2;}view.hand=new[]{"m1","z1"};view.drawn="z1";client.ShowValidationFixture(view);yield return new WaitForSecondsRealtime(.4f);yield return Shot(folder,"after-"+n+"-seats");
   }
   Debug.Log("RIICHI OPPONENT HAND PREVIEW READY: actual Unity renderer; preview only, regression tests and APK build not run.");
  }
 }
}
#endif
