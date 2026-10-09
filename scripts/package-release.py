"""Package only the explicit source and release files; exclude tools and local state."""
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
apk = dist / 'yunjian-riichi-0.5.0.apk'
unity_apk = dist / 'yunjian-riichi-unity-0.5.0.apk'
assets = root / 'app/src/main/assets'

# Confirm the final signed package contains the current maintained resources.
with ZipFile(apk) as archive:
    for file in assets.rglob('*'):
        if file.is_file():
            name = 'assets/' + file.relative_to(assets).as_posix()
            assert archive.read(name) == file.read_bytes(), 'Stale APK asset: ' + name
manifest = json.loads((assets / 'voice-manifest.json').read_text(encoding='utf8'))
assert manifest['provider'] == 'yiji' and len(manifest['clips']) == 8
for clip in manifest['clips']:
    assert '/audio/sound/yiji/' in clip['source']
    assert hashlib.sha256((assets / clip['webPath']).read_bytes()).hexdigest() == clip['sha256']

for name in ['README.md', 'ARTWORK.md']:
    (dist / name).write_bytes((root / name).read_bytes())
(dist / 'SHA256-0.5.0.txt').write_text(hashlib.sha256(apk.read_bytes()).hexdigest().upper() + '  ' + apk.name + '\n', encoding='utf8')
(dist / '先看这里-0.5.0.txt').write_text(
    '云间日麻 0.5.0\n\n'
    '直接试玩：安装 yunjian-riichi-0.5.0.apk，这是现有 Android/WebView 版。\n'
    '已加入一姬同角色动作配音、雀魂默认桌布，修复立直棒、横置牌重叠与两次点击出牌，放大并调整副露与拔北。\n'
    '所有手机请统一版本。沿用原包名和本机签名，可覆盖安装；请保留原存档。\n\n'
    'Unity：源码包内 unity 文件夹可添加到 Unity Hub，步骤见 unity/README.md。\n'
    '安装 Unity 6 时勾选 Android Build Support、SDK & NDK Tools、OpenJDK。\n'
    '已在 Unity 6000.6.4f1 完成导入、C# 编译与实际 AI 房间出牌测试。\n'
    + ('Unity 试玩包：yunjian-riichi-unity-0.5.0.apk，独立包名，可与旧版并存。\n' if unity_apk.exists() else 'Unity APK 构建状态请查阅验证记录。\n')
    + '安卓真机、热点联机及后台行为尚未验证。\n\n'
    '已执行及未执行的测试见 验证记录-0.5.0.txt；预览截图使用合成布局场景，不是真实牌谱。\n'
    'table-0.5.0-* 是 WebView 界面；unity-table-* 是 Unity 摄像机输出，不包含界面叠层。\n', encoding='utf8')

excluded = {'.git', 'Library', 'Temp', 'Obj', 'Logs', 'UserSettings', 'Builds', 'node_modules', '__pycache__'}
def source_files():
    for folder in ['app', 'scripts', 'tests', 'vendor', 'unity']:
        for file in (root / folder).rglob('*'):
            if not file.is_file() or any(p in excluded for p in file.relative_to(root).parts): continue
            if file.suffix in {'.jks', '.log', '.pyc'}: continue
            yield file
    for name in ['README.md', 'ARTWORK.md', 'build.ps1', 'desktop.cjs', 'package.json', '.gitignore']:
        yield root / name

source = dist / 'yunjian-riichi-source-0.5.0.zip'
with ZipFile(source, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
    for file in sorted(source_files()): archive.write(file, 'lan-riichi/' + file.relative_to(root).as_posix())
release = dist / 'yunjian-riichi-release-0.5.0.zip'
release_files = [apk, *(dist / n for n in ['README.md','ARTWORK.md','SHA256-0.5.0.txt','先看这里-0.5.0.txt','验证记录-0.5.0.txt']), *sorted((dist / 'previews').glob('*'))]
if unity_apk.exists(): release_files += [unity_apk, dist / 'SHA256-unity-0.5.0.txt']
with ZipFile(release, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
    for file in release_files:
        if file.is_file(): archive.write(file, file.relative_to(dist).as_posix())
for package in [source, release]:
    with ZipFile(package) as archive:
        assert archive.testzip() is None
        for name in archive.namelist():
            assert 'Majsoul_4.0.16_MC.apk' not in name and '.desktop-state' not in name and not name.endswith('.jks')
    print(package.name, package.stat().st_size, hashlib.sha256(package.read_bytes()).hexdigest())
print('PASS: APK asset bytes and single-character clip hashes verified; source and release ZIPs verified.')
