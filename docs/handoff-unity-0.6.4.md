# 云间日麻 Unity 0.6.4 交接（2026-10-19）

## 工程与发布

- 维护工程：`D:\MyWorkSpace\YunJianRiichi_Codex`；保留原工作分支和既有未提交状态。
- 发布检出：`.publish/YunJianRiiChi`，目标 `https://github.com/Rtyu3221cc/YunJianRiiChi` 的 `main`。
- 用户本轮授权测试、生成 APK、上传代码；沿用项目既有 Release 分发大 APK 的流程。
- 版本 `0.6.4`、versionCode `52`，包名 `com.yunjian.riichi.unity`，原签名证书，ARM64 + ARMv7。
- Unity `6000.6.4f1`；minSdk 26、targetSdk 32、compileSdk 36。
- APK：`dist/yunjian-riichi-unity-0.6.4.apk`，`193584457` 字节，SHA-256 `5312613fe82472e4ebdc9a032ae67c8fada9b62299efca66b92f004cc809226b`。
- [Release](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-0.6.4)；最终提交、上传大小及摘要记录于 `build/release-064/published.json`。

## 本版角色系统

四名雀士为未来、一姬、二阶堂美树、寺崎千穗理。共 21 套 APK 服饰、626 段官方原版语音、71 张聊天表情。好感五星，服饰和表情全解锁；语音灯笼暂保留外观，筛选隐藏。

宿舍支持切换、人物档案、头像换装、服饰介绍、缩放/拖动鉴赏、星标及长按排序。两套原始动态皮肤为一姬“校园微风”和寺崎千穗理“玩转夏日”，动态按钮实际切换骨骼/静态图。大厅可按皮肤保存位置与大小。随机出战于整场牌局开始前确定，小局和重连不重抽；公共状态传递角色和皮肤，其他玩家的立绘及语音与该选择一致。

最新视觉修正：返回标题沿用建立房间控件；角色页签位于装扮前方；档案灯笼自带光晕、左右花饰、移除契约提示牌；恢复姓名/CV 字体；有限服饰轮播、卡片构图、使用中印章、灰色按钮；原版鉴赏滑条；全宽排序弹窗；非牌桌樱花；大厅立绘大小、位置设置。

## 立绘构图

`CharacterFraming.cs` 读取统一配置。15 套皮肤使用原始 JSON 的 F0/I0/J0 场景参数，共 135 组构图；6 套旧皮肤使用 full 裁切框和 smallhead 中心。没有给四名角色写专用摆放特例。保留 Unity 正 Y 向上及原包父节点镜像；不要直接使用网页客户端的默认 offset。

细节及来源见 `character-framing-review-1019.md`。本版只迁移服饰卡片、开场、提示器；其他结算/和牌切入尚未逐一迁移。出战切换面板和牌桌其他已知布局问题仍留待后续，不宣称本版已解决。

## 代码入口

- `CharacterRoster.cs`：目录、宿舍/出战偏好与皮肤。
- `LobbyController.Dorm.cs`、`LobbyController.Characters.cs`：宿舍各页面与出战配置。
- `CharacterIllustration.cs`、`ThirdParty/Spine42`：原始 Spine 4.2 运行时与离屏绘制。
- `CharacterVoices.cs`：各角色原版语音，`LobbySakura.cs`：非牌桌樱花。
- `CharacterFraming.cs`、`MatchIntroduction.cs`、`NativeCallouts.cs`：原版配置构图。
- `RoomProtocol.cs`、`engine.js`、`RoomService.java`：角色公共状态及整场牌局快照。
- `ProjectSetup.cs` 明确打包三张角色 shader；Android 构建前重新编译手机房间服务并同步离线引擎。

动态图集实际 2048 像素，而 atlas 保留更大的逻辑画布，必须保持原始逻辑尺寸并使用 `Atlas.FlipV()`。运行时材质使用局部投影，避免修改 GUI 全局视图。素材及官方补充来源 JSON 保留于 `Resources/Majsoul/Characters`。

## 本轮验证

- Node 单元 129 项通过；角色公共状态集成 5 组通过，包含 24 次随机选择与跨小局/重连/恢复固定、非法数据回滚。
- Unity 单元 `524` 项，全部为本轮重新执行；角色资源及构图 375 项，既有二维码/倒计时 62 项、字图/UV 87 项。
- 实际 Unity 牌桌、大厅、房间运行回归，以及角色界面交互检查通过。
- 最终 APK 和发布 ZIP 中的 APK 大小/哈希一致；ZIP 完整性、v1/v2、API23 独立 v1、原证书、16 KB 对齐、双架构、626 段语音、离线角色素材、原始骨骼和角色 shader 检查通过。
- 源码白名单及凭据扫描通过；远端按提交、文件内容、上传大小和 GitHub SHA-256 核对，不重复下载 APK 或 ZIP。

本机记录：`build/release-064`；发布摘要：`dist/unity-0.6.4-verification.json`。预览：`dist/previews/2026-10-19-character-system-r2/index.html`。

Android 真机安装、触摸、音频、后台 Service、相机/相册扫码及多手机热点联机未在本轮实测。编辑器、自动检查和签名校验不能替代这些设备测试。

## 复现

```powershell
node --test --test-isolation=none tests/*.test.cjs
node scripts/verify-characters-1018.cjs
scripts/verify-unity.ps1 -Mode Validate -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe
scripts/run-unity-regression-0542.ps1 -StageNames table,lobby,room
scripts/verify-unity.ps1 -Mode Android -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe
python scripts/verify-unity-apk.py
```

发布阶段：`scripts/release-064.py auth|assemble|curate|commit|publish`。发布克隆保留历史文档及预览；不上传原始参考 APK、工具链、缓存、运行状态和私钥。
