# 云间日麻 Unity 0.6.18 / R22 交接

日期：2026-11-01。当前维护工程 `D:/MyWorkSpace/YunJianRiichi_Codex`。本轮将装扮改为基础内置＋可校验下载资源，先接入全部已定位的活动和牌定义。不要把导入成功、桌面运行检查或可见像素检查写成与原版视频逐像素一致，也不要写成 Android 真机已测。

## 本轮结果

- APK：`dist/yunjian-riichi-unity-0.6.18.apk`，241,432,496 字节，约 241.43 MB / 230.25 MiB；versionName 0.6.18，versionCode 66；包名 `com.yunjian.riichi.unity`。
- SHA-256：`bc97d2f827f718e35c644729f3e9262f1b0063eaf314adf37d3cb99a1bf91ca5`。证书保持 `d1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f`。v1、v2、独立 API-23 v1、双架构及 16 KiB 对齐已验证。
- 基础包每类 3 款可选，手部仍为默认／橘猫手 2 款。默认手部按角色性别使用女性或男性模型，隐藏兼容 ID 保留。基础和牌为雷击／烈焰／Fate，基础立直为苍火／碎冰／火焰。
- 目录有 332 个装扮条目＋22 首背景音乐；和牌可选 60 款，即 59 款已定位原生定义＋默认。固定公开配置另外有 1 个已过期且未找到资源的定义；原生清单还有 5 个未映射键，含默认别名和新资源，不作为已完成可选项宣传。
- Android／Windows 各 380 个可下载包，含共享依赖；Android 合计约 428.56 MB，Windows 约 427.96 MB。下载目录、轻量图标和基础款留在 APK，其余重资源确实移出 Resources。
- 视频：`dist/previews/2026-11-01-wardrobe-r22/index.html`。真实游戏画面，60 个名称跳转、185 秒、4440 帧、24 FPS，2 倍速且无音轨；游戏保留正常速度和原音效。

## 下载、缓存与联机

`WardrobePackService.cs` 沿用角色资源的安装合同：内容指纹文件名、Range 续传、大小和 SHA 校验、可加载内容检查、最后写入完成收据。目录分别为 `Resources/Majsoul/WardrobeDirectory/packs-android.json` 和 `packs-windows.json`，客户端保存在 `persistentDataPath/wardrobe-packs-v1`。

设置的资源页新增角色／装扮子页；装扮资源可按种类批量下载和删除。装扮选择页支持单件下载及下载后选用。缺失／删除资源回到基础款。对局、预览和传输期间保护资源，基础资源不可删除。

缓存上限为 160 MiB / 24 包。预加载期间固定整条依赖图；驱逐共享依赖时同时清理依赖它的缓存 prefab，防止再次取出已经丢失贴图的对象。实际检查发现并修复过这一问题。

房间准备冻结装扮要求，支持 GitHub 下载、局域网接收／房主 HTTP 中转和基础替代。资源能力上限 1024 个 hash；下载中每 30 秒发送心跳，避免长下载被 3 分钟准备过期规则中止。声明字段和客户端解析同步，角色协议兼容路径保留。

## Lua 与原生特效

Lua 解码继续使用 `docs/handoff-lua-effects-2026-10-29.md` 中已验证的流程和本地缓存。输入、Fate／和牌相关可读 Lua 分别在 `build/lua-reverse-20261028/decrypted/readable` 和 `build/fate-r19/lua/decrypted/readable`。AES 的实际 key／IV、解码明文、原始 APK、参考 bundle 和逆向工具都不上传 GitHub；不用运行原版 Lua 或连接雀魂账户。

主要入口是 `DesktopMgr.ShowHuleEffect`、`setNewHuleEffect`、`setOldHuleEffect`，以及 `ActionHule`、`Tools.RonDelay` 和 `ViewPlayer_Other._Hule`。`NativeWinPlayback.cs` 处理新旧根节点、动画牌与回收，`NativeEffectMotion.cs` 播放原编译动画采样，R22 独立 shader 处理八类材质族，含蒙皮网格和材质引用曲线。

特别核对：

1. 原场景 -1000 的偏移映射到本工程场景原点，不能当成离屏定位。`root_move`、`root_rot`、`root_ani`、`root_ani_zimo`、`root_static` 分别绑定。
2. `ron_25yh` 的飞向相机分支：50/60 秒等待、15/60 秒 OutQuad；`ron_26dj` 的空 `root_fly` 占位不抢占正常动画。
3. `ron_liyang`（静伏蛰影）的 `root_static/47/posi` 按原 Lua 屏幕边缘范围定位。
4. 从已缓存 lqc 继续解出的 `outfit_config/ron` 覆盖值：涟漪之空 2.5 秒、静伏蛰影 1.86 秒。证据在 `docs/wardrobe-lua-config-r22.json`。
5. 原压缩动画的唯一对象曲线是 `ron_24summer` 第二材质槽，在 0.2 秒切换。先用 Unity 公共 API 编译已知第零槽的对照 clip，确认属性索引，再恢复原槽位；证据在 `docs/wardrobe-pointer-curve-proof-r22.json`，不能误改主材质。
6. 原版的结算等待与六秒 prefab 销毁分开：荣和使用 2＋RonDelay，自摸从手开始计时 2.8＋RonDelay；本工程扣除已走过的手部 release＋0.1 秒。不要重新用完整特效生命周期堵住结算。

## 必须保留

- R21 自摸手部 0.48，牌接触桌面即切到真实桌牌，随后手撤回；音效、接触、release、FX 启动分开，不能恢复停顿后再落牌。
- 满贯及以上才有立绘横幅；满贯以下直接进入报役种的流程。
- R13 问心之镰是立直键 `effect_liqi_23ex_liandao`，固定 `nativeWorldEuler=[0,180,0]` 且保留四个变体。和牌键 `ron_23ex_liandao` 是另一项，不能混淆。
- R18 原版拖牌、相同牌的实体区分、轮询稳定性、后台 120 秒宽限、既有角色下载与签名身份。

## 资源目录已迁移

10888 个文件按 GUID 保留原样迁移。运行重资源在 `unity/Assets/WardrobePacks/Source`，导入用的原始网格／粒子／动画 JSON 在 `ImportData`，轻量 marker 在 `Markers`。不要把它们整目录复制回 Resources。

`docs/wardrobe-resource-layout-r22.json` 是原路径到现路径的精确映射；`docs/wardrobe-pack-plan-r22.json` 是可复用包计划。Node 检查使用 `tests/wardrobe-source.cjs` 定位源文件，仍核对完整数据和原哈希。`NativeEffectR11Import` 支持迁移后的输入路径。

旧登记脚本不能盲目重跑：它们曾按旧结构重新写包装配置，容易丢失后续校准值；请按迁移映射更新已有文件。`import-native-effects-r11.py --wins-r22` 在拆包后将重新导出到隔离 `build/wardrobe-r22/reexport`，避免覆盖已核验运行资产；审核后再纳入包计划。重建压缩引用曲线后还需执行对应恢复步骤并验证。

资源包重建入口是 `YunJian.Riichi.Editor.BuildWardrobePacks.BuildBoth`。本地优先读取 `build/wardrobe-r22/pack-plan.json`，新检出回退读取 docs 中的计划。已经拆分的工程不要再运行初始 `PlanAndMove`。修改已发布内容时递增包版本、生成新指纹文件；不要覆盖旧附件。

## 验证与发布

- 192 项 Node 测试；真实目录加载与材质检查；480 个四座位荣和／自摸场景；48 个补充 Lua 场景；21 项最终下载流程；60 款视频可见像素检查。
- APK 实际包含 2956 项基础资源，排除 10888 个可选／导入文件。完整结果在 `dist/unity-0.6.18-verification.json`。
- 资源 Release：`https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/wardrobe-resources-v1`。762 个附件通过 GitHub 上传返回的大小及 SHA-256 核验，不重新下载验证。
- 工作区有历史未提交内容；沿用 `.publish/YunJianRiiChi` 单独筛选发布，不对维护工程 reset／clean。源码 ZIP 只留本地。
- Android 真机 GPU、触摸、覆盖安装和多手机热点仍待用户实测；与原版视频逐像素比对未完成。

构建临时工程只在 `build/wardrobe-r22/stage/unity`，主工程 `unity/Library` 保持不建立。D 盘以 15 GiB 为获取／构建预留阈值；原始 APK、参考缓存、签名材料及最终产物不能当作临时文件清理。具体本轮磁盘清理情况查看最终验证记录。
