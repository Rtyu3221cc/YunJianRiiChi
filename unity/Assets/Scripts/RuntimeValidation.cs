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
            var run = Environment.GetEnvironmentVariable("RIICHI_REVIEW_0534")=="1"?ReviewValidation0534.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_REVIEW_0533")=="1"?ReviewValidation0533.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_REVIEW_0532")=="1"?ReviewValidation0532.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_REVIEW_0531")=="1"?ReviewValidation0531.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_REVIEW_1012C")=="1"?ReviewValidation1012c.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_NORTH_ONLY")=="1"?ValidateNorthEffects(GetComponent<RiichiClient>()):Environment.GetEnvironmentVariable("RIICHI_LOOK_1012B")=="1"?ReviewValidation1012b.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_FIDELITY_0526")=="1"?ReviewValidation0526.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_FIDELITY_0525")=="1"?ReviewValidation0525.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_FIDELITY_0524")=="1"?ReviewValidation0524.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_REVIEW_PREVIEW")=="1"?ReviewValidation0523.Run(GetComponent<RiichiClient>(),folder):Environment.GetEnvironmentVariable("RIICHI_NATIVE_PREVIEW")=="1"?NativeFirePreview.Run(GetComponent<RiichiClient>(),folder):Run();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception e) { Debug.LogException(e); File.WriteAllText(Path.Combine(folder, "failed.txt"), e.ToString()); EditorApplication.Exit(1); yield break; }
                if (!next) break;
                yield return run.Current;
            }
            File.WriteAllText(Path.Combine(folder, "passed.txt"), "Actual Unity Play mode: live AI room, delayed second-click discard, raycast, 3/4-seat rendering, safe-area pointer roundtrips, 4 resolutions, 8 role clips, called-river removal, grouped chi choices, all 4 discard hand directions at 15 time samples with opaque materials, call cut-ins, event dedup/reconnect; original 224-vertex outlines, model-foot response glow in 4 directions, 0-4 own melds without hand overlap at 4 resolutions, and 4 inward-facing North tiles per seat. Original turn-light particles: four directions, rendered fade/peak/fade, one-second looping and stable phase across polling. North extraction: all three viewer seats/directions, original cut-in, carried tile/landing match, independent previous North and meld visibility, no petal burst on landing (sakura belongs to screen taps), survival across polling/hand completion and event dedup/reconnect. Win effects: original ron/tsumo cut-ins, Zimo hand and exact tile landing in all 3/4 directions and rotated viewers; nine-layer fire tornado particles, video flame spiral and rubble with visible camera pixels, delayed settlement, double ron, robbed tile targets and polling/reconnect cleanup. Fixture screenshots are synthetic layout scenes, not replays.");
            if(Environment.GetEnvironmentVariable("RIICHI_REVIEW_0531")=="1")File.WriteAllText(Path.Combine(folder,"passed.txt"),"Actual Unity Play mode 0.5.31 targeted check: original-reference meld size, same-scene before/after, unchanged foreground hand size, 1..4 melds at three resolutions without hand/portrait overlap or clipping, titles closer to player names in three/four-seat introductions. Screenshots are actual rendering of synthetic test scenes.");
            if(Environment.GetEnvironmentVariable("RIICHI_REVIEW_0532")=="1")File.WriteAllText(Path.Combine(folder,"passed.txt"),"Actual Unity Play 0.5.32: no duplicate own win hands, losing hand retained, original APK cutin/winlose audio once with polling dedup, own meld down 7px without resizing. Synthetic render fixtures.");
            if(Environment.GetEnvironmentVariable("RIICHI_REVIEW_0533")=="1")File.WriteAllText(Path.Combine(folder,"passed.txt"),"Actual Unity Play 0.5.33: reproduced asymmetric safe-area hand offset; 13/14 tiles centered on cloth/hub axis in 3/4-seat games at four resolutions without resizing; own meld down exactly 8px from 0.5.32 with unchanged visible width at three landscape resolutions, narrow viewport overlap fallback retained; 1..4 melds without hand/portrait overlap or clipping. Synthetic render fixtures.");
            if(Environment.GetEnvironmentVariable("RIICHI_REVIEW_0534")=="1")File.WriteAllText(Path.Combine(folder,"passed.txt"),"Actual Unity Play 0.5.34: queued opponent discard and polling do not dim own hand; own draw reveal finishes before locked tiles dim, drawn s4 bright/pickable; kan/North colours and unique legacy drawn action verified; 0/1/2 riichi pot score breakdown with loser -12000 and winner final 30500. Synthetic rendering fixtures.");
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
            Require(Resources.Load<Font>("Majsoul/Settlement/score-font")!=null && Resources.Load<Font>("Majsoul/Settlement/round-font-chs")!=null,"Native settlement fonts missing");
            foreach(var sprite in new[]{"pg_he","pg_zimo","s_1","s_2","s_3","s_4","s_1_1","s_2_2","s_3_3","s_4_4","Btn_common","bg_head1","flame-detail"})Require(Resources.Load<Texture2D>("Majsoul/Settlement/"+sprite)!=null,"Native settlement art missing: "+sprite);
            var client = GetComponent<RiichiClient>(); var table = GetComponent<TableRenderer>();
            client.BeginValidationPractice(File.ReadAllText(Path.Combine(folder, "host.txt")).Trim());
            float deadline = Time.realtimeSinceStartup + 35;
            while ((client.State == null || client.State.phase != "play" || client.Busy || !client.State.options.Any(x => x.kind == "discard")) && Time.realtimeSinceStartup < deadline){
                // Random opening draws can offer a legal pon/ron before our first
                // discard. Resolve that test-player response rather than timing out.
                if(!client.Busy && client.State?.phase=="reaction" && client.State.options.Any(x=>x.kind=="pass"))client.Act("pass");
                yield return null;
            }
            Require(client.State != null && client.State.phase == "play" && !client.Busy && client.State.options.Any(x => x.kind == "discard"), "Live practice failed to reach the human discard turn");
            while(!client.OptionsReady)yield return null;
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
            var reviewProbe=ReviewValidation0523.Run(client,folder);while(reviewProbe.MoveNext())yield return reviewProbe.Current;
            var review0524Probe=ReviewValidation0524.Run(client,folder);while(review0524Probe.MoveNext())yield return review0524Probe.Current;
            var nativeProbe=NativePresentationValidation.Run(client,folder);while(nativeProbe.MoveNext())yield return nativeProbe.Current;
            var openingProbe=ValidateOpeningAudio(client);while(openingProbe.MoveNext())yield return openingProbe.Current;
            var latestProbe=ValidateLatestFixes(client);while(latestProbe.MoveNext())yield return latestProbe.Current;
            var indicatorProbe=ValidateIndicators(client);while(indicatorProbe.MoveNext())yield return indicatorProbe.Current;
            var winProbe = ValidateWins(client);
            while(winProbe.MoveNext())yield return winProbe.Current;
            var northProbe = ValidateNorthEffects(client);
            while (northProbe.MoveNext()) yield return northProbe.Current;
            var turnProbe = ValidateTurnLight(client);
            while (turnProbe.MoveNext()) yield return turnProbe.Current;
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
            // The marker belongs to this client's own legal calls: a lone pass, or no
            // option at all, means another seat is deciding and nothing is highlighted.
            var ownOptions=effectFixture.options;
            effectFixture.options=new[]{new Choice {id="pass",kind="pass"}};
            client.ShowValidationFixture(effectFixture);yield return null;Physics.SyncTransforms();
            Require(table.ResponseTarget==null,"Response marker shown when only pass is available");
            effectFixture.options=new Choice[0];
            client.ShowValidationFixture(effectFixture);yield return null;Physics.SyncTransforms();
            Require(table.ResponseTarget==null,"Response marker shown with no local option");
            effectFixture.options=ownOptions;
            client.ShowValidationFixture(effectFixture);yield return null;Physics.SyncTransforms();
            Require(table.ResponseTarget!=null,"Response marker missing for a legal local call");
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
                Require(fx.ActiveSeat==seat && GameObject.Find("Original hand seat "+seat)!=null,"Missing directional discard hand");
                var handRenderer=GameObject.Find("Original hand seat "+seat).GetComponentInChildren<MeshRenderer>();
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
            // The riichi declaration uses the longer public LiQiPai clip, lies the
            // carried tile sideways and only swaps in the river tile when it settles.
            // A declaration discard is marked in the river and therefore lies sideways there;
            // the carried tile copies its river tile, so mark this fixture realistically too.
            var declaration=effectFixture.players[0].river.Last();declaration.riichi=true;
            effectFixture.last=new LastDiscard {from=0,tile=declaration.tile};
            client.ShowValidationFixture(effectFixture);yield return null;Physics.SyncTransforms();
            fx.ValidationPlay(0,"discard",effectFixture.players[0].river.Last().tile,true);
            yield return null;yield return null;
            Require(fx.ValidationRiichiDiscard,"Riichi discard did not select the declaration clip");
            Require(Mathf.Abs(fx.ValidationPlayLength-1.70f)<.01f,"Riichi clip length is not the source length");
            Require(Mathf.Abs(fx.ValidationHandoff-1.2667f)<.01f,"Riichi tile hand-off is not the settle time");
            var carried=fx.ValidationAnimatedTile;
            Require(carried!=null,"Riichi discard has no carried tile");
            // The carried declaration tile copies its river tile exactly, including that
            // tile's stable yaw jitter (RiverScatter), so the hand-off cannot snap.
            var carriedTarget=fx.ValidationTarget;
            Require(carriedTarget!=null && Mathf.Abs(Mathf.DeltaAngle(carried.eulerAngles.y,carriedTarget.eulerAngles.y))<.5f,"Riichi carried tile does not match its river tile");
            Require(Mathf.Abs(Mathf.DeltaAngle(carried.eulerAngles.y,table.SeatAngle(0)+90f))<=2.5f,"Riichi carried tile is not sideways");
            for(int sample=0;sample<14;sample++)
            {
                fx.ValidationTime=Mathf.Min(sample*fx.ValidationPlayLength/14,fx.ValidationPlayLength-.03f);yield return null;yield return null;
                Capture(table,Path.Combine(folder,"riichi-hand-"+sample.ToString("D2")+".png"),1688,780);
            }
            fx.ValidationTime=fx.ValidationHandoff+.05f;yield return null;yield return null;
            Require(fx.ValidationAnimatedTile!=null && !fx.ValidationAnimatedTile.gameObject.activeSelf,"Carried riichi tile survived its hand-off");
            // A meld plays the Fulu hand motion and reveals the newest meld at its settle time.
            fx.ValidationPlay(1,"pon");
            yield return null;yield return null;
            Require(fx.ValidationKind=="pon" && GameObject.Find("Original hand seat 1")!=null,"Missing meld hand animation");
            Require(Mathf.Abs(fx.ValidationPlayLength-.90f)<.01f && Mathf.Abs(fx.ValidationHandoff-.70f)<.01f,"Meld clip timing is wrong");
            Capture(table,Path.Combine(folder,"meld-hand.png"),1688,780);
            // Placing the riichi stick is its own motion after the declaration discard.
            var stickFixture=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"effects.json")));
            stickFixture.phase="play";stickFixture.pending=null;stickFixture.players[0].riichi=1;
            stickFixture.last=new LastDiscard {from=0,tile=stickFixture.players[0].river.Last().tile};
            client.ShowValidationFixture(stickFixture);yield return null;Physics.SyncTransforms();
            Require(table.StickTarget(0)!=null,"Deposited riichi stick has no anchor");
            fx.ValidationPlay(0,"riichi-stick");
            yield return null;yield return null;
            Require(fx.ValidationKind=="riichi-stick","Stick placement did not start");
            Require(Mathf.Abs(fx.ValidationPlayLength-.80f)<.01f && Mathf.Abs(fx.ValidationHandoff-.50f)<.01f,"Stick clip timing is wrong");
            var carriedStick=fx.ValidationAnimatedTile;
            Require(carriedStick!=null && carriedStick.Find("Riichi 1000")!=null,"Carried riichi stick is missing");
            Require(!table.StickTarget(0).gameObject.activeSelf,"Deposited stick was not hidden during placement");
            for(int sample=0;sample<10;sample++)
            {
                fx.ValidationTime=Mathf.Min(sample*fx.ValidationPlayLength/10,fx.ValidationPlayLength-.03f);yield return null;yield return null;
                Capture(table,Path.Combine(folder,"riichi-stick-"+sample.ToString("D2")+".png"),1688,780);
            }
            fx.ValidationTime=fx.ValidationHandoff+.05f;yield return null;yield return null;
            Require(table.StickTarget(0).gameObject.activeSelf,"Deposited stick was not revealed after placement");
            // Riichi mode dims exactly the tiles that would break tenpai.
            var riichiFixture=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"effects.json")));
            riichiFixture.phase="play";riichiFixture.pending=null;riichiFixture.turn=riichiFixture.you;
            string keep=riichiFixture.hand[riichiFixture.hand.Length-1];riichiFixture.drawn=keep;
            riichiFixture.options=new[]{new Choice{id="riichi:"+keep+"_",kind="riichi",tile=keep},new Choice{id="discard:"+keep+"_",kind="discard",tile=keep}};
            client.ShowValidationFixture(riichiFixture);yield return null;
            client.ValidationToggleRiichi(true);yield return null;
            int dimmed=0;
            for(int i=0;i<table.ValidationHandCount;i++)
            {
                bool playable=i==table.ValidationHandCount-1;
                if(playable)Require(table.ValidationHandDim(i)==0f,"A discardable riichi tile was dimmed");
                else { Require(table.ValidationHandDim(i)==1f,"An unplayable riichi tile was not dimmed"); dimmed++; }
            }
            Require(dimmed>0,"The riichi fixture dimmed no tile");
            Capture(table,Path.Combine(folder,"riichi-dimmed.png"),1688,780);
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
                // 0.5.25 groups each meld under "Meld n"; the tiles are the group's children.
                foreach(Transform group in melds)foreach(Transform piece in group)Require(piece.localPosition.z <= -3.5f,"Melds are still inside concealed hand row");
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
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            foreach(var box in hand)foreach(var corner in Corners(box))
            {
                var screen=table.HandCamera.WorldToScreenPoint(corner);
                Require(screen.x>0 && screen.x<Screen.width && screen.y>0 && screen.y<Screen.height,"Adaptive hand is clipped");
                min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);
            }
            var handBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            // Layout must preserve readability, including when several edge melds exist.
            Require(handBounds.height>=Screen.safeArea.height*OriginalTiles.HandHeight/18*.98f,"Meld avoidance shrank the foreground hand");
            foreach(Transform seat in root)foreach(Transform group in seat){
                if(group.name!="Exposed melds" && group.name!="Extracted North")continue;
                foreach(var box in group.GetComponentsInChildren<BoxCollider>()){
                    min=new Vector2(float.MaxValue,float.MaxValue);max=new Vector2(float.MinValue,float.MinValue);
                    foreach(var corner in Corners(box)){var p=table.Camera.WorldToScreenPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
                    var bounds=Rect.MinMaxRect(min.x-4,min.y-4,max.x+4,max.y+4);
                    Require(!handBounds.Overlaps(bounds),"Hand covers visible "+seat.name+"/"+group.name);
                }
            }
        }
        static void ValidateNorth(TableRenderer table,RoomView view)
        {
            var root=GameObject.Find("PublicTableState").transform;
            for(int seat=0;seat<view.n;seat++)
            {
                var north=root.Find("Seat "+seat+"/Extracted North");
                Require(north.childCount==view.players[seat].nuki,"North tile count changed");
                int physical=NativeReviewLayout.Physical(table.RelativeSeat(seat),view.n);
                for(int index=0;index<north.childCount;index++)
                {
                    var tile=north.GetChild(index);
                    var inward=Vector3.ProjectOnPlane(-tile.position,Vector3.up).normalized;
                    Require(Vector3.Dot(tile.forward,inward)>.45f,"Extracted North glyph faces away from table");
                    // 0.5.25 replaced the fixed -2.95 row with the scene_mj babei_n anchors,
                    // one 0.0355-unit span apart, facing the owner like a discard.
                    var expected=NativeReviewLayout.NorthLocal(physical,index,table.SeatAngle(seat));
                    Require(Vector3.Distance(tile.localPosition,expected)<.001f && Mathf.Abs(Mathf.DeltaAngle(tile.localRotation.eulerAngles.y,0))<.01f,"North tiles are not on the APK seat anchor");
                }
            }
        }
        IEnumerator ValidateLatestFixes(RiichiClient client)
        {
            var table=GetComponent<TableRenderer>();var fx=GetComponent<TableEffects>();SetGameResolution(1688,780);
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"effects.json")));
            view.phase="play";view.pending=null;view.last=null;view.hand=new[]{"m1","m3","p0","p5","p5","p6","p7","p8","s1","s2","s3","z4","z4"};view.indicators=new[]{"p5","z4"};view.drawn=null;
            view.players[0].melds=new[]{"p5555","p555+"};view.players[0].nuki=1;
            view.players[1].river=new[]{new RiverTile{tile="p5"},new RiverTile{tile="p0",called=true},new RiverTile{tile="z4"}};
            view.options=view.hand.Select(t=>new Choice{id="discard:"+t,kind="discard",tile=t}).ToArray();
            view.players[3].melds=new[]{"s123-","z4444"};
            client.ShowValidationFixture(view);yield return null;client.SelectTile(2,"discard:p0");
            for(int frame=0;frame<8;frame++)yield return null;
            ValidateHandMeldGap(table,view);
            // An animation hides its static target; layout must retain the landing slot.
            var movingMeld=table.MeldTarget(view.you);var landing=movingMeld.position;
            movingMeld.gameObject.SetActive(false);table.FitCamera(Screen.width/(float)Screen.height);
            Require(Vector3.Distance(movingMeld.position,landing)<.001f,"Hiding an animated meld moved its landing slot");
            movingMeld.gameObject.SetActive(true);table.FitCamera(Screen.width/(float)Screen.height);
            foreach(var tile in GameObject.Find("PublicTableState").GetComponentsInChildren<MeshRenderer>()){
                if(tile.name!="Original tile" || !OriginalTiles.IsMatchedModel(tile.transform.parent))continue;
                Require(tile.sharedMaterials.All(m=>m.HasProperty("_Match")),"Blue tint is missing a tile model material");
            }
            Require(table.IsMatchingTile("p5") && table.IsMatchingTile("p0") && !table.IsMatchingTile("s5"),"Red/normal five matching differs");
            Require(table.ValidationMatchCount>=6 && table.ValidationMatchesOnlyFaces,"Same-tile highlight missing or includes a concealed kan back");
            Require(GameObject.Find("Same tile ground glow")==null,"Matching tiles still use a yellow ground glow");
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"matching-tiles-1688.png"));yield return new WaitForSecondsRealtime(.1f);
            table.Render(view);Require(table.IsMatchingTile("p5"),"Polling lost selected match highlight");
            Debug.Log("RIICHI BLUE MATCH PASS: whole-model original blue tint, no matching glow or hand tint, red-five equivalence and polling selection retained.");
            table.SelectHand(-1);Require(table.ValidationMatchCount==0,"Clearing selection leaves highlights behind");
            client.ShowValidationFixture(view);fx.ValidationFreeze=true;fx.ValidationTime=.1f;view.audioRoom="discard-synchronous";view.audioSeq=10;view.audioEvents=new PublicSound[0];fx.Accept(view);
            view.players[1].river=new[]{new RiverTile{tile="p5"},new RiverTile{tile="p5"}};table.Render(view);
            view.audioSeq=12;view.audioEvents=new[]{new PublicSound{id=11,seat=1,kind="discard",tile="p5",riverIndex=1},new PublicSound{id=12,seat=1,kind="discard",tile="p5",riverIndex=2}};fx.Accept(view);
            Require(!table.FindDiscard(1,"p5",1).gameObject.activeSelf && !table.FindDiscard(1,"p5",2).gameObject.activeSelf,"Committed discard flashes before Update/queued hand");
            yield return null;table.Render(view);fx.Accept(view);
            Require(!table.FindDiscard(1,"p5",1).gameObject.activeSelf && !table.FindDiscard(1,"p5",2).gameObject.activeSelf,"Polling reveals queued discard");
            fx.ValidationTime=.41f;yield return null;Require(table.FindDiscard(1,"p5",1).gameObject.activeSelf && !table.FindDiscard(1,"p5",2).gameObject.activeSelf,"River hand-off uses the wrong identical tile");
            fx.ValidationTime=.63f;yield return null;fx.ValidationTime=.1f;yield return null;yield return null;
            fx.ValidationTime=.41f;yield return null;Require(table.FindDiscard(1,"p5",2).gameObject.activeSelf,"Queued discard never lands");fx.Reset();
            foreach(int n in new[]{3,4})for(int seat=0;seat<n;seat++){
                view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"nine-"+n+"-"+seat+".json")));client.ShowValidationFixture(view);
                var display=client.ValidationResult;display.Observe(view);display.Freeze=true;display.FrozenAge=2;yield return new WaitForSecondsRealtime(.6f);yield return null;
                Require(display.ShowingDraw && TableIndicators.Art("jiuzhongjiupai")!=null,"Nine terminals skips native declaration page");
                var hand=GameObject.Find("Nine terminals hand seat "+seat).transform;
                Require(hand.childCount==14 && hand.GetChild(0).Find("Original tile").localRotation==Quaternion.identity,"Abort declaring hand is hidden or inverted");
                Require(FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Nine terminals hand seat"))==1,"Abort reveals another player's hand");
                if(seat==0){ScreenCapture.CaptureScreenshot(Path.Combine(folder,"nine-"+n+"-1688.png"));yield return new WaitForSecondsRealtime(.1f);}
                table.Render(view);display.Observe(view);Require(display.ShowingDraw,"Polling skips nine terminals page");display.Advance(view);Require(display.ShowingScores,"Nine terminals confirmation cannot advance");
            }
            Debug.Log("RIICHI LATEST FIXES PASS: visible-only red-five matching; synchronous and queued river hiding with exact public slots; original nine-terminals declaration and public declaring hand.");
        }
        IEnumerator ValidateIndicators(RiichiClient client)
        {
            var table=GetComponent<TableRenderer>();client.ValidationToggleRiichi(false);
            foreach(string name in new[]{"pic_zhengting","nofan","huangpailiuju","show_tingpai","noting","bg_chipengchoose"})
                Require(TableIndicators.Art(name)!=null,"Native indicator missing: "+name);
            foreach(Vector2 size in new[]{new Vector2(1280,720),new Vector2(1688,780),new Vector2(2760,1256),new Vector2(844,390)}){
                SetGameResolution((int)size.x,(int)size.y);
                var hint=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"hint-open.json")));
                client.ShowValidationFixture(hint);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==0 && client.CurrentWaitPanel.width==0,"Unselected hand shows persistent waits");
                var tile=FindObjectsByType<TileTarget>().First(t=>t.Action.StartsWith("discard:p9"));client.SelectTile(tile.Index,tile.Action);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==2 && client.CurrentWaitDetails.All(w=>!w.ron&&!w.tsumo),"Open no-yaku waits not connected to selected tile");
                Require(client.CurrentWaitPanel.width>100 && client.CurrentWaitPanel.y>=0 && client.CurrentWaitPanel.xMax<=1280,"Wait panel clips at display size");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"indicator-open-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                var other=FindObjectsByType<TileTarget>().First(t=>t.Action=="discard:p2");client.SelectTile(other.Index,other.Action);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==0 && client.CurrentWaitPanel.width==0,"Non-tenpai discard shows the original hand's waits");
                hint=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"hint-furiten.json")));client.ShowValidationFixture(hint);yield return null;yield return null;
                Require(client.CurrentFuriten,"Furiten state missing after draw");
                var badge=client.CurrentFuritenPanel;var nativeBadge=TableIndicators.Art("pic_zhengting");
                Require(badge.height>=55 && Mathf.Abs(badge.width/badge.height-(float)nativeBadge.width/nativeBadge.height)<.01f,"Furiten brush glyph is shrunk or distorted");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"indicator-furiten-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            foreach(Vector2 size in new[]{new Vector2(1688,780),new Vector2(844,390)}){
                SetGameResolution((int)size.x,(int)size.y);
                var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"hint-thirteen.json")));client.ShowValidationFixture(view);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==0 && client.CurrentWaitPanel.width==0,"Already tenpai waits should require holding the button");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"indicator-wait-idle-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                Vector2 press=client.CurrentWaitButton.center,off=press+new Vector2(-220,-90);
                client.ValidationWaitPointer(press,true,true,false,100);client.ValidationWaitPointer(press,false,true,false,100.2f);yield return null;yield return null;
                Require(client.CurrentWaitPanel.width==0,"Short press displays waits before the hold threshold");
                client.ValidationWaitPointer(press,false,false,true,100.21f);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==0 && client.CurrentWaitPanel.width==0,"Short tap latched the wait panel on");
                client.ValidationWaitPointer(press,true,true,false,101);client.ValidationWaitPointer(press,false,true,false,101.4f);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==13 && client.CurrentWaitPanel.height>250,"Held thirteen waits clipped or dropped");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"indicator-thirteen-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                Require(client.ValidationWaitPointer(off,false,false,true,101.5f),"Release outside the inspection button was not consumed");yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==0 && client.CurrentWaitPanel.width==0,"Released hold kept waits visible");
                client.ValidationWaitPointer(press,true,true,false,102);client.ValidationWaitPointer(press,false,true,false,102.4f);yield return null;yield return null;
                Require(client.CurrentWaitDetails.Length==13,"Fresh hold does not show waits");
                client.ValidationWaitPointer(off,false,true,false,102.5f);yield return null;yield return null;
                Require(client.CurrentWaitPanel.width==0,"Dragging off the button kept waits visible");
                client.ValidationWaitPointer(press,false,true,false,102.6f);yield return null;yield return null;
                Require(client.CurrentWaitPanel.width==0,"Cancelled press resumed on re-entering the button");
                client.ValidationWaitPointer(press,false,false,true,102.7f);
                client.ValidationWaitPointer(press,true,true,false,103);client.ValidationWaitPointer(press,false,true,false,103.4f);yield return null;yield return null;
                client.SendMessage("OnApplicationFocus",false);yield return null;yield return null;
                Require(client.CurrentWaitPanel.width==0,"Lost focus kept held waits visible");client.ValidationEndWaitPointer();
                view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"effects.json")));client.ShowValidationFixture(view);yield return null;yield return null;
                Require(client.ValidationChoosingKind==null && view.options.Count(o=>o.kind=="chi")==2,"Chi preview requires an extra selection");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"indicator-chi-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Debug.Log("RIICHI WAIT INPUT PASS: selected legal tenpai discard preview; idle/non-tenpai hidden; short tap ignored; held current waits; release/drag-off/focus cancel; captured release and scaled button hit tests.");
            SetGameResolution(1688,780);
            var closed=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"hint-closed.json")));client.ShowValidationFixture(closed);client.ValidationToggleRiichi(true);yield return null;
            var riichiTile=FindObjectsByType<TileTarget>().First(t=>t.Action.StartsWith("riichi:p9"));client.SelectTile(riichiTile.Index,riichiTile.Action);yield return null;yield return null;
            Require(client.CurrentWaitDetails.Length==2 && client.CurrentWaitDetails.All(w=>w.ron),"Riichi waits incorrectly show no-yaku");
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"indicator-riichi-1688.png"));yield return new WaitForSecondsRealtime(.08f);client.ValidationToggleRiichi(false);
            foreach(int n in new[]{3,4})for(int k=0;k<=n;k++){
                var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"draw-"+n+"-"+k+".json")));
                client.ShowValidationFixture(view);var display=client.ValidationResult;display.Observe(view);display.Freeze=true;display.FrozenAge=.2f;
                yield return new WaitForSecondsRealtime(.15f);
                if(n==4 && k==2){
                    var first=GameObject.Find("Draw hand seat 0").transform.GetChild(0);
                    Require(Quaternion.Angle(first.localRotation,Quaternion.identity)>10,"Draw hand never flips down");
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"draw-flip.png"));yield return new WaitForSecondsRealtime(.08f);
                }
                yield return new WaitForSecondsRealtime(.55f);display.FrozenAge=2;yield return null;yield return null;
                Require(display.ShowingDraw&&!display.ShowingScores,"Exhaustive draw skips tenpai reveal");
                for(int seat=0;seat<n;seat++){
                    var row=GameObject.Find("Draw hand seat "+seat).transform;var d=view.result.drawDetails[seat];
                    Require(row.childCount==13 && row.GetChild(0).name== (d.tenpai?d.tiles[0]:"Concealed"),"Draw reveals noten/private hand or loses tiles");
                }
                table.Render(view);yield return null;Require(Quaternion.Angle(GameObject.Find("Draw hand seat 0").transform.GetChild(0).localRotation,Quaternion.identity)<.1f,"Polling restarts draw flip");
                display.Observe(view);Require(display.ShowingDraw,"Polling skipped draw reveal");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"draw-reveal-"+n+"-"+k+".png"));yield return new WaitForSecondsRealtime(.08f);
                display.Advance(view);Require(display.ShowingScores&&!display.ShowingDraw,"Draw confirmation does not advance to noten points");
                display.FrozenAge=3.5f;for(int seat=0;seat<n;seat++)Require(display.DisplayScore(view,seat)==view.players[seat].score,"Noten points settle to wrong total");
            }
            foreach(Vector2 size in new[]{new Vector2(1280,720),new Vector2(1688,780),new Vector2(2760,1256),new Vector2(844,390)})foreach(int n in new[]{3,4}){
                SetGameResolution((int)size.x,(int)size.y);int k=n==3?1:3;
                var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"draw-"+n+"-"+k+".json")));client.ShowValidationFixture(view);
                var display=client.ValidationResult;display.Observe(view);display.Freeze=true;display.FrozenAge=2;yield return new WaitForSecondsRealtime(.6f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"draw-"+n+"-reveal-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                display.Advance(view);display.FrozenAge=1.5f;yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"draw-"+n+"-scores-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Debug.Log("RIICHI INDICATORS PASS: automatic chi mini tiles; selected discard/no-yaku/riichi waits, 13-sided waits and furiten; 3/4-seat exhaustive draw with every tenpai count, private backs, reveal flip/polling, both settlement stages and four display sizes.");
        }
        IEnumerator ValidateWins(RiichiClient client)
        {
            var table=GetComponent<TableRenderer>();var fx=GetComponent<TableEffects>();
            SetGameResolution(1688,780);
            foreach(int n in new[]{3,4})foreach(string kind in new[]{"ron","tsumo"})
            for(int you=0;you<2;you++)for(int relative=0;relative<n;relative++)
            {
                int winner=(you+relative)%n;
                var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-"+n+"-"+kind+"-"+winner+"-"+you+".json")));
                var events=view.audioEvents;int seq=view.audioSeq;view.audioRoom="win-"+n+kind+you+winner;
                view.audioEvents=new PublicSound[0];view.audioSeq=seq-1;client.ShowValidationFixture(view);yield return null;
                fx.ValidationFreeze=true;fx.ValidationTime=.25f;fx.Accept(view);
                int before=fx.SpawnedWinEffects;view.audioEvents=events;view.audioSeq=seq;fx.Accept(view);
                Require(fx.ShowWinPresentation,"Result displayed before pending win cut-in");
                yield return null;yield return null;
                var e=events.Last(x=>x.kind==kind);var target=table.WinningTarget(e);
                Require(target!=null && target.name.EndsWith(" "+e.tile),"Win animation targets the wrong tile");
                Require(fx.ValidationCallKind==kind && fx.Art(kind=="ron"?"show_rong":"show_zimo")!=null && fx.Art("yiji-full")!=null,"Win cut-in missing or character changed");
                if(you==0){ScreenCapture.CaptureScreenshot(Path.Combine(folder,"win-"+n+"-"+kind+"-call-"+relative+".png"));yield return new WaitForSecondsRealtime(.1f);}
                fx.ValidationTime=1.05f;yield return null;yield return null;
                Require(fx.CutIn.Active,"APK win stripe missing after the shout");
                fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=WinCutIn.Duration+.01f;fx.ValidationTime=0;yield return null;yield return null;
                if(kind=="tsumo")
                {
                    Require(fx.ValidationKind==kind && fx.ValidationAnimatedTile.name==e.tile && !target.gameObject.activeSelf,"Self draw hand did not carry the exact winning tile");
                    Require(fx.ValidationWin==null,"Self draw effect starts before tile landing");
                    foreach(float time in new[]{0f,.25f,.6f,.9f,1.13f}){
                        fx.ValidationTime=time;yield return null;
                        Require(Vector3.Dot(fx.ValidationAnimatedTile.up,Vector3.up)>.99f,"Self-draw carried tile flips face down during slap");
                    }
                    fx.ValidationTime=.6f;yield return null;yield return null;
                    var hand=GameObject.Find("Original hand seat "+winner).GetComponentInChildren<MeshRenderer>();
                    Capture(table,Path.Combine(folder,"win-hand-probe-"+n+"-"+you+"-"+relative+".png"),1688,780);
                    Require(VisibleEffectPixels(table,hand)>30,"Original Zimo hand is invisible: "+n+"/"+you+"/"+relative);
                    if(you==0)Capture(table,Path.Combine(folder,"win-"+n+"-tsumo-hand-"+relative+".png"),1688,780);
                    table.Render(view);fx.Accept(view);yield return null;
                    Require(!table.WinningTarget(e).gameObject.activeSelf,"Polling duplicated the carried self draw tile");
                    fx.ValidationTime=fx.ValidationHandoff;yield return null;yield return null;
                    Require(Vector3.Distance(fx.ValidationAnimatedTile.position,table.WinningPoint(e))<.002f,"Self draw tile snaps at landing");
                    Require(!fx.ValidationAnimatedTile.gameObject.activeSelf && table.WinningTarget(e).gameObject.activeSelf,"Self draw tile hand-off failed");
                    fx.ValidationTime=1.55f;yield return null;yield return null;
                    Require(fx.ValidationKind==null,"Zimo hand/face-down flip did not complete");
                }
                else {Require(fx.ValidationKind==null,"Ron incorrectly moved the winner's hand");fx.ValidationTime=WinCutIn.Duration+.73f;yield return null;yield return null;}
                var effect=fx.ValidationWin;
                Require(effect!=null && effect.LayerCount==WinTileEffect.SourceLayerCount && effect.ParticleCount>0 && fx.SpawnedWinEffects==before+1,"Original nine-layer fire tornado burst missing/duplicated");
                Require(Vector3.Distance(effect.transform.position,table.WinningPoint(e)+Vector3.up*.045f)<.001f,"Win effect detached from winning tile");
                int pixels=effect.GetComponentsInChildren<ParticleSystemRenderer>().Max(renderer=>VisibleEffectPixels(table,renderer));
                Require(pixels>20,"Fire win particles invisible on the actual camera");
                effect.Sample(.55f); // Measure the peak independently of the call/hand lead-in.
                Require(effect.MeshLayerCount==2 && effect.GetComponent<FlameSpiral>()==null,"Native spiral meshes replaced by synthetic ribbons");
                var column=effect.GetComponentsInChildren<ParticleSystemRenderer>().Where(x=>x.renderMode==ParticleSystemRenderMode.Mesh).ToArray();
                Require(column.Length>=2 && column.Max(x=>VisibleEffectPixels(table,x))>150,"Native flame mesh/ground flash invisible to the actual camera");
                if(you==0){
                    Capture(table,Path.Combine(folder,"win-"+n+"-"+kind+"-effect-"+relative+".png"),1688,780);
                    if(relative==0)foreach(float sample in new[]{.12f,.45f,.9f,1.5f}){
                        effect.Sample(sample);Capture(table,Path.Combine(folder,"win-"+n+"-"+kind+"-flame-"+sample.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"),1688,780);
                    }
                }
                table.Render(view);fx.Accept(view);yield return null;yield return null;
                Require(fx.ValidationWin==effect && fx.SpawnedWinEffects==before+1 && fx.ShowWinPresentation,"Polling restarted win effect or exposed result too early");
                fx.ValidationTime=5f;yield return null;yield return null;
                Require(!fx.ShowWinPresentation && fx.ValidationWin==null,"Win result never becomes available or effect leaked");
                fx.Reset();fx.Accept(view);yield return null;
                Require(!fx.ShowWinPresentation && !fx.IsAnimating,"Reconnect replayed historical win");
            }
            // Double ron shares one discarded tile and one burst; both winners get a cut-in.
            var multi=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-double-ron.json")));
            var calls=multi.audioEvents;int lastId=multi.audioSeq;multi.audioRoom="double-ron";multi.audioSeq=lastId-2;multi.audioEvents=new PublicSound[0];client.ShowValidationFixture(multi);yield return null;
            fx.ValidationTime=.2f;fx.Accept(multi);multi.audioEvents=calls;multi.audioSeq=lastId;fx.Accept(multi);yield return null;
            int shared=fx.SpawnedWinEffects;Require(fx.ValidationCallKind=="ron","First double ron cut-in missing");
            fx.ValidationTime=1.06f;yield return null;fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=WinCutIn.Duration+.01f;fx.ValidationTime=.2f;yield return null;
            Require(fx.ValidationCallKind=="ron" && fx.SpawnedWinEffects==shared+1,"Second winner missing or shared discard flashes twice");
            fx.ValidationTime=1.06f;yield return null;Require(fx.SpawnedWinEffects==shared+1,"Double ron duplicates the shared tile burst");
            fx.Reset();
            // Robbing a kan/nuki must use the public robbed tile, not an older discard.
            foreach(string source in new[]{"kakan","ankan","nuki"})
            {
                multi.result.details=new[]{new WinDetail{seat=1,from=0,tile="z4",ron=true,winSource=source,score=new ScoreDetail(),name="Robbed tile fixture",hand="z4"}};
                client.ShowValidationFixture(multi);yield return null;
                var e=new PublicSound{seat=1,from=0,kind="ron",tile="z4",winSource=source};
                Require(table.WinningTarget(e)!=null && table.WinningTarget(e)!=table.FindDiscard(0,"z4"),"Robbed tile targets an older river tile");
            }
            fx.ValidationFreeze=false;fx.Reset();
            multi=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-double-ron.json")));
            // Real result GUI at four display sizes: rows, full hand, point transfer and both ron winners.
            foreach(Vector2 size in new[]{new Vector2(1280,720),new Vector2(1688,780),new Vector2(2760,1256),new Vector2(844,390)}){
                SetGameResolution((int)size.x,(int)size.y);client.ShowValidationFixture(multi);
                var display=client.ValidationResult;display.Observe(multi);display.Freeze=true;display.FrozenAge=9;
                yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-hand-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                display.Advance(multi);display.FrozenAge=9;yield return null;
                if(multi.result.details.Length>1){Require(display.WinnerIndex==1 && !display.ShowingScores,"Double ron second settlement missing");display.Advance(multi);}
                Require(display.ShowingScores,"Settlement does not advance to score transfer");
                display.FrozenAge=0;for(int i=0;i<multi.n;i++)Require(display.DisplayScore(multi,i)==ResultPresentation.Before(multi,i),"Score transfer starts at wrong total");
                display.FrozenAge=1.5f;yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-transfer-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                display.FrozenAge=3.5f;for(int i=0;i<multi.n;i++)Require(display.DisplayScore(multi,i)==multi.players[i].score,"Score transfer ends at wrong total");
                yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-scores-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
                display.Observe(multi);Require(display.ShowingScores,"Polling restarted result presentation");
            }
            // Exact reference aspect ratios: ordinary and yakuman presentation with Ichihime.
            var reference=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-double-ron.json")));
            reference.result.details=new[]{reference.result.details[0]};
            var referenceDetail=reference.result.details[0];referenceDetail.tiles=new[]{"p3","p3","p5","p5","p5","p5","p8","p8","p8","p4","p4","p4","p4"};
            referenceDetail.tile="p4";referenceDetail.melds=new string[0];referenceDetail.ura=new[]{"p3"};
            referenceDetail.yaku=new[]{new YakuDetail{name="清一色",han=5},new YakuDetail{name="拔北宝牌",han=1}};
            referenceDetail.limit="跳满";referenceDetail.score=new ScoreDetail{fanshu=6,fu=40,defen=12000};reference.indicators=new[]{"z5","s1"};
            SetGameResolution(934,430);client.ShowValidationFixture(reference);
            var referenceDisplay=client.ValidationResult;referenceDisplay.Observe(reference);referenceDisplay.Freeze=true;referenceDisplay.FrozenAge=9;
            yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-reference-934.png"));yield return new WaitForSecondsRealtime(.08f);
            reference.audioRoom="result-reference-yakuman";referenceDetail.tiles=new[]{"z2","z2","z3","z3","z3","z4","z4"};referenceDetail.melds=new[]{"z111+","z777-"};referenceDetail.tile="z4";
            referenceDetail.yaku=new[]{new YakuDetail{name="字一色",yakuman=1},new YakuDetail{name="小四喜",yakuman=1}};
            referenceDetail.limit="2倍役满";referenceDetail.score=new ScoreDetail{damanguan=2,defen=96000};reference.indicators=new[]{"s1"};
            SetGameResolution(774,438);client.ShowValidationFixture(reference);referenceDisplay.Observe(reference);referenceDisplay.Freeze=true;referenceDisplay.FrozenAge=9;
            yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-reference-774.png"));yield return new WaitForSecondsRealtime(.08f);
            Debug.Log("RIICHI HAND RESULT LAYOUT PASS: native hand height, safe public melds, APK win anchors and name slope, consecutive two-meld section with only two breaks, exact 934x430 / 774x438 reference aspects.");
            // Compare the result texture to an actual gameplay hand mesh render.
            var original=new OriginalTiles();var parityHand=original.CreateHand(null,"m9",new Vector3(50,50,50));
            var handProbe=new GameObject("Settlement gameplay parity camera").AddComponent<Camera>();
            handProbe.enabled=false;handProbe.orthographic=true;handProbe.orthographicSize=OriginalTiles.HandHeight/2;
            handProbe.transform.position=new Vector3(50,50,45);handProbe.cullingMask=1<<12;
            handProbe.clearFlags=CameraClearFlags.SolidColor;handProbe.backgroundColor=Color.clear;
            var handTarget=new RenderTexture(124,200,24,RenderTextureFormat.ARGB32);handTarget.Create();
            handProbe.targetTexture=handTarget;handProbe.Render();
            var gameplay=ReadTile(handTarget,"result-gameplay-tile.png");
            var result=ReadTile((RenderTexture)table.HandTexture("m9"),"result-shared-tile.png");
            float error=0;for(int i=0;i<result.Length;i++)error+=Mathf.Abs(gameplay[i].r-result[i].r)+Mathf.Abs(gameplay[i].g-result[i].g)+Mathf.Abs(gameplay[i].b-result[i].b)+Mathf.Abs(gameplay[i].a-result[i].a);
            Require(error/(result.Length*4)<3,"Settlement face/rim differs from gameplay mesh pixels");
            int left=124,right=0,bottom=200,top=0,redPixels=0;
            for(int y=0;y<200;y++)for(int x=0;x<124;x++){
                var c=result[y*124+x];if(c.a>128){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
                if(c.r>c.g*1.5f && c.r>c.b*1.5f && c.a>128)redPixels++;
            }
            float aspect=(right-left+1f)/(top-bottom+1f);Require(aspect>.60f && aspect<.65f && redPixels>100,"Settlement tile compressed or red face color missing");
            var backPixels=ReadTile((RenderTexture)table.HandTexture(null),"result-shared-back.png");
            Require(backPixels.Count(c=>c.a>128 && c.r>c.g*1.3f)>8000,"Concealed kan back lost the selected rose-red skin");
            ReadTile((RenderTexture)table.DoraTexture(null),"result-dora-back.png");
            parityHand.gameObject.SetActive(false);Destroy(parityHand.gameObject);Destroy(handProbe.gameObject);handTarget.Release();Destroy(handTarget);original.Dispose();
            // Full scoring row with hidden kan ends and all five open indicators/ura.
            var backView=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-double-ron.json")));
            backView.audioRoom="result-backs";backView.result.details=new[]{backView.result.details[0]};
            backView.result.details[0].tiles=new[]{"m1","m2","m3","p4","p5","p6","z1"};
            backView.result.details[0].tile="z1";backView.result.details[0].melds=new[]{"s7777","z555+"};
            backView.indicators=new[]{"p1","p2","p3","p4","p5"};backView.result.details[0].ura=new[]{"s1","s2","s3","s4","s5"};
            foreach(Vector2 size in new[]{new Vector2(1688,780),new Vector2(844,390)}){
                SetGameResolution((int)size.x,(int)size.y);client.ShowValidationFixture(backView);
                var backDisplay=client.ValidationResult;backDisplay.Observe(backView);backDisplay.Freeze=true;backDisplay.FrozenAge=9;
                yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-kan-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Debug.Log("RIICHI RESULT TILE PASS: 0.62 silhouette, actual gameplay mesh/result pixel parity, colored face, selected hand back and table dora back, concealed kan and five indicators at two sizes.");
            // Render native limit glyphs and six-digit points, including long multiple yakuman titles.
            SetGameResolution(844,390);
            var glyphView=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-double-ron.json")));
            glyphView.result.details=new[]{glyphView.result.details[0]};
            var limits=new[]{"满贯","跳满","倍满","三倍满","累计役满","役满","6倍役满"};
            for(int i=0;i<limits.Length;i++){
                glyphView.audioRoom="native-glyphs-"+i;var detail=glyphView.result.details[0];detail.limit=limits[i];
                detail.score=new ScoreDetail{fanshu=13,fu=110,defen=624000,damanguan=i>=5?i-4:0};
                client.ShowValidationFixture(glyphView);var glyphDisplay=client.ValidationResult;glyphDisplay.Observe(glyphView);glyphDisplay.Freeze=true;glyphDisplay.FrozenAge=9;
                yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"result-native-limit-"+i+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            // Compare an actual red/green pixel before and after dimming using the production shader.
            var probe=new Texture2D(2,1);probe.SetPixels(new[]{new Color(.9f,.15f,.08f),new Color(.1f,.8f,.16f)});probe.Apply();
            var back=new Texture2D(1,1);back.SetPixel(0,0,Color.clear);back.Apply();
            var material=new Material(Shader.Find("YunJian/OriginalHand"));material.SetTexture("_MainTex",probe);material.SetTexture("_BackTex",back);
            var rt=RenderTexture.GetTemporary(2,1,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var dimPixels=new Texture2D(2,1,TextureFormat.RGBA32,false,true);
            var prior=RenderTexture.active;material.SetFloat("_Dim",1);Graphics.Blit(probe,rt,material);RenderTexture.active=rt;dimPixels.ReadPixels(new Rect(0,0,2,1),0,0);dimPixels.Apply();RenderTexture.active=prior;
            Color red=dimPixels.GetPixel(0,0),green=dimPixels.GetPixel(1,0);Require(red.r>red.g*2 && green.g>green.r*2,"Dimming desaturates red/green glyphs");Require(red.r<.75f && green.g<.75f,"Dimming does not reduce brightness");
            RenderTexture.ReleaseTemporary(rt);Destroy(probe);Destroy(back);Destroy(material);Destroy(dimPixels);
            Debug.Log("RIICHI FIDELITY PASS: framed chi tiles, original win/score/rank/button sprites and fonts, original fire-tornado meshes at four ages and every seat.");
            Debug.Log("RIICHI RESULT VIDEO PASS: four sizes, yaku and winning hand presentation, two stages, point endpoints, ranks, polling stability, both ron winners and chromatic brightness dimming.");
            Debug.Log("RIICHI WIN EFFECT PASS: ron/tsumo in all three/four-seat directions and rotated viewers; original cut-ins, Zimo mesh pixels, exact landing, nine original fire-tornado layers, original spiral and rubble sheet and visible pixels; delayed result, polling/reconnect cleanup, double ron and robbed tile targets.");
        }

        IEnumerator ValidateOpeningAudio(RiichiClient client)
        {
            var table=GetComponent<TableRenderer>();var audio=GetComponent<GameAudio>();var fx=GetComponent<TableEffects>();
            Require(audio.HasResources && audio.MusicLoops,"Original table audio resources/loop missing");
            audio.Configure(true,.35f,true,.8f);audio.Suspend(false);
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-4.json")));
            view.phase="play";view.you=0;view.dealer=0;view.turn=0;view.audioRoom="opening-audio";view.handSeq=1;
            view.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();view.pending=null;
            view.openingHand=new[]{"s3","p8","m6","z1","m2","p1","s5","m8","p4","z1","s1","m2","p7","z3"};
            view.hand=view.openingHand.OrderBy(x=>"mpsz".IndexOf(x[0])).ThenBy(x=>x[1]).ToArray();view.drawn="z3";
            view.options=view.hand.Distinct().Select(t=>new Choice{id="discard:"+t+(t==view.drawn?"_":""),kind="discard",tile=t}).ToArray();
            foreach(var p in view.players){p.river=new RiverTile[0];p.melds=new string[0];p.nuki=0;p.count=13;}
            table.Opening.Freeze=true;table.Opening.FrozenAge=0;
            int sounds=audio.DealSounds,starts=table.Opening.Started;
            client.ShowValidationFixture(view);SetGameResolution(1688,780);yield return null;
            Require(table.ArrangingHand && table.Opening.Started==starts+1,"Fresh deal did not animate");
            client.SelectTile(0,"discard:m2");Require(client.SelectedIndex==-1,"Opening accepted discard while tiles move");
            float previousX=0;
            foreach(float age in new[]{.21f,.51f,.81f,1.11f,1.40f,1.80f}){
                table.Opening.FrozenAge=age;yield return null;yield return null;
                int visible=Enumerable.Range(0,14).Count(i=>table.ValidationHandOpacity(i)>.95f);
                if(age<1.2f)Require(visible==Mathf.Min(14,((int)(age/.3f)+1)*4),"Opening batches/fade differ from native deal");
                if(age<1.2f)Require(Mathf.Abs(table.ValidationHandPosition(0).x-table.ValidationHandBase(0).x)>1,"Initial hand was already sorted");
                if(age==1.11f)previousX=table.ValidationHandPosition(0).x;
                if(age==1.40f)Require(table.ValidationHandPosition(0).x<previousX && table.ValidationHandPosition(0).x>=table.ValidationHandBase(0).x,"Sort does not slide left toward final position");
                if(age>=1.76f)for(int i=0;i<14;i++)Require(Vector3.Distance(table.ValidationHandPosition(i),table.ValidationHandBase(i))<.001f,"Opening final position differs from normal hand");
                Capture(table,Path.Combine(folder,"opening-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"),1688,780);
            }
            Require(audio.DealSounds==sounds+4 && !table.ArrangingHand,"Deal sound groups repeat or animation never completes");
            table.Render(view);yield return null;Require(table.Opening.Started==starts+1 && audio.DealSounds==sounds+4,"Polling restarts deal");
            table.Opening.Freeze=false;
            float musicAt=audio.MusicTime;for(int i=0;i<10;i++)audio.SetScene(view);
            yield return new WaitForSecondsRealtime(.25f);Require(audio.MusicPlaying && audio.MusicTime>musicAt+.1f,"Polling restarts/stops BGM");
            audio.Configure(false,.35f,true,.8f);yield return null;Require(!audio.MusicPlaying,"BGM mute failed");
            float pausedAt=audio.MusicTime;audio.Configure(true,.35f,false,.8f);yield return new WaitForSecondsRealtime(.1f);
            Require(audio.MusicPlaying && audio.MusicTime>=pausedAt,"BGM resume restarts track");audio.PreviewDiscard();yield return null;Require(!audio.EffectsPlaying,"SFX mute failed");
            audio.Configure(true,.35f,true,.8f);
            view.players[1].river=new[]{new RiverTile{tile="p3"}};client.ShowValidationFixture(view);yield return null;
            int landings=audio.LandingSounds;fx.ValidationPlay(1,"discard","p3",false);fx.ValidationTime=.39f;yield return null;
            Require(audio.LandingSounds==landings,"Discard sound fired before landing");
            fx.ValidationTime=.41f;yield return null;Require(audio.LandingSounds==landings+1 && audio.EffectsPlaying,"Discard landing is silent");
            yield return null;table.Render(view);fx.Accept(view);yield return null;Require(audio.LandingSounds==landings+1,"Discard sound duplicated by sampling/polling");
            fx.Reset();fx.ValidationFreeze=false;
            audio.Suspend(true);yield return null;Require(!audio.MusicPlaying && !audio.EffectsPlaying,"Background suspension leaves audio running");
            audio.Suspend(false);yield return null;Require(audio.MusicPlaying,"BGM does not resume after background");
            view.handSeq=2;view.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-30000;view.players[1].river=new RiverTile[0];
            client.ShowValidationFixture(view);yield return null;Require(!table.ArrangingHand && table.Opening.Started==starts+1,"Late reconnect replays opening");
            view.handSeq=3;view.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();table.Opening.Freeze=true;table.Opening.FrozenAge=.1f;
            client.ShowValidationFixture(view);yield return null;table.Opening.Cancel();yield return null;
            Require(!table.ArrangingHand && Enumerable.Range(0,14).All(i=>table.ValidationHandOpacity(i)>.99f),"Cancelling opening leaves hidden tiles");
            table.Opening.Freeze=true;table.Opening.FrozenAge=.4f;view.handSeq=4;view.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            view.drawn=null;view.hand=view.hand.Take(13).ToArray();view.openingHand=view.openingHand.Take(13).ToArray();view.options=new Choice[0];
            client.ShowValidationFixture(view);yield return null;int nondealerStarts=table.Opening.Started;
            view.hand=view.hand.Concat(new[]{"p9"}).ToArray();view.drawn="p9";table.Render(view);yield return null;
            Require(table.ArrangingHand && table.Opening.Started==nondealerStarts,"First nondealer draw cancelled/restarted opening");
            Require(table.ValidationHandOpacity(13)==0,"First draw appeared inside the initial deal");
            table.Opening.FrozenAge=1.8f;yield return null;yield return null;
            Require(!table.ArrangingHand && Enumerable.Range(0,14).All(i=>table.ValidationHandOpacity(i)>.99f),"First nondealer draw stayed hidden after sorting");
            table.Opening.Freeze=false;audio.Reset();Require(!audio.MusicPlaying && !audio.EffectsPlaying,"Leaving table does not stop audio");
            Debug.Log("RIICHI OPENING AUDIO PASS: native four-tile/fade/sort timings, four deal sounds, exact final hand positions, polling and reconnect, landing-only sound, separate mute/volume, BGM loop/pause/resume and cleanup.");
        }

        IEnumerator ValidateNorthEffects(RiichiClient client)
        {
            var table=GetComponent<TableRenderer>();var fx=GetComponent<TableEffects>();
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-3.json")));
            view.phase="play";view.pending=null;view.options=new Choice[0];
            SetGameResolution(1688,780);
            // Synthetic presentation views exercise public committed nuki events.
            for(int you=0;you<3;you++)for(int relative=0;relative<3;relative++)
            {
                int seat=(you+relative)%3;view.you=you;
                for(int i=0;i<3;i++) {view.players[i].nuki=i==seat?2:0;view.players[i].melds=new[]{"p555+"};}
                view.audioRoom="north-validation-"+you+"-"+seat;view.audioSeq=20;view.audioEvents=new PublicSound[0];
                client.ShowValidationFixture(view);yield return null;
                fx.ValidationFreeze=true;fx.ValidationTime=.28f;fx.Accept(view);
                view.audioSeq=21;view.audioEvents=new[]{new PublicSound{id=21,kind="nuki",seat=seat,tile="z4",nuki=2}};
                fx.Accept(view);yield return null;yield return null;
                int spawned=fx.SpawnedNorthEffects;
                Require(fx.ValidationKind=="nuki" && fx.ValidationCallKind=="nuki","North hand/cut-in did not start together");
                Require(fx.Art("shout_babei")!=null && fx.Art("yiji-full")!=null,"North cut-in lost the existing Ichihime art");
                Require(fx.ValidationAnimatedTile!=null && fx.ValidationAnimatedTile.name=="z4","North hand has no carried North tile");
                Require(fx.ValidationTarget==table.NorthTarget(seat,2) && !table.NorthTarget(seat,2).gameObject.activeSelf,"North hand hides the wrong landing tile");
                Require(table.NorthTarget(seat,1).gameObject.activeSelf && table.MeldTarget(seat).gameObject.activeSelf,"North hand hid an earlier North or a meld");
                Require(fx.ValidationPetals==null,"North petals started before landing");
                if(you==0)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"north-call-"+relative+".png"));yield return new WaitForSecondsRealtime(.12f);
                }
                // Since 0.5.25 North uses the shorter Dapai clip. A seat facing the camera
                // enters from beyond its own table edge, so the hand is only inside the frame
                // during the first part of the reach; require it to be visible somewhere there.
                var handRenderer=GameObject.Find("Original hand seat "+seat).GetComponentInChildren<MeshRenderer>();
                int handPixels=0;float handSample=0;
                foreach(float fraction in new[]{.3f,.45f,.6f})
                {
                    fx.ValidationTime=fx.ValidationHandoff*fraction;yield return null;yield return null;
                    int pixels=VisibleEffectPixels(table,handRenderer);
                    if(pixels>handPixels){handPixels=pixels;handSample=fx.ValidationTime;}
                    if(handPixels>30)break;
                }
                fx.ValidationTime=handSample;
                Require(handPixels>30,"North hand mesh never reaches the visible table: you="+you+" seat="+seat+" pixels="+handPixels+" t="+handSample);
                if(you==0)Capture(table,Path.Combine(folder,"north-hand-"+relative+".png"),1688,780);
                // Real polling rebuilds table objects without resetting the effect.
                table.Render(view);fx.Accept(view);yield return null;yield return null;
                Require(!table.NorthTarget(seat,2).gameObject.activeSelf && table.NorthTarget(seat,1).gameObject.activeSelf,"Polling duplicated the moving North");
                fx.ValidationTime=fx.ValidationHandoff;yield return null;yield return null;
                Require(Vector3.Distance(fx.ValidationAnimatedTile.position,table.NorthPoint(seat,2))<.002f,"Carried North does not land at the extracted tile");
                Require(Quaternion.Angle(fx.ValidationAnimatedTile.rotation,table.NorthTarget(seat,2).rotation)<.1f,"North glyph snaps during hand-off");
                Require(!fx.ValidationAnimatedTile.gameObject.activeSelf && table.NorthTarget(seat,2).gameObject.activeSelf,"North tile hand-off failed");
                // The sakura burst belongs to screen taps (ScreenTapEffect), never to North.
                Require(fx.ValidationPetals==null && fx.SpawnedNorthEffects==spawned,"North landing spawned a petal burst");
                fx.ValidationTime=1.1f;yield return null;yield return null;
                Require(fx.ValidationKind==null && table.NorthTarget(seat,2).gameObject.activeSelf,"North hand did not finish on the landed tile");
                if(you==0)Capture(table,Path.Combine(folder,"north-landed-"+relative+".png"),1688,780);
                table.Render(view);fx.Accept(view);yield return null;yield return null;
                Require(fx.SpawnedNorthEffects==spawned && table.NorthTarget(seat,2).gameObject.activeSelf,"Polling restarted or hid the landed North");
                fx.Reset();fx.Accept(view);yield return null;
                Require(!fx.IsAnimating,"Reconnect replayed a historical North extraction");
            }
            // Event indices keep two closely queued extractions on their own tiles.
            view.you=0;view.players[0].nuki=3;client.ShowValidationFixture(view);yield return null;
            fx.ValidationPlay(0,"nuki");yield return null;
            Require(fx.ValidationAnimatedTile!=null && fx.ValidationTarget==table.NorthTarget(0,3),"Legacy North event without a tile is not compatible");
            fx.Reset();view.audioRoom="north-index-validation";view.audioSeq=30;view.audioEvents=new PublicSound[0];fx.Accept(view);
            fx.ValidationTime=.28f;view.audioSeq=31;view.audioEvents=new[]{new PublicSound{id=31,seat=0,kind="nuki",tile="z4",nuki=2}};fx.Accept(view);yield return null;
            Require(fx.ValidationTarget==table.NorthTarget(0,2) && table.NorthTarget(0,3).gameObject.activeSelf,"Queued North event targeted a later extraction");
            fx.ValidationFreeze=false;fx.Reset();
        }
        Color32[] ReadTile(RenderTexture texture,string name)
        {
            var previous=RenderTexture.active;RenderTexture.active=texture;
            var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());var result=pixels.GetPixels32();Destroy(pixels);return result;
        }
        static int VisibleEffectPixels(TableRenderer table,Renderer effectRenderer)
        {
            var camera=table.Camera;var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1688,780,24);var pixels=new Texture2D(1688,780,TextureFormat.RGB24,false);
            camera.targetTexture=target;table.HandCamera.targetTexture=target;table.FitCamera(1688f/780);
            effectRenderer.enabled=false;camera.Render();table.HandCamera.Render();table.GetComponent<TableEffects>().RenderOverlay(target);RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1688,780),0,0);pixels.Apply();var before=pixels.GetPixels32();
            effectRenderer.enabled=true;camera.Render();table.HandCamera.Render();table.GetComponent<TableEffects>().RenderOverlay(target);pixels.ReadPixels(new Rect(0,0,1688,780),0,0);pixels.Apply();var after=pixels.GetPixels32();
            int visible=0;for(int i=0;i<before.Length;i++)if(Mathf.Abs(after[i].r-before[i].r)+Mathf.Abs(after[i].g-before[i].g)+Mathf.Abs(after[i].b-before[i].b)>20)visible++;
            camera.targetTexture=previous;table.HandCamera.targetTexture=null;RenderTexture.active=active;target.Release();Destroy(target);Destroy(pixels);return visible;
        }
        IEnumerator ValidateTurnLight(RiichiClient client)
        {
            var view = JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-4.json")));
            for (int seat=0;seat<4;seat++)
            {
                view.turn=seat; client.ShowValidationFixture(view); yield return null;
                var hub=GameObject.Find("PublicTableState").transform.Find("Score hub");
                var light=hub.Find("Hub seat "+view.players[seat].wind+"/Turn light");
                Require(light!=null,"Indicator did not follow turn "+seat);
                var particles=light.GetComponentsInChildren<ParticleSystem>();
                Require(particles.Length==2,"Original indicator needs both particle layers");
                var sample=new ParticleSystem.Particle[2];
                for(int frame=0;frame<10 && particles[0].GetParticles(sample)==0;frame++) yield return null;
                Require(particles[0].GetParticles(sample)==1,"Indicator burst is missing");
                float before=1-sample[0].remainingLifetime;
                yield return new WaitForSeconds(.12f);
                client.ShowValidationFixture(view); yield return null;
                light=GameObject.Find("PublicTableState").transform.Find("Score hub/Hub seat "+view.players[seat].wind+"/Turn light");
                particles=light.GetComponentsInChildren<ParticleSystem>();
                Require(particles[0].GetParticles(sample)==1,"Polling removed the indicator particle");
                float elapsed=Mathf.Repeat((1-sample[0].remainingLifetime)-before,1);
                Require(elapsed>.06f && elapsed<.4f,"Polling restarted the indicator blink");
                float dark=0,bright=0,ending=0;
                foreach (float phase in new[] {.01f,.45f,.99f})
                {
                    foreach (var particle in particles)
                    {
                        particle.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                        particle.Simulate(phase,false,true,false);
                        Require(particle.GetParticles(sample)==1,"Indicator disappeared during its one-second lifetime");
                    }
                    // Simulation updates the CPU particles immediately; allow the
                    // renderer a frame to upload the new particle vertex stream.
                    yield return null;
                    float luminance=CaptureHub(Path.Combine(folder,"turn-light-"+seat+"-"+phase.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"),light.position);
                    if (phase<.1f) dark=luminance; else if (phase<.5f) bright=luminance; else ending=luminance;
                }
                Debug.Log("TURN PARTICLE PIXELS "+seat+" dark="+dark+" peak="+bright+" end="+ending);
                Require(bright>dark+.3f,"Indicator particle is not visibly blinking at seat "+seat);
                Require(Mathf.Abs(ending-dark)<.15f,"Indicator does not fade back down at seat "+seat);
            }
            Debug.Log("RIICHI TURN LIGHT PASS: four directions, two original particle layers, rendered fade/peak/fade and stable polling phase.");
        }
        static float CaptureHub(string file,Vector3? samplePoint=null)
        {
            var camera = new GameObject("Score hub validation camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 3, -1.4f); camera.transform.LookAt(new Vector3(0,.092f,0));
            camera.orthographic = true; camera.orthographicSize = 1.14f; camera.cullingMask = ~(1 << 12 | 1 << 13);
            var active = RenderTexture.active; var target = new RenderTexture(900,800,24); var pixels = new Texture2D(900,800,TextureFormat.RGB24,false);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,900,800),0,0); pixels.Apply(); File.WriteAllBytes(file,pixels.EncodeToPNG());
            float luminance=0;
            if (samplePoint.HasValue)
            {
                var point=camera.WorldToScreenPoint(samplePoint.Value);
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
                {
                    var color=pixels.GetPixel(Mathf.RoundToInt(point.x)+x,Mathf.RoundToInt(point.y)+y);
                    luminance+=(color.r+color.g+color.b)/9;
                }
            }
            RenderTexture.active = active; target.Release(); Destroy(target); Destroy(pixels); camera.gameObject.SetActive(false); Destroy(camera.gameObject);
            return luminance;
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
