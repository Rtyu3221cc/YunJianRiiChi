# 玩家、房间与加载反馈 R12 交接

更新：2026-11-24 11:15 东京时间。工作分支 main；起点 `ad670ed024f7f1b6c54ccdde2f935d1bb6b375bc`。用户已明确要求本轮代码先不要上传 GitHub。本轮改动未提交／推送，没有公开 APK 或重新发布可选资源。

## 本轮结果

1. 尤贝尔按原版角色 `20000131` → 手部道具 `30990020` → `hand_youbeier` 绑定。加入四个原版 prefab、四方向各五种动作共 20 个烘焙动作，以及皮肤、四块衣料和独立影子材质的原始贴图／参数。皮肤使用 Custom2，衣料使用 CustomView 的法线、ORM、Ramp、RampMask、MatCap、RimMask 和视角 Ramp 运算；影子不作为手部实体显示。旧 21 套专属手型保留，新目录共 22 套。原表道具名称中的内部“已过期”字样没有取代角色表的实际绑定。
2. 对局 HUD 名称的纵向位置按实际头像框下缘计算，名称不再压在装饰框上；称号直接使用完整的原版显示区域，去除重复缩小。三人／四人所有座位已检查。准备页自己的称号同样取消额外缩小；所有默认称号分支统一至本轮导入的原版 CHS 素材，保留原 PNG 比例。
3. 四风连打等途中流局进入流局提示阶段，再进入点数结算。九种九牌、四家立直、四杠散了保持同一阶段流程和原有语音分派。自动播放保留至少 1.7 秒／语音时长的提示，手动确认沿用原操作。
4. 大厅立绘、骨骼、图集和图集页加入前台优先队列，避免排在宿舍头像缩略图后。立绘缓存按所属角色的资源版本失效，卸载无关角色不再重建当前骨骼。角色／皮肤选择变化时立即预热；新静态图就绪前保留已显示的立绘。
5. 准备页加入“调整装扮”，在房间内显示角色与装扮两页：角色三列列表、外观选择；装扮预设、分类、物品列表、使用和改名，包含滚动与选中状态。角色、皮肤和实际穿戴的装扮通过现有 character 操作发布；准备中的客人和资源准备阶段不能改选。预览入口只用于原版允许的静态装扮类别，预览不替换房间状态。房主标记按用户要求放到立绘右上角。

6. 字体补充：旧大厅书法子集缺少“伦／尤／泠”，旧角色姓名子集缺少“伦／尤”，导致个别字落到备用字体。两者均替换为当前原版 CHS 的 2011 字符字库；旧字库的 1896／2001 个已有字形轮廓、字宽和纵向指标完全一致，保留原 GUID，并将房间角色姓名切换到与宿舍相同的 CharacterNameText 入口；127 名角色的 263 个姓名字符全部覆盖，没有手绘或系统字体补字。

## 原版依据与实现入口

- 参考视频：`D:\neteazy\uuyc\GameViewer\Download\Record_2026-11-24-00-08-59_16d7aaa8b90eece8a9647d2d62a08f7c.mp4`，9.8895 秒，原文件 1080×2374；旋转后为 2374×1080。对照角色页、装扮页、关闭后回房间，以及约 20:9 布局。视频中房主标记在左；本次明确按用户要求改为右上。
- 原版 UI_ChangeDeco 的 RectTransform／CImage 绑定导出为 `RoomCustomization/layout.json` 和 63 张原版 PNG。界面采用 1780／1581 两种原版宽度、889.6 高度、锚点和原 Sprite 的九宫边距；按用户确认的全角色已解锁契约状态，统一使用原版契约底板；UR 角色使用原版 UR 契约边框，保持透明头像；滚动内容对同一纹理做 UV 裁剪，避免边框越界和拼接缝。新增贴图导入规则保留 PNG 原尺寸、Clamp、无 mipmap。
- 原版 UI_PageDeco 的 `SlotNeedPreview` 限定静态装扮预览。本项目原有随机装扮池协议没有在本轮新增；切换和发布使用现有的明确物品选择与预设功能。视频中的“指定／随机”池功能不应记作本轮已完成。
- 资源与源码依据：[参考视频记录](reference/room-feedback-r12/reference-video.json)、[界面原图与摘要](reference/room-feedback-r12/room-art-source.json)、[手型包摘要](reference/room-feedback-r12/hand-source-bundles.json)、[手型材质](reference/room-feedback-r12/hand-materials.json)、[动作时序](reference/room-feedback-r12/animation-timings.json)、[角色姓名字库来源与覆盖](reference/room-feedback-r12/role-font-source.json)。没有重下已发布角色／装扮包；手型取自已确认的官方原生资源。
- 主要运行代码：`LobbyController.RoomCustomization.cs`、`RoomCustomizationLayout.cs`、`LobbyController.cs`、`RiichiClient.cs`、`ResultPresentation.cs`、`CharacterPackService.cs`、`CharacterIllustration.cs`、`LobbyController.Preload.cs`、`WardrobePresets.cs`。
- 原手型导入：`scripts/import-character-hands.py` 与 `CharacterHandImport.cs`。后者支持 `YUNJIAN_HAND_SOURCE`、`YUNJIAN_HAND_OUTPUT`、`YUNJIAN_HAND_MERGE=1`，可增量合并新手型。转换后的本地来源在 `build/room-feedback-r12/hand-source/`，原始素材包没有纳入 Git。

## 验证与产物

- Node **217**、Python **14** 全部通过。
- Unity 实景交互 **200**：按钮、角色／皮肤／装扮发布参数、预设／改名、准备锁定、静态预览保留房间状态、三／四人头像文字布局、四方向五动作的数据和材质、实际四方向手型、四种流局显示与推进。
- 真实 Windows 资源包 **9**：复用缓存的四角色包；缩略图积压下选中立绘先加载；无同步开包；无关包卸载不重建当前 Spine。
- 宽屏 **13**：1600×728，匹配参考视频比例，包括两个实际 Font 对全部角色姓名的 HasCharacter 覆盖、“伦／尤／泠”字形、房间与宿舍使用同一角色字体，以及三／四人准备页、角色和装扮页边界检查；进入资源准备会关掉装扮窗口，且不发送过时的角色请求。
- 本机加载测量：静态约 **0.12 秒**、动态约 **0.43 秒**；请求记录中 full／atlas／skeleton／图集页均排在背景头像前。此为本机编辑器结果，手机上的 GPU 上传、磁盘和内存压力尚未测量。
- [四支专项预览与静帧](../dist/previews/2026-11-24-room-feedback-r12/README.md)，共 **1500 帧**，全帧解码后已复看。
- 本地 APK **0.6.43 / 91** 已生成：373,533,612 字节，SHA-256 `44bed8fdf71450a2dac9dfe92e5fd4bb1d21eb40e1e4bc3e048e25630161308f`。五项实际包内容检查及签名检查通过；Android 真机未测。

## 复现和继续工作

```powershell
npm test
python -B -m unittest discover -s tests -p 'test_*.py'
# 在已配置的隔离 phone-test-r24 工程录制：
# tests/RoomFeedbackR12Review.cs -> stage/unity/Assets/Editor/
# Unity -executeMethod YunJian.Riichi.Editor.RoomFeedbackR12Review.Run
# 真实包测试加 -r12Async；手机宽屏测试加 -r12Wide。
scripts/build-room-feedback-0643.ps1
```

- 编辑器测试环境变量：`YUNJIAN_R10_REPO` 为仓库，`YUNJIAN_R10_OUT` 为独立输出目录。正式本机验证结果在 `build/room-feedback-r12/contract-font-review/`、`async-review/`、`contract-font-wide-review/`；不要把早期失败录像当成交付结果。
- 构建沿用 `build/phone-test-r24/stage/unity`；保留其 Saber 本地覆盖，只合入相同 R12 队列与版本逻辑。当前覆盖摘要 `39a7b4139e563ace755035aeb459041d2abb7ea57725b9b1d84784a463cf46f6`，保护记录见 `build/room-feedback-r12/stage-overlay.json`。不应拿旧脚本的 R11 摘要直接覆盖当前服务。
- `build/release-0.6.43/` 保存构建和导入日志；`dist/unity-0.6.43-*-verification.json` 保存五项实际包核验。编辑器启动时曾出现 UnityEditor.Search 的索引异常；堆栈属于编辑器搜索索引，运行交互与录像检查均完成，未将其当作手机验证。
- 下一步优先覆盖安装到手机：尤贝尔出牌／立直／副露／自摸、三四人头像框、特殊流局提示、房间改角色／皮肤／装扮并由另一台设备观察同步、下载提示和准备锁定、弱网下的发布响应、动态皮肤反复切换及后台返回。Android 真机和真实双机联机本轮未测。
- 如要把房间页进一步扩展到视频中的随机装扮池，需要单独实现可保存的池、开局前抽取／冻结和双方资源准备契约；当前只承诺上面的明确选择与预设切换。
- 保留仓库原有旧 APK 删除状态、历史产物与本机缓存；没有暂存或清理用户其他文件。
