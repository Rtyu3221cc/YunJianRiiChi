"""Deliver the checked Unity recording and a compact source-to-port handoff."""
from pathlib import Path
import hashlib,json,re,shutil,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/original-input-r18';C=B/'capture';O=R/'dist/previews/2026-10-29-original-input-r18';O.mkdir(parents=True,exist_ok=True)
assert 'ORIGINAL INPUT R18 CHECKS PASS 135' in (B/'checks-runtime.log').read_text('utf8',errors='replace')
assert 'ORIGINAL INPUT R18 VIDEO PASS 15 frames=762' in (B/'video-runtime-final.log').read_text('utf8',errors='replace')
assert 'ORIGINAL INPUT PURE PASS 1468' in (B/'pure-tests.log').read_text('utf8',errors='replace')
assert re.search(r'pass\s+185\b',(B/'node-tests.log').read_text('utf8'))
shutil.copy2(C/'original-drag-r18.mp4',O/'original-drag-r18.mp4');shutil.copy2(B/'video-qa/final-270.png',O/'poster.png')
for src,dst in [('checks.txt','input-checks.txt'),('video-checks.txt','video-checks.txt'),('chapters.txt','chapters.txt')]:shutil.copy2(C/src,O/dst)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
frames=0;last=-1
with av.open(str(O/'original-drag-r18.mp4'))as movie:
    stream=movie.streams.video[0];assert (stream.width,stream.height)==(1746,810) and stream.average_rate==30
    for frame in movie.decode(video=0):
        t=float(frame.pts*frame.time_base);assert t>last;last=t;frames+=1
assert frames==762
report=dict(date='2026-10-29',sourceRevision='recovered input Lua / R18 port',width=1746,height=810,fps=30,frames=762,durationSeconds=25.4,silent=True,actualUnityRendering=True,newApk=False,uploaded=False,
    inputRuntimeChecks=135,offlineHandOrderChecks=1468,nodeTests=185,videoChecks=15,allVideoFramesDecoded=True,
    sourceMapping={'dragThreshold':'viewplayer_me.lua:1338-1342','handBoundary':'viewplayer_me.lua:57-69,1166-1211','release':'viewplayer_me.lua:1308-1329','manualOrdering':'viewplayer_me.lua:1356-1378','selectedLiftAndCollider':'handpaiplane.lua:51-64','slotSlide':'handpaiplane.lua:461-489'},
    mediaSHA256=hashlib.sha256((O/'original-drag-r18.mp4').read_bytes()).hexdigest(),
    limitations=['Not an Android recording or device test.','Video captions and touch ring are recording annotations.','Full-character editor unit validation is not reported as passing in the reduced project; its optional illustration assets are intentionally excluded.'])
(O/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
doc='''# 原版 Lua 拖动流程移植 R18

日期：2026-10-29。本次交付实际 Unity 效果视频，未生成新 APK，未发布。手机上的 0.6.17 仍是上一版输入逻辑。

## 已改为解码源码中的流程

- 按下即选中，保持“再次按下同牌并松手出牌”的两次点选方式。
- 位移平方严格大于 400（距离大于 20 屏幕像素）后开始拖动；只在松手帧突然跳远，不会被当作新启动的拖动。
- 松手位置根据原版手牌模板投影 Y 边界判断，不再使用 R17 的中央矩形区域。合法性仍由本客户端收到的操作白名单决定。
- 手动理牌时，在手牌区域内横拖会移动所选牌的显示位置并平移其他牌，松手保留新顺序。偏好设置可切回自动理牌；原版的 `auto_liqi` 对应自动理牌，不能解释为自动立直。
- 原版选牌抬高 .8、选中碰撞体扩大、横向移动速度与手牌投影的 3.75／-1.25 参数已接入当前坐标体系。
- 每张本地显示牌持有稳定身份，摸入牌的身份独立于显示下标。把摸入牌拖到中间后仍提交摸切 `_` 操作，同种手牌则提交手切；红五与普通五保持区分。
- 调整的是本地显示顺序，不修改服务器的手牌数组。轮询保序、重复牌身份、短暂轮询等待、失焦／多指／回合变化取消继续保留。
- 提交后立即隐藏对应手牌；网络错误或操作拒绝时恢复。服务器始终校验实际操作。

主要实现：`ClientHandOrder.cs`、`RiichiClient.HandOrder.cs`、`RiichiClient.HandDrag.cs`、`TableRenderer.cs`。自动／手动理牌设置在 `AppPreferences.cs` 和 `NativeSettingsUi.cs`。

原始源码对应：可读 `viewplayer_me.lua` 第 57～69、1166～1211、1269～1378 行，`handpaiplane.lua` 第 51～64、304～315、461～489 行。原版的百搭牌特殊模式、回放等分支不等同于本项目支持的普通日麻协议；对应合法性由现有操作接口表达，未宣称移植整个原版客户端。

## 验证与视频

1468 项离线手牌身份／排序合同检查、135 项真实 Unity 输入检查、185 项现有引擎检查通过。实际视频另有 15 项检查，762 帧均已完整解码，25.4 秒、1746×810、30 FPS。视频无音轨，触点和顶部说明是录制标注。

视频中的手切、摸切前后状态来自真实离线引擎的合法操作；公开局面在每一步冻结为独立快照。没有读取真实牌局或对原版游戏进行操作。

精简临时工程排除了可选角色源素材，用于验证输入和录制；完整角色图片单元检查会因 403301 插画前置资源缺失停止，本轮未将原有 571 项角色等完整单元检查标成重新通过。日志中的 UnityEditor.Search 索引异常属于编辑器缓存初始化，输入与录制检查正常完成。

原始检查与复现资料在 `build/original-input-r18/`；视频检查摘要在同目录 `verification.json`。复现离线合同：`scripts/test-original-hand-order.ps1`；精简工程准备：`scripts/prepare-original-input-stage.py`；录像入口：`YunJian.Riichi.Editor.OriginalInputR18Preview.RunVideo`。

本次没有 Android 构建，因此不能把编辑器画面描述为手机验证。后续按用户确认结果再生成测试 APK。
'''
(R/'docs/原版Lua拖动流程移植-r18-2026-10-29.md').write_text(doc,'utf8');(O/'说明.md').write_text(doc,'utf8')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 原版拖动流程视频</title><style>body{margin:0;background:#171a22;color:#efe8dc;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:28px}video{width:100%;background:#080a0e;border-radius:8px}h1{font-size:26px}a{color:#ecc887}p{max-width:1100px}</style><main><h1>原版 Lua 拖动流程 · 实际游戏效果</h1><p>25秒：拖回取消、手内换序、轮询保序、换序后的手切与摸切。Unity实际渲染，无音轨；白色触点和顶部文字为录制标注。本次未生成APK。</p><video controls playsinline preload="metadata" poster="poster.png" src="original-drag-r18.mp4"></video><p><a href="original-drag-r18.mp4" download>下载视频</a> · <a href="说明.md">实现与验证说明</a> · <a href="verification.json">检查摘要</a></p></main></html>'''
(O/'index.html').write_text(page,'utf8')
print('Final video ready:',O/'original-drag-r18.mp4',(O/'original-drag-r18.mp4').stat().st_size,'bytes')
