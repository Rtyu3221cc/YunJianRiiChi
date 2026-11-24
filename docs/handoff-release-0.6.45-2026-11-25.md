# 0.6.45源码与安装包发布交接

时间：2026-11-25 01:28（东京时间）。用户本次明确授权将代码上传GitHub，并将APK发布到Release，取代此前“先不上传”的限制。

目标：[unity-0.6.45测试版Release](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-0.6.45)；main基线 `ad670ed024f7f1b6c54ccdde2f935d1bb6b375bc`。本次只发布最新0.6.45 APK，不发布中间0.6.43／0.6.44测试包。

源码包含R12、R13、R14、R15及相应必要资源／meta、测试、交接和三组最终预览。工作区旧APK删除、其他历史SHA和调试预览、SDK／tools／build缓存、密钥、玩家数据、原版参考APK及可选资源源文件不纳入。

已有旧发布副本存在早期未提交改动，因此使用新的独立发布副本并保留旧目录。发布副本重新执行Node与Python测试；本地实际APK完成ZIP完整性、签名及SHA检查后上传；完成时核对GitHub附件大小和SHA-256、远端main提交和README，不重复下载上传的APK或预览。

本地APK373,566,474字节，SHA-256 `15dfcb0bf52cea1f5e4ec6b2872afba3c08f5ba17e9572390b6f6c0b00bf73a1`。七组实际包核验及v1/v2、独立API23 v1、原证书与16KB对齐通过。Android真机、双机待测。功能和实景验证详见[R15交接](handoff-ui-polish-r15-2026-11-25.md)及[R13/R14交接](handoff-bot-room-r13-r14-2026-11-24.md)。

## 发布完成（2026-11-25 01:39 东京时间）

- 源码提交：[`30ec71de`](https://github.com/Rtyu3221cc/YunJianRiiChi/commit/30ec71deaeaa56efe5884b41d4e57419fac5e409)，345个显式清单文件，已正常推送main。Release标签`unity-0.6.45`精确指向此提交；随后只补充本发布记录的文档提交，当前main以`git log -1`为准。
- Release已公开，测试版标记开启：[安装包下载](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.6.45/yunjian-riichi-unity-0.6.45.apk)。附件仅APK；SHA文本、综合检查及两支R15预览随源码提供。
- GitHub返回大小373,566,474字节、状态uploaded、SHA-256与本地逐字节检查一致，远端main／标签和GitHub连接器读取README均已核对。没有重新下载已上传附件。
- 发布副本重新运行Node227／Python14全部通过；R15两比例／缓存回归903项，实际APK七组内容检查与v1/v2／独立API23 v1／原证书／16KB对齐通过。APK与测试输入未改，不重复打包。
- 保留共享工作区原有两份旧APK删除、历史未选产物和旧发布副本；未纳入提交。没有重发角色／装扮包或修改资源目录的URL／摘要。
- 原始核对记录见[preflight.json](reference/release-0.6.45/preflight.json)及[publication.json](reference/release-0.6.45/publication.json)。Release标签对应功能源码，后续文档提交仅记录发布结果。

## 下一步

Android真机覆盖安装、房间角色／装扮触控切换、首次启动缓存、三麻结算与双机同步仍待实测。随机装扮池仍未实现。完整构建方式、Saber暂存覆盖边界及R12–R15细节见各轮交接。
