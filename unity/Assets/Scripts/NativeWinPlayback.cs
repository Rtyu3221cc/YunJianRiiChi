using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace YunJian.Riichi {
    [Serializable] public sealed class NativeWinContract {
        public int sourceId,tileQueueMode;public string sourceKey,auxiliary;public bool newStyle,riichi;
        public float lifetime=6,resultDelay,queueBegin,queueEnd,auxiliaryAt=.6f,auxiliaryLifetime=2;
    }
    // DesktopMgr.ShowHuleEffect / setNewHuleEffect / setOldHuleEffect. Source
    // scene origin and tile origin remain independent, in authored units.
    public sealed class NativeWinPlayback:MonoBehaviour {
        NativeParticleEffect native;NativeWinContract plan;Transform root,space,target,move,rot,ani,aniTsumo,stationary,fly,flyPosition,flyMove,legacyPai,riichiMaque;
        TableRenderer table;PublicSound sound;NativeWinningTileQueue queue;NativeWinTileModel rig;NativeParticleEffect extra;GameObject extraRoot;float rigEnd=1.9f;int relative;
        bool uprightRiichi,riichiPoseCaptured;
        Vector3 riichiStart,riichiLanding;
        Quaternion riichiRotation;
        Transform[] paintFootprints;
        public bool SceneOrigin=>plan.newStyle;
        public void SetRiichiAction(bool upright,bool hasSeatVariants){
            uprightRiichi=upright;
            if(queue!=null)queue.End=hasSeatVariants?3:1;
        }
        public NativeWinContract Contract=>plan;
        public float SourceAge(float age)=>!plan.newStyle&&plan.sourceKey=="ron_llx"?Mathf.Max(0,age-2):Mathf.Max(0,age);
        public void Initialize(NativeParticleEffect owner,Transform authored,Transform container,NativeWinContract contract){
            native=owner;root=authored;space=container;plan=contract;table=GetComponentInParent<TableRenderer>();
            move=root.Find("root_move");rot=root.Find("root_rot");ani=root.Find("root_ani");aniTsumo=root.Find("root_ani_zimo");stationary=root.Find("root_static");fly=root.Find("root_fly");flyMove=fly?.Find("move");flyPosition=fly?.Find("positon");legacyPai=root.Find("pai_container/pai");
            if(plan.riichi)riichiMaque=root.Find("root_maque");
            if(plan.sourceKey=="ron_26shuiyi"&&move!=null)
                paintFootprints=move.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="d1"||t.name=="d2"||t.name=="d3").ToArray();
            if(plan.queueEnd>plan.queueBegin){queue=gameObject.AddComponent<NativeWinningTileQueue>();queue.Begin=plan.queueBegin;queue.End=plan.queueEnd;queue.DepthTest=plan.tileQueueMode==2?8:4;queue.LegacyQueues=plan.tileQueueMode==3;}
        }
        public void Bind(Transform tile,PublicSound item){
            bool first=target==null;
            native.SynchronizeSourceSpace();
            sound=item;if(table!=null&&item!=null)relative=table.RelativeSeat(item.kind=="ron"?item.from:item.seat);
            target=tile;queue?.Bind(tile);if(tile==null||sound==null)return;
            if(plan.riichi&&!riichiPoseCaptured){
                riichiPoseCaptured=true;
                riichiStart=SourcePlaybackContracts.TileModelOrigin(tile)/TableTileGeometry.SourceUnits;
                riichiRotation=tile.rotation;
                var finalTile=table?.FindDiscard(item.seat,item.tile,item.riverIndex);
                riichiLanding=SourcePlaybackContracts.TileModelOrigin(finalTile!=null?finalTile:tile)/TableTileGeometry.SourceUnits;
            }
            if(first&&riichiMaque!=null){
                var localization=riichiMaque.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="localization");
                if(localization!=null){var preferred=localization.Find("chs")??localization.Find("common");foreach(Transform child in localization)child.gameObject.SetActive(child==preferred);}
            }
            PositionNewRoots();
            Transform anchor=null;
            if(plan.newStyle){var owner=sound.kind=="tsumo"&&aniTsumo!=null?aniTsumo:ani;var motion=root.GetComponentInParent<NativeEffectMotion>();if(owner!=null&&owner.childCount>0&&motion!=null){anchor=owner.GetChild(0);rigEnd=motion.DurationFor(owner);}}
            else if(legacyPai!=null&&(sound.kind=="tsumo"||plan.sourceKey=="ron_kuangdu")){anchor=legacyPai;rigEnd=1.9f;}
            if(anchor!=null){if(rig==null){rig=gameObject.AddComponent<NativeWinTileModel>();rig.Initialize(anchor,plan.tileQueueMode,plan.queueBegin,plan.queueEnd);}rig.Bind(tile);}
        }
        void PositionNewRoots(){
            if(!plan.newStyle)return;
            Vector3 point=transform.position/TableTileGeometry.SourceUnits;
            if(paintFootprints!=null&&target!=null)point=SourcePlaybackContracts.TileModelOrigin(target)/TableTileGeometry.SourceUnits;
            Quaternion rotation=target!=null?target.rotation:transform.rotation;
            if(plan.riichi&&uprightRiichi&&riichiPoseCaptured){point=riichiStart;rotation=riichiRotation;}
            if(move!=null)move.position=point;
            if(rot!=null){
                rot.position=point;
                bool reverse=plan.riichi&&(plan.sourceKey=="effect_liqi_23wansheng"||plan.sourceKey=="effect_liqi_23winter"||plan.sourceKey=="effect_liqi_25dongri");
                rot.rotation=rotation*(reverse?Quaternion.Euler(0,180,0):Quaternion.identity)*SourcePlaybackContracts.NativeTileMount;
            }
            if(ani!=null)ani.position=point;
            if(aniTsumo!=null)aniTsumo.position=point;
            if(stationary!=null)stationary.position=Vector3.zero;
            if(riichiMaque!=null&&riichiPoseCaptured)riichiMaque.position=riichiLanding;
            if(paintFootprints!=null){
                // The source uses the model center. The paint quads extend just
                // below it, but the port's table cloth is raised to world Y=0.
                // Rebase only those contact layers to the visible cloth surface.
                for(int i=0;i<paintFootprints.Length;i++){
                    var p=paintFootprints[i].position;
                    p.y=Mathf.Max(p.y,(.004f+i*.001f)/TableTileGeometry.SourceUnits);
                    paintFootprints[i].position=p;
                }
            }
        }
        public void AfterPose(float age){
            bool visible=age>=0;space.gameObject.SetActive(visible);if(!visible){queue?.Sample(-1);rig?.Sample(-1);return;}
            Vector3 point=transform.position/TableTileGeometry.SourceUnits;
            if(plan.newStyle){
                PositionNewRoots();
                if(plan.sourceId==308046&&flyMove!=null&&table!=null){
                    // Tools.ConvertCameraToWorld(camera, camera-local offset, root_fly).
                    var camera=table.Camera.transform;var end=camera.TransformPoint(new Vector3(.04959141f,-.04958072f,.2411938f)*TableTileGeometry.SourceUnits)/TableTileGeometry.SourceUnits;
                    float t=Mathf.Clamp01((age-50f/60)/(15f/60));t=1-(1-t)*(1-t);flyMove.position=Vector3.Lerp(point,end,t);if(flyPosition!=null)flyPosition.position=end;
                }
                if(plan.sourceId==30520009&&stationary!=null&&target!=null&&table!=null){
                    var position=stationary.Find("47/posi");if(position!=null){
                        var pixel=table.Camera.WorldToScreenPoint(target.position);float uiScale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);
                        float x=(pixel.x-Screen.width*.5f)/uiScale,y=(pixel.y-Screen.height*.5f)/uiScale+50;
                        float horizontal=Mathf.Clamp01((Mathf.Abs(x)-200)/400),vertical=Mathf.Clamp01((Mathf.Abs(y)-200)/200);
                        var offset=new Vector3(x>0?-.1f:.1f,0,0)*horizontal+(y>0?new Vector3(0,.05f,-.047f):new Vector3(0,-.014f,.013f))*vertical;
                        position.position=space.rotation*offset;
                    }
                }
            }else{
                if(plan.sourceKey=="ron_hl")root.localRotation=Quaternion.Euler(0,relative*90,0);
                if(plan.sourceKey=="ron_saki"){var p=root.Find("pai_container");if(p!=null)p.localRotation=Quaternion.Euler(0,new[]{0,-90,180,90}[relative%4],0);}
                if(plan.sourceKey=="ron_kuangdu"){var p=root.Find("pai_container");if(p!=null)p.localRotation=Quaternion.Euler(0,(sound?.kind=="tsumo"?new[]{0,-90,180,90}:new[]{-90,180,90,0})[relative%4]-(sound?.kind=="tsumo"?0:target?.localEulerAngles.y??0),0);}
                if(plan.sourceKey=="ron_24chunjie"&&target!=null){var p=root.Find("guang_alpha/guangkuang");if(p!=null){p.position=target.position/TableTileGeometry.SourceUnits+target.rotation*new Vector3(0,-.0012f,.0085f);p.rotation=target.rotation;}}
            }
            if(!string.IsNullOrEmpty(plan.auxiliary)&&age>=plan.auxiliaryAt&&age<plan.auxiliaryAt+plan.auxiliaryLifetime){
                if(extraRoot==null){extraRoot=new GameObject("Lua auxiliary "+plan.sourceKey);extraRoot.transform.SetParent(transform,false);extraRoot.transform.position=Vector3.zero;extraRoot.transform.rotation=transform.rotation;extraRoot.transform.localScale=Vector3.one;extra=extraRoot.AddComponent<NativeParticleEffect>();extra.Initialize(plan.auxiliary,507);}
                extra.Sample(age-plan.auxiliaryAt);
            }else if(extraRoot!=null){extraRoot.SetActive(false);Destroy(extraRoot);extraRoot=null;extra=null;}
            bool animate=rig!=null&&age<rigEnd;if(animate){queue?.Sample(-1);rig.Sample(age);}else{rig?.Sample(-1);queue?.Sample(age);}
        }
        void OnDestroy(){if(extraRoot!=null)Destroy(extraRoot);}
    }
    // The real tile shape is copied; authored-node delta animates it independently
    // of the transformed game mesh basis. A poll only replaces the hidden source.
    public sealed class NativeWinTileModel:MonoBehaviour {
        Transform anchor,holder,target;Matrix4x4 bindPose;int mode;float queueStart,queueEnd;bool hidesSource;
        readonly List<MeshRenderer> originals=new List<MeshRenderer>();readonly List<bool> enabledBefore=new List<bool>();readonly List<Material> copies=new List<Material>();readonly List<int> queues=new List<int>();
        public void Initialize(Transform node,int queueMode,float begin,float end){anchor=node;bindPose=node.localToWorldMatrix;if(Mathf.Abs(bindPose.determinant)<1e-20f){var scale=node.lossyScale;for(int axis=0;axis<3;axis++)if(Mathf.Abs(scale[axis])<1e-6f)scale[axis]=1;bindPose=Matrix4x4.TRS(node.position,node.rotation,scale);}mode=queueMode;queueStart=begin;queueEnd=end;}
        public void Bind(Transform tile){
            if(tile==target)return;Restore();Clear();target=tile;if(tile==null)return;var model=tile.Find("Original tile");if(model==null)return;
            holder=new GameObject("Original winning tile in authored animation").transform;holder.SetParent(anchor,false);
            foreach(var source in model.GetComponentsInChildren<MeshRenderer>(true)){
                var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;
                var part=new GameObject(source.name).transform;part.SetParent(holder,false);part.gameObject.layer=NativeEffectCamera.Layer;
                Matrix4x4 world=Matrix4x4.TRS(source.transform.position/TableTileGeometry.SourceUnits,source.transform.rotation,source.transform.lossyScale/TableTileGeometry.SourceUnits);var local=bindPose.inverse*world;part.localPosition=local.GetColumn(3);part.localRotation=local.rotation;part.localScale=local.lossyScale;
                part.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var renderer=part.gameObject.AddComponent<MeshRenderer>();var materials=new List<Material>();
                foreach(var material in source.sharedMaterials){var clone=new Material(material);materials.Add(clone);copies.Add(clone);queues.Add(material.renderQueue);}renderer.sharedMaterials=materials.ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);renderer.SetPropertyBlock(block);originals.Add(source);enabledBefore.Add(source.enabled);
            }
            holder.gameObject.SetActive(false);
        }
        public void Sample(float age){if(holder==null)return;bool active=age>=0;holder.gameObject.SetActive(active);if(!active){Restore();return;}hidesSource=true;for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].enabled=false;bool queue=age>=queueStart&&age<queueEnd&&mode>0;for(int i=0;i<copies.Count;i++){copies[i].renderQueue=queue?mode==3?3006:3090:queues[i];if(copies[i].HasProperty("_ZTest"))copies[i].SetFloat("_ZTest",queue&&mode==2?8:4);}}
        void Restore(){if(!hidesSource)return;for(int i=0;i<originals.Count;i++)if(originals[i]!=null)originals[i].enabled=enabledBefore[i];hidesSource=false;}
        void Clear(){if(holder!=null){holder.gameObject.SetActive(false);Destroy(holder.gameObject);}holder=null;foreach(var m in copies)Destroy(m);copies.Clear();queues.Clear();originals.Clear();enabledBefore.Clear();}
        void OnDisable(){Restore();if(holder!=null)holder.gameObject.SetActive(false);}
        void OnDestroy(){Restore();Clear();}
    }
}
