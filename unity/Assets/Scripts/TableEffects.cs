using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Original source mesh and bone curves are baked at 30 Hz by the importer.
    // Public committed events drive presentation; no opponent hand is inspected.
    public sealed class TableEffects : MonoBehaviour
    {
        [Serializable] class HandData { public int vertices, frames, fps; public float duration, landTime; public float[] uv, tilePoints; public int[] body, nails; }
        // Every clip is baked from the same public mesh, UVs and triangle groups; only
        // the sampled frames, the carried-object path and the timing differ.
        sealed class Clip { public HandData data; public Vector3[][] vertices, normals; public Vector3[] frameVertices, frameNormals; }
        const string NormalClip="discard-hand", RiichiClip="riichi-hand", StickClip="lizhi-hand", MeldClip="fulu-hand", TsumoClip="tsumo-hand";
        // Majsoul ships no dedicated nuki clip (the hand set is exactly Dapai, LiQiPai,
        // Zimo, Fulu and LiZhi), so extracting North reuses the meld motion.
        static readonly string[] MeldKinds={"chi","pon","kan","nuki"};
        readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        // The hand and the call banner are independent channels: a declaration shout has
        // to run while the discard hand is still moving, not after it.
        readonly Queue<PublicSound> handEvents = new Queue<PublicSound>();
        readonly Queue<PublicSound> callEvents = new Queue<PublicSound>();
        readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        string room;
        int cursor;
        float last, started, callStarted;
        PublicSound current, call;
        TableRenderer table;
        Clip clip;
        Mesh mesh;
        Material body, nails;
        Transform motion, animatedTile, targetTile, targetStick, meldTarget, northTarget, impact;
        TableRenderer.MeldVisual movingMeld;Vector3 meldFinal;Quaternion meldRotation;
        bool GroupCall=>current!=null && (current.kind=="chi" || current.kind=="pon" || current.kind=="kan");
        readonly List<WinTileEffect> winEffects = new List<WinTileEffect>();
        readonly HashSet<string> winningBursts = new HashSet<string>();
        readonly HashSet<string> riichiCalls=new HashSet<string>();
        readonly WinCutIn winCutIn=new WinCutIn();
        readonly NativeCallouts nativeCallouts=new NativeCallouts();
        public WinCutIn CutIn=>winCutIn;
        public int SpawnedWinEffects { get; private set; }
        static bool IsWin(PublicSound e) => e!=null && (e.kind=="ron" || e.kind=="tsumo");
        public bool ShowWinPresentation => IsWin(current) || IsWin(call) || cutInWin!=null || handEvents.Any(IsWin) || callEvents.Any(IsWin) || winEffects.Count>0 || winCutIn.Active;
        public bool WinActionStarted=>IsWin(current) || call?.kind=="ron";
        readonly List<NorthPetals> northEffects = new List<NorthPetals>();
        bool handedOff,impactQueued;
        public int SpawnedNorthEffects { get; private set; }
        float impactStarted;
        NativeParticleEffect nativeImpact;
        RiichiDiscardVisual riichiVisual;
        bool riichiVisualAttempted;
        Camera overlay;
        public int PlayedEvents { get; private set; }
        public bool IsAnimating => current != null || call != null;
        public bool HasPendingActions => IsAnimating || handEvents.Count>0 || callEvents.Count>0;
        public bool AwaitingDiscard(int seat,int index)=>current?.kind=="discard"&&current.seat==seat&&current.riverIndex==index&&!handedOff||handEvents.Any(e=>e.kind=="discard"&&e.seat==seat&&e.riverIndex==index);
        public bool ValidationFreeze;
        public float ValidationTime;
        public int ActiveSeat => current == null ? -1 : current.seat;
        // A discard keeps the source keyframes but is retimed to 0.62 s; the declaration,
        // the stick placement and the meld motion play at their own baked length.
        bool PlainDiscard => current != null && current.kind == "discard" && !current.riichi;
        float ClipLength => current?.kind=="kan"?.45f:(PlainDiscard || clip == null || clip.data.duration <= 0) ? .62f : clip.data.duration;
        // The carried object stops moving when the real river tile / stick / meld takes over.
        float SourceHandoff => current?.kind=="kan"?.35f:(PlainDiscard || clip == null || clip.data.landTime <= 0) ? .40f : clip.data.landTime;
        float PlayLength => current?.kind=="tsumo"?1.16f:ClipLength;
        float Handoff => current?.kind=="tsumo"?1.14f:SourceHandoff;
        float RiichiEffectAt {
            get {var view=table.Client.State;var hand=WardrobeRuntime.Item(view,current.seat,"hand");var effect=WardrobeRuntime.Item(view,current.seat,"riichiEffect");float point=effect.groundEffect?hand.riichiNormalPoint:hand.riichiSidewaysPoint;return point>0?ClipLength*point:Mathf.Max(0,Handoff-.32f);}
        }
        static string ClipFor(PublicSound item)
        {
            if (item.kind == "discard") return item.riichi ? RiichiClip : NormalClip;
            if (item.kind == "riichi-stick") return StickClip;
            if (item.kind == "tsumo") return TsumoClip;
            if(item.kind=="nuki")return NormalClip;
            return MeldKinds.Contains(item.kind) ? MeldClip : null;
        }

        void Awake() { table = GetComponent<TableRenderer>();riichiVisual=gameObject.AddComponent<RiichiDiscardVisual>();gameObject.AddComponent<WardrobeResponseIndicator>(); }
        public Texture2D Art(string name)
        {
            if (!art.TryGetValue(name, out var image)) { image = Resources.Load<Texture2D>("Majsoul/Actions/"+name); art[name] = image; }
            return image;
        }
        public void Reset()
        {
            riichiVisual?.StopVisuals();
            Finish(); FinishCall(); EndImpact(); EndNorthEffects(); EndWinEffects(); winningBursts.Clear();riichiCalls.Clear();winCutIn.Reset();cutInWin=null; ClearQueuedHands(); callEvents.Clear(); room = null; cursor = 0;spokenEvent=-1; last = Time.unscaledTime;
        }
        public void Accept(RoomView view)
        {
            riichiVisual.Observe(view,room!=view.audioRoom||Time.unscaledTime-last>=8);
            if (string.IsNullOrEmpty(view.audioRoom)) { Reset(); return; }
            if (room != view.audioRoom) { Reset(); room = view.audioRoom; cursor = view.audioSeq; return; }
            bool stale = Time.unscaledTime-last >= 8; last = Time.unscaledTime;
            var fresh = (view.audioEvents ?? new PublicSound[0]).Where(e => e.id > cursor).OrderBy(e => e.id).ToArray();
            cursor = Math.Max(cursor, view.audioSeq);
            if (stale) { Finish(); FinishCall(); EndNorthEffects(); EndWinEffects(); ClearQueuedHands(); callEvents.Clear(); return; }
            if(view.phase!="result" && !ShowWinPresentation)winningBursts.Clear();
            // A newly received win is queued behind unfinished discard/declaration hands.
            foreach (var item in fresh)
            {
                if (item.seat < 0 || item.seat >= view.n) continue;
                if(IsWin(item) && string.IsNullOrEmpty(item.tile))
                {
                    var detail=view.result?.details?.FirstOrDefault(d=>d.seat==item.seat);
                    if(detail!=null){item.tile=detail.tile;item.from=detail.ron?(view.last?.from??detail.from):item.seat;item.winSource=detail.winSource;}
                }
                if (ClipFor(item) != null && (item.kind!="discard" || table.FindDiscard(item.seat,item.tile,item.riverIndex)!=null)) handEvents.Enqueue(item);
                string declaration=view.handSeq+":"+view.round+":"+view.honba+":"+item.seat;
                if(item.kind=="discard" && item.riichi && riichiCalls.Add(declaration))
                    callEvents.Enqueue(new PublicSound{id=item.id,seat=item.seat,kind=view.players[item.seat].riichi==2?"double-riichi":"riichi"});
                if (CallArt(item.kind) != null && (!(item.kind=="riichi"||item.kind=="double-riichi") || riichiCalls.Add(declaration))) callEvents.Enqueue(item);
            }
            while (handEvents.Count > 8) handEvents.Dequeue();
            while (callEvents.Count > 4) callEvents.Dequeue();
            // Re-rendering a state does not restart the event currently playing, but it
            // does rebuild the table, so the replaced object has to be hidden again.
            // Start and hide before rendering this accepted state, rather than waiting
            // for Update next frame. Queued discards stay hidden until their hand-off.
            StartQueued();
            if (current != null) HideTarget();
            foreach(var pending in handEvents)if(pending.kind=="discard"){
                var tile=table.FindDiscard(pending.seat,pending.tile,pending.riverIndex);if(tile!=null)tile.gameObject.SetActive(false);
            }
            foreach(var pending in handEvents.Where(e=>e.kind=="chi"||e.kind=="pon"||e.kind=="kan")){
                var meld=table.EventMeld(pending);if(meld!=null)(pending.meldKind=="kakan"?meld.extra:meld.group)?.gameObject.SetActive(false);
            }
            foreach(var pending in handEvents.Where(e=>e.kind=="riichi-stick")){
                var stick=table.StickTarget(pending.seat);if(stick!=null)stick.gameObject.SetActive(false);
            }
            foreach(var pending in handEvents.Where(e=>e.kind=="tsumo"))table.WinningTarget(pending)?.gameObject.SetActive(false);
            foreach(var pending in handEvents.Where(e=>e.kind=="nuki"))table.NorthTarget(pending.seat,pending.nuki)?.gameObject.SetActive(false);
        }
        void ClearQueuedHands()
        {
            foreach(var e in handEvents)if(e.kind=="discard"){
                var target=table.FindDiscard(e.seat,e.tile,e.riverIndex);if(target!=null)target.gameObject.SetActive(true);
            }
            foreach(var e in handEvents.Where(e=>e.kind=="riichi-stick"))table.StickTarget(e.seat)?.gameObject.SetActive(true);
            foreach(var e in handEvents.Where(e=>e.kind=="tsumo"))table.WinningTarget(e)?.gameObject.SetActive(true);
            foreach(var e in handEvents.Where(e=>e.kind=="nuki"))table.NorthTarget(e.seat,e.nuki)?.gameObject.SetActive(true);
            handEvents.Clear();
        }
        string handStyle;
        void LoadClip(string stem)
        {
            var choice=WardrobeRuntime.Item(table.Client.State,current.seat,"hand");
            if(current.kind=="discard"&&current.riichi&&WardrobeRuntime.Item(table.Client.State,current.seat,"riichiEffect").groundEffect)stem=NormalClip;
            stem=WardrobeRuntime.Motion(choice,stem);
            if (!clips.TryGetValue(stem, out clip)) {
            var data = JsonUtility.FromJson<HandData>(Resources.Load<TextAsset>(stem).text);
            byte[] bytes = Resources.Load<TextAsset>(stem+"-frames").bytes;
            var values = new float[bytes.Length/4]; Buffer.BlockCopy(bytes,0,values,0,bytes.Length);
            clip = new Clip { data = data, vertices = new Vector3[data.frames][], normals = new Vector3[data.frames][],
                              frameVertices = new Vector3[data.vertices], frameNormals = new Vector3[data.vertices] };
            for (int f=0;f<data.frames;f++)
            {
                clip.vertices[f] = new Vector3[data.vertices]; clip.normals[f] = new Vector3[data.vertices];
                for (int i=0;i<data.vertices;i++)
                {
                    int p=(f*2*data.vertices+i)*3, n=p+data.vertices*3;
                    clip.vertices[f][i]=new Vector3(values[p],values[p+1],values[p+2]);
                    clip.normals[f][i]=new Vector3(values[n],values[n+1],values[n+2]);
                }
            }
            clips[stem] = clip;
            }
            var surface=clip.data;
            if (mesh == null || handStyle!=choice.id)
            {
                if(mesh!=null)Destroy(mesh);if(body!=null)Destroy(body);if(nails!=null)Destroy(nails);handStyle=choice.id;
                var uv=new Vector2[surface.vertices]; for(int i=0;i<uv.Length;i++) uv[i]=new Vector2(surface.uv[i*2],1-surface.uv[i*2+1]);
                mesh=new Mesh { name="Original animated hand", vertices=clip.vertices[0], normals=clip.normals[0], uv=uv, subMeshCount=2 };
                mesh.SetTriangles(surface.body,0); mesh.SetTriangles(surface.nails,1); mesh.MarkDynamic();
                body=new Material(Shader.Find("Unlit/Texture")) { mainTexture=WardrobeCatalog.Texture(choice.texture) };
                nails=new Material(Shader.Find("Unlit/Texture")) { mainTexture=WardrobeCatalog.Texture(string.IsNullOrEmpty(choice.nailTexture)?choice.texture:choice.nailTexture) };
            }
        }
        int spokenEvent=-1;
        void StartQueued(){
            if(current!=null || call!=null || cutInWin!=null || winCutIn.Active || winEffects.Count>0 || table.ArrangingHand)return;
            int hand=handEvents.Count>0?handEvents.Peek().id:int.MaxValue,shout=callEvents.Count>0?callEvents.Peek().id:int.MaxValue,id=Math.Min(hand,shout);
            if(id==int.MaxValue)return;
            if(hand==id && shout==id && handEvents.Peek().kind=="tsumo"){
                var win=callEvents.Dequeue();BeginCall(win);
                if(win.id!=spokenEvent){spokenEvent=win.id;GetComponent<RoleVoice>()?.QueuePresented(room,win);}return;
            }
            PublicSound item=null;if(hand==id){item=handEvents.Dequeue();Begin(item);}if(shout==id){item=callEvents.Dequeue();BeginCall(item);}
            if(item!=null && item.id!=spokenEvent){spokenEvent=item.id;GetComponent<RoleVoice>()?.QueuePresented(room,item);}
        }
        void Begin(PublicSound item)
        {
            if(item.kind=="discard" && item.riverIndex>0 && table.FindDiscard(item.seat,item.tile,item.riverIndex)==null)return;
            current=item; started=Time.unscaledTime; handedOff=false;impactQueued=false;riichiVisualAttempted=false; PlayedEvents++;
            string stem=ClipFor(item); if (stem==null) return;
            LoadClip(stem);
            bool stick=item.kind=="riichi-stick";
            motion = new GameObject("Original hand seat "+item.seat).transform;
            if(overlay==null) {overlay=new GameObject("Discard hand overlay camera").AddComponent<Camera>();overlay.transform.SetParent(table.Camera.transform,false);}
            ConfigureOverlay();overlay.enabled=true;
            Vector3 anchor=item.kind=="tsumo" ? table.WinningPoint(item) : stick ? table.StickPoint(item.seat) : item.kind=="nuki" ? table.NorthPoint(item.seat,item.nuki) : MeldKinds.Contains(item.kind) ? table.MeldPoint(item.seat) : table.DiscardPoint(item.seat,item.tile,item.riverIndex);
            motion.position=anchor+Vector3.up*.12f;
            motion.rotation=Quaternion.Euler(0,table.SeatAngle(item.seat)+180,0);
            // Zimo lifts the whole original arm; fit its larger sweep to this table.
            motion.localScale=Vector3.one*(TableTileGeometry.SourceUnits*(item.kind=="tsumo"?.36f:.9f));
            // Fulu's node_tile is offset from its root; align its settle keyframe
            // with the event's North tile, including the inward-facing glyph.
            if (item.kind=="discard" || item.kind=="riichi-stick" || item.kind=="nuki" || item.kind=="tsumo") motion.position -= motion.TransformVector(TilePoint(SourceHandoff));
            var hand=new GameObject("Source hand mesh");hand.layer=14;hand.transform.SetParent(motion,false);
            hand.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=hand.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{body,nails};renderer.shadowCastingMode=ShadowCastingMode.Off;
            movingMeld=GroupCall?table.EventMeld(item):null;
            animatedTile = GroupCall?null:stick ? table.CreateEffectStick(item.seat)
                        : item.kind=="nuki" ? table.CreateEffectTile("z4",table.NorthTarget(item.seat,item.nuki))
                        : string.IsNullOrEmpty(item.tile) ? null : table.CreateEffectTile(item.tile,item.kind=="tsumo"?table.WinningTarget(item):table.FindDiscard(item.seat,item.tile,item.riverIndex));
            if (animatedTile != null)
            {
                // The carried self-draw tile shares table depth with its neighbours;
                // only the arm uses the overlay camera that clears scene depth.
                foreach(var child in animatedTile.GetComponentsInChildren<Transform>())child.gameObject.layer=item.kind=="tsumo"||item.kind=="discard"?0:14;
                foreach(var collider in animatedTile.GetComponentsInChildren<Collider>())collider.enabled=false;
            }
            mesh.vertices=clip.vertices[0];mesh.normals=clip.normals[0];mesh.RecalculateBounds();
            if(animatedTile!=null){
                PositionCarriedTile(TilePoint(0));
                animatedTile.rotation=item.kind=="nuki"?table.NorthTarget(item.seat,item.nuki).rotation:Quaternion.Euler(0,table.SeatAngle(item.seat)+(!stick&&item.riichi?90:0),0);
                if(item.kind=="tsumo"){
                    var target=table.WinningTarget(item);if(target!=null)animatedTile.rotation=target.rotation;
                }
            }
            if(movingMeld!=null){meldTarget=item.meldKind=="kakan"?movingMeld.extra:movingMeld.group;meldFinal=meldTarget.position;meldRotation=meldTarget.rotation;motion.position-=motion.TransformVector(TilePoint(SourceHandoff));}
            HideTarget();
        }
        void HideTarget()
        {
            float elapsed=ValidationFreeze?ValidationTime:Time.unscaledTime-started;
            if (current.kind=="riichi-stick") targetStick=table.StickTarget(current.seat);
            else if (current.kind=="tsumo") targetTile=table.WinningTarget(current);
            else if (current.kind=="nuki") northTarget=table.NorthTarget(current.seat,current.nuki);
            else if (GroupCall){
                var rebuilt=table.EventMeld(current);var target=rebuilt==null?table.MeldTarget(current.seat):current.meldKind=="kakan"?rebuilt.extra:rebuilt.group;
                if(target!=meldTarget){movingMeld=rebuilt;meldTarget=target;if(target!=null){meldFinal=target.position;meldRotation=target.rotation;}}
            }
            else targetTile=table.FindDiscard(current.seat,current.tile,current.riverIndex);
            if(elapsed>=Handoff) return;
            if (targetTile!=null) targetTile.gameObject.SetActive(false);
            if (targetStick!=null) targetStick.gameObject.SetActive(false);
            if (meldTarget!=null) meldTarget.gameObject.SetActive(false);
            if (northTarget!=null) northTarget.gameObject.SetActive(false);
        }
        void Finish()
        {
            if (targetTile!=null) targetTile.gameObject.SetActive(true);
            if (targetStick!=null) targetStick.gameObject.SetActive(true);
            if (meldTarget!=null) meldTarget.gameObject.SetActive(true);
            if(meldTarget!=null && movingMeld!=null){meldTarget.position=meldFinal;meldTarget.rotation=meldRotation;foreach(var t in meldTarget.GetComponentsInChildren<Transform>())t.gameObject.layer=0;}
            if (northTarget!=null) northTarget.gameObject.SetActive(true);
            if (motion!=null) { motion.gameObject.SetActive(false); Destroy(motion.gameObject); }
            if (animatedTile!=null) { animatedTile.gameObject.SetActive(false); Destroy(animatedTile.gameObject); }
            motion=null;animatedTile=null;targetTile=null;targetStick=null;meldTarget=null;northTarget=null;movingMeld=null;current=null;
            if(overlay!=null)overlay.enabled=false;
        }
        Vector3 TilePoint(float elapsed)
        {
            float frame=Mathf.Clamp01(elapsed/ClipLength)*(clip.data.frames-1);
            int a=(int)frame,b=Mathf.Min(a+1,clip.data.frames-1); float t=frame-a;
            return Vector3.Lerp(new Vector3(clip.data.tilePoints[a*3],clip.data.tilePoints[a*3+1],clip.data.tilePoints[a*3+2]),new Vector3(clip.data.tilePoints[b*3],clip.data.tilePoints[b*3+1],clip.data.tilePoints[b*3+2]),t);
        }
        void PositionCarriedTile(Vector3 point)
        {
            var position=motion.TransformPoint(point)-Vector3.up*.12f;
            if(current.kind=="tsumo"){
                // Zimo's original arm sweeps sideways. Let the tile rise and land
                // over its reserved slot instead of sweeping across the exposed row.
                var target=table.WinningTarget(current);
                if(target!=null){motion.position+=new Vector3(target.position.x-position.x,0,target.position.z-position.z);position.x=target.position.x;position.z=target.position.z;position.y=Mathf.Max(target.position.y,position.y);}
            }
            animatedTile.position=position;
        }
        void CompleteHandoff()
        {
            if (handedOff) return; handedOff=true;
            riichiVisual?.Settle(current);
            GetComponent<GameAudio>()?.Landing(current.kind);
            if (animatedTile!=null) animatedTile.gameObject.SetActive(false);
            if (targetTile!=null) targetTile.gameObject.SetActive(true);
            if (targetStick!=null) targetStick.gameObject.SetActive(true);
            if (meldTarget!=null) meldTarget.gameObject.SetActive(true);
            if(meldTarget!=null && movingMeld!=null){meldTarget.position=meldFinal;meldTarget.rotation=meldRotation;foreach(var t in meldTarget.GetComponentsInChildren<Transform>())t.gameObject.layer=0;}
            if (northTarget!=null) northTarget.gameObject.SetActive(true);
            if (current.kind=="nuki")
            {
                // The supplied sakura burst belongs to pointer clicks, not North.
            }
            else if (current.kind=="tsumo") SpawnWin(current,ValidationFreeze?Handoff:started+Handoff);
            else if (MeldKinds.Contains(current.kind) && !impactQueued){SpawnImpact();impactQueued=true;}
        }
        void SpawnWin(PublicSound item,float now)
        {
            if(string.IsNullOrEmpty(item.tile))return;
            string key=item.from+":"+item.winSource+":"+item.tile;
            if(!winningBursts.Add(key))return;
            var effect=new GameObject("Original winning tile effect "+key).AddComponent<WinTileEffect>();effect.transform.SetParent(transform,false);
            var choice=WardrobeRuntime.Item(table.Client.State,item.seat,"winEffect");
            effect.Initialize(table.WinningPoint(item),table.SeatAngle(item.kind=="ron"?item.from:item.seat),now+.10f,(uint)(item.id+1),choice.effect,choice.effectScale>0?choice.effectScale:1);effect.SoundCue=choice.cue;
            winEffects.Add(effect);SpawnedWinEffects++;
        }
        void EndWinEffects(){foreach(var e in winEffects)if(e!=null){e.gameObject.SetActive(false);Destroy(e.gameObject);}winEffects.Clear();}
        void UpdateWinEffects()
        {
            float now=ValidationFreeze?ValidationTime:Time.unscaledTime;
            for(int i=winEffects.Count-1;i>=0;i--){
                var e=winEffects[i];if(e==null){winEffects.RemoveAt(i);continue;}
                float age=now-e.StartedAt;
                if(age>=0&&!e.SoundTriggered){e.SoundTriggered=true;GetComponent<GameAudio>()?.Cue(e.SoundCue);}
                e.Sample(age);
                // Continuous emitters have a particle tail beyond the old 1.8 s
                // cutoff. Keep the table visible until that tail really ends.
                if(age>=e.PlaybackDuration&&e.ParticleCount==0){e.gameObject.SetActive(false);Destroy(e.gameObject);winEffects.RemoveAt(i);}
            }
        }
        void EndNorthEffects()
        {
            foreach(var petals in northEffects) if(petals!=null) {petals.gameObject.SetActive(false);Destroy(petals.gameObject);}
            northEffects.Clear();
        }
        void UpdateNorthEffects()
        {
            float now=ValidationFreeze?ValidationTime:Time.unscaledTime;
            for(int i=northEffects.Count-1;i>=0;i--)
            {
                var petals=northEffects[i]; float age=petals==null?NorthPetals.Lifetime:now-petals.StartedAt;
                if(age>=NorthPetals.Lifetime) {if(petals!=null){petals.gameObject.SetActive(false);Destroy(petals.gameObject);}northEffects.RemoveAt(i);}
                else petals.Sample(age);
            }
        }
        // A meld lands with the original additive impact flash; it outlives the hand.
        Vector3 MeldImpactPoint(){
            if(current!=null && movingMeld!=null){
                if(current.meldKind=="kakan" && movingMeld.extra!=null)return movingMeld.extra.position+Vector3.up*.06f;
                var right=Quaternion.Euler(0,table.SeatAngle(current.seat),0)*Vector3.right;
                var edge=movingMeld.group.GetComponentsInChildren<BoxCollider>(true).OrderByDescending(c=>Vector3.Dot(c.transform.position,right)).FirstOrDefault();
                if(edge!=null)return edge.transform.position+right*.18f+Vector3.up*.06f;
            }
            return table.MeldPoint(current.seat)+Vector3.up*.06f;
        }
        void SpawnImpact()
        {
            EndImpact();
            impact=new GameObject("APK meld impact").transform;impact.SetParent(transform,false);nativeImpact=impact.gameObject.AddComponent<NativeParticleEffect>();nativeImpact.Initialize("effect_chipeng",(uint)(current.id+1));
            impact.position = MeldImpactPoint();
            impact.rotation = Quaternion.Euler(0,table.SeatAngle(current.seat),0);impact.localScale=Vector3.one*(TableTileGeometry.SourceUnits);
            impactStarted = ValidationFreeze ? ValidationTime : Time.unscaledTime;
        }
        void EndImpact()
        {
            if (impact == null) return;
            var impactRenderer = impact.GetComponent<MeshRenderer>();
            if (impactRenderer != null && impactRenderer.sharedMaterial != null) Destroy(impactRenderer.sharedMaterial);
            Destroy(impact.gameObject); impact = null;nativeImpact=null;
        }
        void UpdateImpact()
        {
            if (impact == null) return;
            float time = (ValidationFreeze ? ValidationTime : Time.unscaledTime) - impactStarted;
            if(nativeImpact!=null){if(current!=null&&movingMeld!=null)impact.position=MeldImpactPoint();nativeImpact.Sample(time);if(time>=1.5f)EndImpact();return;}
            if (time >= .38f) { EndImpact(); return; }
            float k = time / .38f;
            impact.localScale = Vector3.one * Mathf.Lerp(.25f, 1.15f, Mathf.Sqrt(k));
            float fade = 1f - k * k;
            var impactRenderer = impact.GetComponent<MeshRenderer>();
            if (impactRenderer != null && impactRenderer.sharedMaterial != null)
                impactRenderer.sharedMaterial.SetColor("_Tint", new Color(fade, fade * .95f, fade * .82f, 1f));
            impact.rotation = Quaternion.LookRotation(impact.position - table.Camera.transform.position, Vector3.up);
        }
        PublicSound cutInWin;
        void BeginCall(PublicSound item) { call=item; callStarted=Time.unscaledTime; if(IsWin(item))cutInWin=item; }
        void FinishCall() { call=null; }
        void ConfigureOverlay()
        {
            overlay.CopyFrom(table.Camera);overlay.cullingMask=1<<14;overlay.clearFlags=CameraClearFlags.Depth;overlay.depth=table.HandCamera.depth+1;
        }
        void LateUpdate() {if(overlay!=null && current!=null)ConfigureOverlay();}
        public void RenderOverlay(RenderTexture texture)
        {
            if(overlay==null || !overlay.enabled)return;ConfigureOverlay();overlay.targetTexture=texture;overlay.Render();overlay.targetTexture=null;
        }
        void Update()
        {
            winCutIn.Sample(winCutIn.Age);
            StartQueued();
            UpdateImpact(); UpdateNorthEffects(); UpdateWinEffects();
            float elapsed=ValidationFreeze?ValidationTime:Time.unscaledTime-started;
            if (current!=null && elapsed >= PlayLength) { CompleteHandoff(); Finish(); }
            if(cutInWin!=null && (ValidationFreeze?ValidationTime:Time.unscaledTime-callStarted)>=1.05f){
                var players=GetComponent<RiichiClient>().State?.players;winCutIn.Play(players!=null&&cutInWin.seat<players.Length?players[cutInWin.seat]:null);GetComponent<GameAudio>()?.Cue("cutin");
                // Double ron shares one discarded tile. Show both winners first,
                // then play that tile's complete burst after the final cut-in.
                if(cutInWin.kind=="ron"&&!callEvents.Any(e=>e.kind=="ron"))SpawnWin(cutInWin,(ValidationFreeze?0:Time.unscaledTime)+WinCutIn.Duration+.18f);
                cutInWin=null;
            }
            if (call!=null && (ValidationFreeze?ValidationTime:Time.unscaledTime-callStarted) >= 1.05f) FinishCall();
            if (current==null || motion==null || clip==null) return;
            elapsed=ValidationFreeze?ValidationTime:Time.unscaledTime-started;
            if(GroupCall && meldTarget!=null && !handedOff){
                if(elapsed>=.033f&&!impactQueued){SpawnImpact();impactQueued=true;}
                bool visible=elapsed>=.10f;meldTarget.gameObject.SetActive(visible);
                foreach(var child in meldTarget.GetComponentsInChildren<Transform>())child.gameObject.layer=14;
                Vector3 delta=motion.TransformVector(TilePoint(Mathf.Min(elapsed,Handoff))-TilePoint(SourceHandoff));meldTarget.position=meldFinal+delta;meldTarget.rotation=meldRotation;
            }
            float frame=Mathf.Clamp01(elapsed/ClipLength)*(clip.data.frames-1);int a=(int)frame,b=Mathf.Min(a+1,clip.data.frames-1);float t=frame-a;
            for(int i=0;i<clip.data.vertices;i++) {clip.frameVertices[i]=Vector3.Lerp(clip.vertices[a][i],clip.vertices[b][i],t);clip.frameNormals[i]=Vector3.Lerp(clip.normals[a][i],clip.normals[b][i],t);}
            mesh.vertices=clip.frameVertices;mesh.normals=clip.frameNormals;mesh.RecalculateBounds();
            if(animatedTile!=null)
            {
                // A frame can cross the handoff time. Clamp every carried object
                // to the contact keyframe before revealing its deposited counterpart.
                Vector3 p=TilePoint(Mathf.Min(elapsed,SourceHandoff));
                if(current.kind=="tsumo"){motion.GetComponentInChildren<MeshRenderer>().enabled=elapsed<clip.data.duration;}
                PositionCarriedTile(p);
                // A declared riichi tile lies sideways in the river, matching CreateTable.
                bool stick=current.kind=="riichi-stick";
                animatedTile.rotation=current.kind=="discard"&&targetTile!=null?targetTile.rotation:current.kind=="nuki" && table.NorthTarget(current.seat,current.nuki)!=null
                    ? table.NorthTarget(current.seat,current.nuki).rotation
                    : current.kind=="tsumo" && targetTile!=null?targetTile.rotation:Quaternion.Euler(0,table.SeatAngle(current.seat)+(!stick&&current.riichi?90f:0f),0);
                // Tsumo carries and slaps the tile face up for the entire motion.
            }
            if(current.kind=="discard"&&current.riichi&&!riichiVisualAttempted&&elapsed>=RiichiEffectAt){
                riichiVisualAttempted=true;riichiVisual.TryPlay(current,animatedTile,ValidationFreeze?ValidationTime:Time.unscaledTime);
            }
            if (elapsed>=Handoff) CompleteHandoff();
        }
        static string CallArt(string kind)
        {
            switch(kind) {case "chi":return "shout_chi";case "pon":return "shout_peng";case "kan":return "shout_gang";case "nuki":return "shout_babei";case "riichi":case "double-riichi":return "shout_lizhi";case "ron":return "show_rong";case "tsumo":return "show_zimo";default:return null;}
        }
        public void DrawCall(float width,float height,int n)
        {
            if(winCutIn.Active){winCutIn.Draw(width,height);return;}
            if (call==null) return;
            if(IsWin(call)){NativeAnnouncements.DrawWin(call.kind,table.RelativeSeat(call.seat),n,width,height,ValidationFreeze?ValidationTime:Time.unscaledTime-callStarted);return;}
            string name=CallArt(call.kind);if(name==null)return;
            float time=ValidationFreeze?ValidationTime:Time.unscaledTime-callStarted;
            var players=GetComponent<RiichiClient>().State?.players;
            nativeCallouts.Draw(name,table.RelativeSeat(call.seat),n,width,height,time,players!=null&&call.seat<players.Length?players[call.seat]:null);
        }
#if UNITY_EDITOR
        public void ValidationPlay(int seat,string kind,string tile=null,bool riichi=false,int riverIndex=0)
        {
            Finish();FinishCall();winCutIn.Reset();cutInWin=null;EndNorthEffects();EndWinEffects();winningBursts.Clear();ClearQueuedHands();callEvents.Clear();ValidationFreeze=true;ValidationTime=.28f;
            Begin(new PublicSound {seat=seat,kind=kind,tile=tile,riichi=riichi,riverIndex=riverIndex});
            if (CallArt(kind)!=null) BeginCall(new PublicSound {seat=seat,kind=kind,tile=tile,riichi=riichi});
        }
        public bool ValidationRiichiDiscard => current != null && current.kind=="discard" && current.riichi;
        public string ValidationKind => current==null?null:current.kind;
        public float ValidationPlayLength => PlayLength;
        public float ValidationHandoff => Handoff;
        public Transform ValidationAnimatedTile => animatedTile;
        public Transform ValidationImpact => impact;
        public Transform ValidationTarget => targetTile!=null?targetTile:targetStick!=null?targetStick:northTarget!=null?northTarget:meldTarget;
        public WinTileEffect ValidationWin => winEffects.LastOrDefault();
        public NorthPetals ValidationPetals => northEffects.LastOrDefault();
        public string ValidationCallKind => call==null?null:call.kind;
        public float ValidationCallAlpha => call==null?0f:Mathf.Clamp01((ValidationFreeze?ValidationTime:Time.unscaledTime-callStarted)/.12f);
#endif
        void OnDestroy() {nativeCallouts.Dispose();winCutIn.Reset(); Finish();FinishCall();EndImpact();EndNorthEffects();EndWinEffects();if(overlay!=null)Destroy(overlay.gameObject);if(mesh!=null)Destroy(mesh);if(body!=null)Destroy(body);if(nails!=null)Destroy(nails); }
    }
}
