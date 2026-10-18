# 图像资源来源

Unity 0.5.29 的点击樱花使用官方 CDN 场景 `scene/effect_click_default.lh` 的图层参数与 `plane1X1-Plane01.lm` 平面网格；花瓣贴图为用户 APK `myassets/necessary/texture/yinghua.png`（即已有的 `UiEffects/native-ui-floower_00000.png`），闪光为 APK `myassets/common3d/texture/shanguang.png`（已有 `Actions/win-shanguang.png`）。来源与哈希见 `UiEffects/click-source.json`。

Unity 0.5.28 的 `TableLook` 来自同一 APK：`scene_mj` 方向光、`Catfood` 卡通明暗与描边参数、`effect_shadow` 的 `shadow.png`、`room/sun_shine` 粒子及其 `yuanqiu2.png`、`bg_type` 底图；“暂无称号”为官方 `extendRes/title/notitle.png`（XOR 0x49 解码）。来源与哈希见 `TableLook/source.json`。

Unity 0.5.26 的 `TablePolish` 包含同一 APK 提取的 `hint` 菱形感叹号、姓名底图、纹饰、金币、PT 以及姓名使用的 `SimHei` 字体。来源及 SHA-256 见该目录 `source.json`；未绘制替代菱形图标。

Unity 0.5.25 的 `NativeReview` 来源是同一份用户提供的雀魂 APK：原宝牌反光贴图／网格、四向拔北锚点、12 个箭头变换、排名与终局图片、役满 6 + 6 组及吃碰 2 组粒子模块。逐项 bundle、pathID 与哈希见该目录 `provenance.json`；编译后 Shader 和加密脚本保留为证据。[解包范围](docs/1011优化与解包范围.md)没有将这些内容称为完整可编译的原工程或原始 Shader／脚本源码。

本地试玩版 0.3.0 使用用户提供的 `Majsoul_4.0.16_MC.apk` 中的以下美术资源。这些图片并非本项目原创，也不属于规则引擎的 MIT 许可。

- `tiles/*.png`：默认手牌图集，共 37 张（含三张赤五），每张 80 × 129。原路径：`myassets/deco/mjpface/mjpface_default/3d/texture/hand.png`，图集尺寸 800 × 516。
- `portraits/yiji.png`：一姬头像，256 × 256。原路径：`myassets/deco/character/yiji/smallhead/smallhead.png`。
- `portraits/erjietang.png`：二阶堂美树头像，256 × 256。原路径：`myassets/deco/character/erjietang/smallhead/smallhead.png`。

图片权利归原权利人。此来源说明不授予任何再分发或商业使用许可。

早期版本的桌布、桌框、牌背和计分盘由本项目绘制；后续接入的原始资源分别列在下方。全部资源随 APK 打包，运行时不读取原安装包、不联网下载。原始大型 APK 不包含在交付压缩包中。

资源通过 UnityPy 1.25.4 读取本地 Unity AssetBundle，版本回退值为 2022.3.62f2c1。牌面裁剪记录见 `app/src/main/assets/tile-assets.json`。源码包含已处理图片，正常构建无需 UnityPy 或原 APK。若已有导出的手牌图集，可运行 `python scripts/slice-reference-tiles.py <hand.png路径>` 重新裁剪（需要 Pillow）。

## 0.4.0 语音

`voice/*.wav` 是预先使用本机 Microsoft Huihui Desktop 中文语音生成的 39 段 PCM 单声道音频，不是雀魂角色配音。文本清单见 `voice-lines.json`（“荣和”采用“荣胡”文本以得到胡牌读音），可选重新生成工具为 `scripts/generate-voice.ps1`。运行时只播放内置文件，不调用手机 TTS 服务。

用户自选头像在设备上居中裁切、重新编码为 128×128 JPEG，仅同步处理后的头像，不打包进开发版源码或安装包。

## 0.5.0 资源与来源

- APK 新增：`table/score-hub.png` 来自 `myassets/scenes/mj/mjtable/texture/bg.png`；一姬表情头像来自 `myassets/deco/character/yiji/face/smallhead1.png`；Unity 中文字体来自 `myassets/fonts/common/simhei.ttf`。输出路径、原容器与 SHA-256 见 `app/src/main/assets/majsoul-assets.json`。
- Unity 平放牌面使用 APK 默认 `mjp.png` 图集的 37 个牌面区，手牌继续使用已有 `hand.png` 切图，显示源与头像一致。
- 补充的默认桌布来自雀魂官方公开资源：`https://game.maj-soul.com/1/v0.10.1.w/scene/Assets/Resource/tablecloth/tablecloth_default/Table_Dif.jpg`，原图转为本地 `table/cloth.png`。原图 SHA-256 见 `table/cloth-source.json`。它是默认蓝色桌面与木框，不是角色专用桌布。
- 音频全部使用同一角色一姬：官方公开资源 `https://game.maj-soul.com/1/v0.10.1.w/audio/sound/yiji/act_*.mp3`。包含吃、碰、杠、立直、两立直、自摸、荣和和拔北，共 8 段。每段 URL、输出路径、事件映射与 SHA-256 见 `voice-manifest.json`。资源索引核对版本为 `resversion0.11.252.w.json`。
- 一姬配音与默认桌布不是从此 APK 提取的：它们通过官方公开索引补充。运行时仅使用本地文件，无官方登录、更新或联网下载流程。
- 旧版 39 个合成 WAV 保留在源码中，当前角色清单不引用它们；不会在一姬配音间混播。普通出牌不播合成牌名。
- 此处描述 0.5.0 的本地资源接入；后续发布状态以 GitHub 提交为准。

## Unity 0.5.2 参考外观

- 西瓜桌布：官方资源 https://game.maj-soul.com/1/v0.10.1.w/scene/Assets/Resource/tablecloth/tablecloth_chigua/Table_Dif.jpg ，保持原始 JPEG 随 Unity 包离线加载。
- 宝牌框、蓝色按钮、提示开关和默认头像框：来自用户提供的 Majsoul_4.0.16_MC.apk；精确资源路径、导出文件和 SHA-256 见 unity/Assets/Resources/Majsoul/reference-skin.json，导入脚本为 scripts/import-reference-skin.py。
- 桌面麻将使用原 APK 的 mjp_new 网格、法线与 UV；手牌使用独立正交层和原手牌图集。模型及图集来源见 unity/Assets/Resources/Majsoul/OriginalTile/provenance.json。显示适配、简化材质和 UI 排版由项目代码实现。新增素材权利仍归原权利人。
- 本次修改仅接入 Unity 客户端；运行时无需加载原 APK 或联网获取素材。

### 宝牌栏与计分盘图集

Unity 0.5.2 补充原 APK 的 chang、ju、word_left、number_left、number_score、dir 及 w_0..w_9。用于局数、余牌、方位、点数、供托和本场显示。导入脚本为 scripts/import-reference-hud.py，原材质 UV 与 UI 槽位参数在 unity/Assets/Resources/Majsoul/Hud。宝牌预览由导入的麻将模型离屏渲染，已移除“云”字牌背占位。

## Unity 0.5.4 手部与操作提示

- 吃、碰、杠、和牌、立直、拔北、流局、跳过按钮、操作喊字、切入背景及一姬立绘，来自用户提供 APK 的 mj_main_chs / mj_main_common 图集与 yiji/full。响应箭头来自 effect_mingpaizhishi；原图用于加色粒子材质，导入时将黑底转换为透明度以适配 GUI。
- 默认女性手部模型 Hand_Skin-hand.lm、17 骨骼绑定、Dapai_Ani-Dapai.lani 出牌曲线及皮肤/指甲贴图，来自雀魂官方公开资源。精确 URL、资源容器与所有输出 SHA-256 记录在 `unity/Assets/Resources/Majsoul/Actions/provenance.json`。
- 导入脚本 `scripts/import-reference-actions.py` 解码原 Laya 二进制数据、采样 Hermite 曲线，并离线烘焙 931 顶点、15 帧的网格动画。坐标转换保留正确三角面朝向；运行时插值播放，不透明手部材质不依赖原版 Laya 着色器。
- 原始数据格式参照 [LayaAir-v1 官方解析器](https://github.com/layabox/LayaAir-v1/tree/master/src/d3/src/laya/d3)。原游戏脚本未打包到本工程，C# 代码负责公共事件去重、方向变换、布局与按钮行为；当前切入默认使用一姬素材。
- 素材权利仍归原权利人。正常构建使用已转换的本地文件，无需 UnityPy、原 APK 或联网下载。

## Unity 0.5.5 麻将描边与模型底部指示器

- 麻将描边使用用户提供 APK 内的 `maque_outline` Mesh 031（pathID 7551034896308475998），224 个顶点；沿用原预制体子节点缩放 1.047。颜色读取 `myassets/necessary/material/mjp_outline.mat` 的 `_OutlineColor`，RGB 均为 0.283018887。
- 光圈来自 `myassets/scenes/mj/mjtable/texture/majiangkuang.png`，保持原始 256×256 像素。原 `effect_mingpaizhishi/effect_zhishi/3d/prefab/effect_zhishi.prefab` 的 `kuang` 使用该加色粒子材质。当前将原纹理放到牌底的桌面平面上，按模型脚印缩放，黄色加色并轻微脉动；箭头继续使用已有原素材。
- 来源容器、输出 SHA-256、描边颜色与缩放记录见 `unity/Assets/Resources/Majsoul/OriginalTile/outline-provenance.json`；导入脚本 `scripts/import-reference-outlines.py`。
- 原版编译着色器未作为源码移植。`OriginalTileOutline.shader` 用扩展网格背面实现轮廓，`ResponseGlow.shader` 实现加色纹理；动态位置、手牌避让与拔北方向由 C# 适配。

## Unity 0.5.6 立直棒、副露与撞击特效

- 立直棒放置动画来自雀魂官方公开资源 `scene/Assets/Resource/table/hand/Lizhi_Ani-LiZhi.lani`（0.800 秒，30 fps，60 轨道），驱动骨骼 `Dum_Lizhi/node_liqibang`；与已有的手部网格和骨架共用同一套绑定。
- 副露动画来自 `.../hand/Fulu_Ani-Fulu.lani`（0.900 秒，30 fps，60 轨道），驱动 `Dum_Majiang/node_tile`。吃、碰、杠与三麻拔北共用此动作。**雀魂手部动画全集只有 Dapai、LiQiPai、Zimo、Fulu、LiZhi 五个片段，没有拔北专用动画**——这一点由索引 144,521 条 key、APK 4,356 个 bundle、24 个 `hand_*.lh` 与 62 个手部 `.lani` 的完备枚举确认，不是搜索失败。
- 两个片段与已有的出牌、立直出牌一起由 `scripts/import-hand-animations.py` 烘焙为 `discard-hand`、`riichi-hand`、`lizhi-hand`、`fulu-hand` 四组 JSON 与帧数据。脚本在写入前先逐字节复现已提交的 `discard-hand` 产物作为自检，自检不过则拒绝写出任何文件。
- 鸣牌撞击特效来自 `scene/effect_mingpai_default.lh`（包裹 `effect_chipeng`）：`ci` 粒子使用 `effect/texture/effect_fulu/bang.png`，`smoke` 粒子使用同目录 `smoke.png`，材质为加色混合（`srcBlend` 770 / `dstBlend` 1，关闭深度写入）。本项目只取这两张原始贴图，粒子与淡出由 C# 实现，未移植原版编译着色器。
- 回合灯使用计分盘底图 `score-hub.png` 中点数槽下方 y 210–222 的凹槽，立直棒使用其下方 y 224–240 的槽位；两处都是原底图自带的结构，不是新绘制的贴图。
- 以上来源的原始 URL、字节数与 SHA-256 记录在 `unity/Assets/Resources/Majsoul/Actions/hand-provenance.json` 与 `meld-effect-provenance.json`。素材权利仍归原权利人。

## Unity 0.5.7 计分盘回合灯修正

- `Hud/turn-light.png` 是用户提供 APK 中 `myassets/scenes/mj/mjtable/texture/particle_shining.png` 的原始 86×86 图像，保留透明度。
- 两层原粒子颜色、1 秒循环寿命、零速发射和透明度曲线来自 `scene_mj.prefab` 的 `man/ind`。混合方式与 Tint 核对了雀魂公开原材质；来源与 SHA-256 见 `Hud/turn-light-source.json`，导入脚本为 `scripts/import-reference-turn-light.py`。
- `ReferenceScoreHub.cs` 使用 Unity 原生粒子系统；`TurnLight.shader` 适配原加色材质。灯条在本项目盘面上的姿态、尺寸与轮询相位保持由 C# 负责，未包含原客户端完整游戏脚本。素材权利仍归原权利人。

## Unity 0.5.8 拔北表现

- 喊字继续使用原 APK 的 `shout_babei`，立绘继续使用已有 `yiji-full`，语音继续使用一姬拔北音频；没有替换成用户截图里的另一套角色皮肤。
- 手部沿用已烘焙的 `Fulu_Ani-Fulu` 网格动画，0.9 秒动作、0.7 秒落牌。C# 对齐其 `node_tile` 落牌位置和北牌朝向；事件携带公开北牌与拔北次序，目标与吃碰副露分开。
- `Actions/north-petals.png` 直接导出自用户提供 APK 的 `myassets/necessary/texture/yinghua2.png`，原始 256×256 RGBA、4×4 帧。导入脚本 `scripts/import-reference-north.py`；容器、pathID、哈希和适配说明见 `Actions/north-effect-source.json`。
- `NorthPetals.cs` 和 `NorthPetal.shader` 用 Unity 原生粒子显示翻转的花瓣，九片单次发射、1.1–1.5 秒寿命、上飘与淡出，放在本次拔出的北牌上方。发射器和时序为当前项目的参考图适配，未把大厅的循环飘花预制体或原客户端完整代码搬进运行时。素材权利仍归原权利人。


## Unity 0.5.9 荣和、自摸与默认胡牌特效

- 荣和、自摸使用原 APK 中文图集的 `show_rong`、`show_zimo`，继续使用已有一姬立绘和音频。
- 原 `Zimo_Anim-Zimo.lani` 在同一原手部骨架上烘焙成 28 帧、30 Hz、0.9 秒动作，0.8667 秒交接胡牌。原官方公共资源补齐 APK 资源目录未提供的动画文件，来自官方 Yo-star CDN；模型、骨骼、蒙皮材质继续使用已导入资源。
- 默认 `effect_hupai_default.lh` 七层配置及加色材质来自雀魂官方公共资源；`yuanqiu2`、`shanguang`、`bao`、`kuosan` 贴图直接从用户 APK 导出，`Light2`、`thunder`、`lizi` 缺失原文件由官方 CDN 补齐。容器、pathID、URL、哈希见 `Actions/win-source.json`，导入脚本 `scripts/import-reference-win.py`。
- `WinTileEffect.cs` 与 `WinParticle.shader` 使用原七层的寿命、颜色、发射量、透明度、尺寸曲线和图集，在 Unity 原生粒子中适配坐标、平面朝向及尺度；每次胡牌保证发射源配置的最大 burst 数。自摸动作完整曲线保留，较大手臂摆幅适配到本牌桌单位；落牌目标、播放队列、轮询去重和延后结算由本工程 C# 实现。未将雀魂客户端整套运行脚本复制进工程。
- 所有运行资源已本地打包，离线牌桌无需访问 CDN。素材权利仍归原权利人。

## Unity 0.5.10 录像中的火焰与两阶段结算

- `Results/` 的 52 张结算原图从用户提供 APK 的 `atlas_mj_main_common_*.png` / `atlas_mj_main_chs_0.png` / `atlas_common_yiman_*.png` 精确导出，包括蓝色樱花斜板、役种笔刷、番符圆盘、宝牌标题、点数卡和转移箭头，以及 `h_0..9` 金色番数、`ww_0..9` 白色符数/点数、“番/符/点/役满”字图和满贯系列独立笔刷字图。保持原尺寸比例和原图颜色，无系统字体重绘；bundle、pathID、输出哈希见 `Results/source.json`。
- `Actions/fire-*.png` 使用原火焰、圆形冲击与火星贴图，优先从 APK 同名文件导出，缺失的原纹理由官方公共 CDN 补齐。八层发射、寿命、色彩及透明度/尺寸曲线读取原 `effect_hupai_fire.lh`。可重复导入脚本为 `scripts/import-video-resources.py`。
- 录像所用皮肤未作为独立特效预制体打包在这份 APK 中。`FlameSpiral.cs` 使用原火焰图集制作上升旋涡与环绕碎石，时序按用户视频适配；这是 Unity 中对该皮肤表现的还原，不是该皮肤完整脚本的直接复制。原旧火焰的竖直光层收短以避免穿到牌面下方。自摸保留原 0.9 秒手部曲线，接 0.87–1.14 秒背面翻正面，再于 1.24 秒触发特效。
- `ResultPresentation.cs` 按录像拆成和牌展示和点数转移两阶段。一姬沿用原导入全身图，不跟随录像皮肤切换。公开结果新增的手牌、已公开副露、里宝牌及役种是房主在和牌后发布的计分数据；不会向对手提前泄露手牌或里宝牌。
- 立直不可出牌使用原彩色图集，仅乘以亮度系数 0.5；不再转为灰度。

## Unity 0.5.11 结算手牌与牌背

`ResultPresentation` 不再使用早期 `Tiles/*.png` 结算裁切牌图。那些 PNG 的 NPOT 导入缩放导致运行时纹理比例偏窄，且结算的隐藏牌原为纯色占位块。现在 `OriginalTiles.HandTexture` 在固定 124×200 的透明 RenderTexture 中复用对局 `OriginalHand` 材质、`OriginalTile/hand-face.png` 默认牌面及 `hand-back.png` 玫瑰红牌框/背面；宽高比和 `CreateHand` 网格一致。材质新增默认关闭的背面采样开关，仅供结算暗杠两端使用，不改变对局花色或立直亮度。

宝牌和里宝牌直接使用对局 HUD 已有的 `DoraTexture` 及同一套原三维网格、桌面牌面/玫瑰红背面贴图，包括未翻开的指示牌。视频中的猫图案来自其皮肤；本项目结算继续采用本项目对局已选的默认牌面/玫瑰红背面，不混用录像皮肤。

用户 APK `assets/Android/$0qqs86vvx-2$d224jmac_a_pwwtzzeeg@_e4c587454fdfaad84e4a.majset` 中的 `UI_Win` 控件树提供 80×120 手牌槽、6°倾角、手牌容器 0.96 与宝牌容器 0.7 缩放；Unity 对应使用 40×60 和 29×43.5 牌槽并保持纹理原比例。逐像素验证结算纹理与实际 `CreateHand` 网格摄像机渲染一致。原资源路径仍见 `OriginalTile/provenance.json`。

## Unity 0.5.12 对局提示与荒牌流局

用户《荒牌流局.mp4》和四张截图提供显示顺序与布局参考。导入脚本 scripts/import-indicator-resources.py 从用户本地 Majsoul_4.0.16_MC.apk 提取原简体中文主界面及通用主界面 Sprite。Resources/Majsoul/Indicators/source.json 保存十张图的原包、bundle、pathID、Sprite border 和 SHA-256；版本库仅包含实际用到的 PNG、元数据和导入脚本，不包含原 APK 或录屏。

原图片包括 pic_zhengting（竖排振听字图）、nofan（无役）、show_tingpai（听牌）、noting（未听牌）、huangpailiuju（荒牌流局）和 bg_chipengchoose 等花饰/辅助底图。导入保留 NPOT 原尺寸，防止书法和框线比例被修改。UI_ChiPengHu 和 UI_TingPai 的原牌槽为 80×120，吃牌组合、听牌待牌共用当前对局 OriginalTiles.HandTexture，保持 0.62 比例和彩色花色。花饰框支持多列、多行待牌；十三面待牌不会丢失。

荒牌流局使用当前对局三维牌模型摊开手牌，未听牌手牌仅用公开牌数生成背面，不向客户端发送私牌；本项目牌面/玫瑰红背面保留。中央流局、听牌和未听牌字图直接使用 APK Sprite；点数沿用原 scorechange_bg、scorechangearrow_0 与 ww_ 数字图。规则和界面状态衔接在本项目实现，不引用皮肤中的猫牌或录像人物立绘。

## Unity 0.5.13 原结算控件与录像火焰适配

scripts/import-settlement-resources.py 从用户提供的 Majsoul_4.0.16_MC.apk 提取 UI_Win 的 pg_he/pg_zimo、UI_ScoreChange 的 s_1–s_4 与对应“位”、rank_down_arrow / upgrade_arrow、scorechangearrow_1、Btn_common、bg_head1。Resources/Majsoul/Settlement/source.json 记录原 bundle、pathID 和 SHA-256；PNG 保留原尺寸，NPOT 不缩放。点数增减原 CText 引用的 haolong_0 数字字体及局标题 fengyucumaokai 的简体中文对应字体直接从 APK 的 Font.m_FontData 导出。确认与返回大厅都按原 CText 控件用相同书法字体/金色，不使用带另一圈边框的 confirm 字图。按钮命中仍由本项目同步 Busy 与权威房间操作。

《荣牌特效.mp4》呈现亮黄实心、橙色旋转带、上宽下窄的高火焰柱及基部碎石；0.5.12 的细螺旋纹理没有达到该样式。0.5.13 保留八层原火焰粒子，并使用原 fx_fire_003 火焰细节贴图搭建六个宽火焰面及交叉填充核心，依据录像调整高度、横向比例、色彩、上升/衰减和碎石轨迹。该安装包目录索引没有找到此录像皮肤的独立命名预制体，柱体几何和控制逻辑在 Unity 实现；不宣称完整皮肤代码逐字复制。四时间点及三/四麻每个座位实际相机截图用于核对，不只是验证有粒子生成。美术纹理没有使用生成图片。

## Unity 0.5.14 九种九牌与明牌指示

九种九牌字图由 scripts/import-indicator-resources.py 从 Majsoul_4.0.16_MC.apk 的简体中文主牌桌图集直接提取，Indicators/source.json 记录 bundle、pathID、border、SHA-256。对照用户九种九牌.mp4，中央原字图淡入，声明者手牌移到桌沿、按原牌面/牌背模型平放展开；录像中的回放控制条属于录像回放功能，未加入实时对局。只公布依法声明的那一手牌，其他私牌不读取、不展示。

同类牌提示复用原 maque_outline 轮廓与 response-glow 纹理，手牌 shader 以轻微暖色保持牌面颜色；所有可见性由当前公共 DTO 与本人的手牌决定。新公开事件的 riverIndex 只标识已打出的牌在牌河中的位置，不携带对手手牌。出牌隐藏/排队/落牌交接和自摸正面朝向为本项目客户端适配逻辑。Android 构建前自动同步维护中的离线引擎与资源到手机房主插件，APK 校验逐文件比对，避免原生编译成功却装入旧规则。

## Unity 0.5.15 同类牌淡蓝覆盖

按用户 2026-10-11 参考图，明牌白底约 (223,224,222)，选中同类白底约 (137,186,215)。OriginalTileFace 仅在原麻将 mesh 的牌面 submesh 上用 (.614,.830,.968) 乘色，保留字图的颜色与纹理、原轮廓、红色牌背和牌壳。该 shader 是本项目按图适配的代码，不宣称来自安装包。所有牌面、牌背及 mesh 继续使用已提取的原素材；没有新绘制位图。

取消同类牌地面发光与金色轮廓，不影响可吃碰舍牌原本的操作提示光效。OriginalHand 删除同类染色，保留选牌抬起与立直限制的保色亮度降低。宝牌预览按是否匹配分别缓存，只给已公开的牌面乘色，不改变未翻开的牌背。

## Unity 0.5.16 原版整牌蓝色

原版客户端缓存 `build/reference/public/v0.11.252.w/code.js` 的 `ViewPai.OnChoosedPai` 对同类牌使用 Vector4(0.615,0.827,0.976,1)，随后 `setMeshColor` 遍历模型的 sharedMaterials 逐个设置颜色。APK 的 mjp_face/mjp_mid 材质也带有 `_Tint`。本版按这条原版逻辑在 Unity 适配整个公开麻将模型的颜色，取代 0.5.15 根据截图采样的牌面专用乘色；实际材质仍是本项目的 Unity 适配 shader，不宣称整个 shader 源文件从 APK 复制。没有改动原牌面/牌背位图或网格。

蓝色参数同时作用于牌面、牌身侧边和原轮廓，选中牌回收/取消/轮询时统一更新。自己手牌不染色，隐藏牌背不进入匹配集合。原操作提示光圈保留，同类牌不使用额外光圈。左侧桌边副露通过屏幕投影留白，不改其牌面朝向和原模型比例。

## Unity 0.5.17 UI_Win 原版锚点

使用本地 Majsoul_4.0.16_MC.apk 已提取的 UI_Win RectTransform 数据及用户提供的两种宽高比结算截图。内容坐标按 1920×1080 中心锚点换算，手牌容器原点 (-251.6,275.9)，容器缩放 .96、角度 6 度，单牌原尺寸 80×120；宝牌 (27.5,250) 和里宝牌 (536.5,303)，缩放 .7；番符 (365.5,-48)，185×179；得分 (555,-315)，589×110，缩放 1.2；标题 (-62,-412)，650×150，缩放 1.2。使用原牌面、红色牌背、标题字形、番符、背景和一姬立绘，未重新绘制图片。背景铺满宽屏，内容等比缩放并按中心定位。

手牌沿用 APK 手牌相机 orthographicSize 9、牌高 2.54 的原比例。副露避让只调整公开牌组在桌面的位置，不改变手牌、副露和拔北的模型比例。选牌抬起和整牌蓝色逻辑保持。

## Unity 0.5.18 名称倾角与两处间隔

原版 UI_Win/scaler_width/root/container_name/container_name 的 RectTransform 旋转 quaternion.z=0.04142316058、w=0.99914169312，换算约 4.748 度。玩家名称在 Unity GUI 中以 -4.748 度旋转，名牌背景使用原图，不重复旋转位图。

牌列按照手牌、全部副露、和牌三个段落排列。有副露时只在手牌/副露交界和副露/和牌交界分别留半个牌位的大间隔，多组副露之间没有间隔；无副露时只有手牌/和牌间隔。牌框宽度等于步长，高度按实战 HandWidth/HandHeight=.62 换算，解决旧版 1 像素缩窄和 1.5 高度比产生的组内留白。原位图和牌背、暗杠端点处理保持。

实际 Unity 774×438 参考场景改为七张手牌、两组三张副露、一张和牌，核对只有两个大间隔。934×430 无副露、1688×780/844×390 暗杠及五宝牌场景同时核对倾斜名称、连续组内排列和末尾和牌空间。

## Unity 0.5.19 斜名牌可见区域居中

bg_nickname 的斜边和透明区域使完整图片矩形的中心不等于玩家名称所在位置的可见框中心。名称框由基准画布 (32,585,220,33) 上移至 (32,572.5,220,33)，以文字框中心作为局部旋转中心，保留 -4.748 度倾角。姓名、背景和其他结算元素仍统一适配屏幕比例。

实际 Unity 774×438、934×430 参考画面核对名称在框内的上下留白。沿用 0.5.18 的手牌/全部副露/和牌两处间隔及连续牌面布局。

## Unity 0.5.22 提示框、报役和火龙卷

原始 `pop_bound`/`bg_3` 以及 `showtile`、`hule_fan_out`、`fu_appear`、`score_appear`、`mangan`、`beiman`、`yakuman` 等声音从用户 APK 导出。缺失的深蓝 `pop_bg`、一姬 82 条役种/结果语音和火龙卷 305034 场景/音效使用雀魂官方固定版本资源；所有资源离线打包，来源和 SHA-256 见 `SettlementVoice/provenance.json`、`SettlementVoice/manifest.json`、`Audio/provenance.json`、`Indicators/source.json`。真实网格和原版粒子数据取代旧的程序旋风。[详细说明](docs/原版提示与结算音效.md)。

## Unity 0.5.23 提示边框修正

`Indicators/pop_frame.png` 从已有原版 `pop_bound` 派生，保留金色边缘与花纹，去掉半透明深蓝内部像素；原版 `pop_bg` 继续作为底图。出处、转换方式及 SHA-256 记录在 `Indicators/source.json`。其余结算字图、牌面、一姬立绘、音频和火龙卷资源沿用 0.5.22。

## Unity 0.5.24 背景、等级火焰和整局开场

`bg_jiesuan1`、开场四向画框、姓名底纹、自家标签及可匹配贴图从用户APK导出；役满和胜负场景参数/网格以及缺失贴图由雀魂官方固定版本资源补充。开场四张立绘画框由原版画框和已有一姬完整立绘按原节点位置合成。出处与哈希见 Results/source.json、MatchIntro/source.json、UiEffects/source.json。[实现说明](docs/原版背景与整局开场.md)。
# 0.5.30 新增来源

`Review1012c` 中的 `cutinbg`、`cutin_line`、`rank_bg`、`sima_queshi`、`star`、`starbg` 来自所提供雀魂 APK 的原图。`cutin-layout`、`cutin-animation`、`intro-name-layout` 分别来自 `UI_CutIn`、其 AnimationClip 及 `UI_FightBegin` 的序列化数据。原 bundle、pathID 和导出文件 SHA-256 见 `Review1012c/source.json`；导入、校验脚本为 `scripts/import-review-1012c.py` 和 `scripts/verify-review-1012c.py`。一姬立绘及默认称号沿用已记录来源的现有资源，未使用生成图片。


## Unity 0.5.42 终局与中弹窗

终局 `UI_GameEnd` 预制体坐标、名次数字与“位”取自现有雀魂 APK，专用精灵恢复原透明画布，不改局间结算精灵。来源和 SHA-256 见 `Resources/Majsoul/Settlement/Final/source.json`。中弹窗深色 `button`、黄色 `button_light` 以及 AI 托管 `button_text_bottom_s` 使用 APK 原图，按原 Sprite border 拉伸中段并保留两端，来源分别见 Lobby/provenance.json 与 reference-skin.json。未使用生成式美术。

## Unity 0.5.45 役满、接管与大厅音乐

役满机会／确定字图、粒子数据与材质参数来自用户提供的雀魂 APK，见 `Resources/Majsoul/NativeReview/yakuman-provenance.json`、对应 `Evidence` 与源 JSON。保留原粒子参数，修复 Unity 版本迁移所需的序列化字段及透明／加法合成。APK Lua 为加密字节，没有可读原逻辑；本项目用自家手牌评分驱动提示。

博士帽、头像暗遮罩、闪电为 APK 原图，来源与布局见 `Resources/Majsoul/Review1017d/provenance.json` 和 `layout.json`。红点、指示器边框及创建转场沿用原版资源。新增音效 `effect_yakumanready`、`rank1`、`score_change` 来自原音频包，记录见 `Resources/Majsoul/Audio/provenance.json` 与各导入脚本。

大厅《金秋送爽》使用雀魂官方固定版本资源 `audio/music/lobby_autumn.mp3`，由用户试听确认；来源 URL、长度与哈希见 `Resources/Majsoul/Lobby/Audio/autumn-source.json`。二维码由 ZXing 生成，白／暖白／紧留白／透明金色是同一公开邀请数据的颜色及留白比较，不使用生成式美术。
## 2026-10-18：四方向提示器与完整字图

`Majsoul/Callouts1018` 的布局、背景、遮罩与五张文字均从用户提供的 `Majsoul_4.0.16_MC.apk` 提取；`source.json` 保存 bundle、pathID 与输出 SHA-256。文字使用原始 187×143 Sprite 完整画布，保留透明留白；`word-sprites.json` 记录原图坐标、紧边偏移和可见字形边界。显示时按可见字高等比缩放，双字自然增加宽度。角色图沿用已有 APK 导出资源，未使用生成图片代替实际运行效果。
