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
        [Serializable] class HandData { public int vertices, frames, fps; public float duration; public float[] uv, tilePoints; public int[] body, nails; }
        readonly Queue<PublicSound> events = new Queue<PublicSound>();
        readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        string room;
        int cursor;
        float last, started;
        PublicSound current;
        TableRenderer table;
        HandData data;
        Vector3[][] vertices, normals;
        Vector3[] frameVertices, frameNormals;
        Mesh mesh;
        Material body, nails;
        Transform motion, animatedTile, targetTile;
        Camera overlay;
        public int PlayedEvents { get; private set; }
        public bool IsAnimating => current != null;
        public bool ValidationFreeze;
        public float ValidationTime;
        public int ActiveSeat => current == null ? -1 : current.seat;

        void Awake() { table = GetComponent<TableRenderer>(); }
        public Texture2D Art(string name)
        {
            if (!art.TryGetValue(name, out var image)) { image = Resources.Load<Texture2D>("Majsoul/Actions/"+name); art[name] = image; }
            return image;
        }
        public void Reset()
        {
            Finish(); events.Clear(); room = null; cursor = 0; last = Time.unscaledTime;
        }
        public void Accept(RoomView view)
        {
            if (string.IsNullOrEmpty(view.audioRoom)) { Reset(); return; }
            if (room != view.audioRoom) { Reset(); room = view.audioRoom; cursor = view.audioSeq; return; }
            bool stale = Time.unscaledTime-last >= 8; last = Time.unscaledTime;
            var fresh = (view.audioEvents ?? new PublicSound[0]).Where(e => e.id > cursor).OrderBy(e => e.id).ToArray();
            cursor = Math.Max(cursor, view.audioSeq);
            if (stale) { Finish(); events.Clear(); return; }
            foreach (var item in fresh) if (item.seat >= 0 && item.seat < view.n && (item.kind == "discard" || CallArt(item.kind) != null)) events.Enqueue(item);
            while (events.Count > 8) events.Dequeue();
            // Re-rendering a state does not restart the event currently playing.
            if (current != null && current.kind == "discard") HideTarget();
        }
        void LoadHand()
        {
            if (data != null) return;
            data = JsonUtility.FromJson<HandData>(Resources.Load<TextAsset>("Majsoul/Actions/discard-hand").text);
            byte[] bytes = Resources.Load<TextAsset>("Majsoul/Actions/discard-hand-frames").bytes;
            var values = new float[bytes.Length/4]; Buffer.BlockCopy(bytes,0,values,0,bytes.Length);
            vertices = new Vector3[data.frames][]; normals = new Vector3[data.frames][];
            for (int f=0;f<data.frames;f++)
            {
                vertices[f] = new Vector3[data.vertices]; normals[f] = new Vector3[data.vertices];
                for (int i=0;i<data.vertices;i++)
                {
                    int p=(f*2*data.vertices+i)*3, n=p+data.vertices*3;
                    vertices[f][i]=new Vector3(values[p],values[p+1],values[p+2]);
                    normals[f][i]=new Vector3(values[n],values[n+1],values[n+2]);
                }
            }
            var uv=new Vector2[data.vertices]; for(int i=0;i<uv.Length;i++) uv[i]=new Vector2(data.uv[i*2],1-data.uv[i*2+1]);
            mesh=new Mesh { name="Original animated hand", vertices=vertices[0], normals=normals[0], uv=uv, subMeshCount=2 };
            mesh.SetTriangles(data.body,0); mesh.SetTriangles(data.nails,1); mesh.MarkDynamic();
            frameVertices=new Vector3[data.vertices];frameNormals=new Vector3[data.vertices];
            body=new Material(Shader.Find("Unlit/Texture")) { mainTexture=Art("hand-skin") };
            nails=new Material(Shader.Find("Unlit/Texture")) { mainTexture=Art("hand-nails") };
        }
        void Begin(PublicSound item)
        {
            current=item; started=Time.unscaledTime; PlayedEvents++;
            if (item.kind != "discard") return;
            LoadHand();
            motion = new GameObject("Original discard hand seat "+item.seat).transform;
            if(overlay==null) {overlay=new GameObject("Discard hand overlay camera").AddComponent<Camera>();overlay.transform.SetParent(table.Camera.transform,false);}
            ConfigureOverlay();overlay.enabled=true;
            motion.position=table.DiscardPoint(item.seat,item.tile)+Vector3.up*.12f;
            motion.rotation=Quaternion.Euler(0,table.SeatAngle(item.seat)+180,0);
            motion.localScale=Vector3.one*(.33f/.02947767f*.9f);
            var hand=new GameObject("Source hand mesh");hand.layer=14;hand.transform.SetParent(motion,false);
            hand.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=hand.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{body,nails};renderer.shadowCastingMode=ShadowCastingMode.Off;
            animatedTile=table.CreateEffectTile(item.tile);
            foreach(var child in animatedTile.GetComponentsInChildren<Transform>())child.gameObject.layer=14;
            foreach(var collider in animatedTile.GetComponentsInChildren<Collider>())collider.enabled=false;
            HideTarget();
        }
        void HideTarget()
        {
            float elapsed=ValidationFreeze?ValidationTime:Time.unscaledTime-started;
            if(elapsed>=.40f) return;
            targetTile=table.FindDiscard(current.seat,current.tile);
            if (targetTile!=null) targetTile.gameObject.SetActive(false);
        }
        void Finish()
        {
            if (targetTile!=null) targetTile.gameObject.SetActive(true);
            if (motion!=null) { motion.gameObject.SetActive(false); Destroy(motion.gameObject); }
            if (animatedTile!=null) { animatedTile.gameObject.SetActive(false); Destroy(animatedTile.gameObject); }
            motion=null;animatedTile=null;targetTile=null;current=null;
            if(overlay!=null)overlay.enabled=false;
        }
        void ConfigureOverlay()
        {
            overlay.CopyFrom(table.Camera);overlay.cullingMask=1<<14;overlay.clearFlags=CameraClearFlags.Depth;overlay.depth=table.HandCamera.depth+1;
        }
        void LateUpdate() {if(overlay!=null && current!=null && current.kind=="discard")ConfigureOverlay();}
        public void RenderOverlay(RenderTexture texture)
        {
            if(overlay==null || !overlay.enabled)return;ConfigureOverlay();overlay.targetTexture=texture;overlay.Render();overlay.targetTexture=null;
        }
        void Update()
        {
            if (current==null && events.Count>0) Begin(events.Dequeue());
            if (current==null) return;
            float elapsed=ValidationFreeze?ValidationTime:Time.unscaledTime-started;
            if (elapsed >= (current.kind=="discard" ? .62f : 1.05f)) { Finish(); return; }
            if (motion==null) return;
            float frame=Mathf.Clamp01(elapsed/.62f)*(data.frames-1);int a=(int)frame,b=Mathf.Min(a+1,data.frames-1);float t=frame-a;
            for(int i=0;i<data.vertices;i++) {frameVertices[i]=Vector3.Lerp(vertices[a][i],vertices[b][i],t);frameNormals[i]=Vector3.Lerp(normals[a][i],normals[b][i],t);}
            mesh.vertices=frameVertices;mesh.normals=frameNormals;mesh.RecalculateBounds();
            if(animatedTile!=null)
            {
                Vector3 p=Vector3.Lerp(new Vector3(data.tilePoints[a*3],data.tilePoints[a*3+1],data.tilePoints[a*3+2]),new Vector3(data.tilePoints[b*3],data.tilePoints[b*3+1],data.tilePoints[b*3+2]),t);
                animatedTile.position=motion.TransformPoint(p)-Vector3.up*.12f;
                animatedTile.rotation=Quaternion.Euler(0,table.SeatAngle(current.seat),0);
                if (elapsed>.40f) { animatedTile.gameObject.SetActive(false);if(targetTile!=null)targetTile.gameObject.SetActive(true); }
            }
        }
        static string CallArt(string kind)
        {
            switch(kind) {case "chi":return "shout_chi";case "pon":return "shout_peng";case "kan":return "shout_gang";case "nuki":return "shout_babei";case "riichi":case "double-riichi":return "shout_lizhi";case "ron":return "show_rong";case "tsumo":return "show_zimo";default:return null;}
        }
        public void DrawCall(float width,float height,int n)
        {
            if (current==null || current.kind=="discard") return;
            string name=CallArt(current.kind);if(name==null)return;
            float time=ValidationFreeze?ValidationTime:Time.unscaledTime-started;
            float alpha=Mathf.Clamp01(time/.12f)*Mathf.Clamp01((1.05f-time)/.2f);
            int seat=table.RelativeSeat(current.seat);
            float k=Mathf.SmoothStep(0,1,time/.18f),cw=285,ch=270;
            Vector2 origin=seat==0?new Vector2(width*.29f,height*.53f):seat==n-1?new Vector2(-55+(k-1)*180,height*.37f):seat==1&&n==4?new Vector2(width-290+(1-k)*180,height*.37f):new Vector2(width*.59f,-30+(k-1)*180);
            var old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            var bg=Art(seat==0 || (n==4 && seat==2) || (n==3 && seat==1)?"shout_vertical":"shout_horizontal");if(bg!=null)GUI.DrawTexture(new Rect(origin.x,origin.y+70,cw+80,ch-65),bg,ScaleMode.StretchToFill);
            var portrait=Art("yiji-full");if(portrait!=null)GUI.DrawTextureWithTexCoords(new Rect(origin.x,origin.y,cw,ch),portrait,new Rect(0,.45f,1,.55f));
            var word=Art(name);if(word!=null)GUI.DrawTexture(new Rect(origin.x+170,origin.y+85,135,120),word,ScaleMode.ScaleToFit);
            GUI.color=old;
        }
#if UNITY_EDITOR
        public void ValidationPlay(int seat,string kind,string tile=null)
        {
            Finish();events.Clear();ValidationFreeze=true;ValidationTime=.28f;Begin(new PublicSound {seat=seat,kind=kind,tile=tile});
        }
#endif
        void OnDestroy() { Finish();if(overlay!=null)Destroy(overlay.gameObject);if(mesh!=null)Destroy(mesh);if(body!=null)Destroy(body);if(nails!=null)Destroy(nails); }
    }
}
