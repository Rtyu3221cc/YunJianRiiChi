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
            File.WriteAllText(Path.Combine(folder, "passed.txt"), "Actual Unity Play mode: live AI room, delayed second-click discard, raycast, 3/4-seat rendering, uniform UI scale and safe-area pointer roundtrips, 16:9/phone-wide layout, 8 role clips. Fixture screenshots are synthetic layout scenes, not replays.");
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
        }
        static void ValidatePresentation(TableRenderer table, RoomView view)
        {
            var publicRoot = GameObject.Find("PublicTableState").transform;
            for (int i = 0; i < view.n; i++)
            {
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
            camera.targetTexture = target; table.HandCamera.targetTexture = target; table.FitCamera(w / (float)h); camera.Render(); table.HandCamera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, w, h), 0, 0); pixels.Apply(); File.WriteAllBytes(file, pixels.EncodeToPNG());
            camera.targetTexture = previous; table.HandCamera.targetTexture = null; RenderTexture.active = active; target.Release(); Destroy(target); Destroy(pixels);
        }
        static void Require(bool condition, string text) { if (!condition) throw new Exception(text); }
    }
}
#endif
