"""Write the local phone-test handoff only after the signed APK passes its gate."""
from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parent.parent
report=json.loads((R/'dist/unity-0.6.17-verification.json').read_text('utf8'))
apk=R/'dist/yunjian-riichi-unity-0.6.17.apk'
signature=report['apk']
assert report['version']=='0.6.17' and report['versionCode']==65
assert all(signature[k] for k in ['v1','v2','independentV1','alignment16KB'])
assert hashlib.sha256(apk.read_bytes()).hexdigest()==signature['sha256'] and apk.stat().st_size==signature['bytes']
assert report['stickPrefabs']==61 and report['dragRuntimeChecks']==177
size=signature['bytes']
text=f'''# 云间日麻 Unity 0.6.17 实机测试包

日期：2026-10-28。基于 0.6.16，包含非特效装扮扩充与新增拖动出牌。仅生成本地测试包，未发布 GitHub；不包含原始雀魂 APK 或签名密钥。

- 安装文件：`{apk}`。
- 包名：`com.yunjian.riichi.unity`；versionName `0.6.17`；versionCode `65`。
- 大小：{size:,} 字节，约 {size/1048576:.2f} MiB／{size/1000000:.2f} MB。
- SHA-256：`{signature['sha256']}`。
- 已核验与旧版相同的签名证书：`{signature['certificateSHA256']}`，可覆盖安装现有 Unity 版并保留本机数据。

## 这包包含的修改

1. 新增 232 个可选装扮：62 款立直棒（含默认）、66 款静态桌布、56 款牌背、3 款牌面、24 款独立头像框、20 款背景、17 首立直音乐；大厅／对局音乐补齐到 22 首。
2. 结算横幅只在满贯及以上出现，满贯以下直接进入报役种界面。普通和牌不再留下横幅占用的空等待；和牌牌面特效保留。
3. 按住手牌可随手指拖动，在牌桌中央松手则出牌，拖回手牌附近或拖到边缘松手则取消。原有单击选牌、再次单击出牌保留。合法的立直选牌也支持拖动。
4. 拖动中轮询不会换牌或归位；松手时正好有状态轮询，会在短暂等待后重新校验。慢轮询、局面变化、多指或失焦取消，不会延后误出牌。

手部保持默认／橘猫手两项。捞金鱼、紫霞海岸、星间飞行 3 款多层动态桌布仍未接入。

## 已通过的检查

- 当前引擎 185 项；当前 Unity 单元 571 项、拖动交互 177 项、听牌／头像框／滚动交互 58 项。
- 177 项拖动检查覆盖三种分辨率和异形屏安全区，含二次点选、取消、重复牌、拖动中轮询、轮询中松手、慢轮询、多指、失焦、回合变化和立直选牌。
- APK ZIP 完整性；版本、包名、当前离线引擎和 JNI；ARM64／ARMv7；v1／v2 加密签名校验、独立 API-23 v1 校验、原证书和 16 KiB 对齐。
- 包内目录与源数据一致；61 款原生立直棒 prefab、网格、粒子、动画与材质完整；新装扮 shader 未剔除；原有 24 款动作特效、27 个 prefab 和 312 层粒子保留。
- 原有 620 项特效预览、13 项镰刀方向检查及 1045 项 R16 装扮运行检查为前轮证据，未冒充本轮手机或完整对局回归。

尚待本次手机确认：覆盖安装、实际拖动手感和松手区域、声音、GPU 显示、后台恢复及多手机联机。历史拔北间距基线问题未在本包修改。

## 代码和继续工作

装扮交接：`docs/handoff-static-wardrobe-r16.md`。拖动代码与来源：`docs/拖动出牌实现与APK来源-r17.md`。APK 内相关 Lua 已定位但受保护，没有恢复可读原版函数，当前阈值按用户描述适配实现。

构建：`scripts/verify-unity.ps1 -Mode Android`。包验证：`python scripts/verify-native-effects-apk.py`。本包完整验证结果在 `dist/unity-0.6.17-verification.json`；当前原始构建日志为 `build/unity-android.log`，本轮检查日志在 `build/release-0617/` 和 `build/drag-discard-r17/`。

维护工作区原有大量未提交变更，不 reset／clean，不整目录提交。用户本轮只要求本地 APK 实机测试，未授权发布 GitHub。后续发布仍沿用源码 ZIP 仅留本地、不重复下载已发布附件的约定。
'''
(R/'docs/handoff-unity-0.6.17.md').write_text(text,'utf8')
(R/'dist/README-unity-0.6.17-test.md').write_text(text,'utf8')
print('Phone test package ready:',apk.name,size,signature['sha256'])
