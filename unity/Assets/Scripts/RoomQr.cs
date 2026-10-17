using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using ZXing;
using ZXing.QrCode.Internal;

namespace YunJian.Riichi {
    // Public invitation data only. Seat tokens, recovery credentials and host
    // secrets never belong in a QR code. Joining still uses the room service.
    public static class RoomQr {
        public sealed class Invitation {public string pin;public string[] endpoints;}
        public static string EncodeInvite(string pin,IEnumerable<string> addresses){
            var hosts=(addresses??new string[0]).Where(RiichiClient.ValidEndpoint).Distinct().Take(8);
            return "yunjian-riichi://join?v=1&pin="+Uri.EscapeDataString(pin??"")+"&hosts="+Uri.EscapeDataString(string.Join(",",hosts));
        }
        public static bool TryParse(string text,out Invitation invitation){
            invitation=null;
            if(string.IsNullOrEmpty(text)||text.Length>2048||!Uri.TryCreate(text,UriKind.Absolute,out var uri)||uri.Scheme!="yunjian-riichi"||uri.Host!="join"||!string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Fragment)||uri.AbsolutePath!=""&&uri.AbsolutePath!="/")return false;
            var data=new Dictionary<string,string>();
            try{foreach(var part in uri.Query.TrimStart('?').Split('&')){var pair=part.Split(new[]{'='},2);if(pair.Length!=2||data.ContainsKey(pair[0]))return false;data[pair[0]]=Uri.UnescapeDataString(pair[1]);}}catch(UriFormatException){return false;}
            if(!data.TryGetValue("v",out var version)||version!="1"||!data.TryGetValue("pin",out var pin)||pin.Length!=6||pin.Any(c=>c<'0'||c>'9'))return false;
            var hosts=data.TryGetValue("hosts",out var hostList)&&hostList.Length>0?hostList.Split(','):new string[0];
            if(hosts.Length>8||hosts.Any(host=>!RiichiClient.ValidEndpoint(host)))return false;
            invitation=new Invitation{pin=pin,endpoints=hosts.Distinct().ToArray()};return true;
        }
        public static Texture2D Image(string value,int side=384,Color32? foreground=null,Color32? background=null,bool trimQuiet=false,bool tightPreview=false){
            var hints=new Dictionary<EncodeHintType,object>{{EncodeHintType.CHARACTER_SET,"UTF-8"},{EncodeHintType.MARGIN,4},{EncodeHintType.ERROR_CORRECTION,ErrorCorrectionLevel.M}};
            var bits=new MultiFormatWriter().encode(value,BarcodeFormat.QR_CODE,side,side,hints);
            int crop=0;
            if(trimQuiet){
                var natural=new MultiFormatWriter().encode(value,BarcodeFormat.QR_CODE,0,0,hints);int module=Mathf.Max(1,side/natural.Width),padding=side;
                for(int y=0;y<side;y++)for(int x=0;x<padding;x++)if(bits[x,y])padding=x;
                int desired=Mathf.RoundToInt(.4f*padding*side/(side-1.2f*padding));
                crop=Mathf.Max(0,Mathf.Min(desired,padding-(tightPreview?0:4*module)));
            }
            int size=side-2*crop;var dark=foreground??new Color32(0,0,0,255);var light=background??new Color32(255,255,255,255);
            var pixels=new Color32[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[(size-1-y)*size+x]=bits[x+crop,y+crop]?dark:light;
            var image=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Room invitation QR",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};image.SetPixels32(pixels);image.Apply();return image;
        }
        public static string Decode(Color32[] pixels,int width,int height){
            // Unity pixels start at the lower left; ZXing reads top-down rows.
            var rgb=new byte[width*height*3];for(int y=0;y<height;y++)for(int x=0;x<width;x++){var c=pixels[(height-1-y)*width+x];int i=(y*width+x)*3;rgb[i]=c.r;rgb[i+1]=c.g;rgb[i+2]=c.b;}
            // Keep inversion and format hints in the same Options instance.
            var reader=new BarcodeReaderGeneric{AutoRotate=true,Options=new ZXing.Common.DecodingOptions{TryHarder=true,TryInverted=true,PossibleFormats=new List<BarcodeFormat>{BarcodeFormat.QR_CODE}}};
            return reader.Decode(rgb,width,height,RGBLuminanceSource.BitmapFormat.RGB24)?.Text;
        }
    }
}
