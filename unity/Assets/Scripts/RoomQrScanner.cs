using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed class RoomQrScanner:MonoBehaviour {
        public Action<string> Decoded,Failed;
        public Action Cancelled;
        public WebCamTexture CameraTexture {get;private set;}
        public bool Opening {get;private set;}
        Task<string> decode;
        float nextFrame;
        bool active;
        bool resumeAfterPicker;
        int session,decodeSession;
        public void Begin(){StopCamera();active=true;StartCoroutine(OpenCamera(session));}
        IEnumerator OpenCamera(int request){
            Opening=true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)){
                bool answered=false;var callbacks=new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted+=_=>answered=true;callbacks.PermissionDenied+=_=>answered=true;callbacks.PermissionDeniedAndDontAskAgain+=_=>answered=true;
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera,callbacks);
                while(!answered&&request==session)yield return null;
            }
            bool authorized=UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            bool authorized=Application.HasUserAuthorization(UserAuthorization.WebCam);
#endif
            if(request!=session)yield break;
            if(!authorized){Opening=false;Failed?.Invoke("未获得摄像头权限，可使用相册识别二维码。");yield break;}
            var devices=WebCamTexture.devices;if(devices.Length==0){Opening=false;Failed?.Invoke("未找到可用摄像头，可使用相册识别二维码。");yield break;}
            var device=devices.FirstOrDefault(d=>!d.isFrontFacing);if(string.IsNullOrEmpty(device.name))device=devices[0];
            try{CameraTexture=new WebCamTexture(device.name,640,480,24);CameraTexture.Play();Opening=false;}
            catch(Exception){StopCamera();Failed?.Invoke("暂时无法开启摄像头，可使用相册识别二维码。");}
        }
        void Update(){
            if(decode!=null&&decode.IsCompleted){var task=decode;decode=null;if(decodeSession==session&&!task.IsFaulted&&!task.IsCanceled&&!string.IsNullOrEmpty(task.Result)){var result=task.Result;StopCamera();Decoded?.Invoke(result);return;}}
            if(!active||decode!=null||CameraTexture==null||!CameraTexture.didUpdateThisFrame||CameraTexture.width<32||Time.unscaledTime<nextFrame)return;
            nextFrame=Time.unscaledTime+.3f;var pixels=CameraTexture.GetPixels32();int width=CameraTexture.width,height=CameraTexture.height;decodeSession=session;decode=Task.Run(()=>RoomQr.Decode(pixels,width,height));
        }
        public void StopCamera(){session++;active=false;Opening=false;if(CameraTexture!=null){CameraTexture.Stop();Destroy(CameraTexture);CameraTexture=null;}}
        public void PickImage(){
            resumeAfterPicker=CameraTexture!=null&&CameraTexture.isPlaying;
            StopCamera();
#if UNITY_ANDROID && !UNITY_EDITOR
            try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var picker=new AndroidJavaClass("com.yunjian.riichi.QrImagePickerActivity"))picker.CallStatic("open",unity.GetStatic<AndroidJavaObject>("currentActivity"),gameObject.name);}catch(Exception){Failed?.Invoke("暂时无法打开相册，请使用摄像头扫码。");}
#elif UNITY_EDITOR
            var path=UnityEditor.EditorUtility.OpenFilePanel("选择二维码图片","","png,jpg,jpeg");OnImagePicked(path);
#else
            Failed?.Invoke("暂时无法打开相册，请使用摄像头扫码。");
#endif
        }
        [UnityEngine.Scripting.Preserve]
        public void OnImagePicked(string path){
            if(string.IsNullOrEmpty(path)){if(resumeAfterPicker)Cancelled?.Invoke();return;}
            if(path.StartsWith("error:")){Failed?.Invoke("图片读取失败，请重新选择。");return;}
            try{
                var image=new Texture2D(2,2);string value=null;
                try{if(new FileInfo(path).Length>24*1024*1024||!image.LoadImage(File.ReadAllBytes(path)))throw new IOException();value=RoomQr.Decode(image.GetPixels32(),image.width,image.height);}finally{Destroy(image);}
                Decoded?.Invoke(value??"");
            }catch(Exception){Failed?.Invoke("图片读取失败，请重新选择。");}
#if UNITY_ANDROID && !UNITY_EDITOR
            finally{try{string own=Path.GetFullPath(Application.temporaryCachePath)+Path.DirectorySeparatorChar;if(Path.GetFullPath(path).StartsWith(own,StringComparison.Ordinal)&&Path.GetFileName(path).StartsWith("yunjian-qr-"))File.Delete(path);}catch(IOException){}}
#endif
        }
        void OnApplicationPause(bool paused){if(paused)StopCamera();}
        void OnDestroy(){StopCamera();}
    }
}
