# 称号簿滚动与绿色完成标记 R9

日期：2026-11-22，东京时间。在 R8 的未提交修改上继续；当前 HEAD 仍为 `38f0c350`。

## 两个问题

1. 称号簿是滚动列表。原实现已经有 `GUI.BeginScrollView` 和拖动逻辑，但未完整处理手势捕获及松手。现改用项目已有 `NativeScroll`：支持滚轮、上下拖动、拖出视口后的持续滚动、首尾边界限制；拖动松手先消费事件，防止同一次操作选择称号。保留原版布局和素材。
2. 上版资源准备误用了装扮界面的金色 `Majsoul/Wardrobe/UI/mark_selected`。现直接调用 `CharacterRoster.Texture("UI/skin_using")`，与 `LobbyController.Dorm.cs` 皮肤选择侧卡使用同一张带框绿色勾。PNG、GUID 和原始比例保持不变，没有新增称号或对勾图片。

修改文件：

- `unity/Assets/Scripts/LobbyController.PlayerInfo.cs`：滚动状态、输入与裁切；附仅 `UNITY_EDITOR` 生效的输入回放入口，Android 构建不包含这些测试入口。
- `unity/Assets/Scripts/ResourceRoomCoordinator.cs`：替换完成标记的资源引用。
- `tests/TitleScrollR9Review.cs`、`scripts/review-title-scroll-r9.ps1`：定向验证及截图入口。

## 验证

本轮 Unity 定向检查 19 项通过：

- 滚轮后偏移 420；上下拖动后偏移 220。
- 本地新档案的 121 条非默认已获称号可滚到最后一条，最大偏移 11332.199；可返回顶部。
- 手指／鼠标拖出列表区域后仍继续滚动；松手被消费，hot control 清零，当前称号未改变。
- 原有称号选择动作仍能换装（通过项目既有 `ValidationPress` 入口验证）。
- 绿色勾原图可加载；三个完成头像的截图区域分别检测到 440／440／439 个明显绿色像素。原图字节与 Git HEAD 一致。

输入验证在编辑器的实际列表输入边界回放事件，检查滚动状态、裁切后的实际画面与松手消费。它不等同于 Android 真机触摸验证。没有声称通过隐藏编辑器窗口模拟出的鼠标按键就是真机操作；调试过程的坐标／输入转发失败日志保留在本机工作目录，最终报告单独提供。

本轮没有重跑 Node 或 Python：没有改动引擎或资源输入解析；上轮 215／8 项结果见 R8 交接。未新建 APK、提交、推送或发布。暂存 Unity 工程的源码、设置和资源在录制后恢复，原 Saber 兼容覆盖保留。

## 预览与复跑

- [首尾滚动对照](../dist/previews/2026-11-22-title-scroll-r9/称号簿-首尾滚动验证.jpg)
- [绿色勾近景](../dist/previews/2026-11-22-title-scroll-r9/绿色勾-弹窗近景.png)
- [完整资源准备弹窗](../dist/previews/2026-11-22-title-scroll-r9/资源准备-复用皮肤页绿色勾.png)
- [报告](../dist/previews/2026-11-22-title-scroll-r9/report.json)、[图片来源与像素检查](../dist/previews/2026-11-22-title-scroll-r9/green-check-verification.json)

复跑：`powershell -File scripts/review-title-scroll-r9.ps1`。输出在 `build/title-scroll-r9/review/`。入口沿用 R8 的隔离工程与还原流程，测试录像／截图不得写入玩家真实记录。

下次打包必须同步全部 R8 变更和本轮这两份 runtime 脚本；现有 0.6.40 APK 与 R8 视频尚未包含 R9。
