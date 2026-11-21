"""Record event coverage against the checked-in character voice directory."""
from pathlib import Path
from collections import Counter
from datetime import datetime, timedelta, timezone
import hashlib
import json

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'unity/Assets/Resources/Majsoul/Characters/voice-directory.json'
directory = json.loads(SOURCE.read_text(encoding='utf-8'))
manifest = json.loads((ROOT / 'unity/Assets/Resources/Majsoul/SettlementVoice/manifest.json').read_text(encoding='utf-8'))
settlement = {x['key'] for x in manifest['clips']}
actions = {'act_rich', 'act_drich', 'act_chi', 'act_pon', 'act_kan', 'act_babei', 'act_ron', 'act_tumo'}
events = {'lobby_playerlogin', 'lobby_normal', 'lobby_room_in', 'lobby_room_ready', 'ingame_start', 'game_top', 'game_top_ron', 'game_top_big', 'ingame_lianda', 'ingame_baopai', 'ingame_remain10', 'ingame_yiman', 'ingame_beiman'}
social = {'lobby_selfintro', 'lobby_gift', 'lobby_gift_favor', 'lobby_levelup0', 'lobby_levelup1', 'lobby_levelup2', 'lobby_levelup3', 'lobby_levelmax', 'lobby_qiyue'}


def category(line):
    if line['type'].startswith('scfan_'):
        return 'sichuan_mode_not_implemented'
    if line['type'] in social:
        return 'social_progression_not_implemented'
    if line['path'].endswith('lobby_playerlogin_max'):
        return 'bond_login_not_implemented'
    if line['type'] in settlement | actions | events:
        return 'event_supported'
    raise ValueError(f"Unclassified voice: {line['type']} / {line['path']}")


rows = []
for role in directory['roles']:
    counts = Counter(category(line) for line in role['lines'])
    assert sum(counts.values()) == len(role['lines'])
    rows.append({'role': role['key'], 'lines': len(role['lines']), 'available': sum(x['available'] for x in role['lines']), 'coverage': dict(counts)})
reference = next(role for role in directory['roles'] if role['key'] == 'yiji')
report = {
    'schema': 1,
    'date': datetime.now(timezone(timedelta(hours=9))).strftime('%Y-%m-%d'),
    'source': str(SOURCE.relative_to(ROOT)).replace('\\', '/'),
    'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    'roleCount': len(rows),
    'definitionCount': sum(x['lines'] for x in rows),
    'roles': rows,
    'referenceRole': 'yiji',
    'referenceLines': [dict(line, coverage=category(line)) for line in reference['lines']],
    'policy': {
        'login': 'Once after the startup cover clears; ordinary login variant.',
        'interaction': 'All eight available lobby interaction lines cycle on taps and character changes.',
        'start': 'Local role, once per match, after the introduction; no reconnect replay.',
        'draw': 'Local tenpai/noten or nagashi mangan; abort initiator when supplied.',
        'final': 'First place role; last-hand baiman-or-higher win selects big, otherwise last-hand ron selects ron, otherwise generic. This is this project policy.',
        'special': 'Local player only; each type at most once per hand; calls and win voices take priority.',
        'waits': 'Committed local discard and viewer-only waits; visible dora and nuki included, hidden ura excluded.',
        'unimplemented': 'The 23 lines without matching game systems remain available on the manual voice page.',
    },
}
target = ROOT / f"docs/character-voice-trigger-audit-{report['date']}.json"
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
print(json.dumps({'report': str(target.relative_to(ROOT)), 'roles': len(rows), 'definitions': report['definitionCount'], 'referenceCoverage': rows[0]['coverage']}, ensure_ascii=True))
