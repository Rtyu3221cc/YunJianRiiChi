# R8–R11 源码提交交接

时间：2026-11-24 00:50（东京时间）；执行：Codex。用户本次明确要求“将代码提交至GitHub”，授权对 `Rtyu3221cc/YunJianRiiChi` 当前 `main` 提交并推送。

## 本次范围

代码基线为 `38f0c3505984b14baee02621583fd5fc1bd12fc2`。包含此前尚未提交的 R8 七项优化、R9 称号簿滚动／绿色完成勾、R10 段位结算／大厅切换／11 月内容接入，以及 R11 六项视觉修正。相关构建／验证脚本、必要 Unity 资源与 .meta、原版配置快照、测试和交接一并提交。

四轮最终预览只选择 `dist/previews/2026-11-22-seven-fixes-r8/`、`2026-11-22-title-scroll-r9/`、`2026-11-23-rank-stick-r10/` 和 `2026-11-23-visual-feedback-r11/`。其他历史预览／调试输出保留在本机，没有批量加入仓库。

没有纳入旧 APK 的本机删除、其他历史 SHA 文件、SDK／tools／build 缓存、密钥、玩家存档、原版参考 APK、可选角色本机源素材缓存。可选角色与装扮继续由已经公开的资源 Release 下载，保留既有本机排除规则；本次未改公开附件、URL 或摘要。

本地 APK 0.6.42 / 90：370,603,388 字节，SHA-256 `8869c323d9bdb40b21027a61393e50574e2b1ba1767ab6b49f4a5ceebed4d229`。本次用户要求源码同步，**没有发布新的 APK Release 或上传 APK**。

## 验证

- 本次重新运行 `node --test --test-isolation=none tests/*.test.cjs`：217 通过、0 失败。
- 本次重新运行 `python -B -m unittest discover -s tests -p test_character_inputs.py`：14 通过。
- 昨日 R11 编辑器 266 项、真实包 1552 项和 APK 四类核验结果继续有效；本次没有改动实现或资源输入，没有重复构建 APK 或下载公开资源。
- 显式清单检查了文件大小、缓存／密钥路径及常见私钥／访问令牌内容；每个文件均小于 GitHub 单文件限制。新 Unity 资产和 .meta 同步保留。
- 详细功能与真机待测项见 [R11 交接](handoff-visual-feedback-r11-2026-11-23.md)；Android 真机仍未测。

## 提交结果

准备提交并推送，完成后记录代码提交 SHA 与远端核对结果。包含后续交接记录的最终 HEAD 以 `git log -1` 为准。
