"""Prove that 0.5.39 table/game code remains untouched by the lobby refactor."""
import subprocess,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parent.parent;BASE='codex/backup-before-lobby-1013'
def original(path):return subprocess.check_output(['git','show',BASE+':'+path],cwd=R).replace(b'\r\n',b'\n')
def current(path):return(R/path).read_bytes().replace(b'\r\n',b'\n')
tracked=subprocess.check_output(['git','ls-tree','-r','--name-only',BASE],cwd=R).decode().splitlines();verified=[]
changed=set(subprocess.check_output(['git','diff','--name-only',BASE],cwd=R).decode().splitlines())
for p in tracked:
 if p.startswith(('unity/Assets/Scripts/','unity/Assets/Shaders/','unity/Assets/Resources/Majsoul/'))and p not in ['unity/Assets/Scripts/RiichiClient.cs','unity/Assets/Scripts/RoomProtocol.cs']:
  assert p not in changed,'Protected table file changed: '+p;verified.append(p)
for p,start,end in [
 ('app/src/main/assets/engine.js',' sound(kind',' view(i)'),
 ('unity/Assets/Scripts/RiichiClient.cs','        string selectedFace;','        void EnterHome()'),
 ('unity/Assets/Scripts/RiichiClient.cs','        void DrawTableUI(','        void DrawRoom()'),
 ('unity/Assets/Scripts/RiichiClient.cs','        void OnApplicationPause(','\n    }')]:
 a=original(p).decode('utf8');b=current(p).decode('utf8');assert a[a.index(start):a.index(end,a.index(start))]==b[b.index(start):b.index(end,b.index(start))],p+' protected region changed'
for p in ['app/src/main/assets/core.js','app/src/main/assets/ai.js','app/src/main/assets/table-layout.js']:
 assert original(p)==current(p),p+' changed';verified.append(p)
a=original('app/src/main/assets/engine.js').decode();b=current('app/src/main/assets/engine.js').decode();start="   if(req.op==='profile')";assert a[a.index(start):]==b[b.index(start):],'Existing game dispatch branches changed'
print('PASS:',len(verified),'existing table/resource files and all protected gameplay regions unchanged from',BASE)
out=R/'build/lobby-reference-1013/boundary.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps({'baseline':BASE,'files':verified,'verified':True},ensure_ascii=False,indent=2),encoding='utf8')
