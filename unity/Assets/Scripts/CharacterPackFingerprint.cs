using System;
using System.IO;
using UnityEngine;
namespace YunJian.Riichi {
    public static class CharacterPackFingerprint {
        [Serializable] sealed class Stamp {public int schema=1;public string sha256,ticks;public long bytes;}
        static string StampPath(string path)=>path+".verified.json";
        public static bool Current(CharacterPackRecord pack,string path){
            try{
                var file=new FileInfo(path);string receipt=path+".ok";if(!file.Exists||file.Length!=pack.bytes||!File.Exists(receipt)||File.ReadAllText(receipt)!=pack.sha256)return false;
                string stampPath=StampPath(path);
                if(File.Exists(stampPath)){var stamp=JsonUtility.FromJson<Stamp>(File.ReadAllText(stampPath));return stamp!=null&&stamp.schema==1&&stamp.sha256==pack.sha256&&stamp.bytes==file.Length&&stamp.ticks==file.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);}
                // Old receipts were written after SHA and native bundle validation.
                if(File.GetLastWriteTimeUtc(receipt)<file.LastWriteTimeUtc)return false;
                Remember(pack,path);return true;
            }catch(Exception){return false;}
        }
        public static void Remember(CharacterPackRecord pack,string path){
            try{var file=new FileInfo(path);if(!file.Exists||file.Length!=pack.bytes)return;string final=StampPath(path),temp=final+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(new Stamp{sha256=pack.sha256,bytes=file.Length,ticks=file.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)}));if(File.Exists(final))File.Delete(final);File.Move(temp,final);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
}
