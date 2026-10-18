# Unity 0.6.1 交接（2026-10-17）

用户已要求把第五轮预览和本轮两处修正打包为手机试用 APK。本轮允许修改源码、验证与本地打包；未要求 Git 提交、推送或发布。

## 版本与范围

- `0.6.1`，versionCode `49`，包名 `com.yunjian.riichi.unity`，沿用原证书。
- ARM64 + ARMv7，minSdk 26、targetSdk 32。
- 包含 `docs/preview-2026-10-17-r5.md` 的全部修正：十项符数图解按锚点直接缩放区域并预载，保留 200ms 回弹；规则图文／详细页签取消缩放脉冲；创建内容从下方 30 设计像素以 150ms 上移淡入；物理屏幕遮罩；侧栏与文字偏移；大厅和对局音乐默认全选／随机。
- “默认”只用于没有显式保存选项时；已有用户音乐列表和顺序选择保持。

## 本轮两处修正

1. `room_rule_bg.png` 为 76×76，第一、第二行 alpha=0，且上下几行带浅金色半透明边。原先铺满图片后仍会留下亮边。现在用该素材中部均匀的遮罩区域 `(7,9,14,204)` 绘制物理全屏，排除素材的透明／浅色边缘；不是继续扩张对话框。规则内容仍按安全区布局。
2. 声音首页曲目栏右侧的喇叭图标、“大厅音乐”和曲名保持对齐，在第五轮位置基础上整体上移 1px。仅改该组，左侧“曲目设置”和其他控件不动。偏移按 1688×780 参考尺寸换算。

## 构建与复核

- 当前构建入口 `scripts/verify-unity.ps1 -Mode Android -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe`。
- APK 目标 `unity/Builds/yunjian-riichi-unity-0.6.1.apk`，通过 `scripts/verify-unity-apk.py` 后复制到 `dist`。
- `Release061Review.Run` 渲染 1688×780 与 1280×720 两组带左右及上下安全区的真实 Unity 截图，记录在 `build/settings-061/render`。
- 渲染运行版本为 0.6.1，无 Unity GUI／运行错误。对两种尺寸的顶部、底部各 7 行进行像素复核，均与完整双层遮罩的预期合成一致（最大通道误差小于 1/255），不再出现素材透明边或浅色边。
- 本轮不改规则引擎、牌桌、二维码、结算声音或手机房主逻辑。保留既有 0.6.0 APK，不重复下载已发布安装包。

最终安装包已生成：`dist/yunjian-riichi-unity-0.6.1.apk`，109,699,970 字节，SHA-256 `E3E005048A85F35418D032BCA3D096C6E3C568821E3F76F9F32C2DEE279957BA`。aapt 确认版本 0.6.1／49、原包名及双架构。ZIP、当前离线资源、新 UI 的 IL2CPP 类、原证书、v1/v2、独立 v1 和 16 KB 对齐均通过。

另与本地 0.6.0 APK 比较：globalgamemanagers、global-metadata.dat 和两种架构的 libil2cpp.so 均已变化；包内包含第五轮新增的 ShowPointHelp／ScreenShade，排除只改版本名而打入旧代码的情况。记录在 `build/settings-061/apk-version-code-review.json`。

## 仍需真机确认

手机触摸、图解闪烁是否完全消失、实际音质和多手机局域网尚未验证。“立直音乐”仍仅保存数值，独立立直 BGM 播放链尚未加入。
