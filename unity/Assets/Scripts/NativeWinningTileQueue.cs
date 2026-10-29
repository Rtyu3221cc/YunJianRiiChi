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
        public void Bind(Transform tile){
            if(tile==target)return;Restore();ClearCopies();target=tile;
            if(tile==null)return;var model=tile.Find("Original tile");if(model==null)return;
            root=new GameObject("Lua winning tile queue 3090").transform;root.SetParent(transform,false);
            foreach(var source in model.GetComponentsInChildren<MeshRenderer>(true)){
                var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;
                var part=new GameObject(source.name).transform;part.gameObject.layer=NativeEffectCamera.Layer;part.SetParent(root,false);part.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var renderer=part.gameObject.AddComponent<MeshRenderer>();var copies=new List<Material>();
                foreach(var material in source.sharedMaterials){var copy=new Material(material){renderQueue=3090};materials.Add(copy);copies.Add(copy);}
                renderer.sharedMaterials=copies.ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);renderer.SetPropertyBlock(block);
                originals.Add(source);enabledBefore.Add(source.enabled);clones.Add(renderer);
            }
        }
        public void Sample(float age){
            bool active=age>=0&&age<5.3f&&root!=null; // 3300ms RonDelay + 2s, from item view sargs.
            Active=active;if(root==null)return;root.gameObject.SetActive(active);
            var scale=transform.lossyScale;root.position=Vector3.zero;root.rotation=Quaternion.identity;root.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            for(int i=0;i<originals.Count;i++){
                var source=originals[i];if(source==null)continue;
                var part=clones[i].transform;part.position=source.transform.position/TableTileGeometry.SourceUnits;part.rotation=source.transform.rotation;part.localScale=source.transform.lossyScale/TableTileGeometry.SourceUnits;
                clones[i].enabled=enabledBefore[i];source.enabled=active?false:enabledBefore[i];
            }
        }
        void Restore(){for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].enabled=enabledBefore[i];Active=false;}
        void ClearCopies(){if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);}root=null;foreach(var material in materials)Destroy(material);materials.Clear();originals.Clear();enabledBefore.Clear();clones.Clear();}
        void OnDestroy(){Restore();ClearCopies();}
    }
}
