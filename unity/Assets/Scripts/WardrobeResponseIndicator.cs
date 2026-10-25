using UnityEngine;

namespace YunJian.Riichi {
    // Original DesktopMgr.ShowChiPengEffect follows lastqipai while the local
    // operation panel is open. It is the viewer's outfit, not the discarder’s.
    public sealed class WardrobeResponseIndicator:MonoBehaviour {
        TableRenderer table;NativeParticleEffect effect;string key;float started;
        public NativeParticleEffect Effect=>effect;
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
            if(target==null){if(effect!=null)effect.gameObject.SetActive(false);return;}
            var choice=WardrobeRuntime.Item(view,view.you,"callout");
            string next;
#if UNITY_EDITOR
            if(inspecting)next="inspection:"+InspectionKey+":"+choice.id;else
#endif
            next=view.audioRoom+":"+view.handSeq+":"+view.pending.from+":"+view.players[view.pending.from].river.Length+":"+choice.id;
            if(next!=key){Clear();key=next;started=Time.unscaledTime;var go=new GameObject("Equipped callable river tile indicator");go.transform.SetParent(transform,false);
                // World-space particle systems cache their birth transform. Bind
                // position, scale and orientation before instantiating the prefab.
                go.transform.position=target.position+Vector3.up*table.Geometry.Depth*.5f;go.transform.rotation=target.rotation*NativeParticleEffect.SurfaceMount(choice.nativeEffect);go.transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;
                effect=go.AddComponent<NativeParticleEffect>();effect.Initialize(choice.nativeEffect,507,false,true);
                foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>()){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;}
                go.transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;
            }
            effect.gameObject.SetActive(true);effect.transform.position=target.position+Vector3.up*table.Geometry.Depth*.5f;effect.transform.rotation=target.rotation*effect.SurfaceMountRotation;
            effect.Sample(ValidationFreeze?ValidationAge:Time.unscaledTime-started);
        }
        public void Clear(){if(effect!=null){effect.gameObject.SetActive(false);Destroy(effect.gameObject);}effect=null;key=null;}
        void OnDisable(){Clear();}
    }
}
