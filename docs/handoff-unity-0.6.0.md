# Unity 0.6.0 交接（2026-10-17）

用户已要求生成手机试用 APK，并指定代码和 APK 的版本为 0.6.0。本轮不包含 Git 提交、推送和 GitHub 发布。先前预览限定已由这次打包请求解除。

## 当前版本与构建

- 版本 0.6.0，versionCode 48，包名 `com.yunjian.riichi.unity`。
- 构建入口 `scripts/verify-unity.ps1 -Mode Android -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe`。
- 本地输出 `unity/Builds/yunjian-riichi-unity-0.6.0.apk`，通过 `scripts/verify-unity-apk.py` 后复制到 `dist`。
- 沿用原证书、ARM64 + ARMv7、minSdk 26、targetSdk 32。签名脚本强制验证 v1/v2 和 16 KB 对齐。

## 本轮接入

- `NativeYakuUi.cs`：65 项原版可见役种、8 分类、9 张教程、3 张点数／符数表、10 张图解。
- 图解按原版锚点以 200 ms backOut 缩放弹出；已打开时点击表格关闭。
- 三个役种侧栏按钮整体上移 15 px；“役种一览”和“新手入门”内部文字均累计下移 17 px。偏移以 1688×780 参考画面换算，不改变全局字体或其他按钮。
- `NativeSettingsUi.cs`：声音／画面两页，原版行、静音图标、滑条、曲目弹窗、帧率下拉框。曲目设置这一行文字单独上移 3 px。
- 下拉框按原版面板 496 px、8 px 水平内距、31 px 顶部内距、83 px 单元和 60 px 选中条修正；九宫格共享物理像素边缘，避免拼接细线。
- `AppPreferences.cs`：五路音量／静音设置、30/60/120 帧、异形屏数值持久化。全域静音禁用下级滑条。
- `PlaylistAudio.cs`：大厅／对局独立多选列表和循环／随机模式、独立试听、双 AudioSource 2 秒等功率交叉淡化。原 LobbyController／GameAudio 的旧背景音乐不再与新播放器叠加。
- 异形屏 0–100 对应大厅左右边缘控件向内 0–60 设计像素；场景、人物和居中设置面板不跟随移动。继续尊重系统 Screen.safeArea。
- 原版音效、语音及结算声音接入总音量和对应音量。当前项目尚无独立的立直 BGM 播放链，“立直音乐”数值保存，待后续加入该播放链时接入，不得称该项已完成真机音效验证。
- 预览里额外画出的白圈／金圈已删除；原版樱花点击粒子保留。不要把安卓系统触摸指示还原成游戏效果。

## 资源与来源

新增运行资源在 `unity/Assets/Resources/Majsoul/Settings`。UI／教程／图解来自已提取 APK，曲目与名称使用前轮已取得的官方表和音频。`music.json` 映射名称和资源路径；`provenance.json` 保留来源。官方 Web 代码和 UI 定义只用于静态比对，没有执行或打包远端游戏代码，也没有解密 APK Lua。

## 验证

`Settings060Validation.Run` 已在 Unity 运行，检查资源加载、65 项数据、10 首曲目、1688×780 与 1280×720 截图、两侧控件实际位移、双音源交叉淡化中段／结束、总静音及运行版本。7 项运行检查通过，无 Unity GUI／运行错误。截图和记录在 `build/settings-060`。

浏览器修正预览检查通过：点击 120 帧选项后选择生效且关闭下拉框；设置模拟 cursor 后画面与无 cursor 时逐字节一致，证明附加触摸圆圈已移除；无 JS 错误。记录在 `build/settings-060/browser`。

此前构建启动时 Unity 包管理 IPC／许可服务失败，启动已安装的 Unity Hub 后恢复，不需要修改或绕过许可证。

最终 APK 已生成并通过验证：`dist/yunjian-riichi-unity-0.6.0.apk`，109,699,970 字节，SHA-256 `9847B59B49404D312A84683A7B619BC02BC12EF669D61C0BC71E68AF9A7E7E49`。aapt 确认版本 0.6.0／48、原包名、minSdk 26、targetSdk 32、ARM64 + ARMv7。ZIP／当前离线引擎／新设置资源与 IL2CPP 类名、原证书、v1/v2／独立 v1／16 KB 对齐全部通过。

真机安装、实际音质、触摸操作、相机／相册扫码和多手机局域网尚未实测；不得将桌面验证说成真机通过。不要重下历史发布 APK 来重复验证。工作区含多轮历史未提交修改，不得 reset 或覆盖。
