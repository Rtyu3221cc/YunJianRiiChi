# 云间日麻 Unity 0.6.13 交接与验证

本版为装扮功能实机测试版。包名 `com.yunjian.riichi.unity`，versionName `0.6.13`，versionCode `61`；沿用原证书，支持 ARM64 与 ARMv7，可覆盖安装。

## 功能与修正

- 宿舍装扮提供 11 类、28 件资源，每类至少两件。包括立直棒、和牌特效、立直特效、鸣牌指示、手部、音乐、头像框、桌布、牌背、牌面及大厅背景。
- 选择、预览、保存与牌桌应用已连接；房间只同步目录标识，按玩家冻结整场对局的装扮，断线恢复保留选择。桌布、牌面和牌背由本机选择。
- 气泡鸣牌指示根据原资源根节点平面计算挂载旋转，半球朝上并保留原始漂移；雷驰电掣保留自身已铺平的旋转。新增普通引导、众星捧月，保留原网格、贴图、UV 滚动和材质混合参数。
- 鸣牌指示显示在可响应的弃牌位置，响应结束后清理；副露落地不再生成位置错误的响应指示。
- 烈焰、爆炎龙卷、默认和牌与冰霜／火焰立直接入完整原始粒子层级、尺寸曲线、网格、动画序列和材质参数；和牌特效保留世界朝上的层级，直立火焰使用原垂直 billboard。
- 冰霜使用原地面分支；普通立直棒恢复白棒红点，特殊立直棒在手持和落地阶段使用对应模型。猫爪两种配色与普通人手使用各自原始动作。
- 调整装扮页签、选中描边、列表高度、图标比例、头像框贴合、透明预览底色、保存和关闭按钮边距。

多套装扮预设、预设重命名、装扮资源按需下载尚未实现；当前装扮样例均内置。既有角色资源下载继续使用独立的角色资源发布。

## 本轮发布验证

- 165 项 Node 测试，覆盖牌局规则、房间与会话、装扮目录／同步／重连、资源房间及立直等。
- 8 项 Python 角色输入合并测试，以及角色注册表一致性检查。
- 571 项既有 Unity 单元断言（会话 62、提示与图集 87、角色 385、角色界面 37）。
- Unity 牌桌、大厅（472 项）和房间（623 项）运行回归；403 项装扮动作检查覆盖三种手、三／四人全部座位、立直棒手持到落地、特效粒子和音乐起停；132 项四视角方向检查覆盖四种鸣牌指示。
- 144 项源数据核对，涵盖五套原始特效的完整层级、节点变换、渲染模式、逐轴尺寸及动画序列。
- 最终分发 APK 的 ZIP、资源内容、离线引擎、手机房主服务、双架构、v1／v2、独立 API 23 v1、原证书和 16 KB 对齐检查；发布 ZIP 内 APK 与直接分发 APK 的大小、SHA-256、签名均核对。

本轮修正的是编辑器验证代码：旧图集测试明确使用原始图集；牌背检查验证当前所选资源；旧和牌朝相机平面断言更新为原世界朝上层级与垂直 billboard；旧光环检查更新为实际装备的牌河鸣牌指示；旧布局／时间测试临时采用默认装扮，合成场景停用网络准备和资源协调请求，避免真实测试房间响应覆盖固定画面，不写入装扮偏好。大厅测试等待异步资源准备完成再核对开局。这些验证入口编译在 `UNITY_EDITOR` 中，不改变已生成的 APK。

自动化结果不代表手机安装、触摸、声音、低内存或多手机热点联机已经实测。新增四视角画面是同一公开测试场景的编辑器渲染，不是实际对局录像。

## 发布文件

- 安装包：`yunjian-riichi-unity-0.6.13.apk`，193,543,462 字节。
- SHA-256：`b076818a12efcfc949e81f832d0eff1db827babcb2e9026f365fae9913e13674`。
- 签名证书 SHA-256：`d1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f`。
- 验证 JSON、校验文本、本交接文档、源码 ZIP 和完整安装发布 ZIP 一并作为 GitHub Release 附件。
- [程序发布页](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-0.6.13)。上传后核对远程提交、源码和附件大小／摘要，不重新下载发布附件。

## 后续入口

- 目录与选择：`WardrobeCatalog.cs`、`LobbyController.Wardrobe.cs`，资源位于 `Resources/Majsoul/Wardrobe`。
- 牌桌与动作：`WardrobeResponseIndicator.cs`、`TableEffects.cs`、`RiichiDiscardVisual.cs`、`WinTileEffect.cs`、`NativeParticleEffect.cs`。
- 原资源导入：`import-wardrobe-r2.py`、`import-wardrobe-r3.py`、`import-wardrobe-effects-r4.py`、`import-wardrobe-callouts-r5.py`。这些脚本需要本地原资源输入；普通构建使用已导入资源。
- 原数据核对：`verify-wardrobe-fidelity-r4.py`；动作回归：`WardrobeGameplayAudit.Run`；鸣牌四视角方向检查：`WardrobeFourViewsReview.RunStudy`。
- 发布整理：`release-0613.py`，只在独立 `.publish/YunJianRiiChi` 中暂存和提交，不改工作副本 Git 状态。安装包与 ZIP 只放 Release，不纳入源码 Git。
- [装扮界面预览](../dist/previews/2026-10-25-wardrobe-r3/index.html)、[四视角预览](../dist/previews/2026-10-25-wardrobe-r5/index.html)。
