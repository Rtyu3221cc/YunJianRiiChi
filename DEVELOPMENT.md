# 当前开发进度／Codex 与 Claude Code 交接

- 更新时间：2026-11-22 00:23（东京时间）；上次执行：Codex（0.6.40 / 88 构建、核验与源码发布准备）。
- 共享 Git 根目录：`E:\MyWorkSpace_E\YunJianRiiChi`；Unity 工程：`unity/`；版本：6000.6.4f1。
- 当前分支：`main`；本机 HEAD、origin/main 均为 `e9b35830`。本次未联网刷新远端引用。
- 本轮将 0.6.33–0.6.40 的源码、资源与交接纳入显式提交清单；保留旧 APK 本机删除和未选历史产物，远端提交状态在本节最终回填。
- 最新可安装包：**0.6.40 / 88**（2026-11-22 构建），包含玩家页 R4–R7 与原有 R40/R41；下一次出包用 **0.6.41 / 89**。
- 20:55 Codex 轮只完善了交接文档。21:56 Claude 轮按用户对成片的两条反馈做了 R41 修正（见下节），没有提交、推送、出包、发布或清理缓存。

## 0.6.40 / 88 构建与源码提交（Codex，2026-11-22）

- 用户已明确授权生成 APK、提交并推送 GitHub、更新交接。本机 [APK](dist/yunjian-riichi-unity-0.6.40.apk)，368,081,448 字节；SHA-256 `d988b49eeffc53248e59f6008506f7666056e4c3964e01712926018b2bb25cce`。
- v1/v2、独立 API-23 v1、原证书、ZIP 与 16 KB 对齐通过；Node 209、Python 8 通过。构建强制重新导入 20 张玩家页纹理，APK 内再次检查原尺寸、单级 mipmap、可读遮罩和原字体；核对 7728 个役满与 100 个 Profile 资源。
- ProjectSetup 固化称号／使用中／立绘框／页签／遮罩的导入规则，防止重新导入覆盖手工 meta。版本常量及主 ProjectSettings 均为 0.6.40 / 88。保留此前 Saber 兼容覆盖，以 `scripts/phone-test/CharacterPackService.patch` 记录，并提供分步等价的构建入口。
- [本轮交接](docs/handoff-unity-0.6.40.md)、[基础核验](dist/unity-0.6.40-verification.json)、[功能核验](dist/unity-0.6.40-feature-verification.json)、[玩家页入包核验](dist/unity-0.6.40-profile-verification.json)。原始日志在 build/release-0.6.40/。
- 源码与交接文件已准备提交到 main，远端结果待最后回填。APK 不进 Git，未创建 GitHub Release；Android 真机未测。旧 APK 的本机删除和历史未选预览不加入本次提交。
- 暂存工程现为本版构建输入，保留 CharacterPackService 的原覆盖；后续仍在主工程修改。下一步在手机上覆盖安装，查玩家页触摸／滑动、段位终局、双手机同步和 R41 GPU 效果。

## 玩家信息页 R7：使用中比例与立绘边框（Codex，2026-11-21）

- 使用中原图 130×99 旧导入为 128×128，禁用 NPOT 重采样与 mipmap 后恢复原比例。立绘框恢复原始 120×120，保留原 50 像素九宫格；立绘底图恢复 304×650。既有 PNG 和 GUID 不变。
- 补回原版 UI_PlayerInfo 节点 16 的 waitingroom_mask 原素材及 36／37／37／34 边框，将非零 alpha 轮廓合并为缓存裁切区，修正矩形立绘透出边框底角的问题。只修改玩家页局部绘制，保留 CharacterFraming 和存档规则。
- [使用中放大对照](dist/previews/2026-11-21-player-info-r7/使用中标记-原版与修正对照.jpg)、[立绘底角修正](dist/previews/2026-11-21-player-info-r7/立绘底角-修正前后.jpg)、[完整边框对照](dist/previews/2026-11-21-player-info-r7/立绘边框-原版与修正对照.jpg)、[10 秒实录](dist/previews/2026-11-21-player-info-r7/玩家信息页-R7-比例与边框预览.mp4)。
- Unity 85 项、Node 209 项、Python 8 项通过；复看 16:9／20:9。视频 200 帧全部解码并核对方向。细节与已知预览日志异常见 [修正说明](docs/玩家信息页使用中比例与立绘边框修正-2026-11-21.md)，证据在 build/player-info-source-r7/review/。
- 主工程已更新；暂存工程恢复，CharacterPackService 原覆盖保留。下次构建同步玩家页源码、3 份纹理 meta、新遮罩及 meta、native-art.json，连同 R4–R6 修改一起出包。APK 仍为旧 0.6.39 R1；未构建、发布、测 Android 真机、提交或推送。

## 玩家信息页 R6：称号比例与字体／页签（Codex，2026-11-21）

- 修正 14 张称号被 TextureImporter 的 NPOT 重采样改成 256×64 的问题，恢复 338×65／66 原始尺寸，保留 PNG 和原 GUID。相同位置的首枚称号可见区域从 222×52 恢复为 288×52，宽度恢复、高度相同。
- 场别页签按原 75×75 素材的 20／35 边框绘制九宫格，禁用两个页签纹理的 NPOT 重采样。成就标题和场别标签直接使用原版 fengyucumaokai 40 号字，计数使用原版 haolong_0。标签按参考片保留「段位場／友人場／活動場」字形；场别数据键不变。
- [称号放大对照](dist/previews/2026-11-21-player-info-r6/称号比例-原版与修正对照.jpg)、[字体与页签修正前后](dist/previews/2026-11-21-player-info-r6/成就栏与页签-修正前后.jpg)、[原版对照](dist/previews/2026-11-21-player-info-r6/成就栏与页签-原版对照.jpg)、[16 秒专项预览](dist/previews/2026-11-21-player-info-r6/玩家信息页-R6-两处修正预览.mp4)。
- Unity 专项 76 项、Node 209 项、Python 8 项通过；复看 16:9／20:9，成片 320 帧全帧解码。原字体与两个页签素材的引用、尺寸、来源见 [修正说明](docs/玩家信息页称号比例与页签字体修正-2026-11-21.md)；运行证据位于 build/player-info-source-r6/review/。
- 主工程已更新，暂存工程恢复。下一次构建同步玩家页源码、16 份纹理 meta 和新增 Profile/Fonts 及 meta。APK 仍为旧 0.6.39 R1；未出包、未测 Android 真机、未提交或推送。成就计数仍为不可用占位。

## 玩家信息页 R5 逐组件精修（Codex，2026-11-21）

- 按原视频、UI_PlayerInfo / UI_TitleBook / UI_ChangeName 及布局组件逐项核对，输出 [17 组原版／R4／R5 局部对照](dist/previews/2026-11-21-player-info-r5/index.html)。修正文字尺寸和颜色、分区底纹、牌列比例与间隔、雷达／高光／走势位置、称号簿行位置和改名弹窗；保留原版隐藏节点状态。
- [原片与本轮视频](dist/previews/2026-11-21-player-info-r5/玩家信息页-原片与组件精修对照.mp4)、[完整交互视频](dist/previews/2026-11-21-player-info-r5/玩家信息页-R5.mp4)。走势使用原图圆点、高分猫图标及逐点展开；改名使用透明输入样式、原版占位提示和“我取好了”文字。
- Unity 100 项检查通过，含 16:9／20:9 的原版点击坐标、上下限、空昵称、弹窗拦截、空列表及滚动。Node 209、Python 8 项通过。两支视频全帧解码，核对 25 个走势点和 24 条连接。
- [逐组件核对说明](docs/玩家信息页逐组件核对-2026-11-21.md) 记录具体坐标、图片来源及数据边界。成就条使用不可用占位“—”；风格算法、称号解锁和本地免费改名规则不变。活动场仍兼容 competition 存档键。
- 主工程已更新；暂存工程录制后恢复，原有 CharacterPackService 覆盖保持。APK 仍为 0.6.39 R1，未出包、未测 Android 真机、未提交或推送。下次构建前同步玩家页源码、Profile/native-art.json 与新增 Native 原图及 meta。

## 玩家信息页 R4 源码核对（Codex，2026-11-21）

- 用户确认具备界面和素材复制使用授权。已核对本机 4.0.16 的 UI_PlayerInfo、UI_PlayerTitle、UI_TitleBook、UI_GetTitle，及主界面 792 节点的预制体。按原画布尺寸修正主面板，替换称号图片、高光徽章、立绘框、页签、字体和使用中标记。纠正 R3 将获得称号弹窗背景用于单个称号的错误。
- [原录像与本轮对照视频](dist/previews/2026-11-21-player-info-r4/玩家信息页-原片与源码修正对照.mp4)、[完整交互预览](dist/previews/2026-11-21-player-info-r4/玩家信息页-R4.mp4)、[关键画面对照](dist/previews/2026-11-21-player-info-r4/玩家信息页-原片对照.jpg)。对照按页面状态剪辑，保留各段原速；角色和演示数据不同。
- Unity 57 项检查通过，含原图加载、称号佩戴与卸下、原有本地 ID、昵称和终局记录，复看 16:9 和 20:9。成片全帧解码，检查 25 个走势点和 24 条连接。证据在 build/player-info-source-r4/review/。
- 共享规则中的 Node 回归 209 项、Python 角色输入回归 8 项通过。隔离预览日志中另有装扮校验目录和编辑器搜索索引异常，详见源码核对说明，未修改相关模块。
- 称号获得方式先采用保留现有离线解锁条件的方案。具体原版称号映射、源码证据、资源来源及剩余数据差异见 [源码核对说明](docs/玩家信息页源码核对与修正-2026-11-21.md)。原版成就系统与最近窗口的风格算法尚未接入，本轮修正显示层。
- 当前 APK 仍为 0.6.39 的 R1。下次出包要同步两份 C#、Profile/titles.json、新 Native 和 TitleIcons 资源及 meta，再按原流程构建。暂存工程在录制后恢复。

## 玩家信息页 R3（Codex，2026-11-21）

- 按《點擊進入玩家信息.mp4》重新校准分区和阅读顺序。三麻／四麻放回立绘旁，场别回到上方，精选和牌、风格／高光和走势依序纵排，使用项目的深蓝和暖金色。走势沿用 R2 的坐标修复，ID 保留独立区域。
- 称号移除自绘图标，使用已有提取目录中与工程内字节一致的 title_get_bg.png 和 title_spilit.png 作为共用底板、分隔线。称号名称、佩戴和解锁逻辑仍走本机档案。提取目录没有单个称号的徽章图。
- [R3 预览 MP4](dist/previews/2026-11-21-player-info-r3/玩家信息页-R3.mp4)（65.4 秒，17 章节）和 [R2／R3 对照](dist/previews/2026-11-21-player-info-r3/玩家信息页-修正前后.jpg)。使用隔离数据在 Unity 编辑器复验 16:9 和 20:9，41 项检查通过，核对成片的 25 个走势数据点和 24 条连接线。当前 0.6.39 APK 仍为 R1。
- 主工程改动在 unity/Assets/Scripts/LobbyController.PlayerInfo.cs。暂存工程已恢复至录制前状态，新一轮构建前同步该文件。

## 玩家信息页 R2（Codex，2026-11-21）

- 复看《點擊進入玩家信息.mp4》的主页、称号簿、改名和三麻／四麻切换关键帧。玩家信息页使用云间日麻自绘称号和高光图标，称号簿与主页共用徽章。段位、模式、精选牌型、风格、高光和走势分别布局，玩家 ID 移到立绘下方。
- 修正走势线段在缩放面板内的坐标变换，折线端点与数据点对齐。称号图标作为缓存纹理绘制，避免在滚动容器里错位。既有档案格式、称号解锁条件和段位算分沿用。 另核对已有的雀魂 4.0.16 段位与称号提取目录，称号部分有配置、路径索引和共用界面图，独立称号徽章不在这份提取中。
- [R2 预览 MP4](dist/previews/2026-11-21-player-info-r2/玩家信息页-R2.mp4)（65.4 秒，17 章节）和 [修正前后对照](dist/previews/2026-11-21-player-info-r2/玩家信息页-修正前后.jpg)。使用隔离数据在 Unity 编辑器录制，验过 16:9 和 20:9 的显示与关键操作、41 项检查、走势的 25 个数据点和 24 条连接线。现有 0.6.39 APK 为 R1，R2 尚未出包和真机测试。
- 主工程变化位于 unity/Assets/Scripts/LobbyController.PlayerInfo.cs。录制后的暂存工程已恢复，下一次出包要把该文件同步到暂存工程。

## 0.6.39 功能预览视频（Codex，2026-11-21）

- [玩家信息页与段位 MP4](dist/previews/2026-11-21-0.6.39-preview/0.6.39-玩家信息页与段位.mp4)（65.4 秒，17 个章节）。从大厅头像进入，依次演示空档案、四麻和三麻战绩与段位表、走势场次切换、友人场／比赛场、风格说明、佩戴称号和改昵称。00:54.6 起是四麻半庄终局示例，顺位分 +80、素点 +16，回到玩家信息后段位进度由 354 更新为 450 PT。
- [役满特效修正对照 MP4](dist/previews/2026-11-21-0.6.39-preview/0.6.39-役满特效修正对照.mp4)（30.5 秒，3 个章节）。左侧使用已有 R40 实录，右侧重新录制 0.6.39 的 R41，等速对照国士无双、纯正九莲宝灯和清老头的桌面飞牌与全屏演出，重点展示爆点、水波圈和划光时序。
- 两支均为暂存 Unity 工程录制的静音画面。玩家战绩与终局使用隔离的演示数据。录制过程的玩家档案和操作检查共 40 项通过，成片全帧解码并检查关键画面。日志、章节、校验报告及录制入口在 build/2026-11-21-0.6.39-preview/。本轮是编辑器预览，Android 真机仍待验证。

## 0.6.39 / 87 本机测试包（Claude，2026-11-15 08:36，最新）

- APK：[yunjian-riichi-unity-0.6.39-local.apk](dist/yunjian-riichi-unity-0.6.39-local.apk)，366,834,626 字节，SHA-256 `2c02a32dff991855e504dbe4728cdb1f46dfb3ecd0d9f06693702e641dccbc23`（`dist/SHA256-unity-0.6.39-local.txt`）。
- 构建流程：版本常量改为 0.6.39／87 并同步暂存工程；在暂存工程 `BuildAndroid` 构建，日志 `build/player-info-r1/apk/android-0.6.39.log`；然后用 `scripts/sign-unity-apk.ps1` 签名。
- 构建前用全量对比确认暂存工程与主工程一致。差异只有：换行符、两个编辑器专用文件夹 `.meta` 的 GUID、一份第三方说明文档，以及有意保留的 `CharacterPackService.cs` 覆盖（SHA-256 不变）。
- 核验（均通过）：
  - [基础核验](dist/unity-0.6.39-local-verification.json)：v1/v2、独立 v1、原证书 `d1d8e506…dc2f`、16 KB 对齐；aapt2 读出的清单版本为 0.6.39／87；宿主输入（含新 engine.js）与目录逐字节一致；音频与手型资源齐全。
  - [功能核验](dist/unity-0.6.39-feature-verification.json)：新脚本 `scripts/verify-feedback-apk-r41.py`，版本从 APK 清单读取。检查 7,728 个役满资源与当前 motion，6 个场景的划光颜色曲线，L0 framing，39 个 Profile 资源，段位／信息页等编译代码；确认编辑器测试代码未入包。旧的 `verify-feedback-apk-r39.py` 版本写死为 0.6.38，保留作历史，不要再用于新包。
- 预览：[玩家信息与段位预览](dist/previews/2026-11-15-player-info/index.html)。内容是参考录像帧与 Unity 实际渲染的对照（主页／三麻／无记录／称号簿／改昵称／两张段位表），外加终局页、大厅名牌、宽屏和 R41 役满修正图；由 `scripts/package-player-info-preview.py` 生成，附 `manifest.json`。
- 构建后清理 C 盘：删除两个旧构建留下的 Gradle transforms，释放 1.24 GB，保留最新一次的 transforms 和 `modules-2`。
- **Android 真机未测**。重点测：
  - 打一局段位场后，终局页显示段位变化，玩家信息页有记录；
  - 两台手机同房间时，对方的段位和称号显示正确；
  - 信息页触摸、称号簿滑动、改昵称输入法；
  - R41 的爆点、水波圈和划光时序；
  - 以往的待测项。
- 15 项役满对照视频仍是 R40 版本，没有重录。

## 玩家信息页与段位算分 R1（Claude，2026-11-14 22:44）

按用户参考视频（`E:\MyWorkSpace_E\video\1114\claude`）完成。详细规则、文件表与验证见 [玩家信息与段位](docs/玩家信息与段位-2026-11-14.md)。

- **规则**：
  - 段位表按视频录入；段位分 = 顺位分 + 素点；跌破 0 降一级，初心不降。
  - 顺位分为自定默认值（四麻半庄 +80/+40/0、东风 +40/+20/0，三麻半庄 +100/0、东风 +50/0，末位按表减分），在 `unity/Assets/Resources/Majsoul/Profile/rank-rules.json` 调整。
  - 只有段位场改变段位，四麻、三麻分开计算。
- **引擎**：终局给出 `standings`／`rules`，带每座位的整局统计；同分按离起家近者排前，供托归第一。玩家的段位和称号经 `profile` 同步给他人。两份 `engine.js` 一致。
- **客户端**：
  - 本机档案在 PlayerPrefs `riichi.player.v1`。
  - 终局时记录一次；终局页显示素点、本家 PT 变化和段位条。
  - 大厅头像打开新的玩家信息页，带称号簿、改昵称、段位说明表等弹窗。
  - 大厅名牌、房间卡、开场和 HUD 改为真实段位与称号。
  - 段位徽章复制自既有 APK 提取 `build/apk-rank-title-study/delivery`。
- **测试修复**：`tests/ai.test.cjs` 有一例没有恢复共享的 `RiichiAI.choose`，这就是 `--test-isolation=none` 下 `session-flow` 两项旧失败的原因，已修复。
- **验证**：
  - Node 两种模式均 209/209；Python 8/8。
  - `scripts/test-player-record.ps1`：25 项通过，13 张截图在 `build/player-info-r1/review2/`，已人工查看。
  - Android 真机未运行。
- **待用户确认**：顺位分默认值、8 个自定称号、改昵称是否加冷却。中途退出目前不记录这局，也没有惩罚。
- 已打入 0.6.39／87（见上节）。

## R41：飞牌爆点位置与划光时序（Claude，2026-11-14 21:56）

用户对照成片反馈：① 桌面飞牌特效位置不对（国士无双，爆点落在和牌张远侧）；② 牌列未出现时高光已出现（纯正九莲宝灯）。

1. **爆点位置**：`effect_baodian` 预制体根节点保留原编辑场景偏移 `(0.0243,-0.0015,0.1911)` 源单位（约 2.1 世界单位，朝牌桌远侧）；拖尾无此偏移所以原本正常。`unity/Assets/Scripts/YakumanCinematic.cs` 初始化爆点时改用 `normalizeSurfaceOrigin:true` 将根节点归位到和牌张。原 Lua `ShowYiManEffect` 源码本机未找到，此结论来自预制体数据与录像比对。
2. **划光时序**：`huaguang` 的原始片段除位移外还有 `material._Color` 动画（rgb 平时为 0，仅两次扫过各约 0.4 秒升到 1，alpha 恒为 1）。导入用的着色器只有 `_TintColor`，编辑器采样时该通道被丢弃，划光常亮。受影响 6 个场景：`fan_chunzhengjiulianbaodeng`、`fan_jiulianbaodeng`、`fan_qinglaotou`、`fan_qinglaotou18`、`fan_ziyise`、`fan_ziyise18`；其余 15 个含 `huaguang` 的场景用旧着色器，导入正常。新增 `scripts/repair-yakuman-highlight-r41.py`，从保留的原始片段解码并补一条 `_TintColor` 曲线（幂等）；每份 motion 仅新增 1 条曲线，其余逐项不变。修改前副本在 `build/yakuman-alignment-r41/before/`。`scripts/import-yakuman-r39.py` 与 `NativeEffectR11Import.cs` 未改，**重新导入后需再跑一次该修复脚本**。
3. **桌面水波圈（22:05 追加）**：`effect_baodian` 里的 `quan01`（0–1.2 秒每 0.2 秒一圈）、`quan`（1.2 秒一圈）和 `guang`（光晕）都是水平公告板，粒子本来就在发射，但挂点用的是牌桌牌的枢轴（贴着桌布，y≈0），整片被桌布深度遮住。APK 牌模型以中心为枢轴，现在 `YakumanCinematic.TileCenter` 按和牌张网格包围盒中心挂爆点（国士无双实测 y 0→0.12）。实录中 0.33–1.8 秒可见扩散圈和光晕，与原片时段一致。颜色按源数据（暖橙→黄绿）；原片在蓝色桌布上偏青，本机桌布不同，未改颜色。拖尾仍挂在原枢轴，未动。`tests/YakumanFilmCapture.cs` 另加爆点粒子状态日志（`*-burst.txt`）。
4. 为不覆盖 R40 证据，`scripts/capture-yakuman-video-r40.ps1` 新增 `-Output`，`tests/YakumanFilmCapture.cs` 读取 `YUNJIAN_FILM_OUTPUT`；不传时行为不变。

验证（本轮实际执行）：

| 验证 | 结果 | 证据 |
| --- | --- | --- |
| Unity 编辑器实录 3 项（国士无双、清老头、纯正九莲宝灯） | 已查看关键帧：爆点闪光与星点在和牌张处；牌列出现前无划光，扫光两次落在牌面上 | `build/yakuman-alignment-r41/capture/`，及同目录三张 JPG |
| 水波圈修正后实录 2 项（国士无双、纯正九莲宝灯） | 已查看国士无双前导关键帧，扩散圈与光晕可见；无编译错误 | `build/yakuman-alignment-r41/ripple/`、`国士无双-桌面水波圈.jpg` |
| Node 默认隔离 | 202/202 通过（水波圈修改前运行；之后只改 C#，未重跑） | `build/yakuman-alignment-r41/node-default.log` |
| Node `--test-isolation=none` | 当时 200 通过、2 失败（`session-flow` 旧问题；22:44 已查明并修复，见上节） | `build/yakuman-alignment-r41/node-no-isolation.log` |
| Python 角色输入 | 8/8 通过 | 本轮命令输出 |
| 未运行 | `scripts/test-yakuman-r39.ps1` 23 场景检查；九莲宝灯、字一色及两份 18 张变体的实录；15 项对照成片重新合成；Android 真机 | 只做了数据审计和 3 项实录 |

- 交付目录的对照 MP4、`index.html`、`verification.json` **仍是 R40 版本**，不含本轮修正；需要新成片时重录全部 15 项后再对齐、合成。
- 录制脚本在 Windows PowerShell 5.1 下 `ConvertFrom-Json` 解析 `packs-windows.json` 失败，需用 Codex 运行时的 `pwsh.exe`（`%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\`），并把 Codex 的 `rg.exe` 目录加入 PATH。
- 主／暂存工程的 `YakumanCinematic.cs` 与全部 motion 逐字节一致；暂存 Saber 覆盖摘要不变。
- 下一步：等用户看效果；确认后可重录 15 项并合成新对照，或按下文流程出 0.6.39／87。

## R40 任务与已完成结果（Codex）

用户对 0.6.38 截图反馈部分牌面偏暗、牌列码放／位置与原片不同，要求逐项核对并交付原版对照视频，明确必须包含牌飞上去的过程。参考为 `../video/1114/codex/役滿特效.mp4` 和本地雀魂 4.0.16 APK。

1. 修正演出原生坐标与牌桌转换坐标的光方向差异，仅在演出牌材质上设置局部光方向，保留源明暗层次；并按当前牌桌的牌面／牌背装扮绑定。
2. 牌列只将网格牌计为手牌槽，保留同层爆点粒子的源动画开关。四暗刻原控制器含一个未启用的四杠子状态，旧匯入同时播放两段导致姿态覆盖；现按默认状态修正四暗刻和两份 18 张变体，23 场景共 180 条生效轨道，原始片段输入保留。
3. 桌面前导按 `DesktopMgr.ShowYiManEffect`：0.15 秒延迟 → 抖动／翻起 → 向上加速及拖尾 → 2.15 秒进入全屏 → 完整播放 8 秒。克隆牌飞起时隐藏接触阴影，退出后恢复原牌可见状态。
4. 补 15 组明确的 L0 立绘布局；其余 471 个皮肤使用正确的一姬基准和既有裁切，修正飞牌开场立绘落到屏幕外。其余场景 JSON 数据逐项不变，保持原 LF 换行。
5. 已交付 15 种役满的并排视频，均包含桌面飞起、天空飞牌、结算手牌展开及收尾；已查看成片关键帧和全部牌列。右侧使用一姬，手牌按原片重建后经 `WinDetail` 动态绑定，开发清单不会替换实战手牌。

本轮技术细节、完整文件清单和可复制复验步骤见 [役满牌面与飞牌对照](docs/役满牌面与飞牌对照-2026-11-14.md)。

## 交付物与对应版本

| 内容 | 当前入口／文件 | 状态 |
| --- | --- | --- |
| 15 项役满对照及段落跳转 | [index.html](dist/previews/2026-11-14-yakuman-comparison/index.html) | 本轮修正后的实际 Unity 编辑器录制 |
| 完整对照视频 | [含完整飞牌 MP4](dist/previews/2026-11-14-yakuman-comparison/役满原版与修正对照-含完整飞牌.mp4) | 1920×640、30 fps、4,575 帧、152.5 秒、85,314,710 字节 |
| 视频核验与段落信息 | [verification.json](dist/previews/2026-11-14-yakuman-comparison/verification.json) | 全片解码及关键帧检查；SHA-256 `cc97c57a697733aab5bc54c32f0fce5b3f7c31624a70743aae79bfcd87032431` |
| 上一版测试包 | [0.6.38 本机 APK](dist/yunjian-riichi-unity-0.6.38-local.apk) | 364,877,168 字节；不含 R40 起的修正，最新包见顶部 0.6.39 |
| APK 报告 | [基础核验](dist/unity-0.6.38-local-verification.json)、[画面资源核验](dist/unity-0.6.38-visual-verification.json) | 0.6.38 原有签名和包内容核验 |
| 全手型图集 | [260 张浏览入口](dist/previews/2026-11-14-all-hands/index.html)、[ZIP](dist/previews/2026-11-14-all-hands.zip) | 21 套专属手型及普通／旧手型的实际渲染 |
| 立直／开局立绘对照 | [预览入口](dist/previews/2026-11-14-effects-and-portraits/index.html) | 0.6.38 的虚影祝祷和立绘清晰度修正 |

APK SHA-256：`1eb4fda5e279afc8e0e1e102729a4d27d08e23c040f4c68e4a185f97fe391871`。视频和 APK 摘要在本次文档交接中重新读取文件核对。

对照视频为**静音画面对照**，不能证明音效通过。两侧均为 16:9、同速，只调整起点；原片片段超出区间处标明定格。原片角色、牌桌、压缩及粒子随机不同，未宣称逐像素一致；Android 真机仍未验证。

## 验证结果与已知问题

以下是上轮实际执行的结果，本次仅重新核对其磁盘报告，未重新运行测试。

| 验证 | 结果 | 证据 |
| --- | --- | --- |
| Unity 役满编辑器检查 | 23 场景，1,433 项通过；包含向上飞牌、2.15 秒切换和结算恢复 | `build/yakuman-alignment-r40/final-review/report.json` |
| Node 默认隔离模式 | 202/202 通过 | `build/yakuman-alignment-r40/node-default.log` |
| Node `--test-isolation=none` | 当时 200 通过、2 失败（`session-flow`；原因已在 22:44 查明为 `ai.test.cjs` 未恢复共享 AI，已修复） | `build/yakuman-alignment-r40/node-no-isolation.log` |
| Python 角色输入 | 8/8 通过 | 上轮 `python -m unittest discover -s tests -p test_character_inputs.py` 的实际输出 |
| 对照视频 | 全部 4,575 帧解码、尺寸／时间戳及关键帧通过 | 交付目录的 `verification.json` 和 `成片飞牌与四暗刻核验.jpg` |
| Android 真机 | 未运行 | 编辑器录像和包验证不能代替真机验证 |

- R39 旧 `build/feedback-1114-r39/yakuman-review/report.json` 在调试时被旧脚本清理逻辑移除，现以 R40 最终报告复验。旧截图、0.6.38 APK 及 APK 报告保留；测试脚本已改为只清理实际指定输出目录。不要再引用不存在的旧报告。
- 其余角色手部材质尚未逐套完成亚丝娜级别的 MatCap／PBR 适配；赤麟不在当前角色目录，旧猫爪资源不在可装备列表，图集已有说明。
- 本项目仍为立直麻将；角色每套 133 条语音中 110 条已接入已有事件，其余 23 条缺少对应玩法／养成事件，保留试听，没有新增四川麻将玩法。
- 旧轮次的冷启动、资源准备、多响、拔北、流局、左右家喊话背景等真机待测项继续保留，详见下方历史归档，不代表已取消或已验证。

## Claude 接手顺序

1. 先读取 `AGENTS.md`、`CLAUDE.md`、本页和本轮详细说明，运行共享规则中的 Git 只读检查；从当前共享根目录继续，不切到 `.publish`。当前任务已完成编辑器和视频交付，先结合用户对成片的新反馈决定开发范围。
2. 优先打开视频目录逐项查看，特别是国士无双／小四喜的牌面、四暗刻斜向牌列、桌面飞起到天空的连续过程。需要改动时先检查 `unity/Assets/Scripts/YakumanCinematic.cs` 和三份修正 motion；完整路径见详细说明。
3. 代码或资源变化后使用详细说明中的复验命令，输出到新的 review 子目录，保留现有最终报告与视频；区分自动测试、编辑器预览和真机测试。
4. 若用户要求新 APK，先把 `ProjectSetup.cs` 改为 0.6.40／88，同步暂存工程后构建、签名，再用 `scripts/verify-character-audio-apk.py` 与 `scripts/verify-feedback-apk-r41.py` 核验（命令见顶部 0.6.39 一节）。
5. 完成后更新本页和详细说明。提交、推送、切分支、发布资源、修改公开 URL／摘要、Release 均按 `AGENTS.md` 的授权规则执行；本轮没有这些操作。

## 暂存工程与下次构建

- 编辑器：`D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe`；缓存工程：`build/phone-test-r24/stage/unity`。本次核对 `YakumanCinematic.cs`、`Characters/framing.json` 和三份修正 motion 在主／暂存工程中逐字节一致。
- **保留暂存工程 `CharacterPackService.cs` 的 LocalSaberPart 临时覆盖，不能整文件覆盖。** 当前 SHA-256 为 `3fb23e2ad7253463cfb8eb408c46a2b3882494aee250d184d1435ab77641f6d3`，本次再次核对不变。新素材、运行代码、资源和 `.meta` 要按修改清单同步，不执行覆盖整个工程的同步。
- 当前 `unity/Assets/Editor/ProjectSetup.cs` 和暂存工程均为 0.6.39／87；主 `ProjectSettings.asset` 仍记旧 0.6.22／70。以 `ProjectSetup` 常量和实际 APK 为准，不能只看旧 YAML；`ProjectSetup.BuildAndroid()` 会先调用 `Configure()` 设置包名、显示名和版本。
- 构建入口：`YunJian.Riichi.Editor.ProjectSetup.BuildAndroid`；签名入口：`scripts/sign-unity-apk.ps1`。核验用 `scripts/verify-character-audio-apk.py <apk> <报告>`（签名、对齐、清单版本、宿主输入、音频、手型），以及 `scripts/verify-feedback-apk-r41.py <apk> <报告>`（版本从清单读取，检查役满、Profile、段位代码，并确认编辑器测试代码未入包）。旧的 `verify-feedback-apk-r39.py` 版本写死为 0.6.38，不再使用。新功能如需核验，应在 r41 脚本上追加检查。
- Python 可使用 Codex 运行时自带解释器（详细说明有命令），旧历史中「没有系统 Python」不代表当前无法运行脚本。Unity／Node 受限环境若出现 IPC 或 `spawn EPERM`，应区分环境失败与代码失败。
- 出包后沿用既有缓存清理约定：只处理核验过的旧 Gradle transforms，保留 `modules-2`；不要直接运行含 D 盘旧固定删除路径的 `clear-project-generated-cache.ps1`。0.6.39 出包后已按此清理（释放 1.24 GB）。

## 前几批成果与历史入口

| 批次 | 已实现内容 | 详细说明 |
| --- | --- | --- |
| 0.6.33／81 | 补 50 份特效音频；角色已有语音触发 | [音效核对](docs/特效音效核对-2026-11-11.md)、[语音触发](docs/角色语音触发适配-2026-11-13.md) |
| 0.6.34／82 | 修正神火坠霄等真实装扮预览无声和循环生命周期 | [预览音效修正](docs/预览音效修正-2026-11-14.md) |
| 0.6.35／83 | 大厅／宿舍语音字幕及清理生命周期 | [字幕说明](docs/角色语音字幕-2026-11-14.md) |
| 0.6.36／84、0.6.37／85 | 角色专属默认手型、亚丝娜材质补全 | [手型与材质](docs/角色专属默认手型-2026-11-14.md) |
| 0.6.38／86 | 虚影祝祷黑牌、开局立绘清晰度、原生役满演出和全手型图集 | [1114 反馈修复](docs/1114反馈修复与役满演出-2026-11-14.md) |
| 0.6.39／87 | R40 役满牌面光照与飞牌、R41 爆点／水波圈／划光，以及玩家信息页与段位 | [役满对照](docs/役满牌面与飞牌对照-2026-11-14.md)、[玩家信息与段位](docs/玩家信息与段位-2026-11-14.md) |

整理前的完整进度页逐字节保存在 [历史归档](docs/DEVELOPMENT-history-2026-11-14.md)，包含旧版 APK、旧轮次待测清单和完整协作记录。归档中的「最新」「下一版」均是当时状态，以本页当前版本为准。[旧协同手册](docs/CODEX-HANDOFF.md) 保留 2026-11-06 基线，不能替代当前交接。
