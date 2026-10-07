package com.yunjian.riichi;
import android.app.*;
import android.os.*;
import android.content.*;
import android.view.*;
import android.webkit.*;
import android.graphics.*;
import android.media.ExifInterface;
import android.media.AudioManager;
import android.net.Uri;
import android.util.Base64;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.*;
import org.json.*;
public class MainActivity extends Activity {
 private WebView web;
 private final Handler handler=new Handler(Looper.getMainLooper());
 private final ExecutorService network=Executors.newFixedThreadPool(3);
 private volatile boolean destroyed;
 private boolean avatarPicking;
 private static final int PICK_AVATAR=41;
 private void immersive(){getWindow().getDecorView().setSystemUiVisibility(5894);}
 @Override public void onWindowFocusChanged(boolean focus){super.onWindowFocusChanged(focus);if(focus)immersive();}
 @Override public void onCreate(Bundle saved){
  super.onCreate(saved);setVolumeControlStream(AudioManager.STREAM_MUSIC);getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);immersive();
  startForegroundService(new Intent(this,RoomService.class));
  web=new WebView(this);web.setBackgroundColor(0xff112b2b);web.getSettings().setJavaScriptEnabled(true);web.getSettings().setMediaPlaybackRequiresUserGesture(false);web.getSettings().setDomStorageEnabled(true);web.getSettings().setAllowFileAccess(false);web.getSettings().setAllowContentAccess(false);
  web.addJavascriptInterface(new Bridge(),"NativeApp");
  web.setWebViewClient(new WebViewClient(){@Override public boolean shouldOverrideUrlLoading(WebView v,WebResourceRequest r){return true;}});
  setContentView(web);web.loadUrl("file:///android_asset/index.html");
 }
 private void avatarResult(String data,String error){handler.post(()->{if(!destroyed)web.evaluateJavascript("window.avatarPicked&&window.avatarPicked("+JSONObject.quote(data)+","+JSONObject.quote(error)+")",null);});}
 @Override protected void onActivityResult(int request,int result,Intent data){
  super.onActivityResult(request,result,data);if(request!=PICK_AVATAR)return;avatarPicking=false;
  if(result!=RESULT_OK||data==null||data.getData()==null)return;final Uri uri=data.getData();
  network.execute(()->{Bitmap source=null,rotated=null,cropped=null,small=null;try{
   if(!"content".equals(uri.getScheme()))throw new IOException("Unsupported image location");
   BitmapFactory.Options bounds=new BitmapFactory.Options();bounds.inJustDecodeBounds=true;
   try(InputStream in=getContentResolver().openInputStream(uri)){BitmapFactory.decodeStream(in,null,bounds);}
   if(bounds.outWidth<=0||bounds.outHeight<=0)throw new IOException("Invalid image");
   BitmapFactory.Options opts=new BitmapFactory.Options();opts.inSampleSize=1;while(Math.max(bounds.outWidth,bounds.outHeight)/opts.inSampleSize>512)opts.inSampleSize*=2;
   try(InputStream in=getContentResolver().openInputStream(uri)){source=BitmapFactory.decodeStream(in,null,opts);}if(source==null)throw new IOException("Invalid image");
   int orientation=1;try(InputStream in=getContentResolver().openInputStream(uri)){orientation=new ExifInterface(in).getAttributeInt(ExifInterface.TAG_ORIENTATION,1);}catch(Exception ignored){}
   Matrix matrix=new Matrix();switch(orientation){case 2:matrix.setScale(-1,1);break;case 3:matrix.setRotate(180);break;case 4:matrix.setScale(1,-1);break;case 5:matrix.setRotate(90);matrix.postScale(-1,1);break;case 6:matrix.setRotate(90);break;case 7:matrix.setRotate(-90);matrix.postScale(-1,1);break;case 8:matrix.setRotate(-90);break;}
   rotated=Bitmap.createBitmap(source,0,0,source.getWidth(),source.getHeight(),matrix,true);int edge=Math.min(rotated.getWidth(),rotated.getHeight());
   cropped=Bitmap.createBitmap(rotated,(rotated.getWidth()-edge)/2,(rotated.getHeight()-edge)/2,edge,edge);
   small=Bitmap.createBitmap(128,128,Bitmap.Config.ARGB_8888);Canvas canvas=new Canvas(small);canvas.drawColor(Color.WHITE);Paint paint=new Paint(Paint.FILTER_BITMAP_FLAG);canvas.drawBitmap(cropped,null,new Rect(0,0,128,128),paint);
   String encoded="";for(int quality:new int[]{85,70,55,40,25}){ByteArrayOutputStream out=new ByteArrayOutputStream();small.compress(Bitmap.CompressFormat.JPEG,quality,out);encoded="data:image/jpeg;base64,"+Base64.encodeToString(out.toByteArray(),Base64.NO_WRAP);if(encoded.length()<=12288)break;}
   if(encoded.length()>12288)throw new IOException("Image too large");avatarResult(encoded,"");
  }catch(Exception e){avatarResult("","无法读取这张图片，请换一张 JPG 或 PNG");}finally{if(small!=null)small.recycle();if(cropped!=null&&cropped!=rotated&&cropped!=source)cropped.recycle();if(rotated!=null&&rotated!=source)rotated.recycle();if(source!=null)source.recycle();}});
 }
 @Override protected void onResume(){super.onResume();if(web!=null)web.evaluateJavascript("window.RiichiVoice&&window.RiichiVoice.suspend(false)",null);}
 @Override protected void onPause(){if(web!=null)web.evaluateJavascript("window.RiichiVoice&&window.RiichiVoice.suspend(true)",null);super.onPause();}
 public class Bridge {
  @JavascriptInterface public void pickAvatar(){handler.post(()->{if(destroyed||avatarPicking)return;try{Intent intent=new Intent(Intent.ACTION_OPEN_DOCUMENT);intent.setType("image/*");intent.addCategory(Intent.CATEGORY_OPENABLE);avatarPicking=true;startActivityForResult(intent,PICK_AVATAR);}catch(Exception e){avatarPicking=false;avatarResult("","未找到图片选择器");}});}
  @JavascriptInterface public String config(){try{return new JSONObject().put("host",RoomService.hostSecret(MainActivity.this)).put("addresses",new JSONArray(RoomService.addresses())).put("ready",RoomService.ready).toString();}catch(Exception e){return "{}";}}
  @JavascriptInterface public void settings(){handler.post(()->{try{startActivity(new Intent(android.provider.Settings.ACTION_WIRELESS_SETTINGS));}catch(Exception e){startActivity(new Intent(android.provider.Settings.ACTION_SETTINGS));}});}
  @JavascriptInterface public void request(String id,String endpoint,String body){
   if(destroyed)return;
   network.execute(()->{String result;HttpURLConnection conn=null;
    try{
     URL url=LanEndpoint.api(endpoint);if(body.length()>16384)throw new IOException("请求过大");
     conn=(HttpURLConnection)url.openConnection();conn.setInstanceFollowRedirects(false);conn.setConnectTimeout(4000);conn.setReadTimeout(14000);conn.setRequestMethod("POST");conn.setDoOutput(true);conn.setRequestProperty("Content-Type","application/json");
     byte[] data=body.getBytes(StandardCharsets.UTF_8);conn.setFixedLengthStreamingMode(data.length);try(OutputStream out=conn.getOutputStream()){out.write(data);}
     if(conn.getResponseCode()!=200)throw new IOException("房间连接失败");
     try(InputStream in=conn.getInputStream();ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] b=new byte[4096];int n;while((n=in.read(b))!=-1){out.write(b,0,n);if(out.size()>262144)throw new IOException("响应过大");}result=out.toString("UTF-8");new JSONObject(result);}
    }catch(Exception e){result="{\"ok\":false,\"networkError\":true,\"error\":\"无法连接房主，请检查热点和地址\"}";}finally{if(conn!=null)conn.disconnect();}
    final String response=result;handler.post(()->{if(!destroyed)web.evaluateJavascript("window.nativeResponse("+JSONObject.quote(id)+","+JSONObject.quote(response)+")",null);});
   });
  }
 }
 @Override public void onBackPressed(){new AlertDialog.Builder(this).setTitle("离开牌桌").setMessage("牌局会自动保存。房主退出后，其他手机暂时无法继续对局。").setPositiveButton("继续打牌",null).setNeutralButton("返回大厅",(d,w)->web.evaluateJavascript("window.showHome()",null)).setNegativeButton("保存并退出",(d,w)->{stopService(new Intent(this,RoomService.class));finish();}).show();}
 @Override protected void onDestroy(){destroyed=true;handler.removeCallbacksAndMessages(null);network.shutdownNow();if(web!=null)web.destroy();super.onDestroy();}
}
