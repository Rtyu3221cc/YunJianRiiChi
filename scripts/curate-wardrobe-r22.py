"""Curate only explicit source trees, split-resource relocations and R22 artifacts."""
from pathlib import Path
import concurrent.futures,importlib.util,json,re,shutil,subprocess,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/source-push-r22';B.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('publish_helper',R/'scripts/prepare-github-0545.py');P=importlib.util.module_from_spec(spec);spec.loader.exec_module(P)
EXPECTED='https://github.com/Rtyu3221cc/YunJianRiiChi.git'
def save(name,value):(B/name).write_text(json.dumps(value,ensure_ascii=False,indent=1)+'\n','utf8')
def prepare():
 assert P.git('remote','get-url','origin')==EXPECTED and P.git('branch','--show-current')=='main'and not P.git('status','--porcelain')
 base=P.git('rev-parse','HEAD');assert P.git('ls-remote','origin','refs/heads/main').split()[0]==base
 verification=json.loads((R/'dist/unity-0.6.18-verification.json').read_text('utf8'));assert verification['apk']['bytes']<=300000000
 assert re.search(r'pass\s+192\b',(R/'build/wardrobe-r22/node-final.log').read_text('utf8'))
 tracked=set(P.git('ls-files','-z',binary=True).decode('utf8').rstrip('\0').split('\0'));prefix='unity/Assets/CharacterPacks/Source/'
 roles={p[len(prefix):].split('/')[0].removesuffix('.meta')for p in tracked if p.startswith(prefix)}
 allowed=[]
 for file in P.source_paths():
  relative=file.relative_to(R).as_posix()
  if relative.startswith(prefix)and relative[len(prefix):].split('/')[0].removesuffix('.meta')not in roles:continue
  allowed.append(file)
 def changed(file):
  target=P.PUBLISH/file.relative_to(R)
  return file if not target.is_file()or file.stat().st_size!=target.stat().st_size or P.sha256(file)!=P.sha256(target)else None
 print('COMPARING',len(allowed),'allowed source files',flush=True)
 with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:candidates=[file for file in pool.map(changed,allowed)if file is not None]
 candidates.extend(p for p in(R/'dist/previews/2026-11-01-wardrobe-r22').rglob('*')if p.is_file())
 for n in ['dist/unity-0.6.18-verification.json','dist/SHA256-unity-0.6.18.txt','dist/README-unity-0.6.18.md']:candidates.append(R/n)
 candidates=list({f.relative_to(R).as_posix():f for f in candidates}.values());print('SCANNING',len(candidates),'R22 source/artifact files',flush=True)
 with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:rows=list(pool.map(lambda file:P.check_files([file])[0],candidates))
 layout=json.loads((R/'docs/wardrobe-resource-layout-r22.json').read_text('utf8'));deleted=[]
 for move in layout['moves']:
  for suffix in ['', '.meta']:
   n='unity/'+move['from']+suffix;target=P.PUBLISH/n
   if target.is_file():assert target.resolve().is_relative_to(P.PUBLISH.resolve())and not target.is_symlink();deleted.append(n)
 save('manifest.json',dict(baseCommit=base,repository=EXPECTED,credentialScanPassed=True,changedFiles=rows,deletedFiles=deleted,totalBytes=sum(r['bytes']for r in rows),apkInGit=False))
 for folder in sorted({(P.PUBLISH/x['path']).parent for x in rows},key=lambda p:len(p.parts)):
  assert folder.absolute().is_relative_to(P.PUBLISH.absolute());folder.mkdir(parents=True,exist_ok=True)
 def copy(row):
  source=R/row['path'];target=P.PUBLISH/row['path'];assert target.resolve().is_relative_to(P.PUBLISH.resolve())and not target.is_symlink(),row['path'];shutil.copy2(source,target)
 with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:
  for i,_ in enumerate(pool.map(copy,rows)):
   if i%2000==0:print('SOURCE COPY',i,'/',len(rows),flush=True)
 for name in deleted:(P.PUBLISH/name).unlink()
 names=[row['path']for row in rows]+deleted;pathspec=B/'paths.bin';pathspec.write_bytes(b'\0'.join(b':(literal)'+n.encode('utf8')for n in names)+b'\0');P.git('add','-f','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul')
 staged=set(P.git('diff','--cached','--no-renames','--name-only','-z',binary=True).decode('utf8').rstrip('\0').split('\0'));assert staged==set(names)
 assert not any(Path(n).suffix.lower()in {'.apk','.zip','.jks','.keystore','.log','.majset'}for n in staged)
 print('R22 SOURCE CURATION PASS',len(rows),'changed,',len(deleted),'removed;',sum(x['bytes']for x in rows),'bytes',flush=True)
def test():
 tests=sorted(str(p.relative_to(P.PUBLISH))for p in(P.PUBLISH/'tests').glob('*.test.cjs'))
 with(B/'publication-node-tests.log').open('w',encoding='utf8')as output:subprocess.run(['C:/Users/Rtyu3221/AppData/Local/Programs/node/node.exe','--test','--test-isolation=none',*tests],cwd=P.PUBLISH,stdout=output,stderr=subprocess.STDOUT,check=True)
 print('PUBLICATION NODE TESTS PASS',flush=True)
def resume():
 m=json.loads((B/'manifest.json').read_text('utf8'));assert P.git('rev-parse','HEAD')==m['baseCommit']and P.git('remote','get-url','origin')==EXPECTED
 rows=m['changedFiles'];current={x['path']:x for x in rows}
 # Only this resumable utility was edited after its interrupted copy.
 current['scripts/curate-wardrobe-r22.py']=P.check_files([R/'scripts/curate-wardrobe-r22.py'])[0]
 rows=list(current.values());m['changedFiles']=rows;m['totalBytes']=sum(x['bytes']for x in rows);save('manifest.json',m)
 roots=P.PUBLISH.resolve()
 # Create parents before resolving targets. Windows can report transient path
 # forms while another thread creates one of the same missing ancestors.
 for folder in sorted({(P.PUBLISH/x['path']).parent for x in rows},key=lambda p:len(p.parts)):
  assert folder.absolute().is_relative_to(P.PUBLISH.absolute());folder.mkdir(parents=True,exist_ok=True)
 def copy(row):
  source=R/row['path'];target=P.PUBLISH/row['path'];assert target.resolve().is_relative_to(roots)and not target.is_symlink(),row['path'];assert P.sha256(source)==row['sha256'],row['path']
  if not target.is_file()or target.stat().st_size!=row['bytes']or P.sha256(target)!=row['sha256']:shutil.copy2(source,target)
 with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:
  for i,_ in enumerate(pool.map(copy,rows)):
   if i%2000==0:print('RESUME SOURCE COPY',i,'/',len(rows),flush=True)
 for name in m['deletedFiles']:
  target=P.PUBLISH/name;assert target.resolve().is_relative_to(roots)and not target.is_symlink()
  if target.exists():target.unlink()
 names=[x['path']for x in rows]+m['deletedFiles'];spec=B/'paths.bin';spec.write_bytes(b'\0'.join(b':(literal)'+x.encode('utf8')for x in names)+b'\0');P.git('add','-f','--pathspec-from-file='+str(spec),'--pathspec-file-nul')
 staged=set(P.git('diff','--cached','--no-renames','--name-only','-z',binary=True).decode('utf8').rstrip('\0').split('\0'));assert staged==set(names)
 print('R22 SOURCE RESUME PASS',len(rows),'changed,',len(m['deletedFiles']),'removed',flush=True)
def commit():
 m=json.loads((B/'manifest.json').read_text('utf8'));log=(B/'publication-node-tests.log').read_text('utf8');assert re.search(r'pass\s+192\b',log)and re.search(r'fail\s+0\b',log);assert P.git('rev-parse','HEAD')==m['baseCommit']
 for row in m['changedFiles']:assert P.sha256(P.PUBLISH/row['path'])==row['sha256'],row['path']
 expected={row['path']for row in m['changedFiles']}|set(m['deletedFiles']);staged=set(P.git('diff','--cached','--no-renames','--name-only','-z',binary=True).decode('utf8').rstrip('\0').split('\0'));assert staged==expected
 P.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Release Unity 0.6.18: downloadable wardrobes and source-backed win effects')
 save('commit.json',dict(commit=P.git('rev-parse','HEAD')));print('R22 SOURCE COMMIT',P.git('rev-parse','HEAD'),flush=True)
def push():
 m=json.loads((B/'manifest.json').read_text('utf8'));sha=json.loads((B/'commit.json').read_text('utf8'))['commit'];assert P.git('rev-parse','HEAD')==sha and not P.git('status','--porcelain');assert P.git('ls-remote','origin','refs/heads/main').split()[0]==m['baseCommit'];P.git('push','origin','main');assert P.git('ls-remote','origin','refs/heads/main').split()[0]==sha;save('published.json',dict(repository=EXPECTED,commit=sha,artifactRedownloaded=False,remoteFilesVerified=False));print('R22 SOURCE PUSH PASS',sha,flush=True)
if __name__=='__main__':{'prepare':prepare,'resume':resume,'test':test,'commit':commit,'push':push}[sys.argv[1]]()
