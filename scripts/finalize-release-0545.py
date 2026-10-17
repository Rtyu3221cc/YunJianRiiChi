"""Assemble release evidence and the Markdown handoff from completed local checks."""
import importlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
DIST = ROOT / 'dist'
BUILD = ROOT / 'build'
VERSION = '0.5.45'
sys.path.insert(0, str(ROOT / 'scripts'))


def main():
    node = (BUILD / 'node-tests-0545.log').read_text('utf8')
    assert re.search(r'fail 0\b', node), 'Node tests failed'
    count = int(re.search(r'pass (\d+)', node)[1])
    unit = int(re.match(r'\d+', (BUILD / 'release-0545/unit-passed.txt').read_text('utf8'))[0])
    checks = int(re.search(r': (\d+) assertions', (BUILD / 'release-0545/settlement-passed.txt').read_text('utf8'))[1])
    regress = BUILD / 'release-0542'
    for name, marker in [('table', 'RIICHI UNITY PLAY PASS'), ('lobby', 'RIICHI LOBBY 1013 PASS'), ('room', 'LOBBY RELEASE ONLY PASS')]:
        log = regress / (name + '.log')
        assert log.stat().st_mtime > (BUILD / 'node-tests-0545.log').stat().st_mtime, 'Stale regression log: ' + name
        assert marker in log.read_text('utf8'), 'Missing regression success: ' + name
        shutil.copyfile(regress / (name + '-passed.txt'), BUILD / 'release-0545' / (name + '-passed.txt'))
    lobby = int(re.match(r'\d+', (regress / 'lobby-passed.txt').read_text('utf8'))[0])
    room = int(re.search(r'\d+', (regress / 'room-passed.txt').read_text('utf8'))[0])
    java = (BUILD / 'release-0545/java-tests.log').read_text('utf-8-sig')
    assert 'PASS: 5 LAN addresses; 14 invalid destinations rejected' in java
    assert 'PASS: final APK' in (BUILD / 'release-0545/apk-verify.log').read_text('utf8')
    apk = DIST / f'yunjian-riichi-unity-{VERSION}.apk'
    signature = importlib.import_module('verify-apk-signature').verify_signature(apk)
    aapt = 'D:/Unity/Lib/6000.6.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt2.exe'
    badging = subprocess.run([aapt, 'dump', 'badging', str(apk)], check=True, capture_output=True, text=True, encoding='utf8').stdout
    for expected in ["name='com.yunjian.riichi.unity'", "versionCode='46'", "versionName='0.5.45'", "minSdkVersion:'26'", "targetSdkVersion:'32'", "'arm64-v8a'", "'armeabi-v7a'"]:
        assert expected in badging, 'APK metadata mismatch: ' + expected
    permissions = subprocess.run([aapt, 'dump', 'permissions', str(apk)], check=True, capture_output=True, text=True, encoding='utf8').stdout
    assert 'android.permission.CAMERA' in permissions
    (BUILD / 'release-0545/apk-badging.txt').write_text(badging + '\n' + permissions, 'utf8')
    timeline = json.loads((BUILD / 'review-1017e/render/audio-timeline.json').read_text('utf8'))
    effects = timeline['events']
    cues = {key: sum(e['page'] == 'scores' and e['key'] == key for e in effects) for key in ['score_change', 'rank1', 'winlose']}
    cues['final'] = sum(e['page'] == 'final' for e in effects)
    assert cues == {'score_change': 11, 'rank1': 1, 'winlose': 0, 'final': 0}
    matches = json.loads((BUILD / 'review-1017d/fixtures/rehearsal-report.json').read_text('utf8'))['matches']
    assert len(matches) == 2 and all(m['terminal'] and m['heldAfterReady'] for m in matches)
    total = unit + checks + lobby + room
    report = {'version': VERSION, 'versionCode': 46, 'date': '2026-10-17', 'packageName': 'com.yunjian.riichi.unity',
              'minSdk': 26, 'targetSdk': 32, 'abis': ['arm64-v8a', 'armeabi-v7a'], 'nodeTests': count,
              'unityUnitChecks': unit, 'javaChecks': 19, 'unityChecks': total, 'winSettlementChecks': checks,
              'lobbyChecks': lobby, 'waitingRoomChecks': room, 'runtimePlayPassed': True, 'apk': signature,
              'settlementAudioRehearsal': cues, 'fullAiMatches': matches, 'previewFramesReused': True, 'androidDeviceTested': False,
              'remoteArtifactsRedownloaded': False}
    (DIST / f'unity-{VERSION}-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', 'utf8')
    handoff = ROOT / f'docs/handoff-unity-{VERSION}.md'
    template = BUILD / 'handoff-template-0545.md'
    if not template.exists():
        shutil.copyfile(handoff, template)
    text = template.read_text('utf8')
    values = {'APK_BYTES': signature['bytes'], 'APK_SHA256': signature['sha256'], 'NODE_TESTS': count, 'UNITY_UNIT': unit,
              'SETTLEMENT_CHECKS': checks, 'LOBBY_CHECKS': lobby, 'ROOM_CHECKS': room, 'UNITY_CHECKS': total}
    for key, value in values.items():
        text = text.replace('__' + key + '__', str(value))
    assert not re.search(r'__[A-Z_]+__', text), 'Unfilled handoff field'
    handoff.write_text(text, 'utf8')
    notes = f'''# Unity {VERSION} 验证记录

2026-10-17。Node {count} 项单元测试（0 失败）、Unity 单元 {unit} 项、Java 地址检查 19 项通过。
Unity 自摸与结算 {checks} 项、大厅 {lobby} 项、准备房间 {room} 项通过；精确计数的 Unity 断言共 {total} 项。
完整 Play 牌桌回归通过，不混入精确计数；听牌用例按点击开关及自动显示更新，双响演出用例改为真实结束时刻，避免旧固定时间跳过新倒计时。

APK：com.yunjian.riichi.unity，{VERSION}，versionCode 46，ARM64/ARMv7。
ZIP、v1/v2、强制 API23 独立 v1、原签名证书及 16 KB 对齐通过。字节数 {signature['bytes']}；SHA256 `{signature['sha256']}`。
签名证书 SHA256 `{signature['certificateSHA256']}`。

预览复用已有画面；积分滴声 11 次、第一位 rank1 一次、winlose 零次、终局无音效。
金色反色二维码及普通码的缩放／旋转解码通过。Android CAMERA 权限与相册桥接类已打包。
没有 Android 真机实测，安装、触摸、摄像头／相册、声音、后台服务、多手机局域网仍待验证。
上传后只核对元数据与源码；未重新下载远端 APK、ZIP 或附件。

[完整交接文档](https://github.com/Rtyu3221cc/YunJianRiiChi/blob/main/docs/handoff-unity-{VERSION}.md)。本地日志位于 build/release-0545 等目录。
'''
    (DIST / f'验证记录-unity-{VERSION}.md').write_text(notes, 'utf8')
    preview = DIST / f'previews/unity-{VERSION}'
    preview.mkdir(parents=True, exist_ok=True)
    old = DIST / 'preview-20261017'
    videos = ['score-audio-review.mp4', 'double-yakuman-audio-fixed.mp4', 'riichi-depth-review.mp4', 'response-order-review.mp4']
    pictures = ['lobby-spacing.png', 'shortcut-spacing.png', 'qr-compact-white.png', 'qr-compact-warm.png', 'qr-compact-tight.png', 'qr-compact-gold.png',
                'response-before-land.png', 'response-after-land.png', 'create-two-minute.png', 'create-unlimited.png', 'idle-taken.png', 'manual-ai.png', 'idle-returned.png', 'all-ai-terminal.png']
    for name in videos + pictures:
        shutil.copyfile(old / name, preview / name)
    html = '<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.5.45 预览</title><style>body{background:#142034;color:#e8edf5;max-width:1300px;margin:auto;padding:24px;font:16px/1.8 sans-serif}img,video{max-width:100%;display:block}a{color:#efd0a0}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}@media(max-width:700px){.grid{display:block}}</style><h1>Unity 0.5.45 预览</h1><p>复用已生成的实际 Unity 画面；积分变化滴声，第一位出现当一次，终局无音效。金色版反色识别配置已修正，手机实扫待验证；默认仍为保留四模块留白的黑白版。</p>'
    for name in videos:
        html += '<h2>' + name + '</h2><video controls playsinline preload="metadata" src="' + name + '"></video>'
    html += '<div class="grid">'
    for name in pictures:
        html += '<figure><a href="' + name + '"><img loading="lazy" src="' + name + '"></a><figcaption>' + name + '</figcaption></figure>'
    html += '</div></html>'
    (preview / 'index.html').write_text(html, 'utf8')
    readme = ROOT / 'README.md'
    current = readme.read_text('utf8')
    heading = f'## Unity {VERSION}'
    if heading in current:
        start = current.index(heading)
        end = current.index('\n## Unity ', start + len(heading))
        current = current[:start] + current[end + 1:]
    intro = f'''## Unity {VERSION} · 二维码、役满与 AI 接管（2026-10-17）

加入相机／相册扫码、放大的邀请二维码及反色识别；原版役满机会／确定、拔剑音效和秋季《金秋送爽》；整庄模式、听牌点击开关、两分接管、离线遮罩、全 AI 保持终局。修正对家立直牌深度、响应提示时序、右侧四按钮间距及结算声音：数字变化滴声，第一位当一次，终局无音效。

[下载 APK](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-{VERSION}/yunjian-riichi-unity-{VERSION}.apk) · [发布页](https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/unity-{VERSION}) · [新对话交接 Markdown](docs/handoff-unity-{VERSION}.md) · [预览](dist/previews/unity-{VERSION}/index.html) · [验证记录](dist/验证记录-unity-{VERSION}.md)

源码包含完整 Unity、Android 服务、离线引擎、资源和测试；APK 通过 Release 分发。Node {count} 项、Unity 单元 {unit} 项及完整 Play／大厅／房间回归通过。versionCode 46，原证书，双架构；签名及完整性通过。Android 真机安装、触摸、相机／相册扫码、声音及多手机局域网尚未实测。

'''
    current = current.replace('# 云间日麻\n\n', '# 云间日麻\n\n' + intro, 1)
    readme.write_text(current, 'utf8')
    print(json.dumps({'version': VERSION, 'nodeTests': count, 'unityUnitChecks': unit, 'unityChecks': total, 'apk': signature}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
