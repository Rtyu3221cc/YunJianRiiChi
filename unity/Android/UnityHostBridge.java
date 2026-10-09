package com.yunjian.riichi;

import android.app.Activity;
import android.content.Intent;
import android.os.Build;
import org.json.JSONArray;
import org.json.JSONObject;

/** Retains the existing phone-hosted JS rules + AI; Unity owns all rendering. */
public final class UnityHostBridge {
 private static Activity activity;
 public static void start(Activity a) {
  activity=a;
  a.runOnUiThread(()->{
   Intent intent=new Intent(a,RoomService.class);
   if(Build.VERSION.SDK_INT>=26)a.startForegroundService(intent);else a.startService(intent);
  });
 }
 public static String config() {
  try {return new JSONObject().put("host",RoomService.hostSecret(activity)).put("addresses",new JSONArray(RoomService.addresses())).put("ready",RoomService.ready).put("error",RoomService.error==null?"":RoomService.error).toString();}
  catch(Exception e){return "{\"ready\":false,\"error\":\"Host not initialized\"}";}
 }
 public static void settings() {
  activity.runOnUiThread(()->activity.startActivity(new Intent(android.provider.Settings.ACTION_WIRELESS_SETTINGS)));
 }
}
