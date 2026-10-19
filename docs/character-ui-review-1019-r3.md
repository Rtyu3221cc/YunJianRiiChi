# 角色界面修正 R3（2026-10-19，本地预览）

## 本轮范围

- 拜访：档案／语音灯笼文字改为逐字定位，收紧上下间距；人物介绍和表情区支持拖动，右侧菱形滑块也可拖动。
- 宿舍：竖排姓名按独立字格布置，避免毛笔字体默认行高把五字姓名挤出名牌。
- 动态／静态：使用皮肤原始 pivot、spineWidth 及静态全身图比例确定共同画布。离屏渲染超过宽度上限时同时缩放宽高，避免宽图被单独限制宽度造成形变。
- 大厅相机：还原恢复该皮肤的默认大厅位置与大小；保存写入该皮肤的大厅位置和缩放，再次打开仍保留。原生按钮优先于滑条和画布拖动处理，解决底部按钮按下被拖动区域吞掉的问题。
- 缩放条右侧新增原版透明相机；点击隐藏所有编辑控件（含自身），再次点屏幕恢复。隐藏时保留立绘和庭院。

原版 `UI_PriScreen` 提取的 9 张相机／按钮／白色滑条资源位于 `Characters/UI/Photo`。按钮整图等比绘制；滑条按 Sprite border 拉伸，避免接缝和端帽被拉长。来源、bundle、pathID 与 SHA-256 见 `Characters/photo-source.json`。

## 输入与验证

`PhotoPointer` 统一处理按钮按下/抬起、滑条、拖动、滚轮和隐藏恢复；`ProfilePointer` 处理内容拖动、滑块和滚轮。运行时直接读取 IMGUI 鼠标/触摸模拟事件，消费被接管的事件，内容拖动超过阈值后取消子按钮点击；大厅另保留双指缩放。

`CharacterReviewR3` 的照片/拜访操作以屏幕像素坐标输入到同一套运行时路由和命中区域，核对滚动量、默认构图、持久化结果、控件隐藏和恢复。页面导航仍使用验证入口。它是脚本驱动的 Unity 交互录制，不是 Android 真机触摸录屏。不要将过去仅强制 `ValidationPress` 的结果当成实际鼠标事件验证。

当前实际渲染无 Unity 错误；两套动态皮肤均检查输出画布比例与原静态图一致，另保存动态/静态截图供视觉复核。动画本身仍会呼吸、眨眼和移动，因此两帧姿态可以不同。

## 入口

- 画廊：`dist/previews/2026-10-19-character-system-r3/index.html`
- 视频：`walkthrough-r3.mp4`
- 录制和检查：`build/character-review-r3/recording-final.log`、`render/movie.json`、`artifact-checks.json`
- 编辑器入口：`YunJian.Riichi.Editor.CharacterReviewR3.Run`；`-r3Quick` 只截效果图。
- 打包预览：`scripts/package-character-review-r3.py`

本轮没有新 APK 或 GitHub 发布，版本字段仍为已发布的 0.6.4。后续若发布本轮修复，应按更新规则递增版本号/versionCode。牌桌和出战切换面板未在此轮调整。
