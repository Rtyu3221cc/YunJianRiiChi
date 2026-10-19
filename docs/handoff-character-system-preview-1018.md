# 雀士系统效果预览交接 · 2026-10-18

维护工程：`D:\MyWorkSpace\YunJianRiichi_Codex`。承接 `handoff-unity-0.6.3.md`，本轮只提交本地实现与效果预览，不生成 APK、不发布 GitHub，也不更改版本号。

## 本轮范围

- 四名雀士：未来、一姬、二阶堂美树、寺崎千穗理。
- APK 内可获得的 21 套服饰均可选择；其中一姬「校园微风」与寺崎千穗理「玩转夏日」使用原始 Spine 4.2.08 二进制骨骼、图集、动作。不是整体位移的静态图动画。
- 所有角色显示五星好感；契约头像背景及紫色名牌只用于宿舍。
- 宿舍角色、档案、服饰介绍、立绘鉴赏缩放／移动、星标、拖动排序、星标过滤。
- 大厅出战选择与服饰展开、随机出战池及开关。宿舍服饰与大厅正在使用的角色／服饰分开保存。
- 语音灯笼未制作；漏斗筛选按钮隐藏。装扮页签保留参考外观，本轮未扩展装扮系统。
- 已替换大厅、房间、牌桌头像、动作横幅、和牌特写、结算与开局介绍的固定角色入口。
- 报役、满贯等结算语音使用胜者角色，并按其原版语音时长安排字幕／报役行时序。

## 素材来源及授权

立绘、皮肤、UI、头像、贴图和动态骨骼来自 `dist/Majsoul_4.0.16_MC.apk`。原包约 1.8 GB；扫描了 4356 个 majset，并检查无路径 bundle 的 AudioClip，未找到雀士角色语音包。包内有角色头像表情，但没有完整聊天表情资源。

用户在本轮分别明确允许：

1. 从雀魂官方资源服务器补齐原版语音。
2. 从雀魂官方资源服务器补齐原版聊天表情。

已离线导入四名角色共 626 个 MP3、71 张聊天表情：未来 13、一姬 23、二阶堂美树 20、寺崎千穗理 15。来源、字节数和 SHA-256 分别保存在 `Characters/voice-provenance.json`、`emoji-provenance.json`。官方表情 PNG 使用简单的字节异或封装，导入器恢复 PNG 后校验完整签名和图像 CRC。

人物介绍、皮肤名称／介绍与性别、限定标记来自官方 `v0.11.252.w` 自描述配置快照。这个快照包含寺崎千穗理「共沐夜色」，原 APK 没有该素材，因此本轮没有用其他皮肤代替，也未另行下载这套立绘。

加密 Lua 与原 APK 编译元数据没有被冒称为恢复出的游戏源码。界面与局域网适配为本工程实现。Spine 官方 C# 4.2 源码和原许可证位于 `unity/Assets/ThirdParty/Spine42`，`SOURCE.md` 保留来源树标识。本轮为本地预览；正式分发前需遵守随附的运行时许可条款。

## 状态与网络约定

- 本地偏好键：`riichi.characters.v1`。包含宿舍选中角色、各角色服饰、出战选择、随机池、星标和顺序。
- 预览运行 `CharacterRoster.BeginPreview()`，使用内存偏好，结束后恢复正常读取，不把演示选择写入用户偏好。
- `create`、`join`、`character` 请求携带 `character`、`skin`、`randomCharacters`、`characterPool`。
- 服务端限定可用角色与服饰的对应关系；随机池最多 21 项，去重，空池不能开启随机。
- 开始整局时生成 `matchCharacter`、`matchSkin`。先随机选角色，再从该角色加入池的服饰中随机选一套；同一整局的新小局、轮询、重连及存档恢复均不重新抽取。
- 公共 `PlayerView` 只返回当前展示的 `character` 和 `skin`，不返回随机池。其他玩家的头像、演出和语音均依此渲染。
- 已开局时拒绝 `character` 操作；回到大厅更换本地偏好不会改变正在进行的整局角色。
- 维护的 `engine.js` 同步至 Unity Android 插件 assets。

## 入口与关键修正

`CharacterRoster.cs`：目录、偏好、宿舍／出战状态。`LobbyController.Characters.cs`：各角色界面。`CharacterIllustration.cs`：原始骨骼渲染。`CharacterVoices.cs`：按角色寻找原版音频。`RoomProtocol.cs`、`engine.js`：网络状态与开局快照。

动态图集在 APK 中缩为 2048×2048，但 atlas 保留 3937 或 2199 的逻辑坐标。运行时必须保留 atlas 的逻辑尺寸来归一化 UV，不能用实际贴图尺寸覆盖。Unity 纹理坐标使用 `Atlas.FlipV()`。离屏绘制采用材质局部投影并恢复当前 RenderTexture，不能改写当前 GUI 的全局视图／投影矩阵。

静态贴图按容器精确路径选择 Sprite，并保留其逻辑画布。九宫格选中框、边框按原 border 绘制；不能把小型九宫格纹理直接拉伸成整张边框。

## 复现

Python：`C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe`。图像依赖用工程 `tools/asset-inspector`，视频编解码用 `tools/video-inspector`。

1. `scripts/study-characters-1018.py`：读取 APK UI 层级、路径。
2. `scripts/character-config-1018.py`：读取下载到 `build/characters-1018` 的官方配置快照。
3. `scripts/import-characters-1018.py`：导入 APK 素材。
4. `scripts/import-character-voices-1018.py`、`scripts/import-character-emoji-1018.py`：补充经授权的官方资源。重新导入 APK 后，须再执行表情导入来恢复完整目录。
5. Unity `-batchmode -projectPath .../unity -executeMethod YunJian.Riichi.Editor.CharacterReview1018.Run -logFile ...`。加 `-characterQuick` 只生成效果图；完整录制后，加 `-characterSupplement` 录制表情、拖动排序、过滤和开局介绍。
6. `scripts/package-character-preview-1018.py`：组合真实 Unity 帧与四角色原版立直语音，生成 MP4 和预览页。

最终入口：`dist/previews/2026-10-18-character-system/index.html`。

## 已完成的检查与边界

内部验证包含 5 组角色公共状态检查（含 24 次随机选择、跨小局／重连／恢复固定、非法请求回滚）以及既有引擎与大厅 68 项检查。Unity 实际预览确认四角色基础画面、两套原始动态皮肤、四方向角色提示、换装、鉴赏、表情、排序和过滤；四角色各 10 个核心音频入口在运行时检查不为空。

统计快照共 122 名：女 86、男 36。对这个 APK 的默认立绘可确认 114 名：女 80、男 34；这 114 名中配置 `limited=1` 的 48 名。完整逐人表位于预览目录的 `character-statistics.csv`。不要把配置快照计数称为实时运营角色总数，也不要把限定标记直接解释为当前可获取性。

本轮没有 Android 安装、触摸、手机性能、实际多手机联网与声音验收。没有发布任何远端文件。

## 2026-10-19 最新界面复核

当前预览入口为 `dist/previews/2026-10-19-character-system-r2/index.html`，视频 `walkthrough-r2.mp4`，29.73 秒。最新修改和原版构图研究见 `docs/character-framing-review-1019.md`。宿舍等页面已拆到 `LobbyController.Dorm.cs`；服饰卡片、开场和提示器共用 `CharacterFraming.cs` 及原包/官方构图配置，替换之前的宽图转头像近似。不要把网页客户端默认 offset 直接套入 Unity 默认坐标。

Unity 实际渲染错误为 0；本地画廊 70 个引用均存在，视频完整解码 892 帧，21 套服饰卡片均留有实际截图，7 项界面交互检查通过。只修正本轮提出的构图问题，出战切换面板和牌桌其他布局继续留待后续；未构建 APK。
