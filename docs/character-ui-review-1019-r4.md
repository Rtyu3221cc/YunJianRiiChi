# 角色界面 R4（本地效果预览）

## 本轮修改

- 宿舍“角色”“装扮”标签分别上移 3 个界面设计像素，不调整其他文字。
- 服饰介绍接入手指拖动与滚轮，上下滚动；移除左右箭头及对应热区，保留卡片滑动。
- 切换雀士／设定对局随机雀士完全移出旧的通用面板实现，使用 `LobbyController.RoleSelection.cs`。
- 按 `UI_RoleSet` 和提供的视频恢复原版深蓝背景、金边、底部装饰、双页签、左侧当前头像和使用中牌、右侧宿舍列表、头像下方服饰弹层、星标过滤。
- 原图保留透明画布；边框／页签／底板按原 Sprite border 九宫格绘制；头像、印章与图标保持原比例，避免被强制铺满矩形拉伸。
- 随机页使用真正的指定／随机开关。指定时不显示可编辑的随机配置；随机时才可配置。允许先开启随机再从空列表添加候选，清空后不自动关闭随机模式。
- 左侧按雀士合并候选，右下角显示已选服饰数量；一名雀士可选多套服饰。可删除整个雀士或逐套切换，服饰弹层可横向滑动。

## 状态边界

`CharacterRoster.Random` 保存用户的模式开关；候选列表是独立设置，关闭随机不会删除它。候选为空时仍使用当前指定雀士，配置完成后随房间请求发送随机候选。`FillRequest` 对空候选发送指定选择，保持原服务器对非法空随机池的校验，不修改牌局引擎。随机结果仍在整场对局开始前固定。

## 素材与复现

`scripts/study-character-review-r4.py` 从给定视频抽取参考帧并整理原包 237 个布局节点；手机视频的旋转元数据在检查图中还原。

`scripts/import-character-roleset-r4.py` 导入 41 张原始精灵到 `Characters/UI/Role`，同时保留原始尺寸与 Sprite border。`Characters/roleset-source.json` 保存 bundle、pathID 与 SHA-256。

预览入口：`dist/previews/2026-10-19-character-system-r4/index.html`；录制入口 `YunJian.Riichi.Editor.CharacterReviewR4.Run`，`-r4Quick` 仅截效果图；`scripts/package-character-review-r4.py` 组合实际 Unity 帧。

本轮只生成本地效果图／视频，没有构建新 APK 或上传 Git。既有 R3 修正保留，牌桌规则不变。脚本驱动预览不能替代 Android 真机触摸验收。
