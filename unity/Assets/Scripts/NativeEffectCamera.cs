using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // Keep source particle simulation in its authored meter scale. A uniformly
    // scaled camera gives identical screen/depth coordinates in the table scene.
    public sealed class NativeEffectCamera:MonoBehaviour {
        public const int Layer=26;
        readonly HashSet<NativeParticleEffect> effects=new HashSet<NativeParticleEffect>();
        TableRenderer table;Camera view;
        public static NativeEffectCamera Ensure(TableRenderer table){var value=table.GetComponent<NativeEffectCamera>();if(value==null)value=table.gameObject.AddComponent<NativeEffectCamera>();value.table=table;return value;}
        public void Register(NativeParticleEffect effect){effects.Add(effect);Sync();}
        public void Remove(NativeParticleEffect effect){effects.Remove(effect);}
        void LateUpdate(){Sync();}
        void Sync(){
            if(table==null||table.Camera==null)return;
            if(view==null){view=new GameObject("Authored-scale native effects camera").AddComponent<Camera>();view.transform.SetParent(transform,false);}
            var source=table.Camera;float scale=TableTileGeometry.SourceUnits;
            source.cullingMask&=~(1<<Layer);table.HandCamera.cullingMask&=~(1<<Layer);
            view.CopyFrom(source);view.cullingMask=1<<Layer;view.clearFlags=CameraClearFlags.Nothing;view.depth=source.depth+.25f;
            view.transform.position=source.transform.position/scale;view.transform.rotation=source.transform.rotation;
            view.nearClipPlane=source.nearClipPlane/scale;view.farClipPlane=source.farClipPlane/scale;view.orthographicSize=source.orthographicSize/scale;view.ResetProjectionMatrix();
            view.enabled=source.enabled&&effects.Any(e=>e!=null&&e.isActiveAndEnabled&&e.gameObject.activeInHierarchy);
        }
        public void RenderTo(RenderTexture texture){Sync();if(view==null||!view.enabled)return;var previous=view.targetTexture;view.targetTexture=texture;view.Render();view.targetTexture=previous;}
        void OnDestroy(){if(view!=null)Destroy(view.gameObject);}
    }
}
