# 段位结算、四方向立直棒与 11 月联动 R10 交接

更新时间：2026-11-23 09:11（东京时间）；执行：Codex。共享根目录 `E:\MyWorkSpace_E\YunJianRiiChi`，Unity 6000.6.4f1，沿用 `main`。

本轮用户要求：终局后增加原版段位积分页；逐款检查四方向立直棒并录制；大厅小段位切换三／四麻；友人场获得段位积分；同步芙莉莲联动和泠星并通过 GitHub 下载。用户明确授权本轮新资源发布与目录 URL／摘要更新。本轮没有提交、推送源码或公开 APK。

## 一、交付物与安装

- APK：[yunjian-riichi-unity-0.6.41.apk](../dist/yunjian-riichi-unity-0.6.41.apk)，版本 **0.6.41 / 89**。已构建，370,599,198 字节；SHA-256 `5d8769f6654cecbf22ca2a417e5e568aee9da30ccfebf86b61237bc918734f64`。
- 包含此前 R8 七项优化、R9 称号簿滚动／皮肤页同款绿色完成勾，以及本轮 R10。
- 新角色、完整立绘、语音与装扮继续作为可选下载包，不把完整资源塞入 APK。需要先安装 0.6.41，再在资源管理中下载对应角色／装扮；旧 APK 的内置目录不会自动增加新角色。
- 角色资源：[character-resources-2026-11](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/character-resources-2026-11)，新增 13 个附件。
- 装扮／音乐资源：[wardrobe-resources-2026-11](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/wardrobe-resources-2026-11)，新增 20 个附件。
- 两个 Release 均已公开，上传后按 GitHub 附件大小／SHA-256 和全部历史引用核验。未覆盖既有公开附件；旧角色与 520 个旧装扮包记录保持不变。

## 二、预览

路径均位于 `dist/previews/2026-11-23-rank-stick-r10/`，是 Unity 编辑器实际渲染的无声预览，使用隔离演示数据，没有改写用户存档。

| 视频 | 时长 | 内容 |
| --- | --- | --- |
| [段位结算与大厅三四麻切换.mp4](../dist/previews/2026-11-23-rank-stick-r10/段位结算与大厅三四麻切换.mp4) | 24.35 秒 | 小徽章切换、四麻／三麻加分、升段、降段、魂天 |
| [立直棒65款-四方向与槽位放大检查.mp4](../dist/previews/2026-11-23-rank-stick-r10/立直棒65款-四方向与槽位放大检查.mp4) | 97.5 秒 | 65 款既有立直棒四方向同时展示，右侧放大计分底板；每款 1.5 秒可暂停 |
| [芙莉莲与泠星-外观及四方向装扮预览.mp4](../dist/previews/2026-11-23-rank-stick-r10/芙莉莲与泠星-外观及四方向装扮预览.mp4) | 64 秒 | 14 套外观、4 套动态皮肤、新桌布／牌背／头像框／法杖，以及四方向立直／和牌特效 |

第三支视频：00:00–00:22 外观；00:22–00:24 静态装扮与新立直棒；00:24–00:40 四方向普通攻击魔法；00:40–01:04 四方向变出花田的魔法。新法杖在第三支视频单独覆盖，所以不计入前一支 65 款历史款式。

全部视频全帧解码通过：487／1950／1280 帧。预览方向和关键画面已人工检查；摘要见同目录 `verification.json`，联动关键帧见 `新增内容-关键帧.jpg`。

原版参考：`E:\MyWorkSpace_E\video\1114\codex\段位积分计算.mp4`（2.183 秒，60 fps）与 `大厅段位显示切换.mp4`（3.567 秒，60 fps）。抽帧表与 RankCalc 纹理出处保存在 `docs/reference/november-2026/`。

## 三、行为与修复

### 段位与大厅

- `ResultPresentation.cs` 在终局顺位页之后显示独立段位积分页，使用原 APK 的 12 张 RankCalc 素材和原布局数据，展示顺位分、素点、进度与升降段。保留继续／回大厅的原始目标，积分动画结束后再执行。
- 进度按每秒 0.3 个进度条推进，初始延迟 0.5 秒，跨段停留 1.2 秒；加分、减分、升段、降段、魂天均有检查。`SettlementAudio.cs` 防止切到积分页时重新播放终局语音。
- `PlayerRecord.cs` 将 `friend` 与 `ranked` 计入对应三／四麻段位；比赛场仍独立。保留房间／局序列去重，终局轮询不会重复加分。
- 友人场战绩仍归友人场，`MatchEntry.ranked` 仍只表示段位场，称号任务中的“段位场”限制不随此修改被放开。
- 大厅点击小徽章先命中 `swapRank`，切换三／四麻主徽章、副徽章和星级，持久化到 `lobbyRankPlayers`（3／4）；不会同时打开资料卡。

### 四方向立直棒

- `TableRenderer.cs` 按原版 256 像素计分底板的灰槽位置计算四方向锚点，并沿相机射线校准可见棒体中心；已放置棒与回退棒使用同一槽位计算。
- `WardrobeModelVisual.cs` 用独立 `Stick fit` 外层适配，避免被原模型动画覆盖。阴影通过所有材质名识别，不能仅凭节点名排除；修复夹心猫团子节点／材质名相反，以及猫神剑玉透明棒体误被当成影子的情况。
- 既有 65 款 × 四方向共 260 个摆放状态；新增菲伦法杖另做四方向检查。保留原模型和动画，没有用手绘棒体替代。

### 新内容清单

| 角色 key | 名称 | 原版角色 ID | 皮肤 ID |
| --- | --- | --- | --- |
| lingxing | 泠星 | 20000127 | 40012701、40012702 |
| fulilian | 芙莉莲 | 20000128 | 40012801、40012802、40012803 |
| feilun | 菲伦 | 20000129 | 40012901、40012902、40012903 |
| xiutaerke | 休塔尔克 | 20000130 | 40013001、40013002、40013003 |
| youbeier | 尤贝尔 | 20000131 | 40013101、40013102、40013103 |

共 5 角色、14 皮肤，其中 4 套“于遥远之地”动态皮肤；新增 675 条原版语音、64 个表情。总目录现为 127 角色、500 皮肤。**不要将新 ID 简写为旧角色的六位格式。**

新增六件联动装扮：308058 和牌-变出花田的魔法、308059 立直-普通攻击魔法、308060 立直棒-菲伦的法杖、308061 桌布-绽放的苍月草、308062 牌背-芙莉莲的法杖、308063 头像框-岁月的花冠。

新增 BGM：30610007「弦上华章」（对局）、30600004「未完诗篇」（大厅）。共 24 条大厅／对局音乐；17 条既有立直音乐未替换。此前已接入的立直棒「定音之弦」、桌布「奇迹舞台」、牌背「鎏金乐章」和「风车花田」背景复用原公开包与摘要。

## 四、来源与原生适配

- 新内容来自官方 DXT 原生资源清单 `https://game.maj-soul.com/assetbundles/DXT/bundle_info_so.majset`，本轮快照版本 **9.10.532**（2026-11-20）。旧 web `version.json` 停在 5 月，不能据此判断没有新联动。
- 原生角色／皮肤配置保存在 `docs/character-resource-inputs/native-2026-11.json` 并合并进 `config.json`；新增项使用 `assetLayout: native`。输入合并器支持这个布局，继续拒绝路径越界和未知 owner，只有既有通用皮肤 400000／400001 允许 owner 0。
- 该批配置的旧式裁切值为 0；实际位置使用 `MyAssets/deco/illust_data/<皮肤ID>.json` 与 APK 默认场景值生成。14 份原文、生成规则和出处位于 `docs/reference/november-2026/illust-data/` 与 `framing-provenance.json`。不得退回全零裁切或把动态皮肤当一张压缩图片。
- Spine 4.2 原始骨骼、atlas 和全部分页都保留。前三套动态皮肤有 `/1`、`/2` 两层，尤贝尔原始只有 `/2`，不是缺资源。
- 音频实际入口为 `https://game.maj-soul.com/assetbundles/DXT/Assets/MyAssets/`；语音是 `audio/sound/<key>/<path>.mp3`，BGM 是 `audio/music/...mp3`。679 个新增文件全部下载成功（675 语音＋2 特效音＋2 BGM），每个 URL、字节和 SHA-256 见 `audio-provenance.json`。
- 原始 bundle 缓存共 142 个，实际来源摘要见 `native-source-bundles.json`。三个共享包的 CDN 实际大小与清单声明不同，保留了实际字节／摘要和差异，均通过 UnityFS 解析与依赖解析；不宣称全部文件与声明大小一致。
- 新特效保留 54 层和牌粒子、131 层立直粒子与原动画。增加独立 `YunJian/NovR10EffectParticle` shader，按本批 FizzFX/Standard 的遮罩、扭曲、溶解 UV 与自定义数据语义适配 Built-in 管线；旧 shader 不变。
- 和牌的三张 `root_static` 环境面原尺寸会露出矩形接缝。仅对 `NovR10/ron_26fll` 按视口扩大环境面的粒子尺寸，使外缘在屏幕外；保留原混合方式、颜色与淡入淡出。没有采用早期试验的混合模式转换。四方向均检查三层覆盖；Android GPU 视觉仍待真机确认。
- 导出审计中的 183 条 unsupported 包含 182 条材质提示和 1 个未使用 BoxCollider。材质提示已通过专用 shader／运行时绑定处理；原始审计保留，不能把这些提示直接当作“183 个丢失的特效层”。

## 五、验证结果

| 范围 | 实际结果 | 证据 |
| --- | --- | --- |
| Node 完整规则／资源目录回归 | 217 通过 | `build/rank-stick-r10/node-tests-final.log` |
| Python 角色输入 | 12 通过 | `build/rank-stick-r10/python-input-tests.txt` |
| 段位／大厅／历史四方向立直棒 | 1170 通过 | `build/rank-stick-r10/review/report.json` |
| 5 个真实 Windows 角色包 | 947 断言通过；14 外观／4 动态／675 语音／64 表情 | `build/rank-stick-r10/characters/validation/checks.json` |
| 9 个真实 Windows 装扮包 | 561 通过；351 地址／188 renderer | `build/rank-stick-r10/wardrobe-validation/checks.json` |
| 新内容完整界面与四方向演出 | 35 通过 | `build/rank-stick-r10/content-final-review/report.json` |
| 新资源与构建输入 | 1617 个文件 meta 齐全；旧包、Saber 覆盖与 Android 引擎一致 | `docs/reference/november-2026/validation/final-input-checks.json` |
| GitHub 资源公开与摘要 | 角色 13、装扮 20 个新附件通过；历史引用保持 | 两个 build 子目录的 `github-publish.log`、`published.json` |
| APK 签名与内容 | 四项入包核验通过；v1/v2、独立 API23 v1、原证书与 16 KB 对齐通过 | `dist/unity-0.6.41-*-verification.json` |

便于查阅的报告副本保存在 `docs/reference/november-2026/validation/`，原日志仍在 build 下。视频是编辑器演示，真实包验证走实际 AssetBundle 加载服务，两者分开记录。

本次第一次 APK 入包检查在 shader 定位阶段失败：验证器假定独立 GUID 文件，实际该 AlwaysIncluded shader 位于 `globalgamemanagers.assets.split0–2`。已合并完整分片并核对真实对象名、全部关键属性以及 GLES3／Vulkan 编译程序，修正后的检查通过，APK 字节未变。保留原 `runner.log` 与最终 `rank-content-verification-final.log`、`final-verification.json`，无需因验证器修正重建 APK。

未完成 Android 真机验证；本轮未重跑完整 HTTP 下载／取消／续传／局域网中转 UI 专项，没有用目录摘要、自动测试或截图冒充手机结果。编辑器 Search.SearchDatabase 的索引异常仍出现在部分日志，未影响本轮测试结果，未顺带清理索引。

## 六、后续维护与可执行入口

1. 安装 0.6.41，在资源管理中下载本批包，真机检查动态立绘、语音、四方向棒位／特效、音乐、结算与友人场积分。再用两台手机检查下载／局域网资源准备和终局同步。
2. 修改继续在主 `unity/`，不要直接改缓存工程。`build/phone-test-r24/stage/unity/Assets/Scripts/CharacterPackService.cs` 的 Saber 兼容覆盖必须保留，SHA-256 为 `3fb23e2ad7253463cfb8eb408c46a2b3882494aee250d184d1435ab77641f6d3`；补丁在 `scripts/phone-test/CharacterPackService.patch`。
3. 本地 APK 可用 `scripts/build-rank-content-0641.ps1` 重建。它按摘要同步本轮输入、保留 Saber 覆盖、以隐藏 Unity 窗口构建，签名并运行四个入包核验脚本；原日志在 `build/release-0.6.41/`。
4. 新角色可从已保存的 `Assets/CharacterPacks/Source/<key>/` 重建：设置 `YUNJIAN_R10_REPO` 指向共享根目录，运行 Unity `YunJian.Riichi.Editor.BuildNovemberCharacters.Run`。发布文件已固定，后续修改须增加包版本／新 tag，不能覆盖 v1。通用准备脚本会复用未变的 native 包；不要用旧 web 提取器强制重建这五个角色。
5. 装扮首次导入入口是 `NativeEffectR11Import.RunNovR10` 和 `BuildWardrobePacks.BuildNovR10`。它们是**一次性导入／迁移流程**，本轮已经运行。素材已移动到 `Assets/WardrobePacks/Source/Majsoul/NativeReview/NovR10/`；原始 Data／Animations／材质提示与 manifest 已连同 meta 归档到 `ImportData/.../NovR10/`，不在 APK Resources 中。不要在迁移后的工程直接重复 `BuildNovR10`，否则 roots 指向的迁移前路径不存在。后续重建应使用当前 `WardrobeDirectory/packs-*.json` 的 `addresses[].source` 和地址映射，先输出到新的验证目录，再按新版本发布；或者在隔离导入工程按归档的 roots／源清单重新导入。
6. 资源发布脚本保留默认角色模式，装扮使用 `scripts/character-packs.py stage|publish --kind wardrobe --build-root <目录>`。本轮两个 Release 已公开，`published.json` 是发布证据，不要重传或重新构建已有同名附件。
7. 尚未提交／推送。若后续用户明确要求，先按文件清单审阅 R8／R9／R10 累积修改，再提交；保留原有未跟踪工作与旧 0.4.0／0.5.0 APK 的既有删除状态。不要将 Library、原始 APK、IL2CPP 转储、密钥、玩家存档与调试视频批量加入 Git。

首次工作区 HEAD 为 `38f0c3505984b14baee02621583fd5fc1bd12fc2`；本轮沿用共享工作区，未切分支、清理或重置。
