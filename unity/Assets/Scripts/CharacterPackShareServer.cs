using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace YunJian.Riichi {
    // A separate streaming service: no room RPC workers or Unity objects on these threads.
    public sealed class CharacterPackShareServer:IDisposable {
        public sealed class Entry {public string hash,key,path,url;public long bytes;}
        readonly object gate=new object();readonly SemaphoreSlim transfers=new SemaphoreSlim(3);Dictionary<string,Entry> entries=new Dictionary<string,Entry>();TcpListener listener;volatile bool running;
        public string Ticket {get;private set;}=Guid.NewGuid().ToString("N");public int Port {get;private set;}public bool Enabled;
        public int RangeRequests,Requests;public int ReviewChunkDelayMs;
        public CharacterPackShareServer(){listener=new TcpListener(IPAddress.Any,0);listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;running=true;new Thread(Accept){IsBackground=true,Name="character-pack-share"}.Start();}
        public void Rotate(){Ticket=Guid.NewGuid().ToString("N");}
        public void SetEntries(IEnumerable<Entry> values){lock(gate)entries=values.GroupBy(x=>x.hash).ToDictionary(g=>g.Key,g=>g.First());}
        void Accept(){while(running)try{var client=listener.AcceptTcpClient();ThreadPool.QueueUserWorkItem(_=>Serve(client));}catch(SocketException){if(!running)return;}catch(ObjectDisposedException){return;}}
        static string Line(Stream input){var bytes=new List<byte>();for(int b;(b=input.ReadByte())>=0;){if(b==10)break;if(b!=13)bytes.Add((byte)b);if(bytes.Count>8192)throw new InvalidDataException("Header too long");}return Encoding.ASCII.GetString(bytes.ToArray());}
        static void Header(Stream output,int status,long length,string extra=""){string h="HTTP/1.1 "+status+" "+(status==200?"OK":status==206?"Partial Content":"Unavailable")+"\r\nContent-Type: application/octet-stream\r\nContent-Length: "+length+"\r\nAccept-Ranges: bytes\r\nConnection: close\r\nCache-Control: no-store\r\n"+extra+"\r\n";byte[] bytes=Encoding.ASCII.GetBytes(h);output.Write(bytes,0,bytes.Length);}
        void Serve(TcpClient client){bool acquired=false,pinned=false;Entry item=null;try{using(client){client.ReceiveTimeout=12000;client.SendTimeout=12000;using(var stream=client.GetStream()){var first=Line(stream).Split(' ');if(first.Length!=3||first[0]!="GET"){Header(stream,405,0);return;}string range=null;for(int i=0;i<40;i++){string line=Line(stream);if(line.Length==0)break;if(line.StartsWith("Range:",StringComparison.OrdinalIgnoreCase))range=line.Substring(6).Trim();if(i==39)throw new InvalidDataException();}
                var uri=new Uri("http://localhost"+first[1]);string hash=uri.AbsolutePath.StartsWith("/packs/")?uri.AbsolutePath.Substring(7):"";if(!Enabled||uri.Query!="?ticket="+Ticket){Header(stream,403,0);return;}if(!System.Text.RegularExpressions.Regex.IsMatch(hash,"^[a-f0-9]{64}$")){Header(stream,404,0);return;}lock(gate)entries.TryGetValue(hash,out item);if(item==null){Header(stream,404,0);return;}if(!transfers.Wait(0)){Header(stream,503,0);return;}acquired=true;
                Interlocked.Increment(ref Requests);long start=0;if(range!=null&&(!range.StartsWith("bytes=")||!range.EndsWith("-")||!long.TryParse(range.Substring(6,range.Length-7),out start)||start<0||start>=item.bytes)){Header(stream,416,0);return;}if(start>0)Interlocked.Increment(ref RangeRequests);
                Stream input;WebResponse response=null;
                if(item.path!=null){CharacterPackService.Pin(item.key);pinned=true;input=new FileStream(item.path,FileMode.Open,FileAccess.Read,FileShare.Read);input.Position=start;}
                else{var request=(HttpWebRequest)WebRequest.Create(item.url);request.Timeout=15000;request.ReadWriteTimeout=15000;if(start>0)request.AddRange(start);response=request.GetResponse();if(start>0&&((HttpWebResponse)response).StatusCode!=HttpStatusCode.PartialContent){response.Dispose();Header(stream,502,0);return;}input=response.GetResponseStream();}
                using(response)using(input){Header(stream,start>0?206:200,item.bytes-start,start>0?"Content-Range: bytes "+start+"-"+(item.bytes-1)+"/"+item.bytes+"\r\n":"");var buffer=new byte[65536];long remaining=item.bytes-start;while(running&&remaining>0){int n=input.Read(buffer,0,(int)Math.Min(remaining,buffer.Length));if(n<=0)break;stream.Write(buffer,0,n);remaining-=n;if(ReviewChunkDelayMs>0)Thread.Sleep(ReviewChunkDelayMs);}}
            }}}catch(Exception){/* Receiver retries or chooses fallback; a transfer never stops the room. */}finally{if(pinned)CharacterPackService.Unpin(item.key);if(acquired)transfers.Release();}}
        public void Dispose(){running=false;Enabled=false;listener?.Stop();}
    }
}
