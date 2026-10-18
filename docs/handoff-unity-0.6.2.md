# Unity 0.6.2 交接（2026-10-18）

用户授权修正音乐信息位置、恢复教程翻页指示器、按视频加入首次立直牌特效和音效，并生成 0.6.2 手机试用包。未授权 Git 提交、推送或发布。

## 版本

- 0.6.2，versionCode 50，包名 `com.yunjian.riichi.unity`，原证书。
- ARM64 + ARMv7，minSdk 26、targetSdk 32。
- 构建入口 `scripts/verify-unity.ps1 -Mode Android -EditorPath D:/Unity/Lib/6000.6.4f1/Editor/Unity.exe`。
- 输出 `unity/Builds/yunjian-riichi-unity-0.6.2.apk`；经 `scripts/verify-unity-apk.py` 验证后复制到 `dist`。

## 三项修改

1. `NativeSettingsUi.cs`：右侧喇叭、“大厅音乐”和曲名在 0.6.1 基础上整体下移 1px，保持对齐，其他控件位置保留。
2. `NativeYakuUi.cs`：教程使用显式局部旋转矩阵绘制原版 `course_arrow`，避免 `GUIUtility.RotateAroundPivot` 与缩放画布叠加后把箭头移出屏幕。第 1 页只显示下一页，第 9 页只显示上一页，中间两者均显示。
3. `RiichiDiscardVisual.cs` 与 `TableEffects.cs`：立直打牌手动画接近落点前启动九层火焰粒子和短音效，随手中牌移动，交接到真实牌河牌后在原落点收尾。

## 每局／每玩家一次

触发来自已有公开 `discard` 事件的 `riichi` 标记，仅首次立直弃牌触发，普通弃牌及后续打牌不触发。去重使用房间、matchSeq、handSeq、round、honba 组成的本局标识和玩家 seat。它保留在演出重置、暂停和牌桌重建之间；新局清空。首次加入／恢复快照中已经立直的玩家被标记为已显示，避免重播历史事件。音效由同一成功触发点调用 `GameAudio.Cue("effect_rich_fire")`，遵守全域／音效音量及静音。

## 资源来源

已完整检索本地 `Majsoul_4.0.16_MC.apk` 的 4356 个资源包。对应外观是官方表中的 `305022 / effect_liqi_fire`，音效 `236 / effect_rich_fire`。基础 APK 不含这套可选外观的完整粒子图和音效，因此并非声称全部从 APK 提取。

- 四张贴图直接从 APK 导出；缺失的粒子图、材质、网格、三张贴图和 1.104 秒音效由官方固定版本资源补齐。运行时全部离线。
- `scripts/import-riichi-fire.py` 转换九个发射器、五个源平面网格、颜色／透明度／尺寸曲线、三轴尺寸和旋转；通用 `WinTileEffect` 仅增添可选三轴字段和独立初始化入口，已有和牌／樱花特效的路径不变。
- 资源 `Actions/riichi-fire-*`、`Audio/effect_rich_fire.mp3`；来源与哈希在 `Actions/riichi-fire-source.json` 和音频 provenance 中。原始图和官方表取证在 `build/release-062/source`、`item-views.json`、`official-riichi-source.txt`。
- 未执行远端游戏代码，未解密 APK Lua；未改玩法规则、协议或服务器引擎。

## 验证

`Release062Validation.Run` 的 32 项针对性检查通过：三人／四人每个 seat 首次与重复声明、普通弃牌、演出重置／同局轮询、新局重置、恢复快照、静音、实际立直手动画触发、九层粒子和五个源网格、音效资源与调用次数、运行版本及 GUI／运行错误。

真实 Unity 截图及 150 帧动画在 `build/release-062/render`；教程第 1、2、9 页已目视核对。`dist/previews/unity-0.6.2/riichi-fire-with-audio.mp4` 是实际渲染帧，并按运行记录中的第 57 帧对齐原音效，用于效果核对。没有用视频截图代替游戏特效。

最终包已生成并验证：`dist/yunjian-riichi-unity-0.6.2.apk`，109,811,605 字节，SHA-256 `849E0F4E7EC513767BFADBFEC7CB62A9FFEEBBADE091D8D07533C81D931FAFB1`。aapt 确认 0.6.2／50、原包名和双架构；原证书、v1/v2、独立 v1、16 KB 对齐、ZIP、当前离线资源、立直粒子／贴图／音效以及新 IL2CPP 类均通过检查。

仍需手机检查：闪烁是否完全消除、实际音质／音量、触摸及多手机局域网。“立直音乐”独立 BGM 播放链仍未加入，本次新增的是立直牌特效的短音效。
