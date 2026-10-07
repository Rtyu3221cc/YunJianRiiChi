import com.yunjian.riichi.LanEndpoint;
public class LanEndpointTest {
 public static void main(String[] args)throws Exception{
  String[] good={"http://127.0.0.1:8765","http://192.168.43.1:8765/","http://10.0.0.1:8765","http://172.16.0.1:8765","http://172.31.255.254:8765"};
  String[] bad={"https://192.168.1.1:8765","http://example.com:8765","http://8.8.8.8:8765","http://172.32.0.1:8765","http://172.15.0.1:8765","http://127.0.0.2:8765","http://192.168.1.999:8765","http://192.168.01.1:8765","http://192.168.1.1:80","http://192.168.1.1:8765/path","http://user@192.168.1.1:8765","http://192.168.1.1:8765?x=1","http://192.168.1.1:8765#x","http://[::1]:8765"};
  for(String s:good)if(!LanEndpoint.api(s).getPath().equals("/api"))throw new AssertionError(s);
  for(String s:bad){boolean failed=false;try{LanEndpoint.api(s);}catch(Exception e){failed=true;}if(!failed)throw new AssertionError("Accepted invalid URL "+s);}
  System.out.println("PASS: "+good.length+" LAN addresses; "+bad.length+" invalid destinations rejected");
 }
}
