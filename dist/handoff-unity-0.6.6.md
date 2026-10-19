# 云间日麻 Unity 0.6.6 交接

本版把角色资源实验接入 APK，版本号 0.6.6，versionCode 54；包名 `com.yunjian.riichi.unity`，沿用现有签名证书。源码、APK 和角色资源均发布至 `Rtyu3221cc/YunJianRiiChi`。

## 角色范围

推荐下载集合为一姬、卡维、福姬、八木唯、辉夜姬、相原舞、青鸾、北见纱和子、小鸟游雏田、二阶堂美树，以及汪次郎，共 11 名、71 套服饰、17 套动态服饰。名单里的“莱兰／セイラン”按原始配置名称青鸾处理，汪太郎按原始名称汪次郎处理。另保留未来、寺崎千穗理的既有资源包，共 13 名可用角色、79 套服饰。

一姬、二阶堂美树的完整资源继续内置且不可删除。其他角色默认仅有轻量头像和元数据；宿舍头像带黑色遮罩，下载并校验安装后可使用。两名基础角色的独立 bundle 一并归档到资源 Release，不要求手机重复下载。

资源发布页：<https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/character-resources-v1>。Android 和 Windows 编辑器分别构建，不能混用。可信目录给出版本、字节数、SHA-256 和不可变的下载地址。老实验 Release 保留供历史版本使用。

## 使用方式

首次进入大厅提示全部下载、推荐集合或暂不下载；之后可从设定的“资源”页批量／自选下载、管理列表或删除已下载角色。下载显示进度，失败可重试；已完整接收的文件经过大小、SHA-256 和 AssetBundle 加载验证后才写安装收据。取消保留部分文件用于续传。

大厅出战、随机候选与星标只允许已安装角色。删除角色时清理当前出战、服饰偏好、星标和随机候选；基础角色、正在下载／共享／对局使用的资源受保护。

房间准备时冻结本局角色。缺包可以从 GitHub 下载、通过局域网从另一玩家接收（支持房主中转和断点），或在本机采用一姬显示替代。房主等待所有真人确认后发牌；替代不会更改对方真实角色或游戏状态。取消、离开、准备超时或房主恢复会使过期确认失效；不重复抽取本场随机结果。

## 验证与限制

具体执行结果和 APK 摘要见同版 `unity-0.6.6-verification.json`；交付 APK 及发布 ZIP 内 APK 均检查 ZIP 完整性、v1/v2、API23 独立 v1、原证书和 16KB 对齐。

自动化包括 Node 单元测试、既有 Unity 断言、两个平台的 bundle 内容／摘要检查、全部 79 套服饰加载及动态渲染、下载／安装／取消／续传／损坏拒绝／删除保护，以及房间等待与一姬替代流程。局域网传输是单机真实 HTTP 源与中转模拟，其他房间玩家通过测试请求模拟。**Android 真机安装、触摸、声音、多手机热点、后台下载和低内存设备未实测。**

全部角色目录包含 122 名的轻量元数据；本版只开放已发布的 13 名，其他头像明确不可下载。目录采用客户端内置可信版本；运行中发现全新角色目录、任意远程代码更新不在本版范围。新增目录或不兼容骨骼格式需要更新客户端。批量下载串行，取消会清除本批排队任务。

## 维护入口

- `scripts/prepare-character-resources.py`：从已提供原 APK 及匹配的官方资源版本整理 11 名目标角色；补齐原 APK 缺少的相原舞／福姬新服饰。
- `BuildCharacterPacks.Run -allCharacterPacks`：Windows 验证包；添加 `-packAndroid` 和 `-buildTarget Android` 构建 Android 包。
- `scripts/publish-character-resources.py finalize`：生成可信目录；`publish` 上传资源附件并检查服务端摘要。
- `CharacterResourceValidation.Run`：加载当前构建的全部 Windows 资源，检查静态图、骨骼渲染、音频、表情和安装保护。
- `CharacterPackExperiment.Run`：实际下载与局域网／房间流程验证。
- `scripts/verify-unity.ps1 -Mode Validate`：既有 Unity 断言；`-Mode Android` 构建并签名 APK。
- `scripts/release-066.py`：明确范围的源码整理、发布打包及远程提交／摘要核对；不会回下载已发布附件。

工程保留角色原始素材以便重建 bundle；`Library`、工具、原始参考 APK、密钥、运行时状态不纳入源码发布。角色素材的出处与既有项目相同，见 `ARTWORK.md`。
