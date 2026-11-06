# 云间日麻 · 共享开发规则

本文件是 Codex 与 Claude Code 唯一的共享规则文件。两方在同一个工作区 `E:\MyWorkSpace_E\YunJianRiiChi` 轮流开发，**同一时间只由当前接手的一方修改**，额度用完时由用户手动切换。当前进度写在 [DEVELOPMENT.md](DEVELOPMENT.md)。

## 项目结构

- Git 根目录即本目录。GitHub 为 [Rtyu3221cc/YunJianRiiChi](https://github.com/Rtyu3221cc/YunJianRiiChi)，默认分支 `main`。
- Unity 工程根目录是 `unity/`（含 `Assets/`、`Packages/`、`ProjectSettings/`），Unity 版本 `6000.6.4f1`，编辑器 `D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe`。Android 构建用同一安装中的 Build Support、SDK、NDK 与 OpenJDK。
- 规则与 AI 逻辑在 Unity 之外，因此本规则放在 Git 根目录，而非 `unity/`。

| 位置 | 用途 |
| --- | --- |
| `app/src/main/assets/engine.js` | 本地房间、规则与 AI 的主要逻辑 |
| `unity/Assets/Plugins/Android/riichi-host.androidlib/assets/` | 安卓宿主携带的引擎资产，构建时与主引擎同步 |
| `unity/Assets/Scripts/WinCutIn.cs`、`TableEffects.cs`、`NativeWinPlayback.cs` | 和牌横幅、出场顺序和原版牌面特效 |
| `unity/Assets/Scripts/LobbyController.*.cs` | 大厅、宿舍、角色与装扮界面 |
| `unity/Assets/Resources/Majsoul/CharacterDirectory/`、`WardrobeDirectory/` | 角色及装扮下载目录、固定 URL 和摘要 |
| `docs/character-resource-maintenance.md` | 资源来源、重建、校验及发布步骤 |
| `docs/CODEX-HANDOFF.md` | 2026-11-06 的基线与版本详情 |
| `tests/` | Node、Python 等自动测试 |

## 每次接手

1. 重新读取磁盘上的 `AGENTS.md`、`CLAUDE.md`、`DEVELOPMENT.md`，以及待改目录里的说明文件。即使沿用旧会话也要重读。
2. 修改前执行只读检查：

   ```powershell
   git rev-parse --show-toplevel
   git status --short --branch
   git --no-pager diff
   git --no-pager diff --cached
   git --no-pager log -5 --oneline --decorate
   ```

3. 对照 `DEVELOPMENT.md` 与实际代码确认做到哪一步。若上一方额度耗尽没来得及更新进度，用差异、近期提交和代码还原现场。先简述接手点，再继续已明确的下一步。有无法判断的冲突时保留现场，写明需要用户确认的具体文件或决定。

## Unity 与 Git 约定

- 保留资产和目录对应的 `.meta` 文件及现有 GUID。移动或重命名资产优先在 Unity 编辑器里做；从文件系统操作时同时移动 `.meta`。新资产的 `.meta` 与资产一起纳入版本控制。
- `unity/.gitignore` 已排除 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/`、`Builds/`、`.utmp/` 及 IDE 工程文件；根 `.gitignore` 排除 `build/`、`tools/`、`.publish/`、大部分 `dist/` 与签名文件。发现已被跟踪的缓存时记录路径，保留现场。
- 本仓库设置 `core.autocrlf=false`，资源校验依赖原始字节。不要改这个设置，也不要批量转换换行。
- 保留用户和上一方留下的暂存、未暂存及未跟踪改动，只改本次任务需要的文件。禁止用 `git reset --hard`、`git clean -fdx`、批量还原或删除来清理工作区。
- 沿用当前分支。**提交、推送、切换分支、改写历史，都要用户明确指示后才执行。**

## 需要用户单独授权的操作

用户说「开始做」只代表可以开发。以下操作每次都要用户明确同意，并写进 `DEVELOPMENT.md`：

- 推送或合并到 `main`；强制推送；删除分支或标签
- 角色包、装扮包等资源发布，修改公开资源目录里的 URL 或校验值
- APK 发布、GitHub Release
- 运行 `scripts/clear-project-generated-cache.ps1`（内含迁移前 D 盘的固定删除路径，迁移后不要直接运行）

## 验证

```powershell
node --test --test-isolation=none tests/*.test.cjs
python -m unittest discover -s tests -p test_character_inputs.py
```

- Node 22 不认 `--test-isolation=none`，此时去掉该参数。
- 只写实际运行过的命令和结果，未运行的写明「未运行」及原因。
- 区分三层：自动测试通过 / 编辑器或截图预览 / Android 真机验证。前一层通过不能写成后一层通过。
- 迁移前生成的日志、报告和预览可能记录 D 盘旧路径。启动 Unity 时传入本工作区路径。

## 更新进度并交回

- 每完成一个可核验的小阶段就更新 `DEVELOPMENT.md`。遇到中断、阻塞或准备切换时，写下当前修改和第一条可执行的下一步。
- 交回前再看一次 `git status` 和相关差异。
- `DEVELOPMENT.md` 只保留当前状态和近期要点，详细历史交给 Git 与 `docs/` 下的版本说明。
- 时间一律用东京时间 `YYYY-MM-DD HH:MM`。仓库内文件写相对路径。
- 签名密钥、凭据、玩家存档不写进任何文档。
