"""Write the local 0.6.20 phone-test handoff; never publish or commit."""
from pathlib import Path
import hashlib, json

R = Path(__file__).resolve().parent.parent
D = R/'dist'
report = json.loads((D/'unity-0.6.20-verification.json').read_text('utf8'))
a = report['apk']
apk = D/a['file']
assert report['versionCode']==68 and all(a[k] for k in ['v1','v2','independentV1','alignment16KB'])
assert apk.stat().st_size==a['bytes'] and hashlib.sha256(apk.read_bytes()).hexdigest()==a['sha256']
notes = f'''# 云间日麻 0.6.20 实机测试包

日期：2026-11-03。包含 R25–R28 当前已完成修改，仅本地测试，未提交或推送 Git，未发布 GitHub Release。

- APK：`{apk}`。
- 版本：0.6.20 / versionCode 68，包名 `com.yunjian.riichi.unity`。
- 大小：{a['bytes']:,} 字节，约 {a['bytes']/1000000:.2f} MB / {a['bytes']/1048576:.2f} MiB。
- SHA-256：`{a['sha256']}`。
- 沿用原证书 `{a['certificateSHA256']}`，保留 ARM64／ARMv7、minSdk 26、targetSdk 32。已校验 v1／v2、独立 API-23 v1 和 16 KiB 原生库对齐。

## 包含修改

1. 宿舍选择角色／装扮后自动保存，移除手动保存按钮并加高分类列表；单件资源下载按钮高 84、文字 36。资源、设置及房间弹窗补充窗外关闭处理。
2. 角色语音页加入原版名称、台词、滚动、播放和停止；目录覆盖 122 名角色、16,226 条可见语音。修复三上千织等早期角色的立直／两立直／自摸命名兼容，语音仍使用对应角色原音频。
3. 听牌指示按本局首次选中可听牌弃牌展开，后续手动开合，拖动手牌不再带动提示窗。二维码标题显示实际房间号。
4. 立直棒按实体部分缩放，汪次郎默认使用狗爪；空结算分数可按已声明满贯及以上标记显示横幅。立直特效按已解码弃牌分支调整挂载和回收。
5. 鸣牌指示按两段原版录像复核：底光留在牌下，正常接受麻将遮挡；恢复雷电火花的上升方向。咕噜咕噜、众星捧月同步修正覆盖渲染，普通引导保留原深度策略。待鸣目标仍严格匹配实际弃牌。

## 验证与实机重点

本次 192 项 Node 测试通过；安卓构建前 QR／结算、鸣牌文字和角色选择检查共 186 项通过。成品 APK 已核对全部基础资源、新语音目录、当前运行类与方法、离线引擎、双架构、原证书、签名和对齐；下载用重资源保持排除。此前实际 Unity 预览的语音页 122 角色／16,226 次查找以及 R28 四款四座位 131 项检查保留为预览证据，没有冒充本次 Android 真机结果。

请重点测试覆盖安装后装扮和已下载资源保留、自动保存、角色语音页和役种播报、窗外关闭与拖动听牌窗口，以及鸣牌指示与立直特效的方向和遮挡。当前没有连接手机完成安装、触控、声音、GPU 或多机热点测试。

原版全部装扮尚未补全；原版录像与当前镜头／粒子阶段不同，没有标记为逐像素一致。未重建或重新发布角色／装扮下载包，现有资源地址继续使用。

构建复用 `build/phone-test-r24/stage/unity` 安卓缓存，日志为 `build/phone-test-r29/android-build.log`。使用已安装 API 36 完成编译与 SDK 预检，最终 Gradle 和 APK targetSdk 保持 32。旧聚合检查要求可下载角色大图全部内置，不适用于当前精简构建；本轮运行独立的适用检查，并从 APK 核对实际打包和排除内容。

预览：`dist/previews/2026-11-03-callout-reference-r28/index.html`。详细记录：`dist/unity-0.6.20-verification.json`。
'''
(D/'README-unity-0.6.20-test.md').write_text(notes, 'utf8')
(R/'docs/handoff-unity-0.6.20-test.md').write_text(notes, 'utf8')
page = f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.6.20 测试 APK</title><style>body{{background:#111722;color:#ead9bd;font:17px/1.8 system-ui,"Microsoft YaHei",sans-serif;margin:0}}main{{max-width:820px;margin:8vh auto;padding:24px}}h1{{font-size:28px}}a{{color:#efd29d}}.download{{display:inline-block;padding:14px 24px;background:#dbb36b;color:#201b15;border-radius:8px;text-decoration:none;font-weight:bold}}code{{word-break:break-all;font-size:13px;color:#bfc7d8}}</style><main><h1>云间日麻 0.6.20 实机测试包</h1><p>包含宿舍自动保存、角色语音页、听牌提示及最新鸣牌效果修正。</p><p><a class="download" download href="{a['file']}">下载 APK · {a['bytes']/1000000:.2f} MB</a></p><p>沿用原包名与签名，用于覆盖安装测试。Android 8.0 及以上，支持 ARM64／ARMv7。额外角色和装扮继续按需下载。</p><p><a href="README-unity-0.6.20-test.md">修改说明与测试重点</a> · <a href="unity-0.6.20-verification.json">安装包核验记录</a></p><p>SHA-256<br><code>{a['sha256']}</code></p></main></html>'''
(D/'phone-test-0.6.20.html').write_text(page, 'utf8')
readme = R/'README.md'
text = readme.read_text('utf8')
section = '''## Unity 0.6.20 · 实机反馈修正测试包（2026-11-03）

本地测试 APK 已包含宿舍装扮自动保存、窗外关闭、角色语音分页与早期语音命名兼容、听牌提示与拖动修正、立直棒和狗爪默认手，以及按原版录像复核后的鸣牌位置／深度遮挡。versionCode 68，沿用原包名、证书、ARM64／ARMv7 和按需下载资源。

[本地 APK](dist/yunjian-riichi-unity-0.6.20.apk) · [修改与真机测试重点](docs/handoff-unity-0.6.20-test.md) · [鸣牌原版对照预览](dist/previews/2026-11-03-callout-reference-r28/index.html) · [APK 检查记录](dist/unity-0.6.20-verification.json) · [SHA-256](dist/SHA256-unity-0.6.20.txt)

192 项 Node 测试及成品包资源、运行代码、签名和对齐检查通过。手机安装、触控、声音与热点待实机验证；本轮未提交或推送代码、未发布 GitHub Release。

'''
assert text.startswith('# 云间日麻\n')
if '## Unity 0.6.20 ·' not in text:
    text = text.replace('# 云间日麻\n\n', '# 云间日麻\n\n'+section, 1)
    text = text.replace('当前源码对应本地实机测试包 0.6.19', '该版源码对应本地实机测试包 0.6.19', 1)
    readme.write_text(text, 'utf8')
print('LOCAL PHONE TEST READY', apk, a['bytes'], a['sha256'], flush=True)
