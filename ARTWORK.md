# 图像资源来源

本地试玩版 0.3.0 使用用户提供的 `Majsoul_4.0.16_MC.apk` 中的以下美术资源。这些图片并非本项目原创，也不属于规则引擎的 MIT 许可。

- `tiles/*.png`：默认手牌图集，共 37 张（含三张赤五），每张 80 × 129。原路径：`myassets/deco/mjpface/mjpface_default/3d/texture/hand.png`，图集尺寸 800 × 516。
- `portraits/yiji.png`：一姬头像，256 × 256。原路径：`myassets/deco/character/yiji/smallhead/smallhead.png`。
- `portraits/erjietang.png`：二阶堂美树头像，256 × 256。原路径：`myassets/deco/character/erjietang/smallhead/smallhead.png`。

图片权利归原权利人。此来源说明不授予任何再分发或商业使用许可。

桌布、桌框、牌背、计分盘、页面排版与交互由本项目实现。全部图片随 APK 打包，运行时不读取原安装包、不联网下载。原始大型 APK 不包含在交付压缩包中。

资源通过 UnityPy 1.25.4 读取本地 Unity AssetBundle，版本回退值为 2022.3.62f2c1。牌面裁剪记录见 `app/src/main/assets/tile-assets.json`。源码包含已处理图片，正常构建无需 UnityPy 或原 APK。若已有导出的手牌图集，可运行 `python scripts/slice-reference-tiles.py <hand.png路径>` 重新裁剪（需要 Pillow）。

## 0.4.0 语音

`voice/*.wav` 是预先使用本机 Microsoft Huihui Desktop 中文语音生成的 39 段 PCM 单声道音频，不是雀魂角色配音。文本清单见 `voice-lines.json`（“荣和”采用“荣胡”文本以得到胡牌读音），可选重新生成工具为 `scripts/generate-voice.ps1`。运行时只播放内置文件，不调用手机 TTS 服务。

用户自选头像在设备上居中裁切、重新编码为 128×128 JPEG，仅同步处理后的头像，不打包进开发版源码或安装包。
