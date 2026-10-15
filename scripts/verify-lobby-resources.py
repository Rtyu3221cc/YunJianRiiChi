"""Check native source provenance, reused assets and final Android packaging."""
from pathlib import Path
import hashlib,json,re,sys
from zipfile import ZipFile
R=Path(__file__).resolve().parent.parent;OUT=R/'unity/Assets/Resources/Majsoul/Lobby'
m=json.loads((OUT/'provenance.json').read_text(encoding='utf8'))
for row in m['assets']+m['derived']:
 assert hashlib.sha256((OUT/row['output']).read_bytes()).hexdigest()==row['sha256'],row['output']+' source hash mismatch'
for row in m['reused']:
 assert hashlib.sha256((R/'unity/Assets/Resources'/row['resource']).with_suffix('.ttf').read_bytes()).hexdigest()==row['sha256'],'Reused font hash mismatch'
motion=json.loads((OUT/'motion.json').read_text());clips={c['name']:c for c in motion['animations']}
assert set(clips)=={'lobby_title_in','lobby_title_out','friend_room_in','friend_room_out','ui_pop','ui_popout'}
assert abs(clips['friend_room_in']['duration']-25/60)<1e-5
assert abs(clips['ui_pop']['duration']-.15)<1e-5
assert any(t['attribute']==1574349066 and t['values'][-1]==0 for t in clips['ui_popout']['tracks'])
if len(sys.argv)>1:
 apk=Path(sys.argv[1]);z=ZipFile(apk);assert z.testzip()is None
 for file in OUT.rglob('*'):
  if not file.is_file()or file.suffix=='.meta':continue
  meta=Path(str(file)+'.meta');guid=re.search(r'guid: (\w+)',meta.read_text()).group(1);entry='assets/bin/Data/'+guid
  assert entry in z.namelist(),'APK missing lobby resource '+str(file)
  if file.suffix in ['.json','.bytes']:assert file.read_bytes()in z.read(entry),'APK has stale lobby resource '+str(file)
  if file.suffix in ['.wav','.mp3']:assert entry+'.resource'in z.namelist(),'APK missing lobby audio data'
 metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
 assert b'LobbyController'in metadata and b'LanRoomDiscovery'in metadata,'Lobby classes stripped from Android build'
print('PASS:',len(m['assets']),'exact APK assets, reused native font, six decoded original animation clips'+(' and final Android packaging'if len(sys.argv)>1 else ''))
