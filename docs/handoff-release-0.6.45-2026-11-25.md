# 0.6.45源码与安装包发布交接

时间：2026-11-25 01:28（东京时间）。用户本次明确授权将代码上传GitHub，并将APK发布到Release，取代此前“先不上传”的限制。

目标：[unity-0.6.45测试版Release](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-0.6.45)；main基线 `ad670ed024f7f1b6c54ccdde2f935d1bb6b375bc`。本次只发布最新0.6.45 APK，不发布中间0.6.43／0.6.44测试包。

源码包含R12、R13、R14、R15及相应必要资源／meta、测试、交接和三组最终预览。工作区旧APK删除、其他历史SHA和调试预览、SDK／tools／build缓存、密钥、玩家数据、原版参考APK及可选资源源文件不纳入。

已有旧发布副本存在早期未提交改动，因此使用新的独立发布副本并保留旧目录。发布副本重新执行Node与Python测试；本地实际APK完成ZIP完整性、签名及SHA检查后上传；完成时核对GitHub附件大小和SHA-256、远端main提交和README，不重复下载上传的APK或预览。

本地APK373,566,474字节，SHA-256 `15dfcb0bf52cea1f5e4ec6b2872afba3c08f5ba17e9572390b6f6c0b00bf73a1`。七组实际包核验及v1/v2、独立API23 v1、原证书与16KB对齐通过。Android真机、双机待测。功能和实景验证详见[R15交接](handoff-ui-polish-r15-2026-11-25.md)及[R13/R14交接](handoff-bot-room-r13-r14-2026-11-24.md)。

本文件在发布核对完成后补记源码提交、附件和远端核验结果。
