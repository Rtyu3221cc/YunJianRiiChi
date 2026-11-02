"""Write the local 0.6.19 phone-test handoff after the signed APK passes."""
from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parent.parent;D=R/'dist';B=R/'build/phone-test-r24'
report=json.loads((D/'unity-0.6.19-verification.json').read_text('utf8'));a=report['apk'];apk=D/a['file']
assert report['versionCode']==67 and report['resourceUiChecks']==62 and all(a[k]for k in ['v1','v2','independentV1','alignment16KB'])
assert apk.stat().st_size==a['bytes']and hashlib.sha256(apk.read_bytes()).hexdigest()==a['sha256']
text=f'''# 云间日麻 0.6.19 实机测试包

日期：2026-11-02。本轮基于 R23 已预览源码生成本地测试 APK，未发布新版 GitHub Release。

- 文件：`{apk}`。
- versionName：0.6.19；versionCode：67；包名：`com.yunjian.riichi.unity`。
- 大小：{a['bytes']:,} 字节，约 {a['bytes']/1000000:.2f} MB / {a['bytes']/1048576:.2f} MiB。
- SHA-256：`{a['sha256']}`。
- 原证书保持 `{a['certificateSHA256']}`；v1／v2、独立 API-23 v1、ARM64／ARMv7 与 16 KiB 对齐通过，可用于覆盖现有 Unity 版测试。

## 包含修改

1. 角色卡与装扮卡使用同款未下载提示条；单件下载弹窗统一，提供“下载”和“下载并选用”。
2. 角色／装扮资源页统一五按钮、行高、列表与滚动区域。
3. 全部／部分／自选下载先展示本次空间和当前网络，移动网络明确提示流量使用。
4. 已下载装扮在宿舍列表中优先；下载并选用完成后定位到前列。
5. 分类选择等资源弹窗使用原版 × 关闭按钮；手牌理牌与动态服饰共用相同分段开关。
6. 宿舍角色与装扮页共享面板 Y 和高度，切换时保持高度稳定。
7. 修复共享依赖让未点选特效误显示“下载中”：仅主动请求的条目标记下载状态，共享素材仍复用。

基础资源与已发布的装扮包协议不变。当前全部额外装扮（含 BGM）约 428.56 MB；连角色一起约 2.79 GB，确认框会扣除本机已有内容。原版全装扮尚未全部迁移。

## 已验证与真机待测

R23 的 62 项编辑器实际流程检查通过，含真实 HTTP 装扮／角色下载并选用、共享依赖状态、排序、两种分辨率尺寸、切换高度和开关功能。本 APK 的当前离线引擎、运行类、基础资源、重资源排除、版本、包名、签名及架构已从包内核对。

真机请重点观察网络类型提示及 Wi-Fi／流量切换、覆盖安装后资源保留、下载与自动选用、只标记所选条目的下载状态、界面尺寸与滚动。编辑器的移动网络提示是模拟，不代表这些手机行为已验证。

预览：`dist/previews/2026-11-02-resource-ui-r23/index.html`。修改说明：`docs/资源下载界面-r23-2026-11-02.md`。完整包检查：`dist/unity-0.6.19-verification.json`。隔离构建日志：`build/phone-test-r24/android-build.log`。原始参考 APK、Lua 缓存、已发布资源、签名材料和旧 0.6.18 测试包保持。
'''
(D/'README-unity-0.6.19-test.md').write_text(text,'utf8');(R/'docs/handoff-unity-0.6.19-test.md').write_text(text,'utf8')
p=B/'progress.json';progress=json.loads(p.read_text('utf8'));progress.update(status='verified-local-phone-test-ready',apk=a,githubReleaseCreated=False);p.write_text(json.dumps(progress,ensure_ascii=False,indent=1)+'\n','utf8');print('LOCAL PHONE TEST READY',apk,a['bytes'],a['sha256'])
