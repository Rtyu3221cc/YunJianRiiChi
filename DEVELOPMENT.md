# 当前开发进度

- 更新时间：2026-11-10 19:55（东京时间）
- 最近接手：Claude Code（准备交回 Codex）
- 共享 Git 根目录：E:\MyWorkSpace_E\YunJianRiiChi
- Unity 工程：unity/，Unity 6000.6.4f1
- 当前分支：main。2026-11-10 用户授权「代碼上傳至github」后，0.6.23 → 0.6.32 的代码已作为一个提交推送到 origin/main，一同推送的还有此前的两个协作文件提交 a7c37100、e63b5b4b。未打标签，也未发布 Release。

## 交接给 Codex（Claude Code，2026-11-10）

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
  - 下一版从 0.6.33 / 81 开始。
- 出包后清理 C 盘（用户要求定时做）：删除 `%USERPROFILE%\.gradle\caches\9.3.1\transforms` 里比本次构建旧、且大于 50 MB 的目录，保留 `modules-2`。
- 测试环境：
  - Node 24 直接运行 `node --test tests/*.test.cjs`，上次结果是 196/196 通过。
  - Python 用 codex 运行时自带的 python.exe。
- 第一步：用户真机反馈 0.6.32 之后，再按反馈修改（见文末「下一步」）。

## 已完成

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
  - 第 2 项（原版结算、大厅提示语音）：未做。Claude Code 不从原版新增语音；可做的是在结算、进大厅、切换角色、进对局时播放角色包已有语音的触发点，待用户决定。
  - 特效预览牌桌只在大厅（未进房间）可用；进房后不进入预览。预览使用当前查看的预设装扮，未下载的物品用默认物品。
  - 别家荣和、本家听牌的流局没有原版录像，节奏按已有录像推断。
- 编辑器录制时，左右家立直喊话的合成背景板显示为整块青色（`NativeCallouts.Compose`，本轮未改）；r37 立直手臂章节因此关闭了喊话。真机上是否正常需确认。
- 第 8 项：宿舍「选择服饰」只在宿舍角色就是当前出战角色时改出战皮肤；给其他角色换装不影响大厅。
- 本机编辑器（含非 batchmode）里 AudioSource 不会真正播放（isPlaying 始终为 false），所以语音打断无法在编辑器里录到，只能真机验证。
- unity/Assets/CharacterPacks/Source/ 未跟踪部分在本机 .git/info/exclude 中排除。新增素材入库需要 `git add -f`。
- Android 真机测试未做。

## 验证

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

1. 用户真机测试 0.6.32：冷启动时 Unity 标志后是否先显示「正在加载」再整体淡入大厅、入场动画只播一次。以及 0.6.31 的：一炮多响同时喊和与单次特效、立直棒预览、电脑保持默认手、资源准备面板。以及 0.6.30 的：资源准备是否明显变快、电脑随机装扮（手）、进出宿舍是否还闪、各和牌特效到结算的衔接、四方向特效预览、倒牌速度。以及 0.6.29 的：咒层界等长特效是否播完再结算、电脑在立直等演出后才行动、宿舍切装扮和冷启动是否还卡顿、「电脑装扮」随机后其他玩家能否下载到。另外仍待测 0.6.27/0.6.28 的：8 项反馈（荣和、役名、预览入场动画、预设提示、立直手臂、资源准备按钮、拔北穿模、宿舍换装与随机雀士），以及 0.6.26 的自摸拍牌；顺带看左右家立直喊话背景在真机上是否正常。
2. 由用户决定第 2 项是否做「角色包已有语音的触发点」。
3. 工作区里仍未纳入提交的旧文件（见「交接给 Codex」）如何处理，由用户决定。其中 `docs/CODEX-HANDOFF.md` 被 AGENTS.md 引用，但仍未跟踪。
