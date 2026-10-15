using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YunJian.Riichi
{
    // Only a public room number is advertised. Tokens, host keys and hands
    // never enter UDP; joining still authenticates through the existing API.
    public sealed class LanRoomDiscovery : IDisposable
    {
        const int Port=8766;
        const string Prefix="YUNJIAN-ROOM/1 ";
        volatile string published="";
        volatile bool stopped;
        UdpClient listener;
        public void Publish(RoomView view) { published=view!=null&&view.host&&view.phase!="empty"&&view.phase!="join"?view.pin??"":""; }
        public void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // The foreground RoomService answers even when Unity is paused.
            // Only one listener owns UDP 8766 on each phone.
            return;
#else
            try {
                listener=new UdpClient(new IPEndPoint(IPAddress.Any,Port));
                listener.Client.ReceiveTimeout=400;
                Task.Run(()=>{
                    while(!stopped)try {
                        var peer=new IPEndPoint(IPAddress.Any,0);var packet=listener.Receive(ref peer);
                        string pin=published;
                        if(pin.Length!=6||packet.Length>64||peer.Address.AddressFamily!=AddressFamily.InterNetwork)continue;
                        if(Encoding.UTF8.GetString(packet)!=Prefix+pin)continue;
                        if(!RiichiClient.ValidEndpoint("http://"+peer.Address+":8765"))continue;
                        var reply=Encoding.UTF8.GetBytes(Prefix+pin+" OK");listener.Send(reply,reply.Length,peer);
                    }catch(SocketException){}catch(ObjectDisposedException){break;}
                });
            }catch(SocketException){listener?.Close();listener=null;}
#endif
        }
        public async Task<string> Find(string pin,CancellationToken cancel)
        {
            if(pin.Length!=6)return null;
            var targets=new HashSet<string>{"255.255.255.255","127.0.0.1"};
            try { foreach(var nic in NetworkInterface.GetAllNetworkInterfaces())foreach(var u in nic.GetIPProperties().UnicastAddresses) {
                if(u.Address.AddressFamily!=AddressFamily.InterNetwork||u.IPv4Mask==null)continue;
                var ip=u.Address.GetAddressBytes();var mask=u.IPv4Mask.GetAddressBytes();var b=new byte[4];
                for(int i=0;i<4;i++)b[i]=(byte)(ip[i]|~mask[i]);targets.Add(new IPAddress(b).ToString());
            }}catch(NetworkInformationException){}
            return await Task.Run(()=>{
                using(var socket=new UdpClient(0)){
                    socket.EnableBroadcast=true;socket.Client.ReceiveTimeout=160;
                    var query=Encoding.UTF8.GetBytes(Prefix+pin);var deadline=DateTime.UtcNow.AddSeconds(4);var next=DateTime.MinValue;
                    while(DateTime.UtcNow<deadline&&!cancel.IsCancellationRequested){
                        if(DateTime.UtcNow>=next){next=DateTime.UtcNow.AddMilliseconds(450);foreach(var target in targets)try{socket.Send(query,query.Length,new IPEndPoint(IPAddress.Parse(target),Port));}catch(SocketException){}}
                        try {
                            var peer=new IPEndPoint(IPAddress.Any,0);var bytes=socket.Receive(ref peer);
                            if(bytes.Length>64||Encoding.UTF8.GetString(bytes)!=Prefix+pin+" OK")continue;
                            string endpoint="http://"+peer.Address+":8765";if(RiichiClient.ValidEndpoint(endpoint))return endpoint;
                        }catch(SocketException){}
                    }
                    return null;
                }
            },cancel);
        }
        public void Dispose(){stopped=true;published="";listener?.Close();}
    }
}
