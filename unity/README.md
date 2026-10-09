# 云间日麻 Unity 三维客户端

## 0.5.4 吃碰布局与原始动作

安装 `../dist/yunjian-riichi-unity-0.5.4.apk`，同包名覆盖更新，versionCode 5。

- 被取走的舍牌从牌河显示中移除，保留引擎历史记录；四方副露放到外侧角落，左右暗牌外移，拔北牌独立摆放。
- 使用原始操作按钮、黄色响应箭头、角色切入背景与操作喊字，多种吃牌组合展开牌面供选择。
- 原始手部、皮肤与 Dapai 曲线转换为 Unity 网格动画，四方出牌均播放；修正三角面朝向并使用不透明材质。
- 原始资源与本项目代码边界见 `../docs/麻将渲染对照.md`。Play 验证覆盖四方手部各 15 个时间采样、移除已被吃碰牌、选择组合、事件去重和重连；示例图片为合成公共布局。

构建脚本输出 `Builds/yunjian-riichi-unity-0.5.4.apk`。安卓真机触控、声音和热点联机待实测。

## 0.5.3 对手竖立手牌与计分盘

安装 `../dist/yunjian-riichi-unity-0.5.3.apk`，同包名覆盖更新，versionCode 4。

- 对手暗牌沿用原始麻将模型，站在短边上，红色牌背朝桌心；修正落地高度、碰撞盒和贴地阴影。暗杠、牌河和副露继续平放。
- 原始计分盘底图保持完整 UV；点数、方位、中央上下窗口与回合灯放在同一根节点，按座位统一旋转。点数提高到 19.2px 字高，中心位置为 (128,192)，完整放在黑色区域；回合灯改成黄色渐变梯形。
- Play 验证检查三麻/四麻每个观察座位的对手手牌数量、竖立尺寸、牌背方向、落地高度，以及负分/六位点数槽位和当前回合灯归属。四种横屏尺寸同时检查对手与自身手牌边界和选牌射线。

构建脚本输出 `Builds/yunjian-riichi-unity-0.5.3.apk`；旧版本说明及原始资源证据保留在下方。实现复用了原始模型与图集，布局及材质适配边界仍见 `../docs/麻将渲染对照.md`。

## 0.5.2 原始麻将显示方式

安装 `../dist/yunjian-riichi-unity-0.5.2.apk`，同包名覆盖更新 Unity 0.5.0 / 0.5.1，versionCode 为 3。

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
5. 英文路径下可点击 **云间日麻 → 配置安卓构建**，然后 **云间日麻 → 构建安卓 APK**。含中文的项目路径请使用下方构建脚本，它会将文件复制到英文临时构建目录，并将输出取回 `Builds/yunjian-riichi-unity-0.5.4.apk`。

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
