using UnityEngine;
namespace YunJian.Riichi
{
    // Short anticipation, accelerating fall, table contact and a small settle-back.
    // Reference: exhaustive draw and own ron recordings; the row moves together.
    public static class HandRevealMotion
    {
        public const float Duration=.58f;
        static readonly AnimationCurve motion=new AnimationCurve(
            new Keyframe(0,0,0,0),new Keyframe(.08f,.015f,0,.3f),
            new Keyframe(.22f,.20f,2.6f,2.6f),new Keyframe(.36f,1.04f,0,0),
            new Keyframe(.44f,.97f,0,0),new Keyframe(Duration,1,0,0));
        public static float Progress(float seconds)=>seconds<=0?0:seconds>=Duration?1:motion.Evaluate(seconds);
    }
}
