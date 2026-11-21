using System.Globalization;
using UnityEngine;

namespace YunJian.Riichi {
    // One caption belongs to one successfully started voice, on the audio clock.
    public sealed class CharacterVoiceCaption {
        public string Text{get;private set;}="";
        public float StartedAt{get;private set;}
        public float EndsAt{get;private set;}
        int[] elements=System.Array.Empty<int>();
        float rate;
        public void Clear(){Text="";elements=System.Array.Empty<int>();StartedAt=EndsAt=0;}
        public void Begin(string template,string nickname,float now,float duration){
            Clear();if(string.IsNullOrWhiteSpace(template)||duration<=0)return;
            Text=template.Replace("{name}",string.IsNullOrWhiteSpace(nickname)?"旅人":nickname);
            elements=StringInfo.ParseCombiningCharacters(Text);StartedAt=now;EndsAt=now+duration;
            // The reference reveals about 20 characters/s. Short clips still
            // finish revealing their whole line before the audio ends.
            rate=Mathf.Max(20,elements.Length/Mathf.Max(.05f,duration-.35f));
        }
        public string Revealed(float now){
            if(Text.Length==0||now<StartedAt||now>=EndsAt)return "";
            int count=Mathf.Clamp(Mathf.FloorToInt((now-StartedAt)*rate),0,elements.Length);
            return count==elements.Length?Text:Text.Substring(0,elements[count]);
        }
        public float Opacity(float now)=>Text.Length==0?0:Mathf.Clamp01((now-StartedAt)/.12f)*Mathf.Clamp01((EndsAt-now)/.15f);
    }
}
