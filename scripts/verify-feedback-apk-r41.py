"""Verify the 0.6.39+ Android payload: yakuman R40/R41 fixes and the player-info/rank round.

Unlike verify-feedback-apk-r39.py (fixed 0.6.38 fields), the version is read from the
APK manifest itself and must equal unity/Assets/Editor/ProjectSetup.cs.
Usage: python -X utf8 scripts/verify-feedback-apk-r41.py <apk> <report.json>
"""
from pathlib import Path
from zipfile import ZipFile
import hashlib, json, re, subprocess, sys

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
import UnityPy
apk = Path(sys.argv[1]).resolve(); output = Path(sys.argv[2]).resolve()
U = ROOT / 'unity/Assets'
folder = U / 'Resources/Majsoul/NativeReview/YakumanR39'
setup = (U / 'Editor/ProjectSetup.cs').read_text('utf8')
version = re.search(r'ReleaseVersion="([^"]+)"', setup)[1]; code = int(re.search(r'ReleaseCode=(\d+)', setup)[1])
badging = subprocess.check_output([str(ROOT / 'tools/sdk/android-15/aapt2.exe'), 'dump', 'badging', str(apk)], text=True, encoding='utf-8', errors='replace')
actual = re.search(r"package: name='([^']+)' versionCode='(\d+)' versionName='([^']+)'", badging)
assert actual and actual[1] == 'com.yunjian.riichi.unity', 'Wrong package'
assert int(actual[2]) == code and actual[3] == version, f'APK manifest {actual[3]}/{actual[2]} differs from ProjectSetup {version}/{code}'

def guid(path): return re.search(r'(?m)^guid: ([a-f0-9]{32})$', Path(str(path) + '.meta').read_text('utf8'))[1]

def text_asset(data):
    rows = [o.read().m_Script for o in UnityPy.load(data).objects if o.type.name == 'TextAsset']
    assert len(rows) == 1; return rows[0]

def same_json(path, data):
    if path.read_bytes() in data: return True
    return json.loads(text_asset(data)) == json.loads(path.read_text('utf-8-sig'))

with ZipFile(apk) as archive:
    names = set(archive.namelist())
    def payload(path): return archive.read('assets/bin/Data/' + guid(path))
    metadata = archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    runtime = ['YakumanCinematic', 'TileCenter', 'HandTiles', 'NativeWinningTileQueue', 'MatchIntroduction',
               'RankRules', 'PlayerRecord', 'PlayerTitles', 'DrawPlayerInfo', 'DrawRankTable', 'DrawTitleBook', 'FinalRankBand', 'PublishRankProfile', 'StandingView', 'PlayerRankProfile']
    for value in runtime: assert value.encode() in metadata, 'Missing compiled runtime ' + value
    for value in ['Feedback1114Capture', 'YakumanCinematicCapture', 'CharacterHandGalleryCapture', 'YakumanFilmRunner', 'PlayerRecordCapture', 'PlayerRecordReview']:
        assert value.encode() not in metadata, 'Editor fixture leaked ' + value
    # Host engine: every app asset is packaged byte-for-byte, including the new standings/statistics.
    for file in (ROOT / 'app/src/main/assets').rglob('*'):
        if file.is_file(): assert archive.read('assets/' + file.relative_to(ROOT / 'app/src/main/assets').as_posix()) == file.read_bytes(), 'Stale host input ' + file.name
    engine = archive.read('assets/engine.js')
    for marker in [b'recordHand(result)', b'result.standings=', b'function rankProfile', b'profile:!p.bot&&p.profile||null']:
        assert marker in engine, 'Engine lacks ' + marker.decode()
    # Yakuman scenes: every file packaged; motions and catalogs are the current bytes (R40 tracks, R41 sweep colour).
    checked = 0
    for path in folder.rglob('*'):
        if not path.is_file() or path.suffix == '.meta': continue
        assert 'assets/bin/Data/' + guid(path) in names, 'Missing cinematic asset ' + path.name
        if path.suffix == '.json': assert same_json(path, payload(path)), 'Stale motion/catalog ' + path.name
        checked += 1
    repaired = json.loads((ROOT / 'build/yakuman-alignment-r41/highlight-repair.json').read_text('utf8'))
    sweeps = [r['scene'] for r in repaired]
    for scene in sweeps:
        motion = json.loads(text_asset(payload(folder / (scene + '-motion.json'))))
        assert any(c['kind'] == 3 and c['property'] == '_TintColor' for t in motion['tracks'] for c in t['curves']), 'Sweep colour curve missing ' + scene
    assert len(sweeps) == 6
    assert same_json(U / 'Resources/Majsoul/Characters/framing.json', payload(U / 'Resources/Majsoul/Characters/framing.json')), 'Stale L0 framing'
    # Player info resources and rules.
    profile = U / 'Resources/Majsoul/Profile'; profile_files = 0
    for path in profile.rglob('*'):
        if not path.is_file() or path.suffix == '.meta': continue
        assert 'assets/bin/Data/' + guid(path) in names, 'Missing profile asset ' + path.name
        if path.suffix == '.json': assert same_json(path, payload(path)), 'Stale profile config ' + path.name
        profile_files += 1
    assert profile_files >= 39, "Missing baseline profile resources"
report = dict(localPhoneTestOnly=True, version=version, versionCode=code, manifestVersion=actual[3], manifestVersionCode=int(actual[2]),
              apk=apk.name, bytes=apk.stat().st_size, sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),
              cinematicAssets=checked, sweepColourScenes=sweeps, profileResources=profile_files, compiledRuntime=runtime, androidDeviceTested=False)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', 'utf8')
print(json.dumps(report, ensure_ascii=True))
