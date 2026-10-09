# 图像资源来源

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
