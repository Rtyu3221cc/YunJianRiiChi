# 云间日麻交接：Lua 解码与全部特效还原

日期：2026-10-29。下一对话的目标是在当前源码上继续还原剩余和牌、立直及鸣牌指示特效，按批次提供实际游戏预览。本文是最新交接；历史文档中“Lua 尚未解密”已经过时。

## 工作区与本轮状态

- 维护目录：`D:\MyWorkSpace\YunJianRiichi_Codex`。新对话可能默认落在聊天目录，所有命令显式指定工作区。
- GitHub：`https://github.com/Rtyu3221cc/YunJianRiiChi`，`main`。发布副本为 `.publish/YunJianRiiChi`。R20 已推送为 `aed3a85e601043eff063a018d1c40f7cd64ea6f7`；后续 R21 推送结果见 `build/source-push-r21/published.json`。
- 维护目录原本就有大量修改和未跟踪文件。不要 reset、clean、批量删除或一键提交全部目录；代码推送在发布副本内完成。
- 当前源码包含 R16 静态装扮扩展、R18 原版拖动／手内换序、R19 Fate 和牌特效、R20 自摸手放大，以及 R21 拍桌／收手分离。自摸手缩放保持 `0.48`；牌在拍桌时固定，不再继续跟随骨架后半段二次下落。声音、手部寿命和释放后的特效按各手型源配置分别处理，已移除自摸固定 `1.14`／`1.16` 秒等待。其他动作仍使用原有缩放。
- 最近本地 APK 仍为 `dist/yunjian-riichi-unity-0.6.17.apk`，504,198,384 字节，SHA-256 `c3057d3547409ffe9ff0005447ee042de3bbdc57099750353f90b6d778114976`。它包含 R16／R17，**不包含 R18～R21**。本轮只同步源码、资源、文档和预览，不构建新 APK、不建立新版 Release。
- `ProjectSetup.ReleaseVersion`／`ReleaseCode` 仍为 `0.6.17`／`65`；这不是新构建完成的证据。下一次用户要求安装包时，再按新版本构建并验证。

相关预览：

- R16：`dist/previews/2026-10-28-wardrobe-r16/index.html`，静态装扮实际截图。
- R18：`dist/previews/2026-10-29-original-input-r18/index.html`，25.4 秒拖动／换序／手切／摸切视频。
- R19：`dist/previews/2026-10-29-fate-r19/index.html`，27.6 秒 Fate 荣和、自摸视频。
- R20：`dist/previews/2026-10-29-tsumo-r20/index.html`，自摸手调整前后及完整特效衔接；准确检查数、帧数见同目录 `verification.json`。
- R21：`dist/previews/2026-10-29-tsumo-r21/index.html`，修复拍桌前低位停顿与二次下落，包含慢放及正常速度。以此作为最新自摸动作预览。

### R21 自摸时序补充

旧实现虽使用原始挂点数据，却把挂点回到骨架静止姿态的尾段也当成携牌下落，并将接管固定到 1.14 秒。新实现从原版 Zimo 的声效标记之后寻找首个进入原始牌厚范围的采样帧，按该帧校准触桌落点；女性手约 0.5 秒、男性／橘猫约 0.533 秒，到点切换成真实桌面牌，手继续收回。牌尺寸、最终位置及旋转不变。

`SourcePlaybackContracts.HandAction` 读取对应手型的 Zimo 配置；`TableEffects` 分离触桌、声效、寿命和特效开始时间。声音遵循配置 cue；手使用配置 lifetime；特效仍在 release 后 0.1 秒触发，不能为了提前落牌而一并提前特效。自摸不再把旧 `landTime` 当成实际拍桌点。

四人桌四方向 × 女性／男性／橘猫手，另加两组三人桌，共 14 组、268 项运行断言通过。验证包含触桌前连续下降、切换零跳变、后续牌不动、轮询不二次下落、手继续收回、落牌声只响一次和完整 Fate／役种流程。新视频为 16.6 秒、498 帧、30 FPS，全部视频和音轨解码通过。细节见 `docs/自摸落牌时序修正-r21-2026-10-29.md`。

## Lua 已经怎样解码

固定原始包是 `dist/Majsoul_4.0.16_MC.apk`，约 1.67 GiB。APK SHA-256：

`73b28ea9c35a35ce179c0b43a13faaf72d41387a8a6ff1d9967b24a43ed318c7`

Lua 为 AssetBundle 内的 TextAsset，路径形如 `luabyte/lua/game/mj/actions/actionhule.lua.bytes`。原始文本不是普通 Lua 字节码，实际链路为：

`Base64 → AES-128-CBC → PKCS7 去填充 → 严格 UTF-8 → 必要时清理 BOM／尾部控制字节`

密钥和 IV 来自这个 APK 的 IL2CPP metadata 字段，不要手填猜测，也不要重新把整个包展开一遍。`scripts/recover-majsoul-input-lua.py` 已实现固定来源校验和恢复。参数仅通过子进程标准输入传给 Node crypto，不打印实际密钥／IV，不写入游戏资源。

已核实的定位证据：

| 项目 | 值 |
|---|---|
| metadata 路径 | `assets/bin/Data/Managed/Metadata/global-metadata.dat` |
| metadata 格式 | magic `0xfab11baf`，version 31 |
| metadata SHA-256 | `ab1e5303842daa26b6c52ce87fe3416d08210dff24c5e64c1f6e20a9860f43fb` |
| 类 | `StringObfuscator` |
| Decrypt RVA | `0x21546bc` |
| 静态构造 RVA | `0x2155b28` |
| SanitizeLuaBytes RVA | `0x215511c` |
| key 字段 | metadata 偏移 `0x5c3240`，16 字节；哈希在恢复脚本内校验 |
| IV 字段 | metadata 偏移 `0x5bdcc8`，16 字节；哈希在恢复脚本内校验 |

这些偏移只对上述固定 metadata 有效。换 APK 时若摘要不符，应重新定位，不要删除断言后继续解密。

每份恢复脚本均检查 PKCS7、严格 UTF-8、重新加密后与密文逐字节相等。`scripts/format-recovered-lua.cjs` 用本地 luaparse 按 LuaJIT 语法解析、添加空白，再比较前后 AST；没有执行原游戏 Lua。

### 现成结果在哪里

R18 输入相关 5 份：

`build/lua-reverse-20261028/decrypted/` 及其 `readable/`

文件为 `hand.lua`、`handpai3d.lua`、`handpaiplane.lua`、`mjpai.lua`、`viewplayer_me.lua`。

R19 特效相关 10 份：

`build/fate-r19/lua/decrypted/` 及其 `readable/`

文件为 `actionhule.lua`、`desktopmgr.lua`、`viewplayer.lua`、`viewplayer_other.lua`、`modelanimationcontroller.lua`、`ui_huleshow.lua`、`scene_mj.lua`、`tools.lua`、`gamemgr.lua`、`cameraratiolimitareamgr.lua`。

`decryption-checks.json`、`syntax-checks.json`、上级 `crypto-evidence.json` 保留校验。已恢复的是这 15 份定向脚本，**没有声称整个客户端 Lua 已全部解码**。

原始 APK、密文、解码明文、IL2CPP dump、工具和构建缓存保留在本地，不加入发布源码。GitHub 保存恢复工具、移植代码、运行资源和交接说明；本机下一个对话可直接读取上述已存在明文。

### 复现与扩展命令

在维护目录运行，`python`／`node` 可替换为下文给出的本机完整路径：

```powershell
python scripts/recover-majsoul-input-lua.py --profile input
node scripts/format-recovered-lua.cjs build/lua-reverse-20261028/decrypted
python scripts/recover-majsoul-input-lua.py --profile fate-r19
node scripts/format-recovered-lua.cjs build/fate-r19/lua/decrypted
```

R20 增加按精确资源路径选择其他脚本的入口，重复 `--script` 可选多份：

```powershell
python scripts/recover-majsoul-input-lua.py --script luabyte/lua/game/mj/actions/actionhule.lua.bytes --output-dir build/next-effects/lua/decrypted
node scripts/format-recovered-lua.cjs build/next-effects/lua/decrypted
```

自定义输出被限制在当前工作区 `build/` 内；同次调用不能选择重名输出文件。精确 TextAsset 路径从 `scripts/study-characters-1018.py` 的 `paths` 索引查询，不要凭名字猜包内路径。该辅助脚本通过现有 `build/reference/*.jsonl` 和 `tile-study/cab-map.json` 解析 APK 中的 bundle／依赖。

最初定位使用 `tools/il2cpp-dumper-local` 的 Il2CppDumper v6.7.46 和 Unity 自带 .NET 8。`config.json` 的 `RequireAnyKey=false`，避免无交互终端在最后 `ReadKey` 异常。相关小型定位证据在 `build/lua-reverse-20261028/`；完整临时解包库与大 dump 已清理。后续已有固定恢复脚本时通常不必重跑 Dumper。

## 特效应先看哪些源码

以下行号均指 R19／R18 `readable/` 中的文件，便于直接定位：

| 入口 | 位置与用途 |
|---|---|
| 和牌事件调度 | `actionhule.lua`，荣和／自摸、切入、推牌与结果延迟；不要只读资源名 |
| 特效入口 | `desktopmgr.lua:1074` `ShowHuleEffect`；按 `view.old_effect_mark` 分流新旧流程，读 `audio_id`，普通路径有 `2+4` 秒销毁上限 |
| 新版和牌 | `desktopmgr.lua:1133` `setNewHuleEffect`；处理多个根、动画牌、场景固定部分与特殊 ID |
| 老版和牌 | `desktopmgr.lua:1339` `setOldHuleEffect`；有多款按 ID／资源路径定制的移动、转向、遮罩与镜头行为 |
| 和牌牌面队列 | `desktopmgr.lua:1006` `SetPaopaiZtestNew`；默认队列 3090，center／top 的深度规则不同 |
| 装备数据装配 | `desktopmgr.lua:2417` 起；item/view → prefab path/audio/view，立直还有方向和动作类型 |
| 结果延迟 | `tools.lua:3501` `RonDelay`，读 `view.sargs[1]` 毫秒并除 1000 |
| 自摸手与触发点 | `viewplayer_other.lua:823` `_Hule`，读 `Zimo` 配置，将牌挂在 `hand.node_tile`，按 keypoint 放下，再延迟 0.1 秒触发特效 |
| 手动作配置 | `modelanimationcontroller.lua` 与现有 `playback-contracts.json`；原始动作和时长不要替换成猜测 |

`root_move` 是仅跟随牌位置，`root_rot` 还跟随旋转，`root_ani`／`root_ani_zimo` 可能含实际牌面动画，`root_static` 留在场景原点，`root_fly` 等需要核对 Lua 分支。原源码 `-1000` 来自 `scene_main/Root_Scene` 原点偏移，不能直接当作独立全屏相机或保留到本项目世界坐标。

R19 证明了源码调度可以移植，但目前新的根绑定分支仅对 `FateR19/` 启用。不要直接把所有新资源改名放进这个目录；先按每款 `view`、根层级和特殊 Lua 分支设计明确合同。

## Fate 已实现内容与复用边界

物品 `308041`，资源 `ron_25hf`，当前本地装备 ID `ron-fate-hf`，名称「咒层界・恶念祝祭」。

- 原 APK 提供相关 Lua，但它的已收录 bundle 没有该和牌资源组。图形来自已有固定原生清单 **4.0.51／0.16.289** 的官方 bundle；网页配置和声音使用固定 **0.11.252.w**。三种版本号不可混称同一版本。
- 213 个节点，43 层粒子，114 个模型／贴片。原生编译动画 `fx_skill_079_ron_unity` 长 6.483333 秒，Unity Playables 60 FPS 采样出 442 组变化通道。
- 特殊 Renderer 绑定中有材质参数，不能一律解释为 `enabled`。此动画的显隐由 `_Alpha` 驱动，`Renderer.enabled` 本身没有变化。
- 新版根绑定区分场景原点和牌位置，结合本项目镜头做世界 Y 轴 180 度适配；这不是所有其他特效都适用的全局转向。
- `sargs=3300,1` 对应结果额外延迟 3.3 秒，牌面队列保持 5.3 秒后恢复；六秒按源入口销毁。其 `root_ani`／`root_rot` 是空辅助节点，没有移植别款的牌面运动来冒充。
- `YunJian/FateParticle` 独立着色器按原始 GLES 程序还原 Main、Mask alpha、Distortion green、DMask red、溶解内外 smoothstep、ambient、UV 变换与混合／深度状态。源材质 `DEF_*` 关键字控制启用分支。不能假定它覆盖后续所有 Shader。

关键实现：

- `NativeParticleEffect.cs`：原单位模拟、原相机层 26、分步动画／粒子采样及 Fate Lua 根绑定。
- `NativeEffectMotion.cs`：变换、材质 float/color/vector、active 和可选 renderer 启闭通道。
- `NativeWinningTileQueue.cs`：原单位相机内临时绘制精确牌面模型，队列 3090；轮询替换模型后重绑、到时和销毁时恢复，避免改共享材质。
- `WinTileEffect.cs`、`TableEffects.cs`：按实际胜者冻结装扮播放、公开事件去重、原时限销毁及结果页衔接。
- `NativeEffectR11Import.cs`：完整粒子模块、网格、内建 Quad、原始编译动画采样；有独立 `RunFateR19`，不覆盖已批准的 R11。
- `Resources/Majsoul/NativeReview/FateR19/`、`Actions/native-ron_25hf.json`、`Wardrobe/provenance-r19.json`：运行资源与来源。

R19 142 项实际 Unity 检查、5 项视频检查和 185 项 Node 测试通过。R20 新跑 32 项检查，覆盖四方向、女性／男性默认手与橘猫手、牌尺寸与落点，以及完整自摸 → Fate → 役种页；19.6 秒视频共 588 帧，全部解码通过。Node 185 项、Python 角色 8 项和兼容门禁 3 项再次通过，日志保存在 `build/source-push-r20/`。这些是编辑器实录和专项检查，**未做原版 Fate 录像逐段对照，也未做 Android 真机验证**。

## 下一对话还原所有特效的入口

先读 `docs/effects-restoration-index-r20.json`，由 `scripts/index-effects-r20.py` 生成：

- 固定公开配置含 60 条和牌、57 条立直、3 条鸣牌指示，共 120 条定义；包含历史／内部项，不是可购清单。
- `remainingFrom0616` 从旧的 93 条队列扣除 Fate 后剩 **92 条**，保留原生 group 缺失项，不能把找到清单等同于已找到完整资源。
- 另列 **11 个**尚未映射公开定义的原生资源键，后续也需审计，不能仅凭旧网页配置宣布“全部完成”。
- 鸣牌指示已有跨类别试装款，不能把这些试装数量计作新增官方定义。普通和牌默认、历史别名及同资源条目也要去重。

“全部”建议按可审计批次推进：

1. item ID → view/res_name／icon 别名 → 原生 group／prefab → 共享依赖，建立每项状态。原生清单本地入口 `build/effects-native-manifest.json`；固定资源 URL 模板及已获取 bundle 在现有来源记录中。
2. 先查 APK 路径索引；缺少时复用已缓存官方固定版本 bundle，仅补该项及必要依赖。不得下载整个新客户端或重新解包全部 APK 来试错。
3. 先读 Lua 新旧分支和特殊 ID，再审计全部组件、材质关键字、贴图槽、网格、编译动画、粒子模块、声音和方向变体。未知组件／Shader／动画属性明确列为待补，不能默默丢弃。
4. `scripts/import-native-effects-r11.py --fate-r19` 是安全的单批次扩展示例。默认 R11 的 WIN／RIICHI 列表仍是老批次；新增其他批次用独立输出目录、审计文件及 Unity 入口，禁止无脑重跑覆盖所有已批准资源。
5. 编译动画继续通过 Unity Playables 采样真实变化的通道。材质动画不能只导出 Transform；若有贴图切换、Light、Camera、Trail、子发射器或脚本控制，先补实现和证据。
6. 根据源码处理根节点、动作挂载、牌面队列、延迟与销毁。原生 authored units 由 layer 26 相机转换，不能为了单款修改整张牌桌尺度。
7. 接入 Wardrobe catalog、服务器白名单、本局冻结 loadout 和音效。校验其他玩家装扮、轮询／重连不重播、换局清理、四方向、荣和／自摸、普通和牌／满贯以上等实际路径。
8. 每批生成真实游戏视频，区分“参数移植通过”“原版画面对照通过”“用户批准”“安卓实机通过”。用户不要求 APK 时不自动构建。
9. 扩大资源数量时同时记录实际纹理／网格／粒子数量和内存。结束销毁 GameObject 不等于纹理马上释放；现有缓存与资源卸载尚没有完整总预算，不能承诺全量装扮的手机内存或帧率。

可先从旧交接建议的歌剧魅影／魔法少女／阖家欢及配对立直入手，再处理群星之寂、厄里斯之瞳、天降正义等有特殊控制的组。先分类、逐批完成，目标是覆盖所有可定位条目，遇到确实缺资源的项保存缺口清单，不用其他特效冒充。

## 必须保留的已有决定

- 问心之镰 R13 `nativeWorldEuler=[0,180,0]`，原生四方向变体，用户批准的竖直下劈；只跟随牌的位置，不再次叠加牌旋转。
- 结算横幅仅满贯及以上，按实际 han/fu／役满判定，不能靠含本场／立直棒的总分；普通和牌直接进入役种页。
- 手部 UI 只显示默认手和橘猫手；默认手按角色性别选择。兼容 ID 仍保留，不重新显示旧五种手部。
- 原版输入保留按下即选、距离严格大于 20 屏幕像素才开始拖动、按手牌投影 Y 边界决定松手出牌／取消、手内换序及再次点击同牌出牌。
- 源码 `auto_liqi` 是自动**理牌**，不是自动立直。排序之后仍通过物理牌身份区分摸切 `_` 和手切；轮询不重置手势／顺序，非法操作仍由原引擎白名单拒绝。
- 和牌听牌提示不被同一决策的 presence 轮询关闭；后台宽限为完整 120 秒，12 秒只作在线状态显示。

## 复现工具、磁盘与发布

本机运行路径：

```text
Python: C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
Node: C:/Users/Rtyu3221/AppData/Local/Programs/node/node.exe
Unity: D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe
.NET: D:/Unity/Lib/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe
```

UnityPy 位于 `tools/asset-inspector`，旧包 fallback `2022.3.62f2c1`；luaparse 位于 `tools/lua-parser-local/luaparse.js`；视频解码／编码 PyAV 位于 `tools/video-inspector`。这些本地工具目录不上传，克隆到其他机器时需要单独恢复依赖。

`python scripts/prepare-original-input-stage.py --batch fate-r19`、`--batch tsumo-r20` 或 `--batch tsumo-r21` 创建精简临时工程：只有不可变媒体使用硬链接，文本和 meta 为副本，排除可选的 `CharacterPacks/Source`。不要在这些硬链接媒体上编辑像素。录制直接向 PyAV 管道发送 RGBA，没有逐帧 PNG 大缓存。运行入口见 `FateR19Preview.Study/Movie`、`TsumoHandR20Preview.Run`、`TsumoContactR21Preview.Run`；环境变量分别为 `YUNJIAN_R19_REPO`／`YUNJIAN_R20_REPO`／`YUNJIAN_R21_REPO`。

Unity 在沙箱内会遇到许可／UPM IPC 问题，当前有效方式是本机进程以 Hidden 窗口启动，单次一个 Unity，读取日志并核对退出码。R21 首次导入遇到系统内存不足，仅关闭该批次残留后以 `-job-worker-count 2 -assetImportWorkerCount 1 -force-d3d11` 复用缓存成功完成；低内存时优先限制导入并发。完整角色源素材在精简工程中被排除，不能将对应角色图片单元测试宣称重新通过。

此前已在用户授权下清理项目生成的 C／D 盘缓存约 46 GiB；记录在 `build/cleanup-20261028/`，没有清除原 APK、源码、原始分析、最终 APK、预览视频或签名密钥。主工程 `unity/Library` 保持不存在；录制完成后核对最终资源已回存维护目录，再按精确路径清理本批次 `build/<batch>/stage`。不要笼统删除整个 build／tools／dist 或共享 SDK。

推送只在 `.publish/YunJianRiiChi` 按清单同步，先检查远端 main、变更范围、凭据扫描和测试。不得提交原 APK、解码明文、SDK、密钥、运行快照或临时 Library；完整源码 ZIP 只留本地、不上传。当前用户只要求本轮源码 push，不代表要求上传旧 APK 充当新构建。推送后核对远端 ref 与关键文件内容，不回下载发布附件重复验证。发布结果以相应 `build/source-push-r20/published.json`／`build/source-push-r21/published.json` 和实际远端提交为准。

## 下一对话可直接粘贴

> 在 D:\MyWorkSpace\YunJianRiichi_Codex 继续云间日麻。先读 docs/handoff-lua-effects-2026-10-29.md 和 docs/effects-restoration-index-r20.json，再沿用已恢复 Lua 与固定原生资源逐批还原剩余全部和牌／立直／鸣牌特效，包含原生清单尚未映射的条目。相关 Lua 已定向解码成功，不要重新从“Lua 加密读不了”开始。先核对 DesktopMgr 的新旧流程和每个特殊 ID，完整导入材质、网格、粒子与编译动画，提供实际游戏视频；没有资源或尚未支持的组件要保留缺口。保留 Fate、原版拖动、自摸手 0.48 与 R21 拍桌即落稳／收手分离、满贯才切入横幅、镰刀竖劈等已确认结果。磁盘要节省，使用精简临时工程，不自动打 APK，不把导入成功当作与原版画面完全一致。
