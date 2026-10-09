# 云间日麻 Unity 三维客户端

## 0.5.7 计分盘出牌指示灯

最新安卓包：[下载 Unity 0.5.7 APK](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.7/yunjian-riichi-unity-0.5.7.apk)，versionCode 8，与原 Unity 包签名一致，可覆盖更新。

- 回合灯使用雀魂原始 `particle_shining` 短梯形纹理，保留高亮边缘和柔光；移除旧的代码生成常亮宽灯条。
- 按原两层粒子颜色、加色混合及 1 秒透明度曲线淡入、停留、淡出。原纹理和粒子配置适配现有 Unity 计分盘；坐标和尺寸换算见素材来源记录。
- 相同座位和局数的数据刷新保持闪烁相位，换回合重新开始，避免轮询导致动画反复重置。
- 验证覆盖四方向实际渲染、暗/亮/淡出截图及刷新连续性；原有三/四人布局、选牌和手部动作检查继续通过。安卓真机效果待实测。

预览见 [亮阶段截图](../dist/previews/unity-0.5.7-turn-light-0-0.45.png)，完整检查见 [0.5.7 验证记录](../dist/验证记录-unity-0.5.7.txt)，素材来源见 [ARTWORK.md](../ARTWORK.md)。

构建脚本当前输出 `Builds/yunjian-riichi-unity-0.5.7.apk`。

## 0.5.6 手部动画补全与操作修正

安装 [yunjian-riichi-unity-0.5.6.apk](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.6/yunjian-riichi-unity-0.5.6.apk)，同包名覆盖更新，versionCode 7。

- 立直宣言牌播放原版 `Anim_Dapai_heng-LiQiPai` 横置动作（1.700 秒）；立直喊字与出牌同时开始，宣言成立后接 `Lizhi_Ani-LiZhi`（0.800 秒）把立直棒放进计分盘槽位。
- 吃、碰、杠和拔北播放原版 `Fulu_Ani-Fulu`（0.900 秒）；落牌时在副露锚点叠加 `effect_chipeng` 的 `bang` 加色闪光。
- `TableEffects` 拆成手部与喊字两条独立通道，出牌动作不再把立直喊字推迟 1.7 秒。
- 立直模式下不可打出的手牌置灰并禁用拾取，只在轮到自己选牌时生效；着色器 `_Dim` 只做去饱和压暗，不改变透明度。
- 回合灯与立直棒对齐 `score-hub.png` 原有凹槽；立直棒另需抬高到盘面之上，否则会被盘面遮挡。
- 新导入脚本 `../scripts/import-hand-animations.py`（四个手部动画）与 `../scripts/import-meld-effect.py`（撞击贴图）。

0.5.6 构建脚本输出 `Builds/yunjian-riichi-unity-0.5.6.apk`。安卓真机触控、声音和热点联机待实测。

## 0.5.5 描边、响应光圈与手牌避让

安装 [yunjian-riichi-unity-0.5.5.apk](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.5/yunjian-riichi-unity-0.5.5.apk)，versionCode 6。

复用原版 224 顶点描边网格、描边颜色和 `effect_zhishi` 对应的 `majiangkuang` 光圈纹理；光圈位于牌底的桌面空间。自己手牌根据副露实际屏幕投影向左避让，拔北牌朝桌心并沿侧边排列。资源来源及本项目着色器适配见 `../ARTWORK.md`。最新构建脚本输出 `Builds/yunjian-riichi-unity-0.5.5.apk`。

## 0.5.4 吃碰布局与原始动作

安装 [yunjian-riichi-unity-0.5.4.apk](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.4/yunjian-riichi-unity-0.5.4.apk)，同包名覆盖更新，versionCode 5。

- 被取走的舍牌从牌河显示中移除，保留引擎历史记录；四方副露放到外侧角落，左右暗牌外移，拔北牌独立摆放。
- 使用原始操作按钮、黄色响应箭头、角色切入背景与操作喊字，多种吃牌组合展开牌面供选择。
- 原始手部、皮肤与 Dapai 曲线转换为 Unity 网格动画，四方出牌均播放；修正三角面朝向并使用不透明材质。
- 原始资源与本项目代码边界见 `../docs/麻将渲染对照.md`。Play 验证覆盖四方手部各 15 个时间采样、移除已被吃碰牌、选择组合、事件去重和重连；示例图片为合成公共布局。

构建脚本输出 `Builds/yunjian-riichi-unity-0.5.4.apk`。安卓真机触控、声音和热点联机待实测。

## 0.5.3 对手竖立手牌与计分盘

安装 [yunjian-riichi-unity-0.5.3.apk](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.3/yunjian-riichi-unity-0.5.3.apk)，同包名覆盖更新，versionCode 4。

- 对手暗牌沿用原始麻将模型，站在短边上，红色牌背朝桌心；修正落地高度、碰撞盒和贴地阴影。暗杠、牌河和副露继续平放。
- 原始计分盘底图保持完整 UV；点数、方位、中央上下窗口与回合灯放在同一根节点，按座位统一旋转。点数提高到 19.2px 字高，中心位置为 (128,192)，完整放在黑色区域；回合灯改成黄色渐变梯形。
- Play 验证检查三麻/四麻每个观察座位的对手手牌数量、竖立尺寸、牌背方向、落地高度，以及负分/六位点数槽位和当前回合灯归属。四种横屏尺寸同时检查对手与自身手牌边界和选牌射线。

构建脚本输出 `Builds/yunjian-riichi-unity-0.5.3.apk`；旧版本说明及原始资源证据保留在下方。实现复用了原始模型与图集，布局及材质适配边界仍见 `../docs/麻将渲染对照.md`。

## 0.5.2 原始麻将显示方式

安装 [yunjian-riichi-unity-0.5.2.apk](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.2/yunjian-riichi-unity-0.5.2.apk)，同包名覆盖更新 Unity 0.5.0 / 0.5.1，versionCode 为 3。

- 桌面麻将使用原 APK 的 `mjp_new` 模型、法线、UV 和两个子网格，宽高比约 0.753；只做坐标轴变换和整体等比缩放。
- 手牌使用独立正交相机（原预制体 orthographicSize=9）、原始 800×516 手图集与 160×129 红色牌背层。原始平面 2.54×2.54，横向缩放 0.62，单牌单元 80×129；不再将桌面图贴到自制手牌模型上。
- 主相机沿用原预制体 28° 视角、四元数朝向和位置比例；手机宽高比变化时调整视野和安全区，不改变模型或牌面比例。
- 使用参考图西瓜桌布、宝牌面板、工具按钮和头像框；头像与自身手牌保留间距。
- 新资源导入脚本：`../scripts/import-original-tiles.py`；完整证据与边界：`../docs/麻将渲染对照.md`。

Unity Play 验证覆盖三麻/四麻及 1280×720、1688×780、2760×1256、844×390；每张可交互手牌都检查边界、射线命中及实际投影宽高比。真实 AI 房间验证两次点击出牌与 8 段角色音频。预览来自规则引擎生成的合法公共视图，压力图另行标记。真机触控与热点联机待实测。

工程目录为本文件所在的 `unity` 文件夹。新增原生三维牌桌，沿用现有规则、AI、存档和局域网房间接口。Android 手机仍能独立担任房主，游玩不依赖电脑或互联网。

已通过 Unity 6000.6.4f1 的 C# 编译、实际 Play 模式和布局验证。Android 构建与真机状态见交付验证记录。

Unity Android APK 交付副本在上一级 `dist`。构建为 IL2CPP ARM64 + ARMv7；签名、对齐和离线资源检查结果见对应版本验证记录。当前未连接 Android 测试设备，尚未完成真机运行和热点联机验证。APK 使用本机开发签名。

## 安装后打开

1. Unity Hub 安装 Unity 6 编辑器，并勾选 **Android Build Support、Android SDK & NDK Tools、OpenJDK**。
2. Hub 中“添加现有项目”，选择这个 `unity` 文件夹。工程已升级并实测 **6000.6.4f1**；使用其他版本需要重新导入和验证。
3. 首次导入后会自动创建 `Assets/Scenes/Table.unity`。若未出现，点击菜单 **云间日麻 → 创建或打开牌桌场景**。
4. 点击 **云间日麻 → 验证资源与房间协议**。通过后 Console 应显示 `RIICHI UNITY VALIDATION PASS`。这一步同时检验 C# 编译和导入资源，不代替运行时验证。
5. 英文路径下可点击 **云间日麻 → 配置安卓构建**，然后 **云间日麻 → 构建安卓 APK**。含中文的项目路径请使用下方构建脚本，它会将文件复制到英文临时构建目录，并将输出取回 `Builds/yunjian-riichi-unity-0.5.5.apk`。

Unity APK 使用独立包名 `com.yunjian.riichi.unity`，可和原有 Android/WebView 版并存，各自保存自己的牌桌。测试 Unity 手机房主前先退出同机旧版房主，避免同时占用 8765 端口。开发版 target API 32 沿用旧版离线分发约定，未进行应用商店发布适配。

## 编辑器内实际试玩

编辑器/Windows 端通过现有 Node 测试房主运行规则，Android 构建通过内置插件运行规则。

在上一级 `lan-riichi` 目录执行：

```powershell
node desktop.cjs
```

复制输出 `#host=` 后的本地测试房主密钥。Unity 点击 Play，在大厅填：

- 房主地址：`http://127.0.0.1:8765`
- 测试房主密钥：刚复制的值
- 昵称、三麻/四麻、东风/半庄及赤宝牌选项

点击“单人练习”或“创建房间”。Android 真机不显示测试密钥输入框，由本机插件自动读取房主密钥并启动服务。

## 已写入的功能

- 三维牌桌、透视摄像机、麻将实体厚度、四方暗牌与牌河、当前回合提示。
- 单次点击选中，再次点击同一实体牌出牌；切换牌只更改选中，点击时间没有上限。
- 立直横置牌按实际宽度排列，供托交付后在座位前显示立直棒。
- 吃碰明杠、暗杠、加杠和拔北的显示；副露牌组在手牌外侧角落，加杠牌使用独立高度。
- 单人 AI 练习、手机开房与加入、恢复座位、AI 添加/移除、结算确认、下一局。
- 同一角色一姬的吃/碰/杠/立直/两立直/自摸/荣和/拔北音频，独立音量与静音、前后台与重连去重。
- 自定义头像由房间公共视图读取并显示；Unity 本轮尚未实现相册头像编辑器。原有 Android/WebView 版头像编辑器保留。

普通出牌没有逐张牌名播报。一姬语音清单仅绑定原游戏实际存在的操作配音，不混入旧版合成语音。

## 代码与资源

- `Assets/Scripts/RiichiClient.cs`：房间请求、大厅、操作、结算和两次点击选择。
- `Assets/Scripts/TableRenderer.cs`：真正的三维麻将与桌面布局。
- `Assets/Scripts/RoomProtocol.cs`：只接收服务端 `Room.view()` 返回的脱敏玩家视图。
- `Assets/Scripts/RoleVoice.cs`：同一角色的离线音频与事件去重。
- `Assets/Scripts/TableEffects.cs`：原始手部动画、角色操作切入与公共事件去重。
- `Android/UnityHostBridge.java`：启动已有手机房主服务；编译插件位于 `Assets/Plugins/Android/riichi-host.androidlib`。
- `Assets/Resources/Majsoul`：37 张手牌、37 张平放牌面、头像、中文字体、默认桌布、计分盘及角色音频。

规则、AI、HTTP 服务、浏览器资源变动后，在上一级目录执行：

```powershell
pwsh -NoProfile -File scripts/sync-unity.ps1
```

同步后会重新编译 Java 插件并复制资源。不要直接修改插件内的 `core.js` / `engine.js` 等生成副本。

命令行验证（需已安装并激活 Unity Editor）：

```powershell
pwsh -NoProfile -File scripts/verify-unity.ps1 -EditorPath '你安装的Unity.exe绝对路径'
```

该脚本检查 Unity 实际退出码和验证成功标记，日志写入 `build/unity-validate.log`。它能自动发现 Unity Hub 的默认与自定义安装目录。

Android 构建（IL2CPP，ARM64 + ARMv7）：

```powershell
pwsh -NoProfile -File scripts/verify-unity.ps1 -Mode Android
```

日志为 `build/unity-android.log`。临时构建目录位于系统 TEMP，原工程仍在此处。Android 房主库使用安装模块中提供的 API 36 编译平台。

重跑 Play 模式验证时，先在独立终端启动专用测试房主（确保 8765 未被占用）：

```powershell
$env:RIICHI_HOST_SECRET='unity-local-validation'
$env:RIICHI_STATE_FILE='build/unity-smoke-state.json'
node desktop.cjs
```

另开终端执行 `node scripts/unity-fixtures.cjs` 和 `pwsh -NoProfile -File scripts/verify-unity.ps1 -Mode Play`。截图和通过标记保存在 `build/unity-smoke`。`RuntimeValidation.cs` 仅在编辑器内编译，不会进入 Android 玩家程序；测试不会写入玩家的连接凭据。

Android 库接入按 [Unity 官方 Android Library 插件文档](https://docs.unity3d.com/cn/6000.0/Manual/android-library-project-import.html) 实现；局域网请求使用 [UnityWebRequest](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/networking/unitywebrequest)。素材来源见上一级 `ARTWORK.md`，不属于规则引擎的 MIT 授权。

## 仍需实测

Android 真机安装与触摸选牌、扬声器音频播放、Android Service 启动、真实热点双手机互通、断线恢复及后台行为。当前已接入四向出牌手部及操作切入；摸牌、角色专属皮肤和其他完整动画系统仍未接入。
