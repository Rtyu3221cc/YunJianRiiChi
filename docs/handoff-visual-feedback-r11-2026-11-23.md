# 六项视觉反馈 R11 交接

更新时间：2026-11-23 23:21（东京时间）；执行：Codex。共享根目录 `E:\MyWorkSpace_E\YunJianRiiChi`，Unity 6000.6.4f1，沿用 main，HEAD `38f0c3505984b14baee02621583fd5fc1bd12fc2`。保留此前 R8／R9／R10 和原工作区改动。本轮无源码提交、推送、公开 APK 或资源发布。

## 交付与安装

- 本地 APK：[yunjian-riichi-unity-0.6.42.apk](../dist/yunjian-riichi-unity-0.6.42.apk)，**0.6.42 / 90**，370,603,388 字节，SHA-256 `8869c323d9bdb40b21027a61393e50574e2b1ba1767ab6b49f4a5ceebed4d229`。包含 R8–R11；原版称号、玩家卡、绿色勾等此前修改继续保留。
- 覆盖安装后可继续使用已下载的角色／装扮包。本轮没有修改公开下载 URL、包摘要或发布附件，不需要为了这次修正重新下载包。新安装仍须在资源管理下载所需可选角色／装扮。
- 四支专项视频和对比图：[R11 预览目录](../dist/previews/2026-11-23-visual-feedback-r11/README.md)。均为 Unity 编辑器演示；Android 真机未测。

## 逐项修正与证据

### 1. 五款棒体颜色与喵星棒大小

`Wardrobe/StaticDecoration.shader` 不再乘入源材质中遗留的 `_Color`。用户原 APK 的 Simple Texture、Unlit、Cartoon 等对应 shader 并未声明／读取这个字段；原先通用移植 shader 误用它，造成乘风破浪约 0.588、冰糖葫芦及猫团约 0.8、汽水约 0.791、柴犬约 0.906 的额外压暗。保留贴图、原始 Tint、强度和光照参数，没有后期统一拉亮视频。

`WardrobeModelVisual.RestoreModelShaders` 让已发布包中的旧同名 shader 使用本轮应用内实现，同时保留材质参数与 renderQueue。因此公开 v1 资源无需重打包。

喵星棒 `liqi_23wansheng` 原节点 `yinzi` 是阴影，但材质名是 `Material #73`，此前未被识别；另外巨大 glow 平面参与了尺寸计算。现在排除该阴影，并按实体网格计算长度，仍保留原始光效与动画。透明实体／玻璃保留在实体候选中，原来的不透明／全实体 0.4 判断继续保留，避免缩小其他棒体。棒体可见长度从约 0.22 恢复到正常 0.74；六款四方向实录，65 款模型包 × 4 方向均通过加载／材质检查。

### 2. 段位进度与辉光错位

错误来自原 Sprite 导出时只保存了 textureRect 裁切矩形，丢失原透明画布位置。恢复 `m_Rect` 逻辑大小与 `m_RD.textureRectOffset` 后：expbar_base／expbar 从裁切 77×217 放回 112×277 的 (17,58)，expbar_full 从 112×236 放回 112×277 的 (0,41)。其他 9 张图片保持源逻辑画布。所有 .meta／GUID 保留。

原版 Unity Image 对该弧形贴图使用**纵向填充**，不是径向填充；本轮没有改动 `ResultPresentation.cs` 或原布局。12 张图的来源 pathID、裁切和逻辑尺寸见 [rank-ui-logical-canvas-r11.json](reference/november-2026/rank-ui-logical-canvas-r11.json)。

### 3–5. 四套联动动态立绘构图

原版新皮肤 `skin_version=2`，`SkinTool.GetSkinConfig` 从独立 `<skinID>_spine_offset.json` 读取画布／锚点，而旧表同名字段已经过期。只修正 `CharacterDirectory/catalog.json` 的四套数据，没有添加大厅专用缩放补丁，没有改通用 CharacterFraming／CharacterIllustration 或原场景 illust_data。

| 皮肤 | 原版宽×高 | pivotX／pivotY |
| --- | --- | --- |
| 芙莉莲 40012803 | 3690×5000 | 1748／3455 |
| 菲伦 40012903 | 4079×5000 | 2159／2500 |
| 休塔尔克 40013003 | 4117×5000 | 2017／4151 |
| 尤贝尔 40013103 | 5003×5000 | 2446／2802 |

原文、层数 info 与 SHA-256 在 [spine-v2](reference/november-2026/spine-v2/)。前三套为 1、2 两层，尤贝尔仅第 2 层，沿用原骨骼。`prepare-character-resources.py` 的原生包复用分支同步应用该独立配置，避免以后通用准备流程覆盖回旧表数值；原生强制重建仍使用 R10 原生导入路线。

### 6. 他家花田特效偏暗

不是对其他座位额外加了灰色 tint。R10 导出描述遗漏了源材质 renderQueue，且替换 shader 会重置队列，花朵与暗幕开始按距离排序，远处花田被后绘制的暗幕压住。

从官方原生 9.10.532 缓存重新解析两套特效的实际 Material：优先原 customQueue，缺省时使用原 shader 的 QUEUE 标签。应用内 [render-queues.json](../unity/Assets/Resources/Majsoul/NativeReview/NovR10/render-queues.json) 保存 55 条独立材质值，`NativeParticleEffect.SourceRenderQueue` 在 shader 替换后恢复它们；已有包带显式 sourceQueue 时优先使用显式值，历史非 NovR10 特效保留原队列。

花田暗幕 3001、环境光 3002、花层 3010–3018 等按源值恢复；全部 54 个粒子 renderer 在四个座位逐个对照源队列检查。没有把其他座位特效统一提亮，没有移除原版场景暗幕，也没有改原根节点旋转。独立出处见 [原材质记录](reference/november-2026/validation/r11/original-material-queues.json) 和 [54 层 renderer 顺序](reference/november-2026/flower-render-order-r11.json)。

## 实际验证

| 层次 | 结果 | 证据 |
| --- | --- | --- |
| Node 全套回归 | 217 通过 | build/visual-feedback-r11/node-tests.log |
| Python 外部输入与原图画布 | 14 通过 | build/visual-feedback-r11/python-tests.log |
| Unity 实际界面、实体棒、原构图、四座位材质队列 | 266 通过 | build/visual-feedback-r11/final-review-2/report.json |
| 已下载的真实 Windows 资源包 | 1552 通过，66 包，65 棒／260 方向，54 层花田材质 | build/visual-feedback-r11/bundle-validation/checks.json |
| 最终四支预览 | 1400 帧全部解码，人工关键帧复看 | dist/previews/2026-11-23-visual-feedback-r11/verification.json |
| APK 入包 | 音频、功能、玩家卡、R10/R11 四份核验通过 | dist/unity-0.6.42-*-verification.json |
| APK 签名 | v1／v2、独立 API23 v1、原证书和 16 KB 对齐通过 | scripts/build-visual-feedback-0642.ps1 完成输出 |
| Android 真机 | 未运行 | 待覆盖安装、真实 GPU 和双机验证 |

便于交接的报告副本在 [validation/r11](reference/november-2026/validation/r11/)。APK 入包检查包含四套构图值、修复后的纹理逻辑尺寸、原队列表、两个实际 GLES3／Vulkan shader 程序和编译后的修正方法。发布包通过本地缓存装载，没有重新下载 GitHub 已发布资源。

## 重现、状态与下一步

1. 回归：`node --test --test-isolation=none tests/*.test.cjs`；`python -B -m unittest discover -s tests -p test_character_inputs.py`。Node 22 按 AGENTS 去掉不支持的 isolation 参数。
2. 场景录制：将 `tests/VisualFeedbackR11Review.cs` 放入隔离工程 Editor 目录；设置 `YUNJIAN_R10_REPO` 和独立 `YUNJIAN_R10_OUT`，调用 `YunJian.Riichi.Editor.VisualFeedbackR11Review.Run`。脚本必须真正离开牌桌并进入大厅；不能只调用 lobby.Home 后截取仍在对局的视图。源 AssetDatabase 模式需要六款旧棒体的源资产可用。
3. 真实包：同理安装 `tests/R11BundleValidation.cs` 并调用其 Run；使用现有三处缓存目录，检查真实下载服务、SHA 收据和旧 shader 修复。不要为重复验证重新下载公开附件。
4. 构建：`scripts/build-visual-feedback-0642.ps1`。主工程 0.6.42／90，暂存工程按清单同步脚本、原图、目录、StaticDecoration shader 与 NovR10 核心队列表；脚本排除 CharacterPackService 的 Saber 覆盖，摘要仍为 `3fb23e2ad7253463cfb8eb408c46a2b3882494aee250d184d1435ab77641f6d3`。
5. 只交付 `final-review-2` 派生的四支最终视频。早期 after／after-2／after-3、final-review 失败结果及动态页面错误截成牌桌的调试预览均只留在 build，不作为通过证据；before-home 是专门重新录制的有效旧版大厅。
6. 下一步先手机覆盖安装 0.6.42，验证下载缓存复用、四方向花田的实际 GPU 混合、四套大厅动态触碰／比例、段位加分与升降段。若满意，再按用户后续指令提交源码或公开 APK；本轮没有执行这些发布操作。

分支名称、HEAD 和 Git 暂存区均未变更；旧 0.4.0／0.5.0 APK 的既有删除、所有用户存档和其他未提交内容保留。历史 R10 视频／APK 继续留存，不作为当前修正版。
