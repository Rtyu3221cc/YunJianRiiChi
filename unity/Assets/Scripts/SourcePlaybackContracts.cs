using System;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
    [Serializable] public class SourceHandAction {public string type,action;public float speed,lifetime,cue,release;public int sourceId;}
    [Serializable] public class SourceEffectBinding {public string id,sourceName,controller,action;public int sourceId;public bool ground;}
    [Serializable] sealed class SourcePlaybackData {public SourceHandAction[] rows;public SourceEffectBinding[] bindings;}
    [Serializable] sealed class SourceTileFrameData {public float sourceWidth;public float[] mount;}
    public static class SourcePlaybackContracts {
        static SourcePlaybackData data;static SourceTileFrameData frame;
        static SourcePlaybackData Data=>data??(data=JsonUtility.FromJson<SourcePlaybackData>(Resources.Load<TextAsset>("Majsoul/Actions/playback-contracts").text));
        static SourceTileFrameData Frame=>frame??(frame=JsonUtility.FromJson<SourceTileFrameData>(Resources.Load<TextAsset>("Majsoul/Actions/source-tile-frame").text));
        public static SourceEffectBinding Binding(WardrobeItem item)=>Data.bindings.FirstOrDefault(b=>b.id==item.id);
        public static SourceHandAction Declaration(WardrobeItem hand,WardrobeItem effect){var binding=Binding(effect);if(binding==null||binding.controller!="legacy")throw new InvalidOperationException("Missing verified declaration binding: "+effect.id);var type=string.IsNullOrEmpty(hand.animationType)?"hand_human":hand.animationType;return Data.rows.First(r=>r.type==type&&r.action==binding.action);}
        public static Quaternion TileMount=>new Quaternion(Frame.mount[0],Frame.mount[1],Frame.mount[2],Frame.mount[3]);
        public static Quaternion NativeTileMount=>new Quaternion(0,.7071067811865475f,.7071067811865475f,0);
        public static float TileWidth=>Frame.sourceWidth;
    }
}
