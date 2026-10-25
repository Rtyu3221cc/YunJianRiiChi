"""Package actual Unity screenshots and the locally verified 0.6.8 fix report."""
import hashlib
import html
import json
from pathlib import Path
import re
from zipfile import ZipFile
from PIL import Image

R=Path(__file__).resolve().parent.parent
B=R/'build/portrait-fix-068'
P=R/'dist/previews/unity-0.6.8-portraits'
P.mkdir(parents=True,exist_ok=True)
apk=R/'dist/yunjian-riichi-unity-0.6.8.apk'
sha=hashlib.sha256(apk.read_bytes()).hexdigest()
log=(B/'apk-verify.log').read_text('utf8',errors='replace')
assert sha in log and 'PASS: final APK v1 + v2' in log
with ZipFile(apk) as archive:
    metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for symbol in [b'hasWaitingroom',b'artWidth',b'DisplayAsset',b'FrameRect',b'RefreshInstalledCache']:
        assert symbol in metadata, 'Stale compiled Android code: '+symbol.decode()
runtime=json.loads((B/'render/checks.json').read_text('utf8'))
audit=json.loads((B/'framing-audit.json').read_text('utf8'))
node=(B/'node-tests.log').read_text('utf8')
assert 'pass 152' in node and 'fail 0' in node
assert 'PORTRAIT 068 REVIEW PASS' in (B/'runtime.log').read_text('utf8',errors='replace')
assert 'RESOURCE UI REVIEW PASS 31' in (B/'resource-flow.log').read_text('utf8',errors='replace')
assert 'RIICHI UNITY VALIDATION PASS' in (R/'build/unity-validate.log').read_text('utf8',errors='replace')
unchanged=all((R/'unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+platform+'.json')).read_bytes()==(R/'.publish/YunJianRiiChi/unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+platform+'.json')).read_bytes() for platform in ['android','windows'])
assert unchanged
labels={
 'room-fuji-yiji':'房间卡片：福姬「假日之滨」与默认一姬',
 'intro-fuji-yiji':'开局：四个方向恢复人物上半身构图',
 'riichi-seat-2':'对家立直：显示人物头部和上半身',
 'riichi-seat-0':'自家立直：人物随原版遮罩显示',
 'room-legacy-mixed':'房间：二阶堂专用 waitingroom 与未来原版构图',
 'intro-legacy-mixed':'开局：旧裁切表、未来、现代服饰混合',
 'riichi-seat-1':'右侧立直', 'riichi-seat-3':'左侧立直',
 'home-fuji':'大厅 A0 场景', 'dorm-fuji':'宿舍 B0 场景',
 'skin-fuji':'服饰卡片 F0 场景', 'win-cutin-fuji':'和牌切入 K0 场景',
 'result-win-fuji':'和牌结算 B1 场景','result-final-fuji':'终局 B2 场景',
 'cold-role-selection':'122 名角色安装后的首次切换页面',
 'cold-dorm':'122 名角色安装后的首次宿舍页面',
 'dorm-live-yiji':'内置动态服饰回归'}
for name in labels:
    with Image.open(B/'render'/(name+'.png')) as im:
        im.convert('RGB').save(P/(name+'.jpg'),quality=88,optimize=True)
before={
 'room-fuji-yiji':'codex-clipboard-4190787c-d61a-472e-8341-6e8cc7a307be.png',
 'intro-fuji-yiji':'codex-clipboard-53ce4b20-d830-4203-a3d5-dc7247cdbc56.png',
 'riichi-seat-2':'codex-clipboard-732c4322-95e1-4bfb-bf2d-28b002ca4d45.png',
 'riichi-seat-0':'codex-clipboard-5c42ebcf-cd2d-445b-87a7-3a6b3fbb7017.png'}
cards=[]
for name,label in labels.items():
    pair=''
    if name in before:
        file=Path.home()/'AppData/Local/Temp'/before[name]
        if file.exists():
            with Image.open(file) as im:
                im.thumbnail((1800,1000));im.convert('RGB').save(P/(name+'-before.jpg'),quality=88,optimize=True)
            pair=f'<figure><figcaption>用户反馈原图</figcaption><a href="{name}-before.jpg"><img loading="lazy" src="{name}-before.jpg"></a></figure>'
    cards.append(f'<article><h2>{html.escape(label)}</h2><div class="pair">{pair}<figure><figcaption>0.6.8 · Unity 实际运行</figcaption><a href="{name}.jpg"><img loading="lazy" src="{name}.jpg"></a></figure></div></article>')
rows=''.join('<tr><td>'+html.escape(m['page'])+'</td><td>'+f"{m['firstFrameMs']:.1f} ms"+'</td><td>'+str(m['syncBundleLoads'])+'</td><td>'+str(m['loadedBundles'])+'</td></tr>' for m in runtime['measurements'])
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.6.8 立绘修复验证</title><style>body{margin:0;background:#17151e;color:#eee9df;font:16px/1.65 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:30px 20px}h1{color:#e9c587}h2{font-size:20px}article{margin:28px 0;padding:18px;background:#24212d;border-radius:12px}.pair{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,560px),1fr));gap:14px}figure{margin:0}figcaption{color:#c9bba3;margin-bottom:8px}img{display:block;width:100%;border-radius:6px}table{border-collapse:collapse}td,th{padding:8px 16px;border:1px solid #5b4d60;text-align:left}a{color:#ead099}p{max-width:1000px}</style><main><h1>云间日麻 0.6.8 · 立绘与页面卡顿修复</h1><p>2026-10-22。构图参数以本地雀魂 APK 为基准，逐项核对 122 名角色、486 套服饰。以下画面来自实际 Unity 编辑器运行的公开测试场景，非概念图。尚未进行 Android 真机测试。</p><p>全部可选角色包继续复用；更新 APK 可保留原有下载资源。</p><h2>首次打开页面的编辑器测量</h2><table><tr><th>页面</th><th>入口帧</th><th>同步打开角色包</th><th>常驻可选包</th></tr>'''+rows+'''</table><p>测量环境为本机 Unity 编辑器，122 名角色均已安装。数值不能代替手机冷启动耗时；零同步包加载说明已移除原来随整个列表阻塞主线程的路径。</p>'''+''.join(cards)+'</main></html>'
(P/'index.html').write_text(page,'utf8')
report=dict(version='0.6.8',versionCode=56,date='2026-10-22',apk=dict(file=apk.name,bytes=apk.stat().st_size,sha256=sha),
    nodeTests=152,inputTests=8,existingUnityAssertions=571,resourceFlowChecks=31,runtimeChecks=len(runtime['checks']),
    roles=122,skins=486,correctedExistingPoses=audit['correctedExistingPoses'],nativePlacements=audit['placements'],
    optionalPackCatalogsUnchanged=unchanged,compiledAndroidFixSymbolsChecked=True,androidDeviceTested=False,measurements=runtime['measurements'],gallery='previews/unity-0.6.8-portraits/index.html')
(R/'dist/unity-0.6.8-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
notes=f'''# 云间日麻 0.6.8：立绘与页面卡顿修复

完成日期：2026-10-22，Asia/Tokyo。versionCode 56，包名与原签名保持兼容，可覆盖安装；已有角色包目录和下载资源继续复用。

## 修复原因与范围

批量角色导入脚本把网页版默认偏移用于 Unity 的父节点坐标，覆盖了此前正确的部分场景配置。本次直接复核参考 `Majsoul_4.0.16_MC.apk` 中的 `default` TextAsset，并将全部导入入口统一到 `scripts/character_framing.py`。

- 核对 122 名角色、486 套服饰，修正 242 套服饰的既有位置，生成 3,444 条明确场景配置。246 套使用场景配置，其他服饰按原版裁切表处理。
- 大厅 A0、宿舍 B0、和牌 B1、终局 B2、卡片 F0、开局 I0、提示器 J0、切入 K0 各用对应参数。保留实际 Sprite 画布比例与原版正反向素材。
- 房间卡片接入原版场景构图。旧服饰直接显示专用 `waitingroom.png`，移除对预裁切图额外放大 1.6 倍的处理。
- 旧服饰的大厅／宿舍采用原始 full 坐标及 lobby／liaoshe 偏移；开局、提示器、切入继续采用原版 smallhead 中心规则。
- 切换列表与宿舍网格使用 160px 内置头像，跳过屏幕外条目。非默认服饰头像、主立绘和骨骼页面通过队列异步加载。服饰预览卡也采用异步图片请求。
- 安装状态短时缓存，下载、删除、坏包拒绝时立即失效。启动校验在后台计算摘要，不再解包全部角色。同步与异步消费者复用同一个进行中的 bundle 请求。

APK 依据：`assets/Android/uwv8ssu6@$hgi286m8668y_7cb3e04d6842705b50ce.majset`，TextAsset pathID `4331372021175156098`，SHA-256 `{audit['nativeDefault']['sha256']}`。旧服饰专用图分支对应 `UI_Character_Skin._setLoadedTexture`；场景计算对应 `Tools._calculateIllust/getSkinData/charaPart`。复现输入见 `docs/character-resource-inputs/framing-native-default.json` 与 `framing-illustrations.json`。

## 本轮验证

- Node：152 项通过，其中 3 项新回归检查固定 APK 坐标、未来既有构图及全部服饰场景覆盖。
- Python 输入合并：8 项通过。
- Unity 既有断言：571 项通过。
- 资源下载／删除／坏包／回退：31 项通过，使用本机缓存和局域网测试源。
- 实际 Unity 画面／加载回归：{len(runtime['checks'])} 项通过。覆盖用户截图中的福姬「假日之滨」403805、默认一姬、旧服饰、未来、四向立直、和牌切入、两种结算、内置及可下载动态服饰。
- 全部角色安装时，首次打开切换页和宿舍均为 0 次同步可选包加载、0 个列表常驻可选包。详细入口帧测量与截图见验证 JSON 和实际画廊。
- 最终 APK：ZIP 完整性、ARM64＋ARMv7、v1/v2、API23 独立 v1、原签名证书、16KB 对齐及最新打包资源通过。

尚未进行 Android 真机安装、触摸、低内存、真实手机冷启动及多手机热点测试。全部服饰做了配置与资源维度核对，实际界面截图覆盖上述代表场景。

## 本地交付

- APK：`dist/{apk.name}`，{apk.stat().st_size:,} 字节。
- SHA-256：`{sha}`。
- 验证记录：`dist/unity-0.6.8-verification.json`。
- 实际画廊：`dist/previews/unity-0.6.8-portraits/index.html`。
- 原始测试证据：`build/portrait-fix-068/`，含 APK 日志、资源流程日志、构图审计、原始 PNG 和测量 JSON。

本次生成本地修复 APK，GitHub 仍保留原发布状态。
'''
(R/'docs/立绘与页面卡顿修复-0.6.8.md').write_text(notes,'utf8')
print(json.dumps(report,ensure_ascii=False))
