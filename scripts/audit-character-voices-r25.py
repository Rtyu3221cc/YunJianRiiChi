"""Audit every current character's action/yaku voice against real local audio."""
from pathlib import Path
import concurrent.futures, hashlib, json, sys

R = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(R / 'tools/video-inspector'))
import av
import numpy as np

roles = json.loads((R / 'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))['roles']
manifest = json.loads((R / 'unity/Assets/Resources/Majsoul/SettlementVoice/manifest.json').read_text('utf8'))
expected = sorted({x['key'] for x in manifest['clips'] if x['key'].startswith(('fan_', 'gameend_'))} |
                  {'act_rich', 'act_drich', 'act_chi', 'act_pon', 'act_kan', 'act_ron', 'act_tumo', 'act_babei'})
aliases = {'fan_liqi': 'fan_rich', 'fan_dliqi': 'fan_drich', 'fan_zimo': 'fan_tumo'}

def inspect(role):
    folder = R / ('unity/Assets/Resources/Majsoul/Characters' if role['key'] in ('yiji', 'erjietang')
                  else 'unity/Assets/CharacterPacks/Source/' + role['key']) / 'Voice' / role['key']
    clips, missing, errors = [], [], []
    for key in expected:
        source = folder / (key + '.mp3')
        resolved = key
        if not source.is_file() and key in aliases:
            resolved = aliases[key]
            source = folder / (resolved + '.mp3')
        if not source.is_file():
            missing.append(key)
            continue
        samples, peak, rate = 0, 0.0, 0
        try:
            with av.open(str(source)) as audio:
                for frame in audio.decode(audio=0):
                    data = frame.to_ndarray()
                    samples += frame.samples
                    rate = frame.sample_rate
                    peak = max(peak, float(np.max(np.abs(data.astype(np.float32)))))
            if samples == 0 or rate == 0 or peak <= 0.00001:
                raise ValueError('No audible decoded samples')
            clips.append(dict(key=key, resolved=resolved, source=source.relative_to(R).as_posix(),
                              bytes=source.stat().st_size, sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                              duration=round(samples / rate, 4), peak=round(peak, 6)))
        except Exception as error:
            errors.append(dict(key=key, error=str(error)))
    return dict(key=role['key'], name=role['name'], clips=clips, missing=missing, errors=errors)

rows = []
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
    for row in pool.map(inspect, roles):
        rows.append(row)
        if len(rows) % 20 == 0:
            print('VOICE AUDIT', len(rows), '/', len(roles), flush=True)
report = dict(characters=len(rows), expectedPerCharacter=len(expected),
              clipsDecoded=sum(len(x['clips']) for x in rows),
              aliasesResolved=sum(c['key'] != c['resolved'] for x in rows for c in x['clips']),
              missing=sum(len(x['missing']) for x in rows), decodeErrors=sum(len(x['errors']) for x in rows),
              sourceAudioOnly=True, runtimeBundleLookupCheckedSeparately=True, roles=rows)
out = R / 'build/review-r25/voice-audit.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, ensure_ascii=False, indent=1) + '\n', 'utf8')
print(json.dumps({k: v for k, v in report.items() if k != 'roles'}, ensure_ascii=False), flush=True)
assert not report['missing'] and not report['decodeErrors'], 'See per-character audit failures'
