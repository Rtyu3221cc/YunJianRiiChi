using UnityEngine;
namespace YunJian.Riichi
{
    // Hands are laid down quickly (ease-out, no anticipation or bounce).
    // Re-measured 2026-11-08 at 60 fps in the exhaustive draw recording: the own
    // noten row starts to lean at 1.37s, is flat at 1.43s and settles by 1.47s,
    // about 0.10s in all. The former 0.05s ended before the push-down sound did.
    // See docs/research-2026-11-07-presets-and-tile-motion.md.
    public static class HandRevealMotion
    {
        public const float Duration=.10f;
        // Exhaustive draw lays hands down seat by seat, relative to the viewer.
        public const float SideDelay=.35f,AcrossDelay=.40f;
        // The viewer's own hand first moves from the hand camera onto the table,
        // standing, then acts after the others: tenpai and nine terminals push
        // down face up, noten tips face down (exhaustive draw and nine
        // terminals recordings: switch at 0, action at +0.55s).
        public const float SelfDelay=.55f;
        public static float Progress(float seconds)
        {
            if(seconds<=0)return 0;if(seconds>=Duration)return 1;
            float t=seconds/Duration;return 1-(1-t)*(1-t);
        }
        // relative: 0 self, 1 right, n-1 left, otherwise across.
        public static float SeatDelay(int relative,int players)=>relative==0?0:relative==1||relative==players-1?SideDelay:AcrossDelay;
    }
}
