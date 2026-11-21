# 云间日麻 Codex 协同开发交接

当前接手入口是根目录的 [DEVELOPMENT.md](../DEVELOPMENT.md)。2026-11-14 的追加役满修正及完整飞牌对照见 [最新详细交接](役满牌面与飞牌对照-2026-11-14.md)。本页以下内容保留 2026-11-06 的历史基线，其中版本、提交及测试包不代表当前状态。

更新于 2026-11-06（东京时间）。Codex 和 Claude Code 每次接手先读根目录的 DEVELOPMENT.md，再查阅本页的基线和参考信息。

## 仓库和本机工作区

- GitHub 仓库为 [Rtyu3221cc/YunJianRiiChi](https://github.com/Rtyu3221cc/YunJianRiiChi)。`origin` 是 `https://github.com/Rtyu3221cc/YunJianRiiChi.git`，默认分支为 `main`。
- 本次已推送并核对远端 [`9987935e26c30284e14795d539cc9f1b1b48db67`](https://github.com/Rtyu3221cc/YunJianRiiChi/commit/9987935e26c30284e14795d539cc9f1b1b48db67)。开始新任务时重新核对 `origin/main`。
- 唯一的共享开发工作区为 `E:\MyWorkSpace_E\YunJianRiiChi`。本地 `main` 跟踪 `origin/main`。原分支 `codex/lobby-room-1013` 和对齐备份可用于查阅旧状态。
- `.publish/YunJianRiiChi` 是历史发布检出。新任务和交接统一在共享根目录进行，以 `DEVELOPMENT.md` 和实际 Git 状态为准。
- Unity 工程入口是 `unity/`。本机使用 Unity `6000.6.4f1`，编辑器安装在 `D:\Unity\Lib\6000.6.4f1\Editor\Unity.exe`。Android 构建用同一安装中的 Android Build Support、SDK、NDK 和 OpenJDK。

## 当前基线和已验证范围

- GitHub `9987935e` 包含和牌横幅独立合成、神火坠霄人物贴片屏蔽、装扮名称系统字体和全角色原版名字字体，以及 Saber「命运之夜」的 Web 主体图集、骨骼和来源记录。
- `dist/yunjian-riichi-unity-0.6.23-local.apk` 是本地试装版，版本号 `0.6.23`、versionCode `71`，包名 `com.yunjian.riichi.unity`。SHA-256 为 `555c0840208045b9ac35f56d3ec358a8ab144c3ce2a035386eb6b2d3578f92ea`。
- 本地试装版带有 Saber 主体图集临时覆盖。公开 Git 源码保留正式角色包的下载架构。正式分发新版 Saber 需要重建 Android 与 Windows 角色包，更新版本目录及校验值，再走资源发布流程。现有公开角色目录仍指向 Saber v1。
- 隔离 Unity 回归覆盖三麻两名对手的跳满自摸和荣和、四麻自家跳满、满贯以下跳过横幅、神火坠霄、Saber 动态鉴赏及角色缺字。APK 签名和 16 KB 对齐已校验，Android 真机测试待做。证据在本机 `build/phone-feedback-1106/`，其中 `apk-verification.json` 是包验证摘要。
- 69 套动态立绘中有 42 套存在更清晰的 Web 候选源。Saber 已做运行验证，其余候选待逐层核对。筛查明细为本机 `build/phone-feedback-1106/atlas-source-audit.json`。

## 开发入口

| 位置 | 用途 |
| --- | --- |
| `app/src/main/assets/engine.js` | 本地房间、规则与 AI 的主要逻辑 |
| `unity/Assets/Plugins/Android/riichi-host.androidlib/assets/` | 安卓宿主携带的引擎资产，构建时与主引擎同步 |
| `unity/Assets/Scripts/WinCutIn.cs`、`TableEffects.cs`、`NativeWinPlayback.cs` | 和牌横幅、出场顺序和原版牌面特效 |
| `unity/Assets/Scripts/LobbyController.*.cs` | 大厅、宿舍、角色与装扮界面 |
| `unity/Assets/Resources/Majsoul/CharacterDirectory/`、`WardrobeDirectory/` | 角色及装扮下载目录、固定 URL 和摘要 |
| [角色资源维护指南](character-resource-maintenance.md) | 资源来源、重建、校验及发布步骤 |

从实际要修改的检出根目录执行。跨任务换分支前，先核对未提交改动的归属。

```powershell
git status --short --branch
git log -1 --oneline
git remote -v
node --test --test-isolation=none tests/*.test.cjs
python -m unittest discover -s tests -p test_character_inputs.py
```

在共享根目录做测试时，从工作区相对路径查找本机可选包和旧录像。迁移前生成的日志、报告和预览可能记录 D 盘旧路径。启动 Unity 时传入 E 盘当前检出路径。`scripts/clear-project-generated-cache.ps1` 是旧机器清理脚本，包含 D 盘固定删除路径，迁移后勿直接运行。

## Codex 之间如何用 Markdown 交接

1. 交接时更新根目录 `DEVELOPMENT.md`，它是唯一的当前进度摘要。详细证据可写到 `docs/handoffs/YYYY-MM-DD-主题.md` 并从进度摘要链接。
2. 开始时写清基线提交、分支、工作区、目标、范围和负责方。对已有的未提交改动注明归属。同步代码优先通过分支与 PR。推送 `main`、资源发布和 APK 发布分别记录授权范围。
3. 交付时写清改了哪些文件、如何复现、实际验证命令和结果、尚未验证的场景、下一步。引用仓库内文件使用相对路径，本机产物注明相对于工作区的位置。GitHub 提交和 PR 用可打开的链接。
4. 共享记录只保留必要的工程信息。签名密钥、凭据、玩家存档、APK、Unity `Library` 和大型缓存留在本机或约定的分发位置。

### 可复制的任务记录模板

```markdown
# 任务标题

- 更新日期与负责方
- 基线提交、工作分支与 PR
- 目标与范围
- 已完成与涉及文件
- 验证命令、结果与证据
- 待验证、阻塞与下一步
```

