# Unity 0.6.40 / 88 构建与交接

最终发布核对：2026-11-22 00:30（东京时间）。

更新时间：2026-11-22 00:23（东京时间）。用户明确要求生成 APK、提交并推送代码到 GitHub、完善交接。主工程是 E 盘共享根目录；沿用 main。源码提交 [`a674b84d`](https://github.com/Rtyu3221cc/YunJianRiiChi/commit/a674b84d729f4df5adde3a29d73985b4ff0a06ab) 已推送 GitHub main，远端 ref 与 GitHub 文件内容已核对。最终交接更新另行提交，最新 HEAD 以 main 为准。

## 安装包与核验

- 本机 APK：`dist/yunjian-riichi-unity-0.6.40.apk`；包名 `com.yunjian.riichi.unity`，版本 0.6.40 / 88，ARM64 + ARMv7。
- 大小：368,081,448 字节；SHA-256：`d988b49eeffc53248e59f6008506f7666056e4c3964e01712926018b2bb25cce`。
- v1、v2、独立 API-23 v1、ZIP 完整性、16 KB 对齐和原证书通过。证书 SHA-256：`d1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f`，支持沿用原签名覆盖安装；实际手机覆盖安装未测。
- [基础核验](../dist/unity-0.6.40-verification.json)、[功能核验](../dist/unity-0.6.40-feature-verification.json)、[玩家页入包核验](../dist/unity-0.6.40-profile-verification.json)、[SHA-256](../dist/SHA256-yunjian-riichi-unity-0.6.40.txt)。APK 是本机交付文件，按仓库约定不进入 Git；本轮未创建 GitHub Release。

## 本包内容

1. 累计主工程 0.6.33–0.6.39 的角色手型、语音、役满 R40/R41 和玩家信息页功能，连同玩家信息页 R4–R7 的显示层精修一起入包。
2. 玩家页使用原版称号、字体、九宫格页签、高光及走势素材；称号簿、成就栏、场别、昵称、三麻／四麻和段位显示已按原视频及本机 APK 预制体逐组件核对。原版成就服务未实现，计数继续显示不可用占位；离线解锁、段位和存档规则保留。
3. 使用中恢复 130×99，立绘框 120×120，底图 304×650，保留原九宫格参数；补回 waitingroom_mask 的原始 alpha 轮廓，修正底角露图。
4. 构建时发现通用 ResourceImport 会重开 mipmap，因此在 ProjectSetup 中固化 14 张称号及 6 张关键 Native 纹理的 NPOT None、mipmap false，遮罩保持 readable。强制重新导入 20 张后通过检查；最终 APK 内也逐张检查尺寸、单级 mipmap、遮罩可读标记，并检查成就原字体字节与新运行方法存在。
5. 保留此前本机测试包的 Saber `Spine/40011003/2` 兼容覆盖。主 CharacterPackService 未改；精确补丁在 `scripts/phone-test/CharacterPackService.patch`，构建脚本同步主工程后再应用。暂存文件 SHA-256 `3fb23e2ad7253463cfb8eb408c46a2b3882494aee250d184d1435ab77641f6d3`。这是现有 Android 测试包兼容方案，不是本轮新角色包发布。

## 实际验证及边界

- 发布校验目录 `.publish/YunJianRiiChi`：`node --test --test-isolation=none tests/*.test.cjs` 209/209；Codex Python `-B -m unittest discover -s tests -p test_character_inputs.py` 8/8。
- 本轮构建强制重导入 20 张纹理；成包核对 7728 个役满资源、6 组划光颜色曲线、100 个 Profile 资源、当前引擎及统计／段位运行类。检查编辑器录制类未进入 APK。
- R7 编辑器检查 85 项及 16:9／20:9 截图、10 秒预览是此前同一任务完成的编辑器证据，未把它重记成 Android 真机检查。
- 原录像和 APK 资源版本、角色及演示数据存在差异，不宣称整页逐像素相同。原版成就服务、最近窗口风格算法仍未接入，免费改名与离线称号解锁沿用现有规则。
- 没有 Android 真机安装、触摸／滚动、GPU／内存、声音或双手机局域网验证。
- 旧 `verify-unity-apk.py` 仍要求已经迁到按需资源目录的 `r4-6cc140846ac3ca01.png`，因此不适用于当前分包布局。本版沿用 0.6.39 的 `verify-character-audio-apk.py`，通过包身份、签名、当前宿主／目录、507 个手型资源、50 个新增特效声音和 327 个基础语音的核验；另用 R41 和玩家页专用核验器验证本轮资源。旧脚本失败记录保留在 build/release-0.6.40/legacy-base-verification.log，不把它写为通过。

## 构建与代码发布

实际构建用 `build/phone-test-r24/stage/unity`，Unity 6000.6.4f1，`ProjectSetup.ReleaseVersion=0.6.40`、`ReleaseCode=88`，主 ProjectSettings 版本同步为 0.6.40 / 88。代码／meta 使用副本，静态媒体可用硬链接；不把角色源包和装扮源包复制为内置 Resources。

可复现入口：

```powershell
./scripts/build-player-info-release.ps1 -PythonPath <可用的Python路径>
```

该入口同步暂存工程、应用已有 Saber 补丁、运行 `tests/Release0640Build.cs` 的导入检查及 BuildAndroid，再进行签名和三层 APK 校验。原签名密钥保留在用户机器，不入库。签名工具使用本机已有 SDK/JDK。构建本轮实际分步执行相同环节；包装入口未额外重复完整构建。

运行证据：`build/release-0.6.40/android-0.6.40.log`、`profile-imports.json`、`base-verification.log`、`signature-verification.json`；R7 录制与来源证据仍在 `build/player-info-source-r7/review/`。这些 build 内容只留在本机。

源码提交 `a674b84d729f4df5adde3a29d73985b4ff0a06ab` 已按显式文件清单包含当前源码、必要原资源、meta、脚本、测试、文档和本轮相关预览；不提交 SDK、缓存、签名密钥、玩家存档、参考 APK，也不顺带提交旧 0.4.0／0.5.0 APK 的本机删除。历史大体积预览如未纳入本轮清单，仍留在本机 dist。提交与远端核实结果已写回 DEVELOPMENT.md。共享主根目录直接提交并推送；`.publish/YunJianRiiChi` 是本轮回归与清单校验镜像，不作为下一位的开发入口。

## 下一位接手

1. 读取 AGENTS.md、CLAUDE.md、DEVELOPMENT.md 和本文件，再检查 Git 状态。沿用 E 盘共享工程，Codex／Claude 顺序修改。
2. 在手机上覆盖安装 0.6.40：重点查使用中长宽比、称号原比例、立绘四角、页签字体、称号簿滑动及佩戴／卸下；同时看三麻／四麻切换和改名。
3. 打一局段位场，核对终局只记一次、段位／PT 变化、最佳一手／走势；再测友人场不改段位，以及另一台手机看到的称号与段位。
4. 复验 R41 的飞牌爆点／水波圈／划光时序、动态角色手型和已装 Saber 资源包。Android 的实际结果另记，不能用静态包检查替代。
5. 本次 APK 构建后暂存工程已同步到 0.6.40，保留 Saber 覆盖；后续修改应在主工程进行，下一次出包用更高 versionCode。

[R7 对照与预览](../dist/previews/2026-11-21-player-info-r7/index.html) · [使用中与边框修正说明](玩家信息页使用中比例与立绘边框修正-2026-11-21.md) · [称号与字体修正说明](玩家信息页称号比例与页签字体修正-2026-11-21.md)
