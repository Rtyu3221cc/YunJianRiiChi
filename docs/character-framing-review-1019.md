# 角色界面修正与立绘构图复核（2026-10-19）

本轮只交付实际 Unity 效果图和视频，不生成 APK，不发布 GitHub。

## 最新标注落实

- 拜访：档案灯笼使用 APK `bf_chosen` 自带光晕；右侧花饰改用纹理镜像；移除“已缔结契约”牌；姓名和 CV 恢复原先大厅毛笔字体。
- 宿舍：先画未选中的装扮页签，再画选中的角色页签，修正重叠层级。
- 服饰：角色名称改回毛笔字体；按钮从 y=898.3 下移到 931.3；介绍可视高度由 148 增至 181。契约皮肤卡片构图改为配置计算。
- 保留上轮的原包返回标题、轮播滑动、鉴赏滑条、实际动态/静态切换、排序、非牌桌樱花、大厅位置和大小保存。

## 原版的构图机制

不是把一张全身图放入所有框，也不是自动识别人脸。原版有共用加载算法和按皮肤保存的美术配置。

已在 APK 的同一资源包内找到 `myassets/deco/illust_data/default.json`、一姬和未来的皮肤 JSON。除了全身、半身、头像、候场图片，JSON 保存原始逻辑画布宽高、人物中心和不同场景的位置、缩放、翻转。`item_definition/skin` 还保留旧皮肤的 full/half/smallhead 裁切参数。

官方公开客户端 v0.11.252.w 的 `UI_Character_Skin.EIllustType` 和 `Tools._calculateIllust` / `getSkinData` 可核对到：

| 场景 | 配置组 |
| --- | --- |
| 大厅 | A0 |
| 宿舍 | B0 |
| 候场与服饰卡片 | F0 |
| 开局四方向介绍 | I0，方向索引 0–3 |
| 吃碰杠/立直等提示器 | J0，方向索引 0–3 |
| 和牌切入 | K0 |

服饰卡片的共用加载器使用 F0，并传入 `110,234,1.58,1.58` 的强制偏移/缩放。Unity 的 Y 轴向上，导入时转为 `110,-234,1.58,1.58`。原包默认 JSON 的 offset 记录 Unity 控件坐标，不可直接套用网页客户端的默认 offset；两端的皮肤相对参数可复用。开场仍保留 APK 原来的斜边遮罩、父层级镜像和入场动画。

旧皮肤走另一条路径：以一姬的参考裁切框为标尺，依据当前皮肤的裁切尺寸和 smallhead 中心计算位移。它会排除窗帘、道具等造成的画布尺寸差异，统一人物关注位置。

之前的 `PresentationPortrait` 宽图转头像近似没有遵循上述规则，导致一姬只剩大脸；开场所有人复用同一图片矩形又导致二阶堂美树显得偏小。本轮在服饰卡片、开场、提示器中替换这些近似，未给四名角色新增专用摆放分支。其他结算/切入等场景尚未在本轮逐一迁移或宣称复核完成。

## 来源与复现

- `scripts/import-character-framing-r2.py` 编译 21 套服饰的配置：15 套使用场景 JSON（135 个方向/场景记录），6 套使用原版旧裁切规则。
- 寺崎千穗理的 4 份 JSON 未在已扫描的 APK 索引中定位，从官方 `lang/base/extendRes/illust_data/` 补齐。地址、版本和 SHA-256 记录在 `build/character-review-r2/official-framing-downloads.json`。
- `build/character-review-r2/framing-provenance.json` 记录 APK bundle / pathID / SHA-256；默认坐标以 APK 为准，官方代码只作规则对照。
- 运行 `YunJian.Riichi.Editor.CharacterReviewR2.Run`，实际帧输出至 `build/character-review-r2/render`；`-r2Quick` 只截效果图。
- `scripts/package-character-review-r2.py` 生成本地画廊、前后对照、21 套卡片图和录制视频。预览不会保存玩家个人选择。

本轮不调整出战切换面板、牌桌布局或牌局规则。开场和提示器的修改仅涉及角色图像构图。
