#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi
{
    // Editor-only probe: drives the real client and renders the real camera.
    public class RuntimeValidation : MonoBehaviour
    {
        string folder;
        IEnumerator Start()
        {
            folder = Path.GetFullPath("../build/unity-smoke"); Directory.CreateDirectory(folder);
            foreach (var name in new[] { "passed.txt", "failed.txt" })
                if (File.Exists(Path.Combine(folder, name))) File.Delete(Path.Combine(folder, name));
            var run = Run();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception e) { Debug.LogException(e); File.WriteAllText(Path.Combine(folder, "failed.txt"), e.ToString()); EditorApplication.Exit(1); yield break; }
                if (!next) break;
                yield return run.Current;
            }
            File.WriteAllText(Path.Combine(folder, "passed.txt"), "Actual Unity Play mode: live AI room, delayed second-click discard, raycast, 3/4-seat rendering, safe-area pointer roundtrips, 4 resolutions, 8 role clips, called-river removal, grouped chi choices, all 4 discard hand directions at 15 time samples with opaque materials, call cut-ins, event dedup/reconnect; original 224-vertex outlines, model-foot response glow in 4 directions, 0-4 own melds without hand overlap at 4 resolutions, and 4 inward-facing North tiles per seat. Fixture screenshots are synthetic layout scenes, not replays.");
            Debug.Log("RIICHI UNITY PLAY PASS"); EditorApplication.Exit(0);
        }
        IEnumerator Run()
        {
            foreach (Vector2 resolution in new[] { new Vector2(1280, 720), new Vector2(1688, 780), new Vector2(2760, 1256), new Vector2(844, 390) })
            foreach (bool isTable in new[] { true, false })
            {
                var safe = new Rect(24, 8, resolution.x - 48, resolution.y - 16);
                var layout = TableViewport.Fit(resolution.x, resolution.y, safe, isTable);
                var point = new Vector3(507, 320, 0); var pixel = layout.Matrix.MultiplyPoint3x4(point);
                var roundtrip = layout.FromScreen(new Vector2(pixel.x, resolution.y - pixel.y), resolution.y);
                Require(Vector2.Distance(roundtrip, point) < .001f, "Safe-area pointer mapping differs from drawing");
                Require(Mathf.Abs(layout.Matrix.m00 - layout.Matrix.m11) < .00001f, "Nonuniform UI scale");
            }
            var client = GetComponent<RiichiClient>(); var table = GetComponent<TableRenderer>();
            client.BeginValidationPractice(File.ReadAllText(Path.Combine(folder, "host.txt")).Trim());
            float deadline = Time.realtimeSinceStartup + 35;
            while ((client.State == null || client.State.phase != "play" || client.Busy || !client.State.options.Any(x => x.kind == "discard")) && Time.realtimeSinceStartup < deadline) yield return null;
            Require(client.State != null && client.State.phase == "play" && !client.Busy && client.State.options.Any(x => x.kind == "discard"), "Live practice failed to reach the human discard turn");
            var state = client.State; int before = state.players[state.you].river.Length;
            yield return null; Physics.SyncTransforms();
            var tile = FindObjectsByType<TileTarget>(FindObjectsSortMode.None).OrderBy(t => t.Index).First();
            Vector3 center = tile.GetComponentInChildren<Collider>().bounds.center;
            Vector3 screen = table.HandCamera.WorldToScreenPoint(center);
            Require(screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height, "Hand outside camera");
            Require(Physics.Raycast(table.HandCamera.ScreenPointToRay(screen), out var hit, 100) && hit.collider.GetComponentInParent<TileTarget>() == tile, "Tile cannot be picked by camera ray");
            client.SelectTile(tile.Index, tile.Action);
            yield return new WaitForSecondsRealtime(1.15f);
            while (client.Busy) yield return null;
            Require(client.State.players[state.you].river.Length == before && client.SelectedIndex == tile.Index, "First click discarded or selection expired");
            client.SelectTile(tile.Index, tile.Action);
            deadline = Time.realtimeSinceStartup + 12;
            while (client.State.players[state.you].river.Length == before && Time.realtimeSinceStartup < deadline) yield return null;
            Require(client.State.players[state.you].river.Length > before, "Delayed second click did not commit discard");
            var manifest = JsonUtility.FromJson<VoiceManifest>(Resources.Load<TextAsset>("Majsoul/voice-manifest").text);
            Require(manifest.provider == "yiji" && manifest.clips.Length == 8, "Mixed character pack");
            foreach (var entry in manifest.clips)
            {
                var clip = Resources.Load<AudioClip>(entry.resource); Require(clip != null && clip.length > 0, "Unreadable audio: " + entry.key);
            }
            foreach (int n in new[] { 3, 4 })
            foreach (bool pressure in new[] { true, false })
            {
                string prefix = pressure ? "" : "preview-";
                var fixture = JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder, (pressure ? "fixture-" : "preview-") + n + ".json")));
                if (pressure)
                {
                    int originalYou = fixture.you, originalTurn = fixture.turn;
                    for (int you = 0; you < n; you++)
                    {
                        fixture.you = you; fixture.turn = (you+1)%n;
                        client.ShowValidationFixture(fixture); yield return null; Physics.SyncTransforms();
                        ValidatePresentation(table, fixture);
                    }
                    fixture.you = originalYou; fixture.turn = originalTurn;
                }
                client.ShowValidationFixture(fixture); yield return null; Physics.SyncTransforms();
                ValidatePresentation(table, fixture);
                Require(table.Camera.GetComponent<AudioListener>() != null, "Missing audio listener");
                if (pressure) Require(GameObject.Find("Riichi 1000") != null, "Missing deposited riichi stick");
                foreach (var item in FindObjectsByType<TileTarget>(FindObjectsSortMode.None))
                {
                    var v = table.HandCamera.WorldToViewportPoint(item.transform.position);
                    Require(v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1, "Hand is clipped in " + n + "-seat layout");
                }
                foreach (Vector2 resolution in new[] { new Vector2(1280, 720), new Vector2(1688, 780), new Vector2(2760, 1256), new Vector2(844, 390) })
                {
                    string suffix = (int)resolution.x + "x" + (int)resolution.y;
                    SetGameResolution((int)resolution.x, (int)resolution.y);
                    for (int frame = 0; frame < 8; frame++) yield return null;
                    table.FitCamera(resolution.x / resolution.y); Physics.SyncTransforms();
                    foreach (var t in GameObject.Find("PublicTableState").GetComponentsInChildren<BoxCollider>())
                    {
                        if (t.name != "Standing concealed hand") continue;
                        foreach (var corner in Corners(t))
                        {
                            var v = table.Camera.WorldToViewportPoint(corner);
                            Require(v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1, "Opponent hand clipped at " + suffix);
                        }
                    }
                    foreach (var item in FindObjectsByType<TileTarget>(FindObjectsSortMode.None))
                    {
                        var box = item.GetComponentInChildren<BoxCollider>();
                        Vector3 origin = box.transform.TransformPoint(box.center);
                        Vector3 left = table.HandCamera.WorldToScreenPoint(origin - box.transform.right * box.size.x * box.transform.lossyScale.x / 2);
                        Vector3 right = table.HandCamera.WorldToScreenPoint(origin + box.transform.right * box.size.x * box.transform.lossyScale.x / 2);
                        Vector3 top = table.HandCamera.WorldToScreenPoint(origin + box.transform.up * box.size.y * box.transform.lossyScale.y / 2);
                        Vector3 bottom = table.HandCamera.WorldToScreenPoint(origin - box.transform.up * box.size.y * box.transform.lossyScale.y / 2);
                        Require(table.HandCamera.orthographic && Mathf.Abs(Vector3.Distance(left, right) / Vector3.Distance(top, bottom) - .62f) < .001f, "Original hand aspect changed at " + suffix);
                        foreach (Vector3 corner in Corners(item.GetComponentInChildren<BoxCollider>()))
                        {
                            var v = table.HandCamera.WorldToViewportPoint(corner);
                            Require(v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1, "Hand corner clipped at " + suffix + ": " + v);
                        }
                        var screenPoint = table.HandCamera.WorldToScreenPoint(item.GetComponentInChildren<Collider>().bounds.center);
                        Require(Physics.Raycast(table.HandCamera.ScreenPointToRay(screenPoint), out var pick, 100) && pick.collider.GetComponentInParent<TileTarget>() == item, "Hand picking failed at " + suffix);
                    }
                    Capture(table, Path.Combine(folder, prefix + "table-" + n + "-" + suffix + ".png"), (int)resolution.x, (int)resolution.y);
                    if (!pressure && resolution.x == 1280)
                        CaptureHub(Path.Combine(folder, "score-hub-" + n + ".png"));
                    string fullFile = Path.Combine(folder, prefix + "ui-" + n + "-" + suffix + ".png");
                    if (File.Exists(fullFile)) File.Delete(fullFile);
                    ScreenCapture.CaptureScreenshot(fullFile);
                    for (int frame = 0; frame < 30 && !File.Exists(fullFile); frame++) yield return null;
                    Require(File.Exists(fullFile), "Full game-view capture failed at " + suffix);
                }
            }
            SetGameResolution(1688,780);
            // Vary legally sized concealed hands independently of the action
            // fixtures, including four melds and four extracted North tiles.
            var layoutFixture=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"effects.json")));
            layoutFixture.phase="play";layoutFixture.pending=null;layoutFixture.drawn=null;
            for(int i=0;i<layoutFixture.n;i++)layoutFixture.players[i].nuki=i==0?2:i>=2?1:0;
            var hand=new[]{"s3","s4","s6","s8","s8","s9","z5","z6","z6","z7","p2","p3","p4","p5"};
            var meldChoices=new[]{"p555+","z666-","s123-","m777="};
            for(int count=0;count<=4;count++)
            {
                layoutFixture.players[0].melds=meldChoices.Take(count).ToArray();
                layoutFixture.hand=hand.Take(14-count*3).ToArray();
                layoutFixture.options=layoutFixture.hand.Distinct().Select(t=>new Choice {id="discard:"+t,kind="discard",tile=t}).ToArray();
                client.ShowValidationFixture(layoutFixture);yield return null;
                foreach(var resolution in new[]{new Vector2(1280,720),new Vector2(1688,780),new Vector2(2760,1256),new Vector2(844,390)})
                {
                    SetGameResolution((int)resolution.x,(int)resolution.y);
                    for(int frame=0;frame<8;frame++)yield return null;
                    table.FitCamera(resolution.x/resolution.y);Physics.SyncTransforms();
                    ValidateHandMeldGap(table,layoutFixture);
                    ValidateNorth(table,layoutFixture);
                    if(resolution.x==1688)Capture(table,Path.Combine(folder,"meld-layout-"+count+".png"),1688,780);
                }
            }
            // North has four physical copies: test four extracted tiles at
            // each seat separately instead of showing sixteen in one scene.
            for(int owner=0;owner<layoutFixture.n;owner++)
            {
                for(int i=0;i<layoutFixture.n;i++)layoutFixture.players[i].nuki=i==owner?4:0;
                client.ShowValidationFixture(layoutFixture);yield return null;
                ValidateNorth(table,layoutFixture);
                Capture(table,Path.Combine(folder,"north-seat-"+owner+".png"),844,390);
            }
            SetGameResolution(1688,780);
            var effectFixture=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"effects.json")));
            client.ShowValidationFixture(effectFixture);yield return null;Physics.SyncTransforms();
            ValidatePresentation(table,effectFixture);
            Require(table.ResponseTarget!=null,"Response discard is not highlighted");
            var groundGlow=table.ResponseTarget.Find("Original response ground glow");
            Require(groundGlow!=null && Mathf.Abs(groundGlow.position.y-.006f)<.001f && groundGlow.GetComponent<MeshRenderer>().sharedMaterial.shader.name=="YunJian/ResponseGlow","Response glow is not attached to the tile foot");
            Require(client.ActionRects(1298,600).Length==3,"Multiple chi choices must share one action button");
            var fx=GetComponent<TableEffects>();
            for(int frame=0;frame<8;frame++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"response-ui.png"));yield return new WaitForSecondsRealtime(.2f);
            int choiceVersion=effectFixture.version;
            client.ValidationChooseActionKind("chi");yield return null;
            Require(client.ValidationChoosingKind=="chi" && !client.Busy && client.State.version==choiceVersion,"Multiple chi choices submitted without selection");
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"response-choices.png"));yield return new WaitForSecondsRealtime(.2f);
            client.ValidationChooseActionKind("chi");Require(client.ValidationChoosingKind==null,"Chi selection cannot be closed");
            for(int seat=0;seat<4;seat++)
            {
                effectFixture.pending.from=seat;
                effectFixture.last=new LastDiscard {from=seat,tile=effectFixture.players[seat].river.Last().tile};
                client.ShowValidationFixture(effectFixture);yield return null;
                Require(table.ResponseTarget.parent.name=="Seat "+seat && table.ResponseTarget.Find("Original response ground glow")!=null,"Directional model-foot glow missing");
                Capture(table,Path.Combine(folder,"response-glow-"+seat+".png"),1688,780);
                fx.ValidationPlay(seat,"discard",effectFixture.players[seat].river.Last().tile);
                yield return null;yield return null;
                Require(fx.ActiveSeat==seat && GameObject.Find("Original discard hand seat "+seat)!=null,"Missing directional discard hand");
                var handRenderer=GameObject.Find("Original discard hand seat "+seat).GetComponentInChildren<MeshRenderer>();
                foreach(var material in handRenderer.sharedMaterials)Require(material.GetTag("RenderType",false)=="Opaque" && material.renderQueue<2500,"Discard hand uses transparent material");
                Capture(table,Path.Combine(folder,"discard-hand-"+seat+".png"),1688,780);
                for(int sample=0;sample<15;sample++)
                {
                    fx.ValidationTime=sample*.60f/14;yield return null;yield return null;
                    Capture(table,Path.Combine(folder,"hand-"+seat+"-frame-"+sample.ToString("D2")+".png"),1688,780);
                }
                fx.Accept(effectFixture);yield return null;
                var visibleTarget=table.FindDiscard(seat,effectFixture.last.tile);
                Require(visibleTarget!=null && visibleTarget.gameObject.activeSelf,"Polling hid the released discard again");
                fx.ValidationPlay(seat,"pon");yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"call-pon-"+seat+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
            fx.ValidationFreeze=false;fx.Reset();
            effectFixture.last=new LastDiscard {from=3,tile="p5"};effectFixture.players[3].river.Last().called=true;effectFixture.pending=null;effectFixture.phase="play";
            client.ShowValidationFixture(effectFixture);yield return null;
            Require(table.ResponseTarget==null && table.FindDiscard(3,"p5")==null,"Claimed discard is still a response/animation target");
            ValidatePresentation(table,effectFixture);
            effectFixture.audioRoom="presentation-validation";effectFixture.audioSeq=10;effectFixture.audioEvents=new PublicSound[0];fx.Accept(effectFixture);
            effectFixture.audioSeq=11;effectFixture.audioEvents=new[]{new PublicSound {id=11,seat=1,kind="pon"}};int played=fx.PlayedEvents;
            fx.Accept(effectFixture);yield return null;fx.Accept(effectFixture);yield return null;
            Require(fx.PlayedEvents==played+1,"Duplicate state replayed call effect");fx.Reset();fx.Accept(effectFixture);yield return null;
            Require(!fx.IsAnimating,"Reconnect replayed historical effect");
        }
        static void ValidatePresentation(TableRenderer table, RoomView view)
        {
            var publicRoot = GameObject.Find("PublicTableState").transform;
            foreach(var edge in publicRoot.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="Original tile outline"))
                Require(edge.sharedMesh.vertexCount==224 && edge.GetComponent<MeshRenderer>().sharedMaterial.shader.name=="YunJian/OriginalTileOutline","Original outline mesh/material missing");
            for (int i = 0; i < view.n; i++)
            {
                var renderedSeat=publicRoot.Find("Seat "+i);
                int riverCount=renderedSeat.Cast<Transform>().Count(t=>t.name.StartsWith("River ",StringComparison.Ordinal));
                Require(riverCount==view.players[i].river.Count(r=>!r.called),"Claimed tile remains in the river");
                var melds=renderedSeat.Find("Exposed melds");
                foreach(Transform piece in melds)Require(piece.localPosition.z <= -3.5f,"Melds are still inside concealed hand row");
                if (i == view.you) continue;
                var seat = publicRoot.Find("Seat " + i);
                var tiles = seat.Cast<Transform>().Where(t => t.name == "Standing concealed hand").ToArray();
                Require(tiles.Length == view.players[i].count, "Concealed hand count changed");
                foreach (var t in tiles)
                {
                    var box = t.GetComponent<BoxCollider>(); var model = t.Find("Original tile");
                    Require(box.size.y > box.size.x && box.size.y > box.size.z * 1.9f, "Opponent tile is lying flat");
                    Vector3 inward = Vector3.ProjectOnPlane(-t.position, Vector3.up).normalized;
                    Require(Vector3.Dot(model.TransformDirection(Vector3.down), inward) > .8f, "Concealed back faces away from table");
                    Require(Mathf.Abs(box.bounds.min.y) < .001f, "Standing tile floats or sinks into cloth");
                }
            }
            var hub = publicRoot.Find("Score hub");
            Require(hub != null && hub.Find("Score plate") != null, "Missing score plate");
            // Check the real plate pixels, rather than only an estimated slot.
            var previous = RenderTexture.active; var target = RenderTexture.GetTemporary(256,256);
            Graphics.Blit(Resources.Load<Texture2D>("Majsoul/Table/score-hub"),target); RenderTexture.active = target;
            var plate = new Texture2D(256,256,TextureFormat.RGB24,false);
            plate.ReadPixels(new Rect(0,0,256,256),0,0); plate.Apply();
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            var dark = plate.GetPixel(128,65);
            int lights = 0;
            foreach (var player in view.players)
            {
                var seat = hub.Find("Hub seat " + player.wind); Require(seat != null, "Missing hub seat");
                foreach (Transform glyph in seat)
                {
                    if (glyph.name == "Turn light") { lights++; continue; }
                    if (glyph.name != "Hub glyph number_score") continue;
                    float x = (glyph.localPosition.x / ReferenceScoreHub.Size + .5f) * 256;
                    float y = (.5f - glyph.localPosition.z / ReferenceScoreHub.Size) * 256;
                    float w = glyph.localScale.x / ReferenceScoreHub.Size * 256;
                    float h = glyph.localScale.y / ReferenceScoreHub.Size * 256;
                    Require(x-w/2 >= 78 && x+w/2 <= 178 && y-h/2 >= 176 && y+h/2 <= 204, "Score leaves its dark recess");
                    foreach (float dx in new[] { -.5f,.5f }) foreach (float dy in new[] { -.5f,.5f })
                    {
                        var point = hub.InverseTransformPoint(glyph.TransformPoint(new Vector3(dx,dy,0)));
                        int px = Mathf.RoundToInt((point.x/ReferenceScoreHub.Size+.5f)*255);
                        int py = Mathf.RoundToInt((point.z/ReferenceScoreHub.Size+.5f)*255);
                        var color = plate.GetPixel(px,py);
                        Require(Mathf.Abs(color.r-dark.r)+Mathf.Abs(color.g-dark.g)+Mathf.Abs(color.b-dark.b) < .08f, "Score overlaps the grey frame: " + player.wind);
                    }
                }
            }
            Require(lights == 1, "Turn light missing or duplicated");
            Require(hub.Find("Hub seat " + view.players[view.turn].wind).Find("Turn light") != null, "Turn light follows wrong seat");
            Destroy(plate);
        }
        static void ValidateHandMeldGap(TableRenderer table,RoomView view)
        {
            var root=GameObject.Find("PublicTableState").transform;
            var hand=root.Find("Camera-facing hand").GetComponentsInChildren<BoxCollider>();
            float right=float.MinValue;
            foreach(var box in hand)foreach(var corner in Corners(box))
            {
                var screen=table.HandCamera.WorldToViewportPoint(corner);
                Require(screen.x>0 && screen.x<1 && screen.y>0 && screen.y<1,"Adaptive hand is clipped");
                right=Mathf.Max(right,screen.x);
            }
            var melds=root.Find("Seat "+view.you+"/Exposed melds").GetComponentsInChildren<BoxCollider>();
            if(melds.Length==0)return;
            float left=float.MaxValue;
            foreach(var box in melds)foreach(var corner in Corners(box))left=Mathf.Min(left,table.Camera.WorldToViewportPoint(corner).x);
            Require(right+8f/Screen.width<left,"Concealed hand covers an exposed meld: "+view.players[view.you].melds.Length);
        }
        static void ValidateNorth(TableRenderer table,RoomView view)
        {
            var root=GameObject.Find("PublicTableState").transform;
            for(int seat=0;seat<view.n;seat++)
            {
                var north=root.Find("Seat "+seat+"/Extracted North");
                Require(north.childCount==view.players[seat].nuki,"North tile count changed");
                foreach(Transform tile in north)
                {
                    var inward=Vector3.ProjectOnPlane(-tile.position,Vector3.up).normalized;
                    Require(Vector3.Dot(tile.forward,inward)>.45f,"Extracted North glyph faces away from table");
                    Require(Mathf.Abs(tile.localPosition.x-(seat==view.you?-3.35f:2.0f))<.001f,"North tiles do not stack along the seat edge");
                }
            }
        }
        static void CaptureHub(string file)
        {
            var camera = new GameObject("Score hub validation camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 3, -1.4f); camera.transform.LookAt(new Vector3(0,.092f,0));
            camera.orthographic = true; camera.orthographicSize = 1.14f; camera.cullingMask = ~(1 << 12 | 1 << 13);
            var active = RenderTexture.active; var target = new RenderTexture(900,800,24); var pixels = new Texture2D(900,800,TextureFormat.RGB24,false);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,900,800),0,0); pixels.Apply(); File.WriteAllBytes(file,pixels.EncodeToPNG());
            RenderTexture.active = active; target.Release(); Destroy(target); Destroy(pixels); camera.gameObject.SetActive(false); Destroy(camera.gameObject);
        }
        static System.Collections.Generic.IEnumerable<Vector3> Corners(BoxCollider collider)
        {
            foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                yield return collider.transform.TransformPoint(collider.center + Vector3.Scale(collider.size / 2, new Vector3(x, y, z)));
        }
        static void SetGameResolution(int width, int height)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(type);
            var method = type.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(method != null, "Editor GameView resolution API unavailable");
            method.Invoke(window, new object[] { new Vector2(width, height), "Riichi layout validation" });
            window.Repaint();
        }
        static void Capture(TableRenderer table, string file, int w, int h)
        {
            Camera camera = table.Camera;
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(w, h, 24); var pixels = new Texture2D(w, h, TextureFormat.RGB24, false);
            camera.targetTexture = target; table.HandCamera.targetTexture = target; table.FitCamera(w / (float)h); camera.Render(); table.HandCamera.Render(); table.GetComponent<TableEffects>().RenderOverlay(target); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, w, h), 0, 0); pixels.Apply(); File.WriteAllBytes(file, pixels.EncodeToPNG());
            camera.targetTexture = previous; table.HandCamera.targetTexture = null; RenderTexture.active = active; target.Release(); Destroy(target); Destroy(pixels);
        }
        static void Require(bool condition, string text) { if (!condition) throw new Exception(text); }
    }
}
#endif
