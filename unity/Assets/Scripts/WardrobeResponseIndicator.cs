using UnityEngine;

namespace YunJian.Riichi {
    // Original DesktopMgr.ShowChiPengEffect follows lastqipai while the local
    // operation panel is open. It is the viewer's outfit, not the discarder’s.
    public sealed class WardrobeResponseIndicator:MonoBehaviour {
        TableRenderer table;NativeParticleEffect effect;WinTileEffect sourceEffect;string key;float started;
        public NativeParticleEffect Effect=>effect;
        public WinTileEffect SourceEffect=>sourceEffect;
        public Transform EffectRoot=>effect!=null?effect.transform:sourceEffect!=null?sourceEffect.transform:null;
        public int ParticleCount=>effect!=null?effect.ParticleCount:sourceEffect!=null?sourceEffect.ParticleCount:0;
        public Transform Target {
            get {
#if UNITY_EDITOR
                if(InspectionMode)return InspectionTarget;
#endif
                return table.ResponseTarget;
            }
        }
#if UNITY_EDITOR
        // Only the editor may inspect a tile from its owner's side. This does
        // not create a legal response to one's own discard in the game.
        public bool InspectionMode;public Transform InspectionTarget;public string InspectionKey;
        public bool ValidationRawOverlay;
#endif
        public bool ValidationFreeze;public float ValidationAge;
        void Awake(){table=GetComponent<TableRenderer>();}
        void LateUpdate(){
            var view=table.Client.State;var target=Target;bool inspecting=false;
#if UNITY_EDITOR
            inspecting=InspectionMode;
#endif
            bool eligible=view!=null&&view.phase=="reaction"&&view.pending!=null&&view.pending.kind=="discard";
            if(inspecting)eligible=view!=null&&target!=null;
            if(!eligible){Clear();return;}
            if(target==null){if(EffectRoot!=null)EffectRoot.gameObject.SetActive(false);return;}
            var choice=WardrobeRuntime.Item(view,view.you,"callout");
            string next;
#if UNITY_EDITOR
            if(inspecting)next="inspection:"+InspectionKey+":"+choice.id;else
#endif
            next=view.audioRoom+":"+view.handSeq+":"+view.pending.from+":"+view.players[view.pending.from].river.Length+":"+choice.id;
            if(next!=key){Clear();key=next;started=Time.unscaledTime;var go=new GameObject("Equipped callable river tile indicator");go.transform.SetParent(transform,false);
                // World-space particle systems cache their birth transform. Bind
                // position, scale and orientation before instantiating the prefab.
                go.transform.position=target.position+Vector3.up*table.Geometry.Depth*.5f;go.transform.rotation=target.rotation;go.transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;
                if(!string.IsNullOrEmpty(choice.nativeEffect)){
                    go.transform.rotation*=NativeParticleEffect.SurfaceMount(choice.nativeEffect);effect=go.AddComponent<NativeParticleEffect>();effect.Initialize(choice.nativeEffect,507,false,true);
                    effect.SetOverlay(choice.id!="effect_zhishi");
                    foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>()){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;}
                    go.transform.localScale=Vector3.one;go.transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;
                }else{
                    sourceEffect=go.AddComponent<WinTileEffect>();sourceEffect.InitializeTable(choice.effect,go.transform.position,target.rotation,0,507,true,choice.effectScale>0?choice.effectScale:1);
                    sourceEffect.SetOverlay(true);
                }
            }
            var root=EffectRoot;root.gameObject.SetActive(true);root.position=target.position+Vector3.up*table.Geometry.Depth*.5f;root.rotation=target.rotation*(effect!=null?effect.SurfaceMountRotation:Quaternion.identity);
            ApplyReadability(choice,target);
            float age=ValidationFreeze?ValidationAge:Time.unscaledTime-started;if(effect!=null)effect.Sample(age);else sourceEffect.Sample(age);
        }
        void ApplyReadability(WardrobeItem choice,Transform target){
            bool readable=choice.id!="effect_zhishi";
#if UNITY_EDITOR
            readable&=!ValidationRawOverlay;
#endif
            float intensity=choice.calloutIntensity>0?choice.calloutIntensity:.85f,faceOpacity=choice.calloutFaceOpacity>0?choice.calloutFaceOpacity:.18f;
            if(effect!=null)effect.SetTileReadability(target,intensity,faceOpacity,readable);else sourceEffect.SetTileReadability(target,intensity,faceOpacity,readable);
        }
#if UNITY_EDITOR
        public void ValidationApplyReadability(){ApplyReadability(WardrobeRuntime.Item(table.Client.State,table.Client.State.you,"callout"),Target);}
#endif
        public void Clear(){if(EffectRoot!=null){EffectRoot.gameObject.SetActive(false);Destroy(EffectRoot.gameObject);}effect=null;sourceEffect=null;key=null;}
        void OnDisable(){Clear();}
    }
}
