package com.yunjian.riichi;
import java.net.*;
/** Only literal local IPv4 room addresses are accepted. */
public final class LanEndpoint {
 public static URL api(String endpoint)throws Exception{
  URI u=new URI(endpoint);String h=u.getHost();
  if(!"http".equals(u.getScheme())||h==null||u.getPort()!=8765||u.getRawUserInfo()!=null||u.getRawQuery()!=null||u.getRawFragment()!=null||!(u.getPath().isEmpty()||"/".equals(u.getPath())))throw new IllegalArgumentException();
  String[] p=h.split("\\.");if(p.length!=4)throw new IllegalArgumentException();int[] a=new int[4];for(int i=0;i<4;i++){if(!p[i].matches("0|[1-9][0-9]{0,2}"))throw new IllegalArgumentException();a[i]=Integer.parseInt(p[i]);if(a[i]>255)throw new IllegalArgumentException();}
  if(!(h.equals("127.0.0.1")||a[0]==10||a[0]==192&&a[1]==168||a[0]==172&&a[1]>=16&&a[1]<=31))throw new IllegalArgumentException();
  return new URL("http://"+h+":8765/api");
 }
}
