"""Generate release evidence from this run's logs and final signed APK."""
import importlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'scripts'))
VERSION = '0.5.43'
DIST = ROOT / 'dist'
APK = DIST / f'yunjian-riichi-unity-{VERSION}.apk'


def main():
    node = (ROOT / 'build/node-tests-0543.log').read_text('utf8')
    assert re.search(r'fail 0\b', node)
    count = int(re.search(r'pass (\d+)', node)[1])
    assert count == 122
    regression = ROOT / 'build/release-0542'
    assert 'RIICHI UNITY PLAY PASS' in (regression / 'table.log').read_text('utf8')
    assert 'RIICHI LOBBY 1013 PASS' in (regression / 'lobby.log').read_text('utf8')
    assert 'LOBBY RELEASE ONLY PASS' in (regression / 'room.log').read_text('utf8')
    lobby = int(re.match(r'(\d+)', (regression / 'lobby-passed.txt').read_text('utf8'))[1])
    room = int(re.search(r'(\d+)', (regression / 'room-passed.txt').read_text('utf8'))[1])
    specific = (ROOT / 'build/win-settlement-0543/passed.txt').read_text('utf8')
    checks = int(re.search(r': (\d+) assertions', specific)[1])
    assert checks == 984
    signature = importlib.import_module('verify-apk-signature').verify_signature(APK)
    aapt = 'D:/Unity/Lib/6000.6.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt2.exe'
    badging = subprocess.run([aapt, 'dump', 'badging', str(APK)], check=True, capture_output=True, text=True, encoding='utf8').stdout
    for expected in ("name='com.yunjian.riichi.unity'", "versionCode='44'", "versionName='0.5.43'", "minSdkVersion:'26'", "targetSdkVersion:'32'", "'arm64-v8a'", "'armeabi-v7a'"):
        assert expected in badging, 'APK metadata mismatch: ' + expected
    (ROOT / 'build/apk-badging-0543.txt').write_text(badging, 'utf8')
    timeline = json.loads((DIST / f'ai-settlement-review-{VERSION}/presentation-timeline.json').read_text('utf8'))
    assert len(timeline) == 4 and all(not row['newHandContainsOldWin'] for row in timeline)
    report = {'version': VERSION, 'versionCode': 44, 'date': '2026-10-16',
        'packageName': 'com.yunjian.riichi.unity', 'minSdk': 26, 'targetSdk': 32,
        'abis': ['arm64-v8a', 'armeabi-v7a'], 'nodeTests': count,
        'unityChecks': checks + lobby + room, 'winSettlementChecks': checks,
        'lobbyChecks': lobby, 'waitingRoomChecks': room, 'runtimePlayPassed': True,
        'apk': signature, 'presentationTimeline': timeline, 'androidDeviceTested': False}
    (DIST / f'unity-{VERSION}-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', 'utf8')
    notes = f'''# 云间日麻 Unity {VERSION} 验证记录

2026-10-16。修复自摸动画临时牌位重叠，暗色结算确认按钮在演出完成后倒计时3秒自动确认，保留手动点击及终局9秒，“确认”与“返回大厅”同为31号字。

- Node {count}项通过，0失败，包含在线托管等待本机确认、三／四人普通／终局换局与旧事件清理。
- Unity 自摸与倒计时专项 {checks}项、大厅 {lobby}项、房间 {room}项断言通过，合计{checks+lobby+room}项。
- 完整牌桌Play回归通过，涵盖真实本机房间、出牌、三／四人、鸣牌、拔北、自摸／荣和、双响、轮询与断线去重、粒子完整结束及结算；该回归没有提供精确断言数量，不计入上面的合计。
- 四段实际Unity流程录制完整解码通过；新局无旧和牌演出。
- 包名com.yunjian.riichi.unity，versionName {VERSION}，versionCode 44，ARM64＋ARMv7。
- 完整APK ZIP、JAR签名文件、v1、v2、强制API23独立v1、原签名证书及16KB对齐通过。发布ZIP内副本和GitHub重新下载结果另由发布脚本验证。
- APK {signature['bytes']}字节；SHA256 `{signature['sha256']}`。
- 证书SHA256 `{signature['certificateSHA256']}`。
- 没有连接Android真机；安装、触摸、声音、后台服务与多手机热点未实测。

模型与倒计时专项截图及四段视频位于ai-settlement-review-{VERSION}。本轮测试日志保留在本机build目录；源码发布不包含运行状态、SDK、原始参考APK和私钥。
'''
    (DIST / f'验证记录-unity-{VERSION}.md').write_text(notes, 'utf8')
    page = f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 {VERSION}</title><style>body{{max-width:980px;margin:40px auto;padding:0 20px;background:#142034;color:#edf0f5;font:17px/1.8 sans-serif}}a{{color:#f3ce88}}img{{max-width:100%}}</style><h1>云间日麻 Unity {VERSION}</h1><p>自摸牌在预留牌位垂直起落并使用正常牌桌深度。结算排名页保留暗色“确认(3)”按钮，3秒自动确认，可提前点击；“确认”与“返回大厅”同为31号字；终局“下一局”仍为9秒。</p><p><a href="https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-{VERSION}/{APK.name}">下载APK</a> · <a href="https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-{VERSION}">GitHub发布页</a></p><p>{count}项Node测试、{checks+lobby+room}项Unity断言及完整牌桌回归通过。APK {signature['bytes']}字节，SHA256：{signature['sha256']}。本地、ZIP内及GitHub下载文件由发布脚本分别校验签名与完整性。</p><p>未连接安卓真机，安装、触摸、音频、后台服务与多手机热点未实测。</p><p><a href="ai-settlement-review-{VERSION}/index.html">发布ZIP内四段实际Unity录制及截图</a></p></html>'''
    (ROOT / f'docs/handoff-unity-{VERSION}.html').write_text(page, 'utf8')
    print(json.dumps({'version': VERSION, 'nodeTests': count, 'unityChecks': checks+lobby+room, 'apk': signature}, indent=2))


if __name__ == '__main__':
    main()
