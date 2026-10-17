# 云间日麻 · 新对话交接（Unity 0.5.46）

更新时间：2026-10-17。请在新对话先阅读本文件，再检查实际工作区与 GitHub 状态。文件描述本轮发布内容，不应将更早预览文档中的旧规则重新套用。

## 项目、版本与交付

- 维护工作区：`D:\MyWorkSpace\YunJianRiichi_Codex`。
- 专用发布克隆：`D:\MyWorkSpace\YunJianRiichi_Codex\.publish\YunJianRiiChi`，分支 `main`。
- GitHub：<https://github.com/Rtyu3221cc/YunJianRiiChi>。
- 发布标签：`unity-0.5.46`；对应源码提交以该标签／Release 的目标提交为准。维护工作区的 Git HEAD 可能早于发布克隆，不要因此重置工作区。
- APK：`dist/yunjian-riichi-unity-0.5.46.apk`，包名 `com.yunjian.riichi.unity`，versionCode **47**，minSdk 26，targetSdk 32，ARM64＋ARMv7，沿用原签名证书。
- [APK 下载](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.46/yunjian-riichi-unity-0.5.46.apk)；[发布页](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-0.5.46)。
- 源码已包括 Unity、Android 房主服务、离线引擎、资源、测试、脚本及文档，见 GitHub main。本次小修只发布 APK、校验和、构建记录与交接 Markdown，不重传旧版源码 ZIP 或发布 ZIP。
- APK 字节数：86490002；SHA-256：`92bc495de6e3a97c46f6187db46c3582a9091695b4c18eabd808fe41703aaf58`。
- 本文件在 `docs` 和 Release 附件中均提供，方便直接交给新对话。

## 用户工作方式与边界

1. 使用中文，回答简短，优先落实修改。界面需要尽量还原原版，先查项目素材与用户提供的 APK／视频，避免自造风格。
2. 用户原先要求预览时不提交、不打包、不做单元测试；0.5.45 曾授权完整测试；这次用户明确要求“这个改动就不要测试了改完直接提交git并且生成apk”，所以 0.5.46 未运行单元测试或回归测试。后续新增 UI 改动仍按当次要求处理。
3. **禁止为了发布复核而重新下载已经上传的 APK、源码 ZIP、发布 ZIP 或其他附件。** 本地检查与上传响应中的大小／哈希、远端提交和少量源码核对即可。只有用户另行明确要求诊断下载文件时才下载。
4. 不要重复录制已有演示。本版沿用已有画面、修正音轨；需要新行为时仅录必要片段。
5. 不要更改已认可的听牌指示器样式，也不要重新加入“打什么牌能听什么牌”的建议列表。

## 本版行为与关键文件

### 大厅、音乐与原版 UI

- 大厅 BGM 为用户试听后确认的 **《金秋送爽》**，循环播放并遵守音乐设置。资源：`unity/Assets/Resources/Majsoul/Lobby/Audio/lobby_autumn.mp3`；来源记录见同目录 `autumn-source.json`。
- 主页面有进行中的牌局时，在“牌局”灯笼显示原版红色感叹号；已按原版素材的透明边距和位置调整，文本后绘制。相关：`NativeUiChrome.cs`、`LobbyController.cs`。
- 设置、帮助、博士帽、相机四按钮按可见圆形边缘等距排列：可见高度 74、边缘间距 38 个设计像素，同列居中。博士帽打开已有规则说明。相关：`LobbyController.Shortcuts.cs`、`Resources/Majsoul/Review1017d`。
- 创建房间有原版转场；房间局数可选整庄；加入失败提示应使用“同一 WIFI 或同一热点”，不能只说热点。

### 扫码加入与二维码

- 加入房间弹窗提供与确认同尺寸的黄色“扫码”按钮；扫码页有摄像头和相册入口；无效二维码用中号提示弹窗。摄像头未开启提示在取景黑区水平、垂直居中。
- 准备房间的分享按钮、邀请好友入口和牌桌星形按钮均可打开邀请二维码。邀请弹窗删除原房主地址及说明长文字，二维码保持正方形且不超出弹窗。
- 邀请弹窗设计尺寸约 `1065.375×825`，二维码显示尺寸 **580×580**，与用户给定的大尺寸截图保持一致。
- **当前默认是透明底金色版**：金色 RGBA `(232,175,113,255)`，背景 alpha 为 0，由弹窗深蓝底透出；保留至少四模块留白及 580×580 显示尺寸。0.5.45 误将其保留为备选，用户纠正后在 0.5.46 设为默认。准备房间分享／邀请和牌桌星形入口共用此默认，不要重新切回黑白版。
- 第四种透明底金色版初次截图解码失败的根因已修正：旧代码先设 `BarcodeReaderGeneric.TryInverted`，再替换 `Options`，导致反色识别被清除。现把 `TryInverted=true` 放在同一个 `DecodingOptions` 中；同一张实际截图已成功解码，生成码、缩放码和旋转码也有单元覆盖。
- 核心：`RoomQr.cs`、`RoomQrScanner.cs`、`LobbyController.Invites.cs`；安卓相册桥接：`unity/Android/QrImagePickerActivity.java`；ZXing 0.16.11：`unity/Assets/Plugins/ZXing`。
- QR 内容仅含公开房间号和局域网地址：`yunjian-riichi://join?v=1&pin=...&hosts=...`。不要加入座位 token、恢复码或房主密钥。解析器只接受符合规则的本地地址。

### 牌桌、听牌与役满

- 指示器背景白线已用原版九宫格像素对齐和半像素内缩处理。
- 听牌按钮位于 AI 接管按钮上方，点击开关。听牌浮层仍在手牌上方居中；牌与张数比原布局下移 5 个设计像素。合法舍牌选择可自动显示对应等待牌；当前决策变化会重置，出牌后关闭。
- 保留原版等待牌、剩余张数、无役提示。UI 不展示舍牌建议列表。相关：`RiichiClient.cs`、`TableIndicators.cs`。
- 役满机会／役满确定采用 APK 原粒子资源、字图与材质参数；役满确定使用 `effect_yakumanready.wav` 的拔剑音效，在特效可见起点触发一次。每手相同级别不会重复弹。
- 原 APK Lua 是加密字节；没有获得可读 Lua 源码。触发判断使用本项目引擎的自家手牌评分与公开结果，不能声称已照抄解密原游戏逻辑。
- `YakumanAnnouncement.cs`、`NativeUiEffect.cs` 与原生资源在 `Resources/Majsoul/NativeReview`；UI 粒子使用 `UiParticleBuffer.shader`／`UiParticleComposite.shader`，避免黑色粒子底块和光晕矩形裁边。
- 对家横打立直牌在移动时使用牌桌深度（layer 0），前排弃牌能遮挡；手部叠加层保持原处理。响应黄框／黄三角等待弃牌交接完成再出现。相关：`TableEffects.cs`、`TableRenderer.cs`。
- 自摸临时模型沿预留牌位垂直起落，沿用正常大小与牌桌深度，落地交接后不重叠。

### AI 接管、遮罩与全 AI 终局

- 长考默认 **“两分接管”**（120 秒没有任何真人操作）；“无限制”关闭闲置接管，不再隐含 120 秒逻辑。
- 正常轮询不刷新操作时间；实际鼠标、触摸或按键活动通过原轮询 `userActive` 带给房主，无额外请求。接管按玩家自己的 `lastInputAt` 计时，不只看当前出牌玩家。
- 断线／离开检测仍独立存在（约 12 秒）。被闲置或离线自动接管时有原版暗色头像遮罩和闪电；主动开启 AI 接管无此遮罩。重新回座或收回闲置接管后清除；离座电脑接替后新真人接座也清除。
- 所有文案统一“AI接管”。相关：`RiichiClient.Session.cs`、`NativeTakeoverBadge.cs`、`RoomProtocol.cs`。
- 全 AI 对局无人在线时可在单局结算约 12 秒后继续，直到终局；终局标记 `result.autoTerminal`，确认或恢复连接后也保持终局，房主显式“再来一场”才重开。在线观众仍等本机结算演出完成后确认。
- 引擎维护源：`app/src/main/assets/engine.js`；Unity 安卓镜像：`unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js`。修改后必须同步；Android 构建脚本自动同步。

### 结算倒计时与声音（以本段为最终规则）

- 和牌役种详情在完整展示后“确认(3)”倒计时，三秒到零推进，支持提前点击。
- 积分结算在排名演出完成后显示暗色“确认(3)”按钮，三秒自动确认；“确认”和“返回大厅”字号一致。终局的下一局仍为九秒机制；全 AI 保留终局的服务端规则独立生效。
- **积分数字开始变化时播放“嘀嘀嘀”，新排名随后出现，第一位指示出现仅“当”一次；终局排名页没有音效。** 进入终局还会停止上一页残留音效。
- `score_change` 与数字动画共用 0.8～2.45 秒，约每 0.15 秒触发；`rank1` 仅积分页一次；不使用 `winlose` 作积分变化音效。
- 相关：`ResultPresentation.cs`、`SettlementAudio.cs`。更早“积分和终局各播放一次 rank1”的预览说明已作废。

## 本轮构建与测试范围

- 本次只有默认二维码配色和版本号等发布记录的修正；`LobbyController.Invites.cs` 的 `invitationGold` 默认初始化为 `true`，沿用反色识别修复。
- **按用户明确要求，本次未运行任何单元测试、Play 回归、录制或截图测试。** 不要把 0.5.45 的测试数量算成本版重新运行的结果。
- 0.5.45 基线曾通过 129 项 Node 测试、19 项 Java 地址检查、2146 项 Unity 断言和完整牌桌回归；这些仅为基线历史记录。
- 本次仅保留生成可安装 APK 所必需的编译和原签名处理，以及文件大小／SHA-256 记录；上传后核对提交和上传响应，不回下载。
- 没有 Android 真机实测，安装、相机／相册扫码、声音与多手机局域网仍未验证。

## 常用入口与命令

工作目录 `D:\MyWorkSpace\YunJianRiichi_Codex`，PowerShell。

```powershell
node --test --test-isolation=none tests/*.test.cjs
./scripts/verify-unity.ps1 -Mode Validate -EditorPath 'D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe'
./scripts/run-unity-regression-0542.ps1
./scripts/verify-unity.ps1 -Mode Android -EditorPath 'D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe'
& 'C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' scripts/verify-unity-apk.py
```

- Unity：`D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe`；版本 6000.6.4f1。启动批处理需隐藏窗口，显式等待退出及成功标记；Package Manager IPC 在受限环境可能需要提权。
- 本次仅构建入口：`scripts/verify-unity.ps1 -Mode Android`。不要调用 `run-unity-release-0545.ps1`，它会运行测试。
- 版本入口：`unity/Assets/Editor/ProjectSetup.cs`；发布版同步修改 `verify-unity.ps1`、`verify-unity-apk.py` 中的输出文件名。
- 本次发布辅助：`scripts/publish-gold-0546.py`（整理白名单、提交推送、发布 APK；不运行测试，也没有回下载模式）。
- 本次构建日志：`build/release-0546/android-build.log`、`build/unity-android.log`。基线测试日志保留在 `build/release-0545` 等旧目录。
- 金色二维码外观参考既有 `dist/previews/unity-0.5.45/qr-compact-gold.png`，本次未重拍；历史完整预览：`dist/preview-20261017/index.html`（本机 8877 服务不是应用运行端）。
- 视频参考：`D:\MyWorkSpace\video\1013` 及 `D:\MyWorkSpace\video`；历史 APK／提取工具位于本地工具目录，原 APK、SDK 和解包环境不上传。
- 代码与资源提交只操作发布克隆，排除 `Library/Temp/Builds/tools/build`、运行存档、私钥；不重置维护工作区，不把历史 APK 塞进源码仓库。APK 由 GitHub Release 提供，源码正常进入 Git。

## 给新对话的起始提示

> 请先读 `D:\MyWorkSpace\YunJianRiichi_Codex\docs\handoff-unity-0.5.46.md`，检查维护目录、发布克隆和 `unity-0.5.46` 的实际状态，再继续我接下来提出的修改。保留既有 UI 和最新结算声音规则；不要自动回下载发布文件，不要重复录制旧演示。只有我要求时再打包或上传。
