# 云间日麻 0.6.16 对话交接

日期：2026-10-28。后续工作是在没有原版装扮预览的情况下，继续还原尚未导入的装扮特效。先读本文件，再检查当前工作区；不要重新从“寻找原版游戏／录制所有特效”开始。

发布约定：用户已明确要求 **完整源码 ZIP 仅留本地备份，不上传 GitHub**。仓库保存代码；Release 提供 APK、校验与交接资料及发布 ZIP。不要重复上传手工源码 ZIP。GitHub 自动生成的 Source code(zip/tar.gz) 链接不属于手工上传附件。

## 工作区、发布位置与安装包

- 维护工作区：`D:\MyWorkSpace\YunJianRiichi_Codex`。新对话的默认目录可能是聊天目录，执行命令时请显式指定项目目录。
- 发布副本：工作区内 `.publish/YunJianRiiChi`，GitHub 仓库为 <https://github.com/Rtyu3221cc/YunJianRiiChi>，分支 `main`。维护工作区本来就有大量未提交变更，不要 reset、clean 或一键提交全部目录。
- 本版 APK：`dist/yunjian-riichi-unity-0.6.16.apk`，包名 `com.yunjian.riichi.unity`，versionCode **64**。
- 大小：**215,009,145 字节**，约 **205.05 MiB / 215.01 MB**。
- SHA-256：`dc39293c5ab29d4636f15698fe158e0d4b0d0b038f6d2bdd19a96edf409bbb4f`。
- 保留原证书：`d1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f`，支持覆盖安装；联机手机请统一到本版。
- [APK 下载](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.6.16/yunjian-riichi-unity-0.6.16.apk)；[发布页](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-0.6.16)。以实际可见发布页及本机 `build/release-0616/published.json` 为发布完成证据；本文档随本版源码和附件提供，不自称已完成手机安装。
- 校验、交接、源码 ZIP 和 release ZIP 都在 `dist/`。精确构建检查见 `dist/unity-0.6.16-verification.json`；远端提交号和附件上传大小／摘要在 `build/release-0616/published.json`。

## 当前已完成内容

1. 已导入并复现 **13 款和牌、11 款立直**原生特效，包含 27 个预制体、312 层粒子；额外 3 个预制体来自问心之镰的四方向变体。默认和牌与 4 个可见鸣牌指示另计。
2. K.O. 文字朝向、猫过留痕、雪球扩散方向已修正。问心之镰在 R13 改为用户确认的竖直下劈。
3. 装扮右侧图标区在框内裁切，支持触摸拖动、滚轮、滑块，切换类别会复位滚动。
4. 手部 UI 只有 **默认手部／橘猫手**，已删除“默认手部随角色性别切换／当前角色……”两行解释。默认手实际仍由角色性别选择；`sex=1` 女性、`sex=2` 男性。一姬为女性、明智英树为男性；不要再把卡维当成男性测试样例。
5. 点数结算和终局页按每位玩家**本局冻结装扮**绘制头像框；三人、四人及默认框／猫耳框混合均已检查。
6. 荣和／自摸按钮出现时，听牌面板的展开／收起不再被同一决策的轮询清空；点击过程中收到轮询也不丢失手势。结果页、新局面、失焦会清除提示。
7. 切到后台后，**完整等待 120 秒**才离线托管。12 秒在线状态阈值只用于状态显示。切后台前已空闲很久也有新的 120 秒宽限；重复后台通知不会重置计时。客户端发送后台 presence 不再被 Busy 轮询状态挡住。

## 核心文件与不可退回的决定

- `unity/Assets/Scripts/LobbyController.Wardrobe.cs`：列表裁切、滚动、两种手部和预览入口。
- `WardrobeCatalog.cs`：目录、默认手性别解析、旧 ID 兼容。旧的 `hand-cat`、`hand-man`、`hand-dog` 仍保留用于历史配置及协议兼容，不代表应重新显示为五种 UI 选项。不要删除服务器白名单里的这些兼容 ID。
- `CharacterRoster.cs`、`Resources/Majsoul/Characters/catalog.json`、`CharacterDirectory/catalog.json`：性别数据。三个角色资源生成脚本已同步保留 `sex`，不要在下一次生成目录时丢失。
- `ResultPresentation.cs` 的 `Avatar`：两种结算页共享的头像框绘制，使用 `WardrobeCatalog.AvatarFrameRect` 对齐素材的头像窗口。
- `RiichiClient.cs` 的 `ObserveWaitState`：用公开局面／手牌决策标识保留听牌开关，**不要改为每次 version 变化就关闭**；version 也会因 presence 更新变化。
- `app/src/main/assets/engine.js` 的 presence 与 resume：`awaySince` 和 120 秒离线宽限。副本为 `unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js`；两份必须同步。`tests/background-takeover.test.cjs` 覆盖后台宽限。
- 原生播放：`NativeParticleEffect.cs`、`NativeEffectMotion.cs`、`NativeEffectCamera.cs`、`WinTileEffect.cs`、`Resources/Majsoul/NativeEffectParticle.shader`。使用原始粒子模块、网格、材质参数及真实动画采样，而非手画近似轨迹。
- 原生相机层为 **26**，保持 authored source units。不要为了单个特效修改整张牌桌的坐标体系。
- **问心之镰：`nativeWorldEuler=[0,180,0]`**。保留原生四个方向预制体；只跟随牌的位置，不叠加横放牌或携牌的完整旋转。R12 的横扫姿态已被用户否定，R13 的竖直下劈已获确认。
- `ProjectSetup.cs` 的 Always Included Shaders 必须包含 `YunJian/NativeEffectParticle`，防止安卓剔除动态创建材质使用的 shader。

## 已有参考与资料存放

所有原版录制、最终游戏预览、来源清单统一保留在本地：

`dist/previews/2026-10-27-effects-reference/`

- `index.html` 为总入口，`r12.html` 为四项修正，`r13.html` 为镰刀最终同步对照。
- `original/` 为原版视频，`game/` 为游戏预览，`source/` 为必要原始资料和检查清单。该目录的原版录像、原生 bundle、WebGL data 均仅作本地参考，没有随本版源码批量上传。
- `source/bundle-index.json`、`effect-bundles.json`、`animation-sources.json`、`native-import-checks.txt` 是下次导入的实际证据入口；原始临时帧和失败试录已清理，不要依赖不存在的帧缓存重新包装。
- 原版运行客户端为 **4.0.51**、原生资源清单 **0.16.289**；公开旧网页配置入口当前为 **0.11.252.w**，两套版本号不可混为一个。
- R13 同步关系：原版视频约 `4 + 1/30` 秒对应游戏预览 0 秒。原版 30 FPS、新预览 60 FPS，测试局面与镜头也不同，不要写成逐像素一致。
- 此轮静态截图：`dist/previews/2026-10-28-wardrobe-r15/`，8 张实际 Unity 画面、58 项检查、`index.html`。本轮没有重新录视频。
- 用户当时的原版入口是：寻觅 → 查看详情 → 向下滚装扮列表 → 选装扮 → 播放 → 等加载完成。只能覆盖部分装扮，不要求用户逐个录视频。
- Edge 自动化曾多次卡住，用户已改用 Codex 内置浏览器。后续真的需要 UI 参考时优先复用已登录内置页；不要假定浏览器当前仍运行或仍登录，也不要读取登录凭据。

## 下一步：无法预览的装扮，怎样继续

**结论：有把握继续资源驱动地制作；没有原版画面时，不能承诺最终视觉已完全一致。**

目前已经证明这条流程能处理普通和牌／立直粒子、网格粒子、遮罩动画、透明度曲线、编译动画和四方向绑定。原始资源完整且使用已支持组件的特效可以先批量还原，无需让用户录制每一个。对新的自定义 shader、脚本控制顺序、相机后处理、缺失依赖，先单独审计和实现，禁止用相近图标或其他特效冒充完成。

本次从公开 item/view 配置建立了候选索引：**93 条未导入和牌／立直记录，其中 90 条存在对应原生 3D 资源组**。记录含历史条目，且部分旧网页 res_name 与新原生目录不同；这不是“93 个全部可下载、可运行且不能预览”的保证。每条原版预览可用性尚未逐项检查。已实现条目已通过名称与资源别名去重。

- 附件：`dist/unpreviewed-effects-0.6.16.json`。
- 每条保存 item ID、中文名、类别、源资源键、对应资源组、历史标记与检查状态。
- `status=manifest-found-not-imported` 仅表示候选索引状态；实际是否找到看 `groups` 是否为空。`previewAvailability=not-checked` 不应改写为已验证无法预览。

建议第一批从以下 6 款开始，均已在当前 manifest 找到资源组，但**本版尚未导入或判定兼容**：

| 类别 | 名称 | item ID | 原生资源键 |
|---|---|---:|---|
| 和牌 | 歌剧魅影 | 305201 | `ron_gejumeiying` |
| 和牌 | 魔法少女 | 305202 | `ron_mofashaonv` |
| 和牌 | 阖家欢 | 305203 | `ron_20chunjie` |
| 立直 | 非常事态 | 305301 | `effect_liqi_feichangshitai` |
| 立直 | 魔法少女 | 305302 | `effect_liqi_mofashaonv` |
| 立直 | 鼠生威 | 305303 | `effect_liqi_20chunjie` |

随后可考虑群星之寂／星陨、厄里斯之瞳／血·朗基努斯枪、天降正义／断罪等配对。优先级是按资源目录与实施批次给出的建议，未宣称这些都是简单 shader 或低成本特效。

实施顺序：

1. 固定当前源版本，按候选 ID → view/res_name → 原生 group → prefab 路径核对。当前原生清单入口为 `https://game.maj-soul.com/assetbundles/DXT/bundle_info_so.majset`；已有本地清单可用，避免无意义重复下载。
2. 只取所需 group 及共享依赖；核对内容哈希、粒子层数、材质／纹理／遮罩、网格、动画、声音及多方向变体。不要只下载单个 prefab 所在文件而漏共享 bundle。
3. 对原始组件和 shader 做兼容性审计，先给每款打上“可直接还原／需补组件或 shader／资源不足”的状态。未知字段不能默默丢弃；`scripts/test-effect-compatibility.py` 是最低层导入门禁，不能把通过它等同于原生图形全兼容。
4. 扩展现有 `scripts/import-native-effects-r11.py` 的候选输入与 `NativeEffectR11Import` 流程，避免重写播放器。现有脚本的 WIN／RIICHI 列表写死为老批次，不能直接无脑重跑覆盖已批准特效；先增加明确批次输出和回归保护。
5. 编译动画继续通过 Unity Playables 采样；保留真正变化的通道。原始四方向 prefab 已带朝向的不能再叠加牌旋转。
6. 接入当前 Wardrobe catalog、服务器白名单和本局冻结 loadout；验证未知 ID 拒绝、轮询／重连不重播、每人各自装扮以及四个座位方向。
7. 用实际游戏渲染做预览：首帧、峰值、收尾和四方向；本轮用户只要截图时就提供截图。对于没有原版参考的条目明确标为“源参数还原，未做原版视觉对照”；有资源数据不等于用户已经批准最终画面。
8. 不要一次塞入全部剩余装扮。每批少量完成资源／运行／内存检查，再交给用户看游戏效果；用户决定是否打 APK 真机测试。

## 包体与运行内存口径

- 官方公开配置 category=5 共 **486 条定义记录**，并非当前商店可购买清单；原生清单另有未映射较新条目。分类数量与估算见 `docs/装扮结算听牌修正与数量估算-r15-2026-10-28.md`，审计数据在 R15 预览目录的 `decoration-audit.json`。
- 装扮全部内置的 APK 总大小 **650 MB～1 GB 是估算**，不是本版包大小，也不是手机运行内存。该估算不含额外全角色立绘、皮肤、语音、表情、全部加载 CG。
- 已内置 29 项特效贴图基础占用约 0.3～13.5 MiB／项，跨项去重合计约 95.53 MiB。数据按 0.6.15 APK Texture2D 有效像素体积核算，资源在 0.6.16 未改变；包含 mipmaps，不包含粒子／模型／音效／驱动／整场景内存，不是 Android PSS 或播放峰值。
- `docs/已内置特效内存估算-2026-10-28.md` 有逐条数据。当前默认未压缩 RGB24／RGBA32，后续若改 ASTC／ETC2，需画质与设备支持验证，不能直接承诺百分比节省。
- 播放结束销毁实例并不等于纹理马上释放。当前没有完整的特效资源卸载与缓存上限；若继续增加装扮，应设计按需加载、合理的缓存预算及场景切换卸载，避免在每次播放中途做阻塞卸载。
- 新特效的峰值粒子数、透明叠加面积和 shader 成本也影响帧率，不能只看 MB 判定性能。

## 本版验证与已知边界

本轮新跑：

- Node：**181 项**，包括游戏规则、本局冻结装扮、资源契约与后台 120 秒接管；发布副本也再次跑相同 181 项。
- Python：角色输入 **8 项**、导入兼容门禁 **3 项**，合计 **11 项**。
- Unity 编辑器单元：62＋87＋385＋37＝**571 项**。
- Unity 实际运行：**58 项**，包含右侧拖动／滚动、手部性别、三／四人结算头像框、荣和／自摸下连续轮询、点击途中轮询、手动收起、十三面听牌、选牌预览、取消拖动和失焦。
- 角色目录：122 角色、486 服饰注册一致；这里的“486 服饰”和“486 装扮定义”是两个独立统计，不要混淆。
- 构建与分发 APK：ZIP 完整性、当前离线引擎精确一致、主机 JNI、ARM64／ARMv7、v1／v2、独立 API-23 v1、原证书与 16 KiB 对齐；另核对 24 款动作包装、27 个原生 prefab、312 层粒子、模型、动画、材质、shader 保留与两种可见手部。
- release ZIP 内的 APK 与本地分发文件核对字节数／SHA-256并独立签名校验。发布后只检查上传大小／摘要、远端 ref 和源码内容，**没有回下载 APK／ZIP 做重复验证**。

历史 R13 证据另列：620 项特效四视角检查、13 项镰刀几何检查，资源未变，本次没有把它们伪装成本轮新跑的完整对局回归。

已知旧问题：完整对局回归中的拔北牌间距断言曾在基线装扮下同样失败（约 `gap=.02413097`），本轮未改对应布局、未放宽断言。因此只能报告上述已通过范围，不能写“完整游戏回归全部通过”。

尚待用户真机检查：覆盖安装、实际触控、音效、后台前 120 秒不托管／到时托管、返回前台、多手机热点局域网、设备 GPU 渲染／帧率与峰值内存。编辑器检查及包签名不代表这些真机行为已验证。

## 常用命令和工具

- Unity：`D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe`。
- Node：`C:/Users/Rtyu3221/AppData/Local/Programs/node/node.exe`。
- Python：`C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`。
- `node --test --test-isolation=none tests/*.test.cjs`；Windows 不用默认隔离进程，避免 spawn EPERM。
- `python -m unittest discover -s tests -p 'test_*.py'` 与 `python scripts/test-effect-compatibility.py` 分别运行；后者文件名含连字符，普通 discovery 不会发现它。
- Unity 单元：`scripts/verify-unity.ps1 -Mode Validate`。Android：同脚本 `-Mode Android`，自动同步 offline assets、编译 Java host、双架构 IL2CPP 构建及签名。使用 `Start-Process -WindowStyle Hidden`，不要同时启动两个 Unity。
- 最终 APK：`python scripts/verify-native-effects-apk.py`。`--resume-native` 只在同一 APK、相同源码、已通过的完整 gate 和不可变校验日志都匹配时使用。
- 此版发布脚本：`scripts/release-0616.py`，有 preflight／assemble／curate／commit／publish 五个显式阶段。只在发布副本提交；不要在维护工作区乱提交。原始工具、密钥、Library／Temp、运行快照和原版参考包不可上传。
- 本地 UnityPy 来自 `tools/asset-inspector`，解析旧原生源包 fallback 为 `2022.3.62f2c1`。这些工具不随源码发布，导入工具使用前需恢复本机依赖。

## 新对话可直接粘贴

> 请先阅读 `D:\MyWorkSpace\YunJianRiichi_Codex\docs\handoff-unity-0.6.16.md`，在当前维护工作区继续云间日麻。下一阶段是复原目前没有原版预览的装扮特效：先查看 `dist/unpreviewed-effects-0.6.16.json`，按原生 bundle／prefab／粒子／动画／shader 完整性审计，首批可考虑歌剧魅影、魔法少女、阖家欢及对应立直候选。不要要求我逐个录视频，也不要把资源导入成功当成已和原版核对。保留问心之镰 R13 固定世界方向竖直下劈、两种手部、结算头像框、听牌轮询修正和后台 120 秒宽限。分批提供真实游戏预览，获得本轮明确要求后再生成 APK；不要重复下载已发布附件验证。先报告候选中哪些能按现有播放器还原、哪些缺组件或 shader，再实施这一批。
