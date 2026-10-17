using UnityEngine;

namespace YunJian.Riichi {
    // Rendering comes from the APK's complete 6/8-emitter particle graphs.
    // The offline host supplies only this viewer's scoring-derived hint level.
    public sealed class YakumanAnnouncement {
        GameObject root;
        Camera camera;
        NativeParticleEffect effect;
        string handKey;
        int shownLevel,level;
        float began;
        bool running,cuePending;
        public bool Freeze;
        public float FrozenAge;
        public float Age=>Freeze?FrozenAge:Time.unscaledTime-began;
        public bool Active=>running&&Age>=0&&Age<1.8f;
        public int Level=>level;
        public int LayerCount=>effect==null?0:effect.LayerCount;
        public int ParticleCount=>effect==null?0:effect.ParticleCount;
        public void Observe(RoomView view){
            if(view==null||view.phase=="empty"||view.phase=="join"||view.phase=="lobby"){Reset();return;}
            string next=view.audioRoom+":"+view.matchSeq+":"+view.handSeq;
            if(next!=handKey){Reset();handKey=next;}
            if(view.phase!="play"&&view.phase!="reaction"){running=false;Dispose();return;}
            int hint=Mathf.Clamp(view.yakumanIndicator,0,2);
            if(hint<=shownLevel)return;
            shownLevel=level=hint;Dispose();began=Time.unscaledTime+.25f;running=true;cuePending=level==2;
        }
        public void Tick(bool visible,GameAudio audio){
            if(!running)return;
            if(Age>=1.8f){Dispose();running=false;return;}
            if(!visible||!Active){if(camera!=null)camera.enabled=false;return;}
            if(cuePending){audio?.Cue("effect_yakumanready");cuePending=false;}
            if(root==null){
                root=new GameObject("APK yakuman announcement");root.transform.position=new Vector3(9000,9000,0);
                var particles=new GameObject("Original APK particle graph");particles.transform.SetParent(root.transform,false);effect=particles.AddComponent<NativeParticleEffect>();
                effect.Initialize(level==2?"effect_ui_yimanqueding_chs":"effect_ui_yimanjihui_chs",507,true);
                foreach(var node in root.GetComponentsInChildren<Transform>())node.gameObject.layer=28;
                var eye=new GameObject("Yakuman overlay camera");eye.transform.SetParent(root.transform,false);eye.transform.localPosition=new Vector3(0,0,-50);camera=eye.AddComponent<Camera>();
                // Composite original additive and alpha particles directly over
                // the table, preserving their blend modes without opaque quads.
                camera.clearFlags=CameraClearFlags.Depth;camera.depth=100;camera.orthographic=true;camera.cullingMask=1<<28;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            }
            var viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,true);
            float pixels=40*viewport.Scale;
            camera.orthographicSize=Screen.height/(2*pixels);camera.aspect=Screen.width/(float)Screen.height;camera.enabled=true;
            var target=viewport.Origin+new Vector2(viewport.Width/2,viewport.Height-125)*viewport.Scale;
            effect.transform.localScale=Vector3.one*.85f;
            effect.transform.localPosition=new Vector3((target.x-Screen.width/2f)/pixels,(Screen.height/2f-target.y)/pixels+(level==1?.66f*.85f:0),0);
            effect.Sample(Age);
        }
        void Dispose(){if(root!=null){root.SetActive(false);Object.Destroy(root);}root=null;effect=null;camera=null;}
        public void Reset(){Dispose();handKey=null;shownLevel=level=0;running=cuePending=false;Freeze=false;}
    }
}
