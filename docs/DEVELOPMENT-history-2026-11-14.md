# 当前开发进度

- 更新时间：2026-11-14 20:24（东京时间）
- 最近接手：Codex（役满牌面／四暗刻牌列／完整飞牌已修正，15 项原版对照视频已完成，尚未出新 APK）
- 共享 Git 根目录：E:\MyWorkSpace_E\YunJianRiiChi
- Unity 工程：unity/，Unity 6000.6.4f1
- 当前分支：main。2026-11-10 用户授权「代碼上傳至github」后，0.6.23 → 0.6.32 的代码已作为一个提交推送到 origin/main，一同推送的还有此前的两个协作文件提交 a7c37100、e63b5b4b。未打标签，也未发布 Release。

## 当前追加任务／交接给 Claude Code（Codex，2026-11-14 20:24）

用户针对 0.6.38 截图要求逐项比对原录像的牌面亮度、码放和位置，并交付对照视频，明确必须含牌飞上去的过程。已重新核对共享规则和工作区，沿用 main、保留已有改动；未构建新 APK、提交或发布。

- 已定位并修正原生演出坐标与牌桌坐标的光方向差异，只给役满牌材质设置转换后的局部光方向；牌桌全局光不变。牌面／牌背按当前实际牌桌装扮绑定。
- 原牌列还含粒子节点，现仅将网格牌节点纳入手牌槽，避免逐帧牌数限制关闭源动画粒子。另定位四暗刻控制器中未启用的四杠子片段覆盖默认姿态，按 APK 控制器默认状态修正四暗刻及两份 18 张变体，共 180 条生效轨道；原始输入保留。
- 按 `DesktopMgr.ShowYiManEffect` 修改桌面前导为 0.15 秒延迟、抖动／翻起、向上加速与拖尾，2.15 秒接入全屏演出，再完整播放 8 秒；飞起时隐藏克隆牌接触阴影。补入 15 组 L0 立绘布局，其余皮肤使用正确基准裁切，修复飞牌开场立绘落到屏幕外；其余原布局 JSON 数据已逐项确认不变，换行保持原 LF。
- [15 项对照视频与跳转目录](dist/previews/2026-11-14-yakuman-comparison/index.html) 已完成：同目录 `役满原版与修正对照-含完整飞牌.mp4`，1920×640、30 fps、4,575 帧、152.5 秒；全部帧解码通过。左右均 16:9、同速，只调整起点；原片超出区间处明确标记定格。视频为静音画面对照，右侧为实际 Unity 渲染，使用一姬，按原片重建的结算手牌动态绑定，含四杠子 18 张。原片角色、牌桌、压缩和粒子随机不同，不作为音效或逐像素一致的证明。
- 最终 23 场景、1,433 项编辑器检查通过，包含向上飞牌、2.15 秒切换和结算恢复。Node 默认隔离模式 202/202、Python 8/8 通过；无隔离模式仍有既有会话测试 2 项失败（200 通过），未更改规则引擎。未进行 Android 真机测试，0.6.38 APK 不含本轮修正。下一次出包使用 0.6.39 / 87。
- 证据与复现：`build/yakuman-alignment-r40/`、`scripts/capture-yakuman-video-r40.ps1`、`scripts/align-yakuman-comparison-r40.py`、`scripts/make-yakuman-comparison-r40.py`，手牌清单 `tests/fixtures/yakuman-video-1114.json`。详细记录见 [役满牌面与飞牌对照](docs/役满牌面与飞牌对照-2026-11-14.md)。Saber 覆盖摘要不变。没有提交、推送、发布资源或 Release。
- 历史 R39 `yakuman-review/report.json` 在调试时被旧脚本输出清理逻辑移除，现以 R40 最终报告复验，不应再引用该旧路径的报告；旧截图和 0.6.38 APK 核验报告保留。脚本已改为清理各自指定输出目录。下一步按用户反馈测试成片，如需 APK 再同步、构建和真机验证。

## 前一任务（2026-11-14，已完成本机开发与验证）

用户参考位于 `../video/1114/codex/`：虚影祝祷立直黑牌截图、开局立绘模糊截图、140.8 秒役满特效录像。优先修复前两项，再依 APK 美术／Lua 与录像接入役满演出和实际获胜手牌特写，另交付全部手型效果预览图。本轮从 0.6.37 / 85 升至 0.6.38 / 86，下次版本为 0.6.39 / 87。原有未提交改动、公开资源目录及暂存工程 LocalSaberPart 保留。

已读三份共享文件、外部交接指南及 Git 状态，参考录像已抽帧。当前分析和证据在 `build/feedback-1114-r39/`。

- 优先修复已通过编辑器核验：`NativeWinningTileQueue.cs`、`NativeWinPlayback.cs` 按 `DesktopMgr.SetPaopaiZtestNew` 只保留牌身／牌面，不让 Always 描边覆盖牌面，保留旧式队列；模型销毁时禁用悬空克隆。`MatchIntroduction.cs` 改为按屏幕像素密度直接合成 RenderTexture，并释放缓存。`tests/Feedback1114Review.cs` 在四座位、四时点及真实装扮预览入口检查；修改前 256、修改后 345 项通过。截图服装为福姬 403807、小鸟游雏田 401906、Saber 40011002，固定官方 full.png 与现有资源尺寸相同；未发现更大静态原图，未替换不同版本画面。2760×1256 下合成由 605–801 宽提升为 1329–1861 宽。对照及全尺寸图在 `dist/previews/2026-11-14-effects-and-portraits/`。
- 已接入原版役满演出：15 种役满、23 个含 18 张变体的全屏场景，加前导爆点／拖尾和独立牌描边，共 26 个预制体、183 段烘焙动画、16 段音频。`YakumanCinematic.cs` 按 Lua 优先级选择演出，以结算的暗手＋副露＋和牌张替换弯曲牌列，并替换获胜者立绘。修复原材质渲染队列遗漏导致背景遮挡、描边误用牌面材质及 Standard Unlit 扫光读错混合参数造成的黑色矩形；最终 1,318 项编辑器检查、全部 23 个场景及实际 TableEffects 调度通过。累计役满不触发。尚未 Android 真机验证。来源及可重复导入记录在 `build/feedback-1114-r39/yakuman/`，未修改公开资源目录。
- 全手型预览：21 套专属手型所有 51 方向 × 5 动作，加 5 套普通／旧手型共 260 张，排版入口 `dist/previews/2026-11-14-all-hands/index.html`，总览 `all-hands-overview.jpg`，ZIP 位于上级目录。赤麟不在当前角色目录、旧猫爪资源不在装备列表，图集中已标注。最终全部 PNG、清单和 ZIP 已校验，置中图像已查看。
- Node 标准隔离模式 202/202、Python 8/8 通过；共享规则中的 `--test-isolation=none` 模式本轮出现会话测试失败，未改引擎掩盖，原因尚未单独定位。0.6.38 / 86 本机 APK 已构建、签名并验证；未提交、推送或发布。

详细说明：[1114 反馈修复与役满演出](docs/1114反馈修复与役满演出-2026-11-14.md)。暂存工程 Saber 覆盖文件摘要仍为 `3fb23e2ad7253463cfb8eb408c46a2b3882494aee250d184d1435ab77641f6d3`，三份引擎一致；新役满资源 7,728 份及对应 `.meta`／唯一 GUID 已核对。

## 上一批交接给 Claude Code（Codex，2026-11-14 18:32，东京时间）

- 当前仍为共享根目录的 `main`，HEAD `e9b35830`，0.6.33–0.6.38 都是本地未提交改动；没有切换分支、提交、推送或发布资源／Release。保留此前未跟踪文件、旧 APK 删除状态、Saber 覆盖和所有 `.meta`／GUID。
- 最新 APK：[0.6.38 本机测试包](dist/yunjian-riichi-unity-0.6.38-local.apk)，364,877,168 字节，SHA-256 `1eb4fda5e279afc8e0e1e102729a4d27d08e23c040f4c68e4a185f97fe391871`。v1/v2、独立 API-23 v1、原证书、16 KB 对齐、两架构、当前宿主输入和目录、327 条基础语音、此前补充的 50 个特效音频、字幕及 507 份手部资源／亚丝娜材质通过。报告 [基础包核验](dist/unity-0.6.38-local-verification.json)、[本轮画面资源核验](dist/unity-0.6.38-visual-verification.json)。
- 新役满包核验：7,728 份资源、26 个预制体、15 类演出、1,731 个原始材质队列、6 个扫光混合状态、独立牌描边、16 段非零音频及实际时长均通过。运行入口与来源见上方说明和详细文档。编辑器 345＋1,318 项检查通过；ADB 无设备，真机未测。
- 全手部图集：[浏览入口](dist/previews/2026-11-14-all-hands/index.html)、[总览](dist/previews/2026-11-14-all-hands/all-hands-overview.jpg)、[260 张 ZIP](dist/previews/2026-11-14-all-hands.zip)。[立直／立绘对照和役满预览](dist/previews/2026-11-14-effects-and-portraits/index.html)。全部为 Unity 实际渲染，图集手牌是开发样例，实际游戏由结算数据生成。
- 构建仍用 `build/phone-test-r24/stage/unity`，本轮仅最终同步 6 个修正材质和版本文件，其余运行代码已在测试时同步。`CharacterPackService.cs` 覆盖摘要不变。构建日志 `build/feedback-1114-r39/android-build.log`，复验入口 `scripts/test-feedback-1114.ps1 -Mode after`、`scripts/test-yakuman-r39.ps1`、`scripts/verify-feedback-apk-r39.py`。打包脚本和同步记录在本轮 build 目录。
- 出包后按既有授权删除 1 项旧 Gradle transforms，逻辑大小 951,593,795 字节，C 盘空闲增加 962,310,144 字节，保留 modules-2；计划、路径检查和结果在本轮 build 目录。
- 下一步先按下方清单测试 0.6.38 的虚影祝祷、开局立绘、役满实战音画和真实手牌特写；保留此前待测内容。下一版 0.6.39 / 87。

## 上一批交接（Codex，2026-11-14 12:10，东京时间，0.6.33–0.6.37 历史）

当前仍在共享根目录的 `main`，HEAD 为 `e9b35830`。以下 0.6.33–0.6.37 开发改动均在本地工作区，尚未提交、推送或发布。接手时按 `AGENTS.md` 重新核对已跟踪及未跟踪文件，保留原有改动和全部 `.meta`／GUID；本次仅补交接文档，没有另改游戏代码或重跑构建。

| 批次 | 已实现内容 | 详细说明 |
| --- | --- | --- |
| 0.6.33 / 81 | 补 50 份和牌／立直音效；角色 133 条语音中 110 条接入已有事件，含大厅、友人房、对局和终局。其余 23 条因缺少对应玩法／养成事件，保留手动试听。本项目仍是立直麻将，没有加入四川麻将规则。 | [音效核对](docs/特效音效核对-2026-11-11.md)、[语音触发适配](docs/角色语音触发适配-2026-11-13.md) |
| 0.6.34 / 82 | 修正真实装扮预览未进入音频牌桌状态造成「神火坠霄」静音；轮换座位等长音效结束，退出时清理。已检查神火坠霄、立直－赤麟／火焰多轮输出。 | [预览音效修正](docs/预览音效修正-2026-11-14.md) |
| 0.6.35 / 83 | 参考用户录像接入大厅／宿舍角色语音字幕，按实际播放路径匹配文本，逐字、换行／滚动，并随语音与页面生命周期清理。 | [角色语音字幕](docs/角色语音字幕-2026-11-14.md) |
| 0.6.36 / 84 | 接入 21 套专属默认手型、51 个方向模型、255 个动作；当前 29 名角色新增支持，加既有汪次郎共 30 名。默认手随角色切换，橘猫爪装备优先，含牌桌及装扮预览。 | [角色专属默认手型](docs/角色专属默认手型-2026-11-14.md)、[逐资源审计](docs/character-hand-audit-2026-11-14.json) |
| 0.6.37 / 85 | 修正亚丝娜偏暗：补 4 个实际材质的原明暗渐层、法线、ORM、MatCap 与原牌桌光色，共 7 张辅助贴图；按源设置禁用 mipmap，渐层 Point／Clamp，消除分带混合斑点。其余角色材质未调整。 | 同上说明的 0.6.37 小节；[修正前后对照](dist/previews/2026-11-14-asuna-material/asuna-before-after.png) |

- 关键代码：语音 `CharacterVoiceTriggers.cs`／`CharacterVoicePlayback.cs`／`CharacterVoices.cs`，字幕 `CharacterVoiceCaption.cs`／`LobbyController.Subtitles.cs`，预览音效 `RiichiClient.DressPreview.cs`／`LobbyController.DressTable.cs`，手型 `CharacterHands.cs`／`CharacterHandVisual.cs`／`TableEffects.cs`／`SourcePlaybackContracts.cs`／`WardrobeCatalog.cs`／`WardrobeMotionPreview.cs`。亚丝娜材质还涉及 `TableLook.cs`、`Resources/Majsoul/CharacterHand.shader`、`Editor/CharacterHandImport.cs`；这些路径均相对 `unity/Assets/`，运行脚本位于 `Scripts/`。新增脚本及资源和对应 `.meta` 都需要一起保留。
- 手型来源仍为 `../YunJianRiiChi_Tmp/Majsoul_角色手型與手部裝飾/`；转换脚本 `scripts/import-character-hands.py`，审计脚本 `scripts/audit-character-hands.py`。当前内置 507 份手部资源。赤麟仅保留模型与 ID 绑定，尚未加入角色目录；其他角色原 MatCap／PBR 材质尚未逐套适配，亚丝娜也没有移植独立描边 Pass。
- 最近实际验证：Node 202/202、Python 8/8；手型编辑器 1,469 项检查，含全部方向／动作、62 组四人桌案例、亚丝娜三人桌及装扮预览；四方向同姿态 A/B 截图已看。字幕上一轮 2,588 项检查和预览音效多轮非零采样记录保留在各批次日志中。自动测试／编辑器预览通过，Android 真机仍未验证，不能等同于真机通过。
- 复验入口：`scripts/test-character-hands.ps1 -OutputFolder character-hand-material-r38`、`scripts/test-character-subtitles.ps1`、`scripts/test-effect-preview-audio.ps1`、`scripts/test-character-voice-triggers.ps1`。本机证据依次在 `build/character-hand-material-r38/`、`build/character-subtitles-r36/`、`build/effect-playback-r35/`、`build/character-voice-r34/`；这些构建目录被忽略，详细维护说明在上表文档中。
- 最新可安装包：[0.6.37 本机测试 APK](dist/yunjian-riichi-unity-0.6.37-local.apk)，275,075,968 字节，SHA-256 `ebd93505ffb196120fd52684b089341077113c56731c36be35ff113171b613a5`；[包内容／签名报告](dist/unity-0.6.37-local-verification.json)。v1/v2、独立 API-23 v1、原证书、16 KB 对齐及亚丝娜实际打包材质均通过。下一版 `0.6.38 / 86`。
- 继续使用 `build/phone-test-r24/stage/unity` 缓存工程，修改后手动同步；**不要覆盖暂存工程 `CharacterPackService.cs` 的 LocalSaberPart**。两份 `engine.js` 要同步。没有修改公开资源目录 URL／摘要，没有提交、推送、打标签或发布资源／Release；这些仍需用户明确授权。
- Claude 接手第一步：先核对本节与 Git 状态，再根据 0.6.37 真机反馈检查亚丝娜四座位与宿舍预览；无新反馈时保留现场。详细未测清单见文末「下一步」，旧 0.6.32 清单属于此前待验证项。

## 上次交接给 Codex（Claude Code，2026-11-10，历史基线）

- 0.6.23 → 0.6.32 的全部改动已提交并推送（见上）。下方「已完成」按轮次列出了每项改动及其文件。
- 范围：
  - 已跟踪文件中有 32 个被修改。
  - 新文件 8 个，各自带 `.meta`，需要一起入库：
    - `WardrobePresets.cs`
    - `DressPreviewStates.cs`
    - `RiichiClient.DressPreview.cs`
    - `LobbyController.DressTable.cs`
    - `LobbyController.Preload.cs`
    - `ReviewR33Capture.cs`
    - `Editor/ReviewR33.cs`
    - `docs/research-2026-11-07-presets-and-tile-motion.md`（没有 `.meta`）
- 以下改动不是 Claude Code 做的，接手前就已存在或者来源没有核实，也没有动过，都**没有纳入提交**，仍留在工作区：
  - `dist/yunjian-riichi-0.4.0.apk`、`0.5.0.apk` 处于删除状态
  - `scripts/prepare-original-input-stage.py` 新增了 `review-r25` 批次
  - `docs/` 下 r25–r28 的说明
  - `scripts/*-r25/r26/r28/r29.py`
  - `dist/` 下的旧 SHA 文件
- 两份 `engine.js` 现在逐字节一致，修改时要同时改两份。
- 构建方式（与仓库默认流程不同）：
  - 在暂存工程 `build/phone-test-r24/stage/unity` 里构建，改过的文件需要手动复制过去。不要覆盖它的 `CharacterPackService.cs`（Saber 临时覆盖）。
  - 构建命令：`Unity.exe -batchmode -quit -projectPath <暂存工程> -buildTarget Android -executeMethod YunJian.Riichi.Editor.ProjectSetup.BuildAndroid`
  - 签名：`& (Resolve-Path scripts\sign-unity-apk.ps1).Path -ApkPath <绝对路径> -EditorPath <Unity.exe>`（必须用完整路径调用）。
  - 最新本机测试包为 0.6.37 / 85；下一版从 0.6.38 / 86 开始。
- 出包后清理 C 盘（用户要求定时做）：删除 `%USERPROFILE%\.gradle\caches\9.3.1\transforms` 里比本次构建旧、且大于 50 MB 的目录，保留 `modules-2`。
- 测试环境：
  - Node 24 直接运行 `node --test tests/*.test.cjs`，最新结果是 202/202 通过。
  - Python 用 codex 运行时自带的 python.exe。
- 当前接手入口为本页顶部「交接给 Claude Code」；0.6.32 的历史真机待测项继续保留，最新测试包为 0.6.37。

## 已完成

- 2026-11-14 亚丝娜手型材质（Codex，未提交）：确认 0.6.36 只画原底色贴图，未接 FxBRDF2 的明暗渐层／法线／ORM／MatCap 和原牌桌光色，导致皮肤与白袖甲发灰。已为亚丝娜四个实际材质补全源参数及 7 张辅助贴图，保留既有 GUID／动画和其他角色材质。纹理按源设置导入（明暗渐层 Point／Clamp、无 mipmap，法线及 ORM 线性）；清除试做时自动 mipmap 混合渐层造成的斑点。1,469 项编辑器检查、62 组牌桌案例及四方向同姿态 A/B 截图通过；四方向变化区域亮度均提升约 47–52%，无三通道白色溢出。Node 202/202、Python 8/8、157 份来源 Bundle 摘要通过。本机 0.6.37 / 85 构建、签名与包内容核对通过；Android 真机未测。日志 build/character-hand-material-r38/，对照图 dist/previews/2026-11-14-asuna-material/。

- 0.6.37 / 85：`dist/yunjian-riichi-unity-0.6.37-local.apk`，275,075,968 字节，SHA-256 `ebd93505ffb196120fd52684b089341077113c56731c36be35ff113171b613a5`。v1/v2、独立 API-23 v1、同证书、16 KB 对齐、两架构、507 份手部资源、实际打包的亚丝娜新着色器／4 材质参数／4 渐层采样及上一轮音频／目录／宿主输入均通过。验证 JSON 在 APK 同目录。构建后删除 1 项旧 Gradle transforms，逻辑大小 949,679,423 字节，C 盘空闲增加 960,045,056 字节，保留 modules-2。没有提交／推送／发布，ADB 无设备。

- 2026-11-14 角色默认手型（Codex，未提交）：已核对 `../YunJianRiiChi_Tmp/Majsoul_角色手型與手部裝飾/` 的角色绑定、157 份原始 Bundle 摘要、材质、骨骼和 Animator。新增 21 套资源（含当前未在角色目录的赤麟），51 个方向模型、255 个动作；当前 29 名角色新接入，加既有汪次郎共 30 名。CharacterHands／CharacterHandVisual 按角色 ID 自动选默认手，橘猫装备优先；TableEffects 与装扮预览已接通，使用原控制器状态、编译动画取样及匹配的持牌坐标。装扮列表和电脑手部随机策略维持原状。修正三套 Fate 手型的投影网格黑块及装扮预热时样式为空的日志。1,393 项编辑器检查通过，涵盖全部方向模型／动作、62 组实际四人牌桌案例、亚丝娜额外三人桌方向及装扮预览；Node 202/202、Python 8/8。说明及逐资源审计见 docs/角色专属默认手型-2026-11-14.md、docs/character-hand-audit-2026-11-14.json。0.6.36 / 84 构建、签名和 500 份新增手部资源的 APK 检查通过；真机未验证。保留字幕、预览音效及 LocalSaberPart，未修改公开资源 URL／摘要。
- 0.6.36 / 84：`dist/yunjian-riichi-unity-0.6.36-local.apk`，274,739,438 字节，SHA-256 `95ff99f2437efaab00f3ef947780ae123663a60ec0491e205f61ed40d95439f6`。v1/v2、独立 API-23 v1、原证书、16 KB 对齐、两架构、角色手部运行类／500 份内置模型动画材质资源及上一轮音频／目录／宿主输入通过。首个内部构建在总览检查中发现间桐樱、远坂凛的同类投影残留，未交付；已补全资源标志与运行时过滤，重新运行全部检查并重建最终包。清理 2 项旧 Gradle transforms，逻辑大小 1,809,400,602 字节，C 盘空闲增加 1,828,888,576 字节，保留 modules-2；详见 build/character-hands-r37/。未提交／推送／发布。
- 2026-11-14 字幕（Codex，未提交）：逐帧核对用户提供的 `../video/1102/角色語言字幕.mp4`，新增 `CharacterVoiceCaption.cs`／`LobbyController.Subtitles.cs` 及 .meta。实际播放成功后按语音路径取目录文本，大厅／宿舍下方半透明黑框白字逐字显示、自动换行、长文本跟随末尾，16:9 避开右侧面板。停止、静音、自然结束、换角色、离开宿舍及暂停同步清理；遮罩和语音试听页不重复显示。仅字幕适配，无新增养成或其他玩法事件。2374×1080、1920×1080 实际编辑器渲染已查看；2,562 条大厅类目录精确匹配及交互／生命周期共 2,588 项检查通过，Node 202/202、Python 8/8。说明见 docs/角色语音字幕-2026-11-14.md，复验脚本 scripts/test-character-subtitles.ps1。0.6.35 / 83 暂存工程构建、签名与包内容验证通过；真机未验证。
- 0.6.35 / 83：`dist/yunjian-riichi-unity-0.6.35-local.apk`，256,882,936 字节，SHA-256 `691344f9015ffb14b897b4d7dbf48d2fc000939954a43ed5d5a7a10272b3f26b`。保留上一轮预览音效修正及暂存工程 LocalSaberPart；字幕运行类／绘制方法／对话框纹理、当前目录／宿主输入／音频、两架构、v1/v2、独立 API-23 v1、原证书及 16 KB 对齐通过。未提交、推送或发布。清理 1 项构建前且大于 50 MB 的旧 Gradle transforms，逻辑大小 859,708,927 字节，C 盘空闲增加 868,839,424 字节，保留 modules-2；报告见 build/character-subtitles-r36/。
- 2026-11-14 本轮（Codex，未提交）：用户确认仅测试了预览。已从真实装扮预览入口重现「神火坠霄」无音效：视觉正常、音效开关开启，但 inTable=false，Cue 被拒绝，声道输出为 0。`RiichiClient.DressPreview.cs` 在预览入场／状态变化时同步 GameAudio.SetScene；`LobbyController.DressTable.cs` 轮换座位前等待长音效结束，主动退出照常停止。修正后神火坠霄、立直－赤麟、立直－火焰连续多轮有非零音效输出，并验证退出清理。上轮仅验证独立音档／Cue，漏测了真实预览路径，0.6.33 因此仍无预览音效。详情见 docs/预览音效修正-2026-11-14.md，证据在 build/effect-playback-r35/。
- 0.6.34 / 82：`dist/yunjian-riichi-unity-0.6.34-local.apk`，256,870,648 字节，SHA-256 `2c6f73f77cc9dbc0d72f5760fe964ef1faa671e8660ed085775869c92a256b29`。暂存工程构建，保留 LocalSaberPart；v1/v2、独立 API-23 v1、原证书、16 KB 对齐及 APK 内修正运行属性／音效／目录／宿主代码通过。未提交、推送或发布。清理 1 项构建前且大于 50 MB 的旧 Gradle transforms，逻辑大小 859,708,471 字节，C 盘实际空闲增加 868,835,328 字节，保留 modules-2。
- 2026-11-13 本轮（Codex，未提交）：用户要求继续音效验证，并接通角色包已有语音的触发点。新增 CharacterVoiceTriggers／CharacterVoicePlayback，覆盖大厅登录、八条交互、友人房进入／准备、整场开局、特殊弃牌／低余牌／听牌提示；补齐本机流局听牌／未听牌、流局满贯、四种途中流局及终局一位变体。每角色 133 条中 110 条对应已有事件，剩余 23 条缺少四川玩法或养成系统，保留手动试听。语音按目录解析子路径，鸣牌优先、轮询去重、暂停／换局清队列；自动语音压低并恢复 BGM。两份 engine.js 同步，仅增加本机听牌的明宝牌／拔北番数，不公开里宝牌或别家暗手。明细见 docs/角色语音触发适配-2026-11-13.md 及逐项审计 JSON。
- 0.6.33 / 81 本机测试 APK：`dist/yunjian-riichi-unity-0.6.33-local.apk`，256,874,744 字节，SHA-256 `cdf124e1b87525fb5bec637e457ca3f7bd5940d30ce6a978137ba5c666462f32`。包含本轮语音触发和前轮 50 份特效音效修正；未提交、推送或发布。构建后按既有约定清理 2 项旧 Gradle transforms，逻辑大小 914,464,801 字节，C 盘实际空闲增加 923,971,584 字节，保留 modules-2；报告在 build/character-voice-r34/cache-cleanup.json。
- 2026-11-11 本轮（Codex，未提交）：已修正 4 款和牌（含「神火坠霄」和默认雷击）及 46 款立直的音效遗漏。50 份官方 MP3 和 .meta 内置于 Resources/Majsoul/Audio/，catalog 补齐 cue。「立直-苍火」原版 audio_id=0，其余 119 项音档完整解码且有非零采样。新增音效回归检查及逐项来源记录，详情见 docs/特效音效核对-2026-11-11.md。Android／Windows 资源地址与依赖检查通过。
- 共享根目录已对齐 GitHub 基线 9987935e，保留原本地分支 codex/lobby-room-1013（bfed191）。版本和试装包详情见 [交接手册](docs/CODEX-HANDOFF.md)。
- 双方在同一目录轮流开发。AGENTS.md、CLAUDE.md 和本页是协作入口。
- 2026-11-07 本轮（Claude Code，未提交）：
  - 鸣牌语音打断：`RoleVoice.cs` 新语音打断正在播的语音（连续拔北、拔北后被碰）；和牌语音不被普通鸣牌打断，双响时排队。
  - BGM 压低：`PlaylistAudio.cs` 改为缓降 0.8 秒、缓升 1.2 秒。语音页标志需每帧刷新，离开页面后自动释放。`LobbyController.CharacterVoice.cs` 只在语音页是当前页时压低（原问题：退出过渡期间旧页面仍在绘制，又把标志设回 true）。
  - 对局皮肤记忆：`CharacterRoster.cs` 新增按角色保存的 `battle` 选择和 `BattleSkin()`。`LobbyController.RoleSelection.cs` 的头像和切换都改用它。宿舍装扮保持独立。
  - 装扮预设（参考原版的交互形式，视觉为自有排版）：`WardrobePresets.cs` 共 5 个预设，存于 PlayerPrefs `riichi.wardrobe.presets.v2`。每个预设都是一套可编辑的装扮：`LobbyController.Wardrobe.cs` 左侧分类列表上方是页签 1–5 和「使用/使用中」按钮。编辑的是正在查看的预设，只有正在使用的预设会同步到 WardrobeSelection；打开装扮页时，正在使用的预设先与当前穿着同步。分类列表从 y=265 开始，高 740。
  - 倒牌与拍牌（依据 docs/research-2026-11-07-presets-and-tile-motion.md）：
    - `HandRevealMotion.cs` 倒牌改为 0.05s 缓出，去掉蓄力和回弹；新增按座位的延迟，流局时左右家 +0.35s、对家 +0.40s。
    - `TableRenderer.cs` 新增 `drawDelays`；自己和牌时去掉手牌相机那排 0.14s 的淡出，与桌面那排同一帧切换。
    - `TableEffects.cs` 自摸牌改为背面朝和牌者立着被拍下，交接后站立 0.10s，再朝和牌者倒下 0.10s（缓入），回弹 4° 用时 0.05s（`UpdateTsumoTopple`）。
    - `ReviewValidation0523.cs` 的旧曲线断言已同步更新。
    - 荣和第二段（「荣和的牌接到和牌者那排右端」）已在 0.6.27 撤销：`自己荣和.mp4` 中和牌者只亮 13 张，和的牌留在放铳者牌河。
    - 流局时未听牌（用户指正后的版本）：未听牌的手牌牌面始终朝向持有者，从立着（`drawStanding` +90°）朝前扣倒成背面朝上，不亮牌。本家未听牌时，立牌用 `state.hand` 的真实牌面（`OriginalTiles.CreateTable` 的 `concealedFace`），在 `SelfNotenDelay` 0.55s 时最后扣倒。之前「先正面朝上再翻面」的做法是误读录像，已移除。
    - 修正一个旧问题：蓋牌立着时牌面原本朝外（对着别家）。
  - 预览录制工具：`Assets/Scripts/ReviewR33Capture.cs`、`Assets/Editor/ReviewR33.cs`。
- 2026-11-08 本轮（Claude Code，未提交，版本 0.6.25 / versionCode 73）：
  - 第 4 项预设（用户决定：新预设空白、不做重命名、不做重置、改竖排、「指定」开关暂不做）：`WardrobePresets.cs` 改为 10 个栏位，存于 `riichi.wardrobe.presets.v3`；新栏位为空，空分类等于默认物品，「使用」时空分类换回默认；打开装扮页时只把非默认的当前穿着同步进正在使用的栏位。`LobbyController.Wardrobe.cs`：栏位竖排在面板左侧，面板与栏位一起水平居中（`DressX`，原先面板贴右，16:9 时偏左）；分类列表上方显示预设名，下方是「使用/使用中」按钮；空分类显示灰色提示语。
  - 第 6 项特效预览牌桌：装扮页立直特效、和牌特效、鸣牌指示的眼睛按钮进入全屏「装扮预览」牌桌。`RiichiClient.DressPreview.cs` 只在本地渲染脚本状态：不连房主、不轮询、会话不激活，退出回到空的大厅状态。`DressPreviewStates.cs` 构造四家牌河与手牌；`LobbyController.DressTable.cs` 播放循环脚本（立直：手把立直牌横着打出；和牌：自摸拍牌加和牌特效；鸣牌：上家打出可碰的牌），进入时装扮页淡出、牌桌淡入（进桌动画），退出时反向淡回，循环之间短暂黑场。`TableEffects.SuppressShouts` 在预览中关闭喊声、切入和语音。
  - 自摸拍牌（0.6.26 / versionCode 74，用户反馈「竖直砸下去、模型穿进牌桌」）：`TableEffects.TsumoHandScale` 由 0.48 改为 1，按制作比例，原版录像中接触时手掌约 5 张牌宽。`PositionCarriedTile` 不再把牌的水平位置钉在落点，改为沿 Zimo 动作的弧线：从右下抬向镜头，再划弧落下。现在只保证立着的牌不低于桌面。分析见研究文档「自摸拍牌复查」。
  - 第 7 项补充（依据 `九种九牌.mp4`、`其他玩家自摸.mp4`、`荒牌流局.mp4` 逐帧）：本家在流局（听牌或未听牌）和九种九牌时先切到桌面立着，+0.55s（`HandRevealMotion.SelfDelay`）再推倒或扣倒；原先本家听牌在切换同一帧就推倒，看不到推倒动作。别家自摸时和牌者手牌在拍牌期间保持立着，和牌特效爆开时才放倒（`TableEffects.BurstFired`，特效缺失时在演出结束后放倒）。

- 2026-11-08 真机反馈 8 项（Claude Code，未提交，0.6.27 / versionCode 75）：
  1. 荣和：删除 `TableRenderer.ShowRonTile/CreateRonTile` 和 `TableEffects.ShowRonTiles`，和牌者手牌旁不再多出一张。
  2. 役种日文：`engine.js` 新增 `YAKU_NAMES`/`yakuName()`，覆盖 core.js 全部役名（ダブル立直→两立直、翻牌→役牌、場風/自風→场风/自风 等），结算语音别名表均已包含这些简体名。两份 engine.js 已同步。
  3. 预览入场动画：`LobbyController.DressTable.cs` 打开预览牌桌时先播开局入场动画（`RiichiClient.PlayDressIntro`，复用 `MatchIntroduction.Reenter`），资源预载与首帧渲染在动画后面完成，动画结束直接进牌桌；`DressPreviewStates` 其余三家改为「特效演示1/2/3」、固定一姬 400101。每次进入预览都会播放（约 3.5s）。
  4. 去掉「已使用预设N。」提示；只在有未下载装扮被跳过时才提示。
  5. 立直手臂：`TableEffects.MeasureArm/ExtendArm` 把前臂远端沿自身方向加长 0.6（源单位），手掌和手腕不动；参考点取「牌离网格最近的那一帧」的牌位置，人手、男手、猫爪、狗爪的权重都只落在前臂上。问题出在左右家放立直棒的那只手（20:9 时前臂末端露在屏幕内）。
  6. `ResourceRoomCoordinator` 收起后不再自己画按钮，改由房间页在「开始」左侧画同款同尺寸的「资源准备」（`LobbyController.DrawRoom`），开局准备期间仅这个按钮可点。
  7. 拔北穿模：`TableRenderer.ClearNorthTiles` 在北牌会碰到本座位副露、亮出的手牌或自摸牌时，把北牌整体向内移到刚好不碰（按最终平放姿态计算）；`AlignOwnRevealedRow` 让本家亮出的手牌和自摸牌留在副露左侧（20:9 下 3 副露时会碰到暗杠）。新增自动穿模检查 `ReviewR33Capture.OverlapScan`（`YUNJIAN_R33_MODE=overlap`）。
  8. 宿舍里给当前出战角色「选择服饰」时同时改出战皮肤并通知房间，大厅立即生效；`engine.js` 的 `rollCharacter` 在建房、进房、切换选择时就抽好随机雀士，房间里直接显示，开局沿用同一个。

- 0.6.28 / versionCode 76（用户反馈：别家自摸时北牌仍被推了一下）：原因是打牌时北牌在原锚点，和牌重绘后才发现亮牌并内移。`ClearNorthTiles` 现在对别家始终预留该座位最终平放的亮牌排加自摸牌位置，北牌从打牌起就在最终位置；`NorthClearance` 由 0.03 改为 0.06。新增 `ReviewR33Capture.NorthStable`：三麻别家 × 1–4 张北 × 0–4 副露，比较打牌/自摸/荣和三种状态下北牌位置。
- 0.6.29 / versionCode 77（真机反馈 6 项）：
  1. 长和牌特效：`TableEffects.WinEffectPlaying` 让牌桌一直保持到所有和牌特效真正结束（上限 12 秒）才进入结算。咒层界（`ron_25hf`）原先在特效开始约 5.3s 时允许结算，特效要到 6s 才结束；现在验证时钟上 8.6s 特效结束、同一刻才允许结算，特效在 8.0–8.2s 自然淡出。
  2. AI 等演出：`engine.js` 的 `presentationLeft` 按最近事件推算客户端还要演多久，AI 在这之后才行动：立直宣言打牌 3.2s、立直棒 1.3s、吃碰杠 1.6s、拔北 1.5s，都从事件时间算起；普通打牌仍是 0.7–1.0s。
  3/4. 预载：新文件 `LobbyController.Preload.cs`。启动时（在 Unity 启动画面后面）预载大厅角色的静态和动态立绘、立绘取景与大厅位置设置、宿舍服饰、大厅背景，以及装扮页字体（`GetOSInstalledFontNames` 在安卓上很慢）、40 张装扮 UI 图和 394 个物品图标（都是内置资源，不开资源包）。如果预载时启动画面还在，画面结束后重播大厅入场动画。打开宿舍时也会补做装扮页预载。
  5. 设置 · 偏好新增「电脑装扮」：默认装扮 / 随机已下载（`AppPreferences.RandomBotWardrobe`，默认关）。客户端把本机已下载的立直棒、立直特效、和牌特效、手、头像框 id 发给房主，`engine.js` 的 `configureBots/addBot` 为每个新电脑各类别随机一件；无效 id 会被丢弃。桌布、牌、鸣牌指示和音乐始终跟随观看者，不随机。
  6. 已有机制，未改代码：开局资源准备（`resourceSetup.wardrobes`）包含每个座位（含电脑）的装扮；本机缺的会提示，可选 GitHub 下载、局域网从其他玩家接收（`ResourceRoomCoordinator.UpdateShare`，房主也会转发别的玩家的包）或用默认装扮代替。新增 Node 测试确认电脑的装扮进入资源准备。
- 0.6.30 / versionCode 78（真机反馈 7 项）：
  1. 开局资源准备：`ResourceRoomCoordinator.Preload` 一次性发出全部角色立绘/语音请求并一起等待，同一角色只加载一次（原先每个资源至少等一帧，全是电脑也要等几秒）；`WardrobePackService.Preload` 同一资源包内的资源并行加载。面板改为大字号、进度条（完成数/总数和正在加载的项目）、每位玩家「正在加载/正在下载/已就绪」，并说明电脑无需加载。
  2. 电脑随机装扮：可选的手只有人手和橘猫手，原先一半电脑抽到默认人手；现在有其他已下载物品时，随机池不含该类别的默认物品（`CharacterRoster.BotPool`）。设置只对之后新增的电脑生效。
  3. 进出宿舍闪默认庭院：`DrawDormBackground` 改为垫自己选的大厅背景（原先写死 yard）。
  4. 和牌后等结算太久：本项目的原生和牌特效都带源里固定的 6 秒寿命，多数 2–3 秒就看不见。`WinEffectPlaying` 改为「还在寿命内且还有粒子」，结算时机 = max(原版结算时机, 最后一个粒子消失)。炽焰：粒子 4.43s 消失，4.57s 结算（0.6.29 要到 8.57s）；咒层界仍在 8.57s 播完后结算。
  5. 特效预览四个方向：`DressPreviewStates` 支持任一座位行动并穿预览装扮；立直和和牌按 自家 → 上家 → 对家 → 下家（屏幕顺时针）循环，鸣牌指示按 上家 → 对家 → 下家 循环（每轮 3.6s），顶部标签显示当前座位。
  6. 未改：图中 4 番 30 符庄家 11600 按规则不算满贯（雀魂不采用切上满贯，4 番需 40 符以上）。角色横幅条件（`WinCutIn.Eligible`）与结算播报「满贯」用同一判定，录像中满贯和牌、一炮双响都有横幅。一炮双响流程已录像：两家依次「和」→ 各自横幅 → 同一张牌上播放和牌特效 → 结算。
  7. 倒牌速度：重新逐帧量 `荒牌流局.mp4`（60fps），本家扣倒 1.37s 起、1.43s 平放、1.47s 停稳，约 0.10s；`HandRevealMotion.Duration` 由 0.05 改为 0.10。研究文档里「≤2 帧」的结论有误。
- 0.6.31 / versionCode 79（真机反馈 4 项，本轮只出测试包）：
  1. 一炮多响按 `1102/一炮多響.mp4`：同一张牌的荣和者同时喊「和」（`TableEffects.BeginCall` 把后续同牌荣和事件并入 `winShouts`，`NativeAnnouncements.DrawWin` 只压暗一次），之后满贯以上者依次切入角色横幅（`cutIns` 队列），最后在牌上只播放一次和牌特效，使用第一个荣和者（`ronLead`）的装扮。结算仍按和牌者逐页显示。
  2. 特效预览：新增立直棒预览（打出立直 → 放置立直棒，`DressPreviewStates.RiichiStick`，四个座位循环，每轮 6.2s）；换座位不再黑屏（`DressTableCurtain` 恒为 0）；预览牌桌隐藏右上角邀请/设置/退出三个按钮，只留关闭按钮。
  3. 电脑随机装扮不再包含手（用户本意是电脑保持默认手，上一轮理解反了）：客户端 `BotWardrobeCategories` 与 `engine.js` `BOT_WARDROBE` 都去掉 hand，随机池恢复包含默认物品；设置页说明同步。
  4. 资源准备面板去掉「所有玩家就绪后自动开局…」一行；取消按钮改为 360×84、字号 34，与宿舍下载按钮同高。
- 0.6.32 / versionCode 80（进游戏卡顿，`1102/進游戲加載卡頓.mp4`）：录像中 Unity 标志后大厅先出现但看板娘缺席约 1.5s（角色包异步加载），出现时大厅入场动画又重播一次（0.6.29 起的「启动画面后重播入场」）。`LobbyController.Preload.cs` 改为启动遮罩：Unity 标志之后保持启动画面同色（#1F1F20）并显示「正在加载···」，直到看板娘静态立绘（开启动态服饰时连同动态部件，`CharacterIllustration.Ready`）和大厅背景可绘制（最长 8s），再淡出 0.35s 并只播放一次入场动画；遮罩期间大厅不响应点击、不播飞入音效。装扮页预热移到遮罩结束之后。编辑器批处理录制不显示遮罩。

## 产物

- 最新本机测试包 `dist/yunjian-riichi-unity-0.6.36-local.apk`；SHA 文件和 `dist/unity-0.6.36-local-verification.json` 同目录。亚丝娜实际牌桌／装扮预览及 Archer 修正图在 `dist/previews/2026-11-14-character-hands/`；完整逐套截图在 build/character-hands-r37/。
- 最新本机测试包 `dist/yunjian-riichi-unity-0.6.35-local.apk`；SHA 文件和 `dist/unity-0.6.35-local-verification.json` 同目录。大厅／宿舍实际截图 `dist/previews/2026-11-14-subtitles/`，六张原始检查图及逐项报告在 `build/character-subtitles-r36/`。本轮未出预览视频。
- 最新测试包 `dist/yunjian-riichi-unity-0.6.32-local.apk`（SHA-256 `f1bc8ce5…`，见 `dist/SHA256-yunjian-riichi-unity-0.6.32-local.txt`），本轮无预览视频。
- 最新测试包 `dist/yunjian-riichi-unity-0.6.31-local.apk`（SHA-256 `e713ba88…`，见 `dist/SHA256-yunjian-riichi-unity-0.6.31-local.txt`）。本轮按用户要求不出预览视频；内部核对录像 `build/claude-1108e/preview-r41.mp4`（`YUNJIAN_R33_MODE=r41`）。
- 最新测试包 `dist/yunjian-riichi-unity-0.6.30-local.apk`（SHA-256 `247d7e27…`，见 `dist/SHA256-yunjian-riichi-unity-0.6.30-local.txt`），预览 `dist/previews/2026-11-08-claude-r40/preview-r40.mp4`（录制命令 `YUNJIAN_R33_MODE=r40`，日志 `build/claude-1108d/`）。
- 最新测试包 `dist/yunjian-riichi-unity-0.6.29-local.apk`（SHA-256 `b0dd0a48…`，见 `dist/SHA256-yunjian-riichi-unity-0.6.29-local.txt`），预览 `dist/previews/2026-11-08-claude-r39/preview-r39.mp4`（咒层界和牌的结算时机 + 设置页「电脑装扮」，录制命令 `YUNJIAN_R33_MODE=r39`，日志 `build/claude-1108c/`）。
- 最新测试包 `dist/yunjian-riichi-unity-0.6.28-local.apk`（SHA-256 `666b2b7e…`，见 `dist/SHA256-yunjian-riichi-unity-0.6.28-local.txt`），预览 `dist/previews/2026-11-08-claude-r38/preview-r38.mp4`（与 r37 同结构，北牌章节已更新）及两份穿模报告。
- 0.6.27 测试包 `dist/yunjian-riichi-unity-0.6.27-local.apk`（SHA-256 `9407ca2c…`，全文见 `dist/SHA256-yunjian-riichi-unity-0.6.27-local.txt`），签名证书与 0.6.23–0.6.26 相同，可覆盖安装。
- 0.6.27 无声预览：`dist/previews/2026-11-08-claude-r37/preview-r37.mp4`（20:9，1800×810，约 68s），及穿模检查报告 `overlap-scan-1600x900.txt`、`overlap-scan-1800x810.txt`。录制命令 `YUNJIAN_R33_MODE=r37`，日志 `build/claude-1108b/`。
- 0.6.26 包 `dist/yunjian-riichi-unity-0.6.26-local.apk`，SHA-256 见 `dist/SHA256-yunjian-riichi-unity-0.6.26-local.txt`。0.6.25 包仍在 `dist/`。
- 0.6.26 无声预览：`dist/previews/2026-11-08-claude-r36/preview-tsumo.mp4`，包括和牌特效预览牌桌、本家自摸（正常速度和 0.25 倍）、别家自摸（正常速度和 0.5 倍）。录制命令为 `YUNJIAN_R33_MODE=tsumo`，日志 `build/claude-1108/capture-tsumo.log`。
- 0.6.25 无声预览：`dist/previews/2026-11-08-claude-r35/preview-wardrobe.mp4`（竖排空白预设、居中、三种特效预览牌桌）与 `preview-motion.mp4`（本家听牌流局、全员未听牌、九种九牌、别家自摸，及本家自摸、荣和对照）。日志在 `build/claude-1108/`。
- 构建在暂存工程 `build/phone-test-r24/stage/unity` 中完成：它有 Library 缓存，并保留 0.6.23 的 Saber 临时覆盖（`CharacterPackService.cs` 的 LocalSaberPart）。本轮改动的文件是手动复制过去的。本机没有系统 Python，`prepare-original-input-stage.py` 无法运行。
- 无声预览视频 `dist/previews/2026-11-07-claude-r33/preview-r33.mp4`（预设、皮肤、BGM），以及 `preview-motion.mp4`（自摸、流局、荣和，含 0.25 倍慢放，使用验证时钟，按真实节奏播放）。日志在 `build/claude-1107/`。

## 已知问题与风险

- 未完成的用户需求：
  - 第 2 项角色语音：2026-11-13 已接通已有玩法的触发点；剩余 23 条依赖尚无的四川麻将／养成系统，保留语音页试听，待新测试包真机反馈。
  - 特效预览牌桌只在大厅（未进房间）可用；进房后不进入预览。预览使用当前查看的预设装扮，未下载的物品用默认物品。
  - 别家荣和、本家听牌的流局没有原版录像，节奏按已有录像推断。
- 编辑器录制时，左右家立直喊话的合成背景板显示为整块青色（`NativeCallouts.Compose`，本轮未改）；r37 立直手臂章节因此关闭了喊话。真机上是否正常需确认。
- 第 8 项：宿舍「选择服饰」只在宿舍角色就是当前出战角色时改出战皮肤；给其他角色换装不影响大厅。
- 旧轮次编辑器曾出现 AudioSource.isPlaying 始终为 false；2026-11-13 本轮 Play 模式已观测到 true，仍未验证手机设备的实际听感。
- unity/Assets/CharacterPacks/Source/ 未跟踪部分在本机 .git/info/exclude 中排除。新增素材入库需要 `git add -f`。
- Android 真机测试未做。

## 验证

- 2026-11-14 02:49：角色默认手型 1,393 项检查通过（21 套新资源／51 方向模型／255 动作；当前 29 名新接入角色；橘猫覆盖所有 122 名角色；62 组实际四人牌桌案例及亚丝娜额外三人桌方向／装扮预览）。查看全部当前手型的逐套截图，修正三套 Fate 投影网格黑块。157 份原始资源 SHA、原表角色／速度／关键时刻与新资源 GUID／摘要审计通过；Node 202/202、Python 8/8。0.6.36 构建与专用 APK 校验通过，ADB 无连接设备，未做真机测试。验证环境显式设置装扮快取与 RespectPreviewAvailability，避免把缺少的下载包当成已安装；保留已知 Unity Search 启动索引异常的单独排除。
- 2026-11-14 01:23：字幕实际点击／播放和 122 名角色共 2,562 条大厅类目录匹配，共 2,588 项检查通过；查看 2374×1080、1920×1080 大厅／宿舍截图及 189 字长台词末尾跟随。静音、找不到音档、连续点选、遮罩、换角色、离开宿舍、自然播完、试听页去重、应用暂停均通过。Node 202/202、Python 8/8；0.6.35 构建和专用 APK 校验通过。ADB 无连接设备，未做真机验证。测试脚本首次沙箱内启动失败于本机 Package Manager IPC，沙箱外正常；首轮 Unicode 测试碰到浮点数边界，调整取样时刻后通过；已知 Unity Search 启动索引异常仍存在，不影响本轮 update 驱动的验证。
- 2026-11-14：真实预览修正前／后对照及保存后的 `scripts/test-effect-preview-audio.ps1` 重跑通过；神火坠霄 22 秒内实际请求 3 次，两款立直各 5 次，三款音效声道均有非零输出，退出后均停止音效并回到大厅。Node 全套 202/202、音效专项 3/3、Python 8/8。0.6.34 Android 构建、签名及针对本轮的包内容检查通过；见 build/effect-playback-r35/apk-verification.json。ADB 无连接设备，未做 Android 真机验证。当前只改预览音效场景与循环生命周期，没有改实际对局的播放逻辑。
- 2026-11-13 01:25：Unity 授权已可用；特效 120 款／119 份非静音音频／119 次 GameAudio 调用通过。角色语音运行验证 122 名／16,226 条路径、2,806 条上下文语音采样、21,982 项检查通过，AudioSource.isPlaying 为 true；包括终局、四种途中流局、提示打断、BGM 压低／释放。独立生产 C# 触发检查 33 项通过，Node 202/202、Python 8/8 通过。0.6.33 / 81 暂存工程 Android 构建通过，保留 LocalSaberPart 临时覆盖；v1+v2、独立 API-23 v1、原证书及 16 KB 对齐通过。包内 50 份新特效音频、327 份基础角色 MP3、两架构、新运行类、当前目录及全部 Android 宿主输入核对通过；原有 voice-directory 的 CRLF 差异按打包 TextAsset 解析后的 JSON 内容核对，语义一致。没有宣称旧通用 APK 校验器全套通过；本轮使用 scripts/verify-character-audio-apk.py。真机尚未验证。
- 2026-11-11 22:54（本轮音效）：Node 默认隔离命令 199/199 通过，Python 角色输入 8/8 通过；119 份音档以独立解码器完整读取并检查有效非零采样。Unity 沙箱外重试返回授权错误 198（com.unity.editor.headless 缺失），验证脚本未执行，游戏内播放与 Android 真机待验证。暂存工程已同步音效及 catalog，保留 Saber 临时覆盖；下一版仍从 0.6.33 / 81 开始。
- 2026-11-09 00:10（0.6.32）：Unity 构建通过（编译无错）；APK 签名（v1+v2、证书 d1d8e506…、16 KB 对齐）通过，aapt 读出 0.6.32 / 80；出包后清理 Gradle 缓存 0.8 GB。启动遮罩在编辑器批处理里不显示，效果只能真机确认；未运行 Node 测试（未改 engine.js）。
- 2026-11-08 21:30（0.6.31）：Node 196/196 通过（电脑装扮测试新增「不含手」断言）。r41 核对录像抽帧：两家同时喊和、两段横幅、只播一次特效、两家都亮牌；立直棒预览自家与上家依次放棒，换家无黑屏，右上角只有关闭按钮。APK 签名（v1+v2、证书 d1d8e506…、16 KB 对齐）通过，aapt 读出 0.6.31 / 79；出包后清理 Gradle 缓存 0.9 GB。未做真机测试。
- 2026-11-08 20:30（0.6.30）：Node 196/196 通过。r40 录制通过，抽帧确认：资源准备面板进度与状态；进出宿舍全程是自选大厅背景；立直/和牌预览四个座位依次触发，鸣牌指示三个座位；炽焰 4.57s、咒层界 8.57s 结算且不早于特效结束；一炮双响两家各自横幅后再播特效；流局扣倒 0.10s。录制脚本此前把同一状态里的第一个荣和事件当作已播放（只影响录像，不影响游戏），已修正。未做真机测试；资源准备提速只在编辑器验证可运行，实际耗时需真机确认。APK 签名（v1+v2、证书 d1d8e506…、16 KB 对齐）通过，aapt 读出 0.6.30 / 78。
- 2026-11-08 18:40（0.6.29）：`node --test tests/*.test.cjs` 196/196 通过，新增 AI 等待立直演出、电脑随机装扮进入资源准备两项。r39 录制通过：咒层界特效结束 8.6s，允许结算 8.6s；设置页新行显示正常。APK 签名（v1+v2、证书 d1d8e506…、16 KB 对齐）通过，aapt 读出 0.6.29 / 77。启动预载与宿舍卡顿只在编辑器里确认能运行，卡顿是否消失需真机确认；AI 等待时长按编辑器动画时长估算，未在真机计时。未运行 Python 测试（本轮未改角色输入）。出包后已清理 Gradle 缓存 1.7 GB。
- 2026-11-08 17:20（0.6.28）：北牌位置检查 16:9 与 20:9 各 80 组比较，最大位移 0；穿模检查各 380 种牌桌 0 处重叠；r38 录制通过，抽帧确认北牌在亮牌前后位置不变、与和的牌有间隙。APK 签名（v1+v2、证书 d1d8e506…、16 KB 对齐）通过，aapt 读出 0.6.28 / 76。本轮只改 `TableRenderer.cs` 与录制脚本，未重跑 Node/Python 测试；未做真机测试。
- C 盘清理：Gradle `transforms` 每次 APK 构建新增 0.7–4.9 GB 一次性缓存（已累计 15 GB）。已删除旧构建留下的大目录共 13.3 GB，C 盘可用由 34.5 GB 增至 47.1 GB；`modules-2` 依赖缓存保留。之后每轮出包后照此清理。%TEMP% 里的大目录来自 Visual Studio 安装器和后台下载，不是本项目产生的，未动。
- 2026-11-08 16:45（0.6.27）：`node --test tests/*.test.cjs` 194/194 通过（Node 24，未加 `--test-isolation=none`），含新增的役名中文化与随机雀士进房即抽两项；`test_character_inputs.py` 8/8 通过（codex 运行时自带 Python）。穿模检查 1600×900 与 1800×810 各 380 种牌桌、约 50 万对牌，0 处重叠（修改前 285 种中有 6 类 132 处，全部为北牌）。r37 录制通过，抽帧确认：入场动画显示特效演示1/2/3；「资源准备」与「开始」同尺寸同一行（221×79，y 相同）；别家荣和只亮 13 张；两种拔北情况不再重叠；左右家放立直棒时，修改前前臂末端在画面内，修改后延伸出屏幕。APK 构建、签名（v1+v2、证书 d1d8e506…、16 KB 对齐）通过，aapt 读出 0.6.27 / 75。未做真机测试；第 8 项与第 2 项只有自动测试，没有录进视频。
- 2026-11-08 10:30（0.6.26）：ReviewR33 tsumo 模式录制通过（965 帧）。抽帧确认：手从右下入场，抬向镜头，划弧落到落点；接触时指尖在牌后方，手不再平贴桌面穿过立着的牌；牌立约 0.1s 后倒成正面朝上。和牌特效预览牌桌、别家自摸照常。APK 构建成功；签名脚本报告 v1+v2、证书 d1d8e506…、16 KB 对齐通过；aapt 读出 0.6.26 / 74。未运行 Node 和 Python 测试（未改 engine.js），也未做真机测试。
- 2026-11-08（0.6.25，编辑器 Play 模式 ReviewR33，均通过）：装扮页左右留白相等（1600×900 时各 59）；切到预设 2 时该栏位为空；在预设 2 选装扮不影响当前穿着，「使用」后换上、切回预设 1 后换回；三种特效预览牌桌均能打开，和牌特效实际生成，鸣牌指示挂到目标牌上，退出后会话不激活且 State 为空。动作录像抽帧确认：本家听牌先立后推倒、别家自摸拍牌时手牌立着、特效爆开时亮牌；九种九牌先立后推倒。
- APK 0.6.25：Unity 构建成功；签名脚本报告 v1+v2、独立 v1 校验、证书 d1d8e506…、16 KB 对齐通过；aapt 读出 versionName 0.6.25、versionCode 73。
- 2026-11-07 自动检查（编辑器 Play 模式，ReviewR33）：在正在使用的预设里改装扮会立即生效；编辑未使用的预设不影响当前穿着；「使用」能在预设 1 和预设 2 之间切换桌布；给一姬选 400103 后切到二阶堂再切回，皮肤保持；离开语音页后 BGM 压低系数恢复到 1。
- 动作录像（ReviewR33，`YUNJIAN_R33_MODE=motion`）：两段荣和都检查到荣和的牌已接上；抽帧确认流局各家依次倒牌、未听牌的手牌牌面朝持有者立着后扣倒、本家最后扣倒，以及自摸先倒牌后拍牌、牌立着拍下后倒成正面朝上。
- APK：Unity 构建成功，签名为 v1+v2，16 KB 对齐通过，aapt 读出 versionName 0.6.24、versionCode 72。
- 未运行：Node 测试与 Python 测试（本轮未改 engine.js；本机无 Python）、Android 真机测试。

## 研究（只分析，未改代码）

- [docs/research-2026-11-07-presets-and-tile-motion.md](docs/research-2026-11-07-presets-and-tile-motion.md)：装扮预设的状态转换，以及自摸、荣和、流局的倒牌与拍牌关键帧，并与本项目实现做了对照。主要结论：原版倒牌约 1–3 帧完成，本项目是 0.58s 慢倒加回弹；流局时原版各家依次倒牌。改动待用户确认。
- 依据只有用户提供的录像和本项目代码，没有使用从原版解出的 Lua 代码。

## 下一步

安装 `dist/yunjian-riichi-unity-0.6.38-local.apk`，先检查虚影祝祷真实装扮预览与实战四座位牌面；三人开局中的福姬 403807、小鸟游雏田 401906、Saber 40011002 清晰度；役满实际荣和／自摸的前导、全屏场景、音效、真实手牌／副露／赤五及结算衔接。编辑器对照与役满预览在 `dist/previews/2026-11-14-effects-and-portraits/`，全手部图集在 `dist/previews/2026-11-14-all-hands/`，本轮证据 `build/feedback-1114-r39/`。继续检查亚丝娜默认手四方向和宿舍肤色、白袖甲、反光；其余人物材质本轮未调整。旧模型、字幕及音效证据仍保留在各批次 build 目录。自动测试、编辑器及包内容核验已通过，真机待反馈；下一版 0.6.39 / 87。

1. 用户真机测试 0.6.32：冷启动时 Unity 标志后是否先显示「正在加载」再整体淡入大厅、入场动画只播一次。以及 0.6.31 的：一炮多响同时喊和与单次特效、立直棒预览、电脑保持默认手、资源准备面板。以及 0.6.30 的：资源准备是否明显变快、电脑随机装扮（手）、进出宿舍是否还闪、各和牌特效到结算的衔接、四方向特效预览、倒牌速度。以及 0.6.29 的：咒层界等长特效是否播完再结算、电脑在立直等演出后才行动、宿舍切装扮和冷启动是否还卡顿、「电脑装扮」随机后其他玩家能否下载到。另外仍待测 0.6.27/0.6.28 的：8 项反馈（荣和、役名、预览入场动画、预设提示、立直手臂、资源准备按钮、拔北穿模、宿舍换装与随机雀士），以及 0.6.26 的自摸拍牌；顺带看左右家立直喊话背景在真机上是否正常。
2. 已按 2026-11-13 用户要求接通「角色包已有语音的触发点」，待本轮 APK 真机反馈；其余 23 条的系统缺口见语音适配文档。
3. 工作区里仍未纳入提交的旧文件（见「交接给 Codex」）如何处理，由用户决定。其中 `docs/CODEX-HANDOFF.md` 被 AGENTS.md 引用，但仍未跟踪。
