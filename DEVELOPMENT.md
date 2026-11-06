# 当前开发进度

- 更新时间：2026-11-06 23:50（东京时间）
- 最近接手：Claude Code
- 共享 Git 根目录：E:\MyWorkSpace_E\YunJianRiiChi
- Unity 工程：unity/，Unity 6000.6.4f1
- 当前分支：本地 main 跟踪 origin/main，两者均为 [9987935e](https://github.com/Rtyu3221cc/YunJianRiiChi/commit/9987935e26c30284e14795d539cc9f1b1b48db67)。

## 已完成

- 共享根目录已对齐 GitHub 最新基线，保留原本地分支 codex/lobby-room-1013（bfed191）。
- 基线包含和牌横幅、神火坠霄、装扮与角色名称字体、Saber 动态立绘修复。版本和试装包详情见 [交接手册](docs/CODEX-HANDOFF.md)。
- 双方在同一目录轮流开发。AGENTS.md、CLAUDE.md 和本页是协作入口。.publish/YunJianRiiChi 留作历史发布检出。
- 2026-11-06 23:50 Claude Code 经用户授权，把 AGENTS.md、CLAUDE.md、DEVELOPMENT.md 提交到本地 main（仅本地提交，未推送）。

## 保留的本地状态

- scripts/prepare-original-input-stage.py 的 review-r25 支持保留为本地未提交改动。
- dist/yunjian-riichi-0.4.0.apk 和 dist/yunjian-riichi-0.5.0.apk 在对齐前已从本机移除，保留这两项本地删除。旧预览、本机素材和试装包留在共享目录。
- 从已核对的 Git 版本补齐或更新了 490 个文件。被替换的 477 份原文件、对齐前 Git 索引和状态、旧交接文档已校验备份到 D:\MyWorkSpace\yunjian-shared-alignment-9987935e。本轮未提交或推送新变更。

## 已知问题与风险

- unity/Assets/CharacterPacks/Source/ 里有 12 个角色已入库；约 58 个角色（4 万余个文件）未跟踪，也没被 .gitignore 排除，所以 git status 很长。是否要加忽略规则，待用户确认。
- 工程的 bundleVersion 仍是 0.6.22（versionCode 70）。0.6.23 试装 APK 含 Saber 临时覆盖，不能从当前源码直接重建。
- Android 真机测试未做。

## 验证

- 共享根目录的 HEAD 与 origin/main 提交差为 0 0。
- Node 自动测试 192 项通过。命令为 node --test --test-isolation=none tests/*.test.cjs。
- Python 测试 8 项通过。命令为 python -m unittest discover -s tests -p test_character_inputs.py。
- 本机 0.6.23 试装 APK 的 SHA-256 与对齐前一致。此前已验证签名和 16 KB 对齐。本轮未重新构建 APK，也未运行 Unity 编辑器或 Android 真机测试。

## 下一步

1. 下一位接手方打开共享 Git 根目录，按 AGENTS.md 和 CLAUDE.md 检查 Git 状态及相关差异，再继续用户指定的任务。
2. 后续每完成一个阶段更新本页。发布或切换分支前按共享规则确认范围。
