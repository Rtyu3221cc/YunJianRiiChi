# Unity 0.5.45 验证记录

2026-10-17。Node 129 项单元测试（0 失败）、Unity 单元 62 项、Java 地址检查 19 项通过。
Unity 自摸与结算 984 项、大厅 472 项、准备房间 628 项通过；精确计数的 Unity 断言共 2146 项。
完整 Play 牌桌回归通过，不混入精确计数；听牌用例按点击开关及自动显示更新，双响演出用例改为真实结束时刻，避免旧固定时间跳过新倒计时。

APK：com.yunjian.riichi.unity，0.5.45，versionCode 46，ARM64/ARMv7。
ZIP、v1/v2、强制 API23 独立 v1、原签名证书及 16 KB 对齐通过。字节数 86490002；SHA256 `7fb3c9c85a1877631d8bb9ecd738c95de7737832a720eaaad93207dd5ae14bd0`。
签名证书 SHA256 `d1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f`。

预览复用已有画面；积分滴声 11 次、第一位 rank1 一次、winlose 零次、终局无音效。
金色反色二维码及普通码的缩放／旋转解码通过。Android CAMERA 权限与相册桥接类已打包。
没有 Android 真机实测，安装、触摸、摄像头／相册、声音、后台服务、多手机局域网仍待验证。
上传后只核对元数据与源码；未重新下载远端 APK、ZIP 或附件。

[完整交接文档](https://github.com/Rtyu3221cc/YunJianRiiChi/blob/main/docs/handoff-unity-0.5.45.md)。本地日志位于 build/release-0545 等目录。
