using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi {
    // DesktopMgr.SetPaopaiZtestNew(..., queue=3090, mode=center). The native
    // effects camera works in source units, so the exact tile mesh is rendered
    // in that same pass while its ordinary table renderer is temporarily hidden.
    public sealed class NativeWinningTileQueue:MonoBehaviour {
        Transform target,root;readonly List<MeshRenderer> originals=new List<MeshRenderer>(),clones=new List<MeshRenderer>();
        readonly List<bool> enabledBefore=new List<bool>();readonly List<Material> materials=new List<Material>();
        public bool Active {get;private set;}
        public int Queue=>3090;
        public float Begin,End=5.3f;public int DepthTest=4;public bool LegacyQueues;
        public void Bind(Transform tile){
            if(tile==target)return;Restore();ClearCopies();target=tile;
            if(tile==null)return;var model=tile.Find("Original tile");if(model==null)return;
            root=new GameObject("Lua winning tile queue 3090").transform;root.SetParent(transform,false);
            foreach(var source in model.GetComponentsInChildren<MeshRenderer>(true)){
                var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;
                var part=new GameObject(source.name).transform;part.gameObject.layer=NativeEffectCamera.Layer;part.SetParent(root,false);part.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var renderer=part.gameObject.AddComponent<MeshRenderer>();var copies=new List<Material>();
                foreach(var material in source.sharedMaterials){int q=LegacyQueues?(source.name.Contains("outline")?3004:copies.Count==0?3005:3006):3090;var copy=new Material(material){renderQueue=q};if(copy.HasProperty("_ZTest"))copy.SetFloat("_ZTest",DepthTest);materials.Add(copy);copies.Add(copy);}
                renderer.sharedMaterials=copies.ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);renderer.SetPropertyBlock(block);
                originals.Add(source);enabledBefore.Add(source.enabled);clones.Add(renderer);
            }
            root.gameObject.SetActive(false);
        }
        public void Sample(float age){
            bool active=age>=Begin&&age<End&&root!=null;
            bool wasActive=Active;Active=active;if(root==null)return;root.gameObject.SetActive(active);
            if(!active&&!wasActive)return;
            var scale=transform.lossyScale;root.position=Vector3.zero;root.rotation=Quaternion.identity;root.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            for(int i=0;i<originals.Count;i++){
                var source=originals[i];if(source==null)continue;
                var part=clones[i].transform;part.position=source.transform.position/TableTileGeometry.SourceUnits;part.rotation=source.transform.rotation;part.localScale=source.transform.lossyScale/TableTileGeometry.SourceUnits;
                clones[i].enabled=enabledBefore[i];source.enabled=active?false:enabledBefore[i];
            }
        }
        void Restore(){if(!Active)return;for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].enabled=enabledBefore[i];Active=false;}
        void ClearCopies(){if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);}root=null;foreach(var material in materials)Destroy(material);materials.Clear();originals.Clear();enabledBefore.Clear();clones.Clear();}
        void OnDisable(){Restore();if(root!=null)root.gameObject.SetActive(false);}
        void OnDestroy(){Restore();ClearCopies();}
    }
}
