package com.yunjian.riichi;

import android.app.*;
import android.content.*;
import android.os.*;
import android.webkit.*;
import android.util.AtomicFile;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.*;
import java.util.concurrent.*;
import org.json.*;

public class RoomService extends Service {
 public static volatile boolean ready=false;
 public static volatile String error;
 private WebView engine;
 private ServerSocket server;
 private DatagramSocket discovery;
 private final Handler main=new Handler(Looper.getMainLooper());
 private final ExecutorService workers=new ThreadPoolExecutor(2,8,30,TimeUnit.SECONDS,new ArrayBlockingQueue<Runnable>(32));
 private PowerManager.WakeLock wake;
 private AtomicFile state;
 private boolean started;
 private final Runnable aiTick=new Runnable(){public void run(){if(started&&engine!=null)engine.evaluateJavascript("room.tick()",v->{if(started)main.postDelayed(this,50);});}};
 public static String hostSecret(Context c){
  android.content.SharedPreferences p=c.getSharedPreferences("room",0);String v=p.getString("host",null);
  if(v==null){byte[] b=new byte[24];new SecureRandom().nextBytes(b);StringBuilder s=new StringBuilder();for(byte x:b)s.append(String.format(java.util.Locale.ROOT,"%02x",x&255));v=s.toString();p.edit().putString("host",v).commit();}return v;
 }
 public static List<String> addresses(){List<String> r=new ArrayList<>();try{Enumeration<NetworkInterface> es=NetworkInterface.getNetworkInterfaces();while(es.hasMoreElements()){NetworkInterface n=es.nextElement();if(!n.isUp()||n.isLoopback())continue;Enumeration<InetAddress> as=n.getInetAddresses();while(as.hasMoreElements()){InetAddress a=as.nextElement();if(a instanceof Inet4Address&&!a.isLoopbackAddress()&&a.isSiteLocalAddress())r.add("http://"+a.getHostAddress()+":8765");}}}catch(Exception ignored){}return r;}
 public static String addressesText(){return String.join("\n",addresses());}
 public class Storage {
  @JavascriptInterface public String load(){try{return new String(state.readFully(),StandardCharsets.UTF_8);}catch(FileNotFoundException e){return "";}catch(Exception e){throw new RuntimeException("牌局存档无法读取，请保留数据并重启",e);}}
  @JavascriptInterface public boolean save(String json){FileOutputStream out=null;try{out=state.startWrite();out.write(json.getBytes(StandardCharsets.UTF_8));state.finishWrite(out);return true;}catch(Exception e){if(out!=null)state.failWrite(out);return false;}}
  @JavascriptInterface public String secret(){return hostSecret(RoomService.this);}
 }
 @Override public void onCreate(){
  super.onCreate();ready=false;error=null;state=new AtomicFile(new File(getFilesDir(),"room.json"));
  NotificationManager nm=(NotificationManager)getSystemService(NOTIFICATION_SERVICE);nm.createNotificationChannel(new NotificationChannel("room","局域网牌桌",NotificationManager.IMPORTANCE_LOW));
  PendingIntent p=PendingIntent.getActivity(this,0,new Intent(this,MainActivity.class),PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
  startForeground(1,new Notification.Builder(this,"room").setContentTitle("云间日麻 · 房主服务运行中").setContentText("局域网联机无需互联网；建议保持 App 亮屏").setSmallIcon(android.R.drawable.ic_menu_myplaces).setContentIntent(p).setOngoing(true).build());
  wake=((PowerManager)getSystemService(POWER_SERVICE)).newWakeLock(PowerManager.PARTIAL_WAKE_LOCK,"lanriichi:room");wake.acquire();
  engine=new WebView(this);engine.getSettings().setJavaScriptEnabled(true);engine.getSettings().setAllowFileAccess(false);engine.getSettings().setAllowContentAccess(false);engine.addJavascriptInterface(new Storage(),"NativeStore");
  engine.setWebViewClient(new WebViewClient(){@Override public void onPageFinished(WebView v,String url){if(started)return;v.evaluateJavascript("window.engineReady===true",value->{if("true".equals(value)){started=true;startServer();main.postDelayed(aiTick,850);}else{error="规则引擎启动失败，请确认 Android System WebView 已更新。";}});}});
  engine.loadUrl("file:///android_asset/engine.html");
 }
 private void startServer(){new Thread(()->{try{server=new ServerSocket();server.setReuseAddress(true);server.bind(new InetSocketAddress("0.0.0.0",8765));ready=true;startDiscovery();while(!server.isClosed()){Socket c=server.accept();try{workers.execute(()->serve(c));}catch(RejectedExecutionException e){c.close();}}}catch(Exception e){if(started)error=e.toString();ready=false;}},"riichi-http").start();}
 private void startDiscovery(){new Thread(()->{
  try{discovery=new DatagramSocket(8766);byte[] bytes=new byte[96];
   while(started&&!discovery.isClosed()){
    DatagramPacket packet=new DatagramPacket(bytes,bytes.length);discovery.receive(packet);
    String query=new String(packet.getData(),packet.getOffset(),packet.getLength(),StandardCharsets.UTF_8);
    if(!query.matches("YUNJIAN-ROOM/1 [0-9]{6}"))continue;
    try{
     // A PIN-only query reveals neither seat credentials nor private hands.
     JSONObject reply=new JSONObject(dispatch(new JSONObject().put("op","discover").put("pin",query.substring(15)).toString()));
     if(!reply.optBoolean("ok"))continue;
     byte[] answer=(query+" OK").getBytes(StandardCharsets.UTF_8);discovery.send(new DatagramPacket(answer,answer.length,packet.getAddress(),packet.getPort()));
    }catch(Exception ignored){}
   }
  }catch(IOException ignored){/* Manual LAN address remains available if UDP is unavailable. */}
 },"riichi-discovery").start();}
 private byte[] read(InputStream in,int n)throws IOException{byte[] b=new byte[n];int p=0;while(p<n){int k=in.read(b,p,n-p);if(k<0)throw new EOFException();p+=k;}return b;}
 private String line(InputStream in)throws IOException{ByteArrayOutputStream b=new ByteArrayOutputStream();int c;while((c=in.read())!=-1){if(c==10)break;if(c!=13)b.write(c);if(b.size()>8192)throw new IOException("header too long");}return b.toString("UTF-8");}
 private byte[] asset(String p)throws IOException{try(InputStream in=getAssets().open(p)){ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] b=new byte[8192];int n;while((n=in.read(b))>0)out.write(b,0,n);return out.toByteArray();}}
 private String dispatch(String body)throws Exception{
  new JSONObject(body); // reject malformed/non-object JSON before evaluation
  CompletableFuture<String> future=new CompletableFuture<>();
  main.post(()->{try{engine.evaluateJavascript("JSON.stringify(room.dispatch(JSON.parse("+JSONObject.quote(body)+")))",v->{try{Object x=new JSONTokener(v).nextValue();future.complete(x instanceof String?(String)x:"{\"ok\":false,\"error\":\"引擎未就绪\"}");}catch(Exception e){future.completeExceptionally(e);}});}catch(Exception e){future.completeExceptionally(e);}});
  return future.get(12,TimeUnit.SECONDS);
 }
 private void serve(Socket c){try(Socket socket=c){socket.setSoTimeout(5000);InputStream in=new BufferedInputStream(socket.getInputStream());String[] first=line(in).split(" ");if(first.length!=3)return;
  String method=first[0],path=first[1];Map<String,String> headers=new HashMap<>();for(int i=0;i<60;i++){String h=line(in);if(h.isEmpty())break;int k=h.indexOf(':');if(k>0)headers.put(h.substring(0,k).toLowerCase(Locale.ROOT),h.substring(k+1).trim());if(i==59)throw new IOException("too many headers");}
  int status=200;String type="text/html; charset=utf-8";byte[] data;
  String origin=headers.get("origin"),host=headers.get("host");
  if(origin!=null&&!origin.equals("http://"+host)){status=403;data="Origin denied".getBytes(StandardCharsets.UTF_8);}
  else if("POST".equals(method)&&"/api".equals(path)){
   int len=Integer.parseInt(headers.getOrDefault("content-length","0"));if(len<2||len>65536)throw new IOException("invalid request size");
   if(!headers.getOrDefault("content-type","").startsWith("application/json")){status=415;data="JSON required".getBytes(StandardCharsets.UTF_8);}
   else {type="application/json; charset=utf-8";JSONObject command=new JSONObject(new String(read(in,len),StandardCharsets.UTF_8));command.put("peerAddress",socket.getInetAddress().getHostAddress());data=dispatch(command.toString()).getBytes(StandardCharsets.UTF_8);}
  }else if("GET".equals(method)&&"/info".equals(path)){type="application/json; charset=utf-8";data=new JSONObject().put("addresses",new JSONArray(addresses())).toString().getBytes(StandardCharsets.UTF_8);}
  else if("GET".equals(method)&&(path.equals("/")||path.equals("/index.html")))data=asset("index.html");
  else if("GET".equals(method)&&path.equals("/style.css")){type="text/css; charset=utf-8";data=asset("style.css");}
  else if("GET".equals(method)&&path.equals("/table.css")){type="text/css; charset=utf-8";data=asset("table.css");}
  else if("GET".equals(method)&&path.equals("/table-layout.js")){type="text/javascript; charset=utf-8";data=asset("table-layout.js");}
  else if("GET".equals(method)&&path.equals("/voice-manifest.js")){type="text/javascript; charset=utf-8";data=asset("voice-manifest.js");}
  else if("GET".equals(method)&&path.equals("/table/cloth.png")){type="image/png";data=asset("table/cloth.png");}
  else if("GET".equals(method)&&path.equals("/table/score-hub.png")){type="image/png";data=asset("table/score-hub.png");}
  else if("GET".equals(method)&&(path.equals("/portraits/yiji.png")||path.equals("/portraits/erjietang.png"))){type="image/png";data=asset(path.substring(1));}
  else if("GET".equals(method)&&path.equals("/felt.svg")){type="image/svg+xml";data=asset("felt.svg");}
  else if("GET".equals(method)&&path.matches("/tiles/(?:[mps][0-9]|z[1-7])\\.png")){type="image/png";data=asset(path.substring(1));}
  else if("GET".equals(method)&&path.matches("/voice/(?:[mps][0-9]|z[1-7]|ron|tsumo|yiji-[a-z0-9_-]+)\\.(?:wav|mp3)")){type=path.endsWith(".mp3")?"audio/mpeg":"audio/wav";data=asset(path.substring(1));}
  else if("GET".equals(method)&&path.equals("/voice.js")){type="text/javascript; charset=utf-8";data=asset("voice.js");}
  else if("GET".equals(method)&&path.equals("/client.js")){type="text/javascript; charset=utf-8";data=asset("client.js");}
  else if("GET".equals(method)&&path.equals("/LICENSE-core.txt")){type="text/plain; charset=utf-8";data=asset("LICENSE-core.txt");}
  else {status=404;data="Not found".getBytes(StandardCharsets.UTF_8);}
  OutputStream out=socket.getOutputStream();String h="HTTP/1.1 "+status+" "+(status==200?"OK":"Error")+"\r\nContent-Type: "+type+"\r\nContent-Length: "+data.length+"\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\nContent-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; media-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'\r\nConnection: close\r\n\r\n";out.write(h.getBytes(StandardCharsets.US_ASCII));out.write(data);out.flush();
 }catch(Exception e){/* disconnected or malformed requests never stop the table */}}
 @Override public int onStartCommand(Intent intent,int flags,int id){return START_STICKY;}
 @Override public IBinder onBind(Intent i){return null;}
 @Override public void onDestroy(){started=false;ready=false;main.removeCallbacksAndMessages(null);try{if(server!=null)server.close();}catch(Exception ignored){}if(discovery!=null)discovery.close();workers.shutdownNow();if(engine!=null)engine.destroy();if(wake!=null&&wake.isHeld())wake.release();super.onDestroy();}
}
