# 云间日麻 Unity 0.6.5 交接（2026-10-19）

维护工程 `D:\MyWorkSpace\YunJianRiichi_Codex`；发布检出 `.publish/YunJianRiiChi`，沿用当前对话已确认的仓库 `https://github.com/Rtyu3221cc/YunJianRiiChi` 和 `main`。保留维护工程原有分支及未提交状态。用户本轮要求修改、单元测试、生成 APK 和上传代码及 APK。

版本 `0.6.5`、versionCode `53`，原包名 `com.yunjian.riichi.unity`、原签名、ARM64+ARMv7，支持覆盖此前 Unity 版。APK 为 `dist/yunjian-riichi-unity-0.6.5.apk`，193831197 字节，SHA-256 `7024c31cc56a854b284bd2fd21872af29affbff77fe40d0ac5e106ddd2ce6d9c`。

## 自 0.6.4 以来的修改

- 拜访：档案与语音灯笼文字间距、整页手指拖动、右侧菱形滑块拖动；姓名/CV 使用既有原版字体。
- 宿舍：完整竖排姓名、角色页签前后层级与文字上移 3px；动态/静态立绘按同一原始画布对齐。
- 服饰：介绍可上下拖动，移除左右箭头，保留皮肤卡片左右滑动；紧凑展开框、三角和边框相接。
- 大厅相机：还原默认位置、保存每套皮肤的位置与大小；透明相机隐藏全部编辑按钮，再次点击恢复。
- 切换雀士：使用原 APK 的面板、边框和标记；保留原比例，九宫格物理像素边界对齐，消除拼接缝；顶部页签可见部分相接，“使用中”文字右移 2px。
- 随机配置：顶部“指定/随机”是功能开关，仅随机模式显示配置；关闭不删除候选，清空不关闭随机；左侧按雀士合并多套服饰，角标数量文字右移 2px、下移 1px。
- 最后修正随机开关：根据原图滑块的阴影留边设置位置，滑块可见部分与底图边缘贴合，避免把整张含阴影的贴图塞进轨道。

`CharacterRoster.Random` 独立于 `Pool`。候选为空时沿用指定雀士；整场牌局开始前选定随机角色，局内、小局与重连不重抽，公共状态同步给所有玩家。原有四雀士、21 套服饰、626 段语音和71张聊天表情保持可用。牌桌其他待改布局本次未扩展。

## 验证与边界

- Node 单元129项，角色公共状态集成5组。
- Unity 单元共 561 项：角色375、原版提示器87、既有二维码/倒计时62、新增随机配置/筛选/滑块边界37。全部在本轮重新执行。
- Unity 实际界面34项检查（含可见控件检查）：随机页全部/仅星标/无星标、关闭弹层、保留已选候选、过滤时服饰选择；1750×800 和1280×720截图。
- Unity 牌桌、大厅、房间运行回归。
- APK ZIP完整性、v1/v2、API23独立v1、原证书、16KB对齐、双架构、离线引擎、角色语音/贴图/原骨骼与shader验证。发布ZIP内APK另验大小、哈希与签名。
- 发布白名单与凭据扫描；GitHub核对提交、源码内容、上传大小和摘要，不回下载APK或ZIP。

没有进行手机真机安装、触摸、声音、后台服务和多手机热点联机验证。界面截图通过编辑器验证选择入口与共用滚动输入路由生成，不代替真机操作。

最终验证记录 `dist/unity-0.6.5-verification.json`；发布结果 `build/release-065/published.json`；效果图 `dist/previews/2026-10-19-character-system-r6/index.html`。历史 R3/R4/R5 图片及视频一并保留，不能作为新包设备验证的证据。

## 复现入口

`scripts/verify-unity.ps1 -Mode Validate` 执行本版单元检查；`scripts/run-unity-regression-0542.ps1 -StageNames table,lobby,room` 执行运行回归；`CharacterReviewR6.Run -r6Quick` 渲染界面；`scripts/verify-unity.ps1 -Mode Android` 编译并签名；`scripts/verify-unity-apk.py` 验证最终包。

发布阶段为 `scripts/release-065.py auth|preflight|assemble|curate|commit|publish`。仅复制白名单源码、资源、文档和预览。原始参考APK、工具链、缓存、日志、运行状态和私钥不进入Git；大APK以Release附件分发。
