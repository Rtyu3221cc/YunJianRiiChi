using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        RoomQrScanner qrScanner;
        Texture2D invitationCode;
        string invitationPayload="",invitationHeading="邀请牌友",qrNotice="",qrStatus="将二维码放入取景框";
        bool invitationThemed,invitationGold=true,invitationTightPreview;
        Rect InvitationRect=>new Rect((width-1065.375f)/2,127.5f,1065.375f,825);
        Rect ScannerRect=>new Rect((width-1000)/2,100,1000,880);
        void EnsureScanner(){
            if(qrScanner!=null)return;
            var owner=new GameObject("RoomQrScanner");owner.transform.SetParent(transform,false);qrScanner=owner.AddComponent<RoomQrScanner>();
            qrScanner.Decoded=HandleScannedQr;
            qrScanner.Failed=message=>ShowQrNotice(message);
            qrScanner.Cancelled=()=>{if(Modal=="scan"&&!modalClosing)qrScanner.Begin();};
        }
        public void OpenInvitation(bool tableEntry=false){
            invitationHeading=tableEntry?"邀请好友":"邀请牌友";
            OpenModal("invite");RefreshInvitation();
        }
        void RefreshInvitation(){
            string payload=client.InvitationPayload;
            if(payload==invitationPayload&&invitationCode!=null)return;
            invitationPayload=payload;if(invitationCode!=null)Destroy(invitationCode);
            invitationCode=string.IsNullOrEmpty(payload)?null:invitationGold?RoomQr.Image(payload,384,new Color32(232,175,113,255),new Color32(20,34,55,0),true):invitationThemed?RoomQr.Image(payload,384,new Color32(20,34,55,255),new Color32(243,229,204,255),true):RoomQr.Image(payload,trimQuiet:true,tightPreview:invitationTightPreview);
        }
        void UpdateQrFlow(){
            if(Modal=="invite")RefreshInvitation();
            if(Modal!="scan")StopQrCamera();
        }
        void StopQrCamera(){if(qrScanner!=null&&(qrScanner.CameraTexture!=null||qrScanner.Opening))qrScanner.StopCamera();}
        void DisposeQrFlow(){if(qrScanner!=null)Destroy(qrScanner.gameObject);if(invitationCode!=null)Destroy(invitationCode);}
        void OpenScanner(){EnsureScanner();OpenModal("scan");qrStatus="将二维码放入取景框";qrScanner.Begin();}
        void DrawInvitation(){
            var r=InvitationRect;ReferencePopup(r,new Rect(r.x+20.45f,r.y+17.55f,r.width-40.89f,r.height-35.1f));
            RoomCentered(new Rect(r.x+80,r.y+32,r.width-160,78),invitationHeading,ReferenceBrush(48),new Color(.9098f,.68627f,.44314f),6);
            if(invitationCode!=null){
                var qr=new Rect(r.center.x-290,r.y+100,580,580);
                if(invitationThemed)Tint(new Rect(qr.x-4,qr.y-4,qr.width+8,qr.height+8),new Color(.69f,.51f,.29f));
                GUI.DrawTexture(qr,invitationCode,ScaleMode.ScaleToFit);
            }
            if(PopupConfirm(new Rect(r.center.x-158.435f,r.yMax-130,316.87f,87.5f),"确认","invite-confirm"))CloseModal();
        }
        void DrawScanner(){
            var r=ScannerRect;ReferencePopup(r,new Rect(r.x+21,r.y+18,r.width-42,r.height-36));
            RoomCentered(new Rect(r.x+80,r.y+30,r.width-160,78),"扫码加入",ReferenceBrush(48),new Color(.9098f,.68627f,.44314f),6);
            var preview=new Rect(r.center.x-360,r.y+130,720,500);Tint(preview,new Color(.025f,.04f,.075f));
            var camera=qrScanner?.CameraTexture;
            if(camera!=null&&camera.width>32){
                var matrix=GUI.matrix;int rotation=camera.videoRotationAngle;GUIUtility.RotateAroundPivot(-rotation,preview.center);
                var image=rotation%180==0?preview:new Rect(preview.center.x-preview.height/2,preview.center.y-preview.width/2,preview.height,preview.width);
                float aspect=camera.width/(float)camera.height;float h=Mathf.Min(image.height,image.width/aspect),w=h*aspect;
                image=new Rect(image.center.x-w/2,image.center.y-h/2,w,h);
                GUI.DrawTextureWithTexCoords(image,camera,new Rect(0,camera.videoVerticallyMirrored?1:0,1,camera.videoVerticallyMirrored?-1:1));GUI.matrix=matrix;
            }else Label(preview,qrScanner?.Opening==true?"正在开启摄像头…":"摄像头未开启",new GUIStyle(descriptionText){alignment=TextAnchor.MiddleCenter},new Color(.75f,.78f,.84f));
            var edge=new Color(.91f,.72f,.43f);float length=35,thickness=4;
            foreach(float x in new[]{preview.x,preview.xMax-length})foreach(float y in new[]{preview.y,preview.yMax-thickness})Tint(new Rect(x,y,length,thickness),edge);
            foreach(float x in new[]{preview.x,preview.xMax-thickness})foreach(float y in new[]{preview.y,preview.yMax-length})Tint(new Rect(x,y,thickness,length),edge);
            Label(new Rect(r.x+80,r.y+650,r.width-160,50),Searching?"正在加入房间…":qrStatus,new GUIStyle(descriptionText){fontSize=30,alignment=TextAnchor.MiddleCenter},new Color(.83f,.81f,.81f));
            bool enabled=GUI.enabled;GUI.enabled&=!Searching;
            if(PopupConfirm(new Rect(r.center.x-290,r.yMax-130,270,87.5f),"摄像头","scan-camera")){EnsureScanner();qrScanner.Begin();}
            if(PopupConfirm(new Rect(r.center.x+20,r.yMax-130,270,87.5f),"相册","scan-gallery")){EnsureScanner();qrScanner.PickImage();}
            GUI.enabled=enabled;
        }
        void ShowQrNotice(string message){qrScanner?.StopCamera();qrNotice=message;Searching=false;OpenModal("qr-invalid");}
        void DrawQrNotice(){
            var r=new Rect((width-852.3f)/2,285,852.3f,510);ReferencePopup(r,new Rect(r.x+20.45f,r.y+17.55f,r.width-40.89f,r.height-35.1f));
            RoomCentered(new Rect(r.x+80,r.y+32,r.width-160,78),"提示",ReferenceBrush(48),new Color(.9098f,.68627f,.44314f),6);
            Label(new Rect(r.x+80,r.y+134,r.width-160,165),qrNotice,new GUIStyle(descriptionText){fontSize=36,alignment=TextAnchor.MiddleCenter},new Color(.83f,.81f,.81f));
            if(PopupConfirm(new Rect(r.center.x-158.435f,r.yMax-130,316.87f,87.5f),"确认","qr-notice-confirm"))OpenModal("scan");
        }
        public void HandleScannedQr(string value){
            if(Modal!="scan"||modalClosing)return;
            qrScanner?.StopCamera();
            if(!RoomQr.TryParse(value,out var invitation)){ShowQrNotice("当前二维码不正确");return;}
            StartCoroutine(JoinInvitation(invitation));
        }
        IEnumerator JoinInvitation(RoomQr.Invitation invitation){
            CancelSearch();search=new System.Threading.CancellationTokenSource();Searching=true;
            string endpoint=null;
            foreach(string candidate in invitation.endpoints){
                using(var request=new UnityWebRequest(candidate+"/api","POST")){
                    request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new RequestBody{op="discover",pin=invitation.pin})));request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=2;request.redirectLimit=0;
                    yield return request.SendWebRequest();
                    if(!Searching||Modal!="scan"||modalClosing)yield break;
                    RoomReply reply=null;if(request.result==UnityWebRequest.Result.Success)try{reply=JsonUtility.FromJson<RoomReply>(request.downloadHandler.text);}catch(ArgumentException){}
                    if(reply?.ok==true){endpoint=candidate;break;}
                }
            }
            if(endpoint==null){
                var task=discovery.Find(invitation.pin,search.Token);while(!task.IsCompleted){if(!Searching||Modal!="scan"||modalClosing)yield break;yield return null;}
                if(!task.IsCanceled&&!task.IsFaulted)endpoint=task.Result;
            }
            Searching=false;if(string.IsNullOrEmpty(endpoint)){ShowQrNotice("未找到房间，请确认与房主连接同一 Wi-Fi 或同一热点。");yield break;}
            number=invitation.pin;CloseModal();client.LobbyJoin(endpoint,invitation.pin,"");
        }
#if UNITY_EDITOR
        public void DesignPreviewQrTheme(bool themed){invitationThemed=themed;invitationGold=invitationTightPreview=false;invitationPayload="";RefreshInvitation();}
        public void DesignPreviewQrGold(){invitationThemed=invitationTightPreview=false;invitationGold=true;invitationPayload="";RefreshInvitation();}
        public void DesignPreviewQrTight(){invitationThemed=invitationGold=false;invitationTightPreview=true;invitationPayload="";RefreshInvitation();}
        public Texture2D DesignPreviewQrTexture=>invitationCode;
        public void DesignPreviewInvitation(bool tableEntry=false){OpenInvitation(tableEntry);}
        public void DesignPreviewScanner(){EnsureScanner();OpenModal("scan");qrStatus="将二维码放入取景框";}
        public void DesignPreviewInvalidQr(){ShowQrNotice("当前二维码不正确");}
#endif
    }
}
