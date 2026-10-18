# 云间日麻 Unity 0.6.3 交接（2026-10-18）

## 从这里继续

- 维护工程：`D:\MyWorkSpace\YunJianRiichi_Codex`。
- 发布仓库：`https://github.com/Rtyu3221cc/YunJianRiiChi`，发布分支 `main`，本版源码标签 `unity-0.6.3`。
- 发布检出：工程下 `.publish/YunJianRiiChi`。日常工程仍保留原工作分支和既有未提交状态；不要直接 reset、clean 或覆盖这些工作文件。发布使用完整白名单同步、凭据扫描和独立检出提交。
- 本次用户明确授权：运行单元测试、生成 APK、将代码和安装包提交到 GitHub，并生成本交接文档。
- 本版实际发布状态与提交 SHA 在本机 `build/release-063/published.json`、`source-push.json`；远端以 `unity-0.6.3` 标签和 Release 定位。最终聊天回复也提供提交链接。

## 版本与安装包

- Unity `6000.6.4f1`，版本 `0.6.3`，versionCode `51`。
- 包名 `com.yunjian.riichi.unity`；ARM64 + ARMv7，minSdk 26、targetSdk 32、compileSdk 36。
- APK：`dist/yunjian-riichi-unity-0.6.3.apk`。
- 大小：`109980763` 字节。
- SHA-256：`008aa1e711eed9e19a32c16265c2c35f5fb49f2e945808e4cbf271c7bc4ead78`。
- 证书 SHA-256：`d1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f`，保留原证书以支持覆盖升级。
- 下载：`https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.6.3/yunjian-riichi-unity-0.6.3.apk`。
- APK 超过 GitHub 单个 Git 文件大小上限，按项目现行方式放在 GitHub Release；源码、资源、测试、校验值和文档提交至 Git。原始雀魂 APK、工具链、缓存、运行状态及密钥不发布。

## 本对话完成的三类修正

1. **掉线标记**：`NativeTakeoverBadge.cs` 将闪电单独上移5个牌桌设计像素，保留头像、遮罩、图标大小与透明度。设计画布宽1280，实际像素随视口等比缩放。
2. **牌面居中**：`OriginalTiles.Face` 使用原始512×512图集；52×72单元中的牌面从(3,3)开始采样50×70。旧裁图从(1,1)采样，导致花色普遍偏右偏下。手牌使用的另一套图集保留。
3. **四方向动作提示**：`NativeCallouts.cs`、`NativeCallout.shader`、`Callouts1018` 资源重建原版四套斜边遮罩、镜像、角色与文字位置；立直／双立直、吃、碰、杠、拔北均使用原始字图，原荣和／自摸路径保留。

参考视频：`D:\MyWorkSpace\video\1018\1`，分别包含自家、东、北、西方向。

## 用户连续纠正后必须保留的最终规则

- 字图不能把紧边裁切图铺满静态节点。五张完整 Sprite 原本都是187×143，但透明留白不同；紧边导出曾让单字变宽、双字被挤压。
- 四个方向的文字不能随角色横幅的不同倍率变化。文字独立绘制，同一种提示四方向大小一致。
- **统一的是每个字的视觉大小，不是整句总宽度。**“立直、拔北”要自然展开为两个字的宽度。
- 当前按 alpha≥8 的字形边界，将可见高度统一为80设计像素；整张原图等比缩放并将可见字形中心对准原方位锚点。双字约为“吃”的1.9倍宽，未横向拉伸。
- 完整原图、透明留白和原始比例必须保留。`NativeCalloutAssetImport` 禁止NPOT重采样，并在通用导入器之后执行，确保资源实际导入为187×143。
- `NativeCallouts.WordRect` 是运行时绘制共用的尺寸计算入口；新增单元检查直接调用该入口，并独立从源PNG透明通道计算预期边界。

最新实际 Unity 预览：`dist/previews/2026-10-18-table-review-character-size/index.html`。其中五种提示四方向的视频、同倍率截图和实际GUI尺寸记录均已保留。旧 `table-review`、`text-fix`、`uniform-words` 预览各有已修正的问题，不再作为最终效果。

## APK 取证与代码边界

`scripts/import-callouts-1018.py` 提取28个节点、5张背景／遮罩及5张完整字图；来源、哈希、原Sprite坐标与可见边界在 `Callouts1018/source.json`、`word-sprites.json`。

APK元数据确认 `capsui.CImage.FixImage`，四方向文字配置 `FixMode=1`，枚举含义为 `FORCE_KEEP_WIDTH`。Lua层有 `ui_desktopinfo.lua.bytes` 与builder字节码。没有将这些编译元数据或缓存网页JavaScript冒称为APK的完整C#源码。

## 相比上次远端版本的累计内容

远端此前停在0.5.46。本次源码还包含此前本地0.6.0–0.6.2尚未推送的更新：原版役种一览、教程、点数与符数图解，声音／画面设置，多选曲目、随机／顺序播放、试听及2秒交叉淡化，异形屏布局修正，教程翻页指示器，以及每局每玩家首次立直弃牌的原版火焰粒子和短音效。运行时资源离线打包。细节可继续参阅 `handoff-unity-0.6.0.md`、`0.6.1.md` 和 `0.6.2.md`。

## 本次测试和验证

- Node 单元测试129项通过，0失败。
- Unity 单元检查共 `149` 项：既有二维码／倒计时／资源检查，加上本轮字形、双字宽度、牌面UV及发布配置检查。
- 实际Unity运行回归覆盖牌桌、大厅、房间三个阶段：真实客户端驱动独立本地演示服务；覆盖三／四人、各方向、不同分辨率、演出、状态轮询与回座。它们不是Android真机实测。
- 旧BGM回归检查仍指向停用的单曲播放器，已更新为当前 `PlaylistAudio` 实际播放通道，保留轮询不重播、静音、SFX静音与后台暂停／恢复断言；测试使用内存音量值并恢复，不改写用户偏好。
- 房间回归原来寻找已被原生役种面板替代的 `yaku-confirm` 按钮，现检查实际原生面板打开、截图，以及当前Back关闭转场后仍停留在原准备房间。
- 最终APK验证ZIP、离线资源、双架构、JNI服务、新提示器类及编译shader；v1/v2、API23独立v1、原证书和16KB对齐通过。
- 发布ZIP中的APK再次检查大小、哈希及签名。远端上传只核对GitHub返回的字节数和SHA-256；不重复下载APK、ZIP或源码包。
- 本机记录：`build/release-063`；可发布摘要：`dist/unity-0.6.3-verification.json`、`dist/验证记录-unity-0.6.3.md`。

## 复现入口

```powershell
node --test --test-isolation=none tests/*.test.cjs
scripts/verify-unity.ps1 -Mode Validate -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe
scripts/run-unity-regression-0542.ps1 -StageNames table,lobby,room
scripts/verify-unity.ps1 -Mode Android -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe
python scripts/verify-unity-apk.py
```

`run-unity-regression-0542.ps1` 是沿用的通用回归入口，文件名中的版本不代表本次运行了旧源码。它使用8765端口；若已有监听会退出，不会停止其他牌局服务。

发布分阶段入口：`scripts/release-063.py auth|assemble|curate|commit|publish`。需先完成测试、构建、验证和交接文档；本脚本沿用本机已有Git凭据且不输出密钥。不要仅因预算或时间不足跳过包校验，也不要把历史测试记录算成本轮通过。

## 下一轮

用户尚未确认Android真机安装、触摸、声音、相机／相册扫码、后台Service和多手机热点行为。不要把编辑器检查或APK签名检查称为这些真机验证已通过。后续新任务以本版源码和最新预览为准；需要发布时仍使用单独发布检出，保留维护工程的工作状态。
