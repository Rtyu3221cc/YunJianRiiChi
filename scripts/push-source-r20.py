"""Publish reviewed source/previews only; no APK, Release, or reference bundle upload."""
from pathlib import Path
import importlib.util,json,re,shutil,subprocess,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/source-push-r20';B.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('publish_helper',R/'scripts/prepare-github-0545.py');P=importlib.util.module_from_spec(spec);spec.loader.exec_module(P)
EXPECTED='https://github.com/Rtyu3221cc/YunJianRiiChi.git'
def save(name,value):(B/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def files():
    tracked=set(filter(None,P.git('ls-files','-z',binary=True).decode('utf8').split('\0')));prefix='unity/Assets/CharacterPacks/Source/'
    roles={p[len(prefix):].split('/')[0].removesuffix('.meta')for p in tracked if p.startswith(prefix)}
    all_files=[]
    for file in P.source_paths():
        n=file.relative_to(R).as_posix()
        if n.startswith(prefix)and n[len(prefix):].split('/')[0].removesuffix('.meta')not in roles:continue
        all_files.append(file)
    for directory in ['2026-10-28-wardrobe-r16','2026-10-29-original-input-r18','2026-10-29-fate-r19','2026-10-29-tsumo-r20']:
        all_files.extend(p for p in (R/'dist/previews'/directory).rglob('*')if p.is_file())
    for n in ['dist/unity-0.6.17-verification.json','dist/SHA256-unity-0.6.17.txt']:
        if(R/n).is_file():all_files.append(R/n)
    return all_files,tracked
def prepare():
    assert P.git('remote','get-url','origin')==EXPECTED and P.git('branch','--show-current')=='main'
    assert not P.git('status','--porcelain'),'Publication clone must start clean'
    base=P.git('rev-parse','HEAD');assert P.git('ls-remote','origin','refs/heads/main').split()[0]==base
    all_files,tracked=files();inventory=json.loads((B/'inventory.json').read_text('utf8'));changed={n for n,_ in inventory['new']+inventory['updated']}
    dirty=P.git('diff','--name-only','-z',cwd=R,binary=True).decode('utf8').split('\0');changed.update(dirty)
    candidates=[f for f in all_files if f.relative_to(R).as_posix()in changed or f.relative_to(R).as_posix()not in tracked]
    print('SCANNING',len(candidates),'candidate files',flush=True)
    rows=P.check_files(candidates)
    rows=[r for r in rows if not(P.PUBLISH/r['path']).is_file() or P.sha256(P.PUBLISH/r['path'])!=r['sha256']]
    assert any(r['path']=='docs/handoff-lua-effects-2026-10-29.md'for r in rows)
    assert any(r['path']=='unity/Assets/Scripts/TableEffects.cs'for r in rows)
    save('manifest.json',dict(baseCommit=base,repository=EXPECTED,sourceOnly=True,apkBuilt=False,credentialScanPassed=True,allAllowedFiles=len(all_files),changedFiles=rows,totalBytes=sum(r['bytes']for r in rows)))
    for row in rows:
        src=R/row['path'];dst=P.PUBLISH/row['path'];assert dst.resolve().is_relative_to(P.PUBLISH.resolve())and not dst.is_symlink();dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dst)
    pathspec=B/'paths.bin';pathspec.write_bytes(b'\0'.join(b':(literal)'+r['path'].encode('utf8')for r in rows)+b'\0')
    P.git('add','-f','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul')
    staged=set(filter(None,P.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')));assert staged=={r['path']for r in rows}
    assert not any(Path(n).suffix.lower()in {'.apk','.jks','.keystore','.log','.zip','.majset'}for n in staged)
    print('SOURCE CURATION PASS',len(rows),'files',sum(r['bytes']for r in rows),'bytes',flush=True);print(P.git('diff','--cached','--shortstat'),flush=True)
def test():
    node='C:/Users/Rtyu3221/AppData/Local/Programs/node/node.exe'
    tests=sorted(str(p.relative_to(P.PUBLISH))for p in(P.PUBLISH/'tests').glob('*.test.cjs'))
    with(B/'publication-node-tests.log').open('w',encoding='utf8')as output:subprocess.run([node,'--test','--test-isolation=none',*tests],cwd=P.PUBLISH,stdout=output,stderr=subprocess.STDOUT,check=True)
    print('PUBLICATION TESTS PASS',flush=True)
def commit():
    manifest=json.loads((B/'manifest.json').read_text('utf8'));log=(B/'publication-node-tests.log').read_text('utf8');assert re.search(r'pass\s+185\b',log)and re.search(r'fail\s+0\b',log)
    assert P.git('rev-parse','HEAD')==manifest['baseCommit']
    for row in manifest['changedFiles']:assert P.sha256(P.PUBLISH/row['path'])==row['sha256'],'Publication copy changed: '+row['path']
    staged=set(filter(None,P.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')));assert staged=={r['path']for r in manifest['changedFiles']}
    P.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Port recovered Lua input and Fate effect; enlarge tsumo hand and document effects workflow')
    save('commit.json',dict(commit=P.git('rev-parse','HEAD'),sourceOnly=True));print('SOURCE COMMIT',P.git('rev-parse','HEAD'),flush=True)
def push():
    commit=json.loads((B/'commit.json').read_text('utf8'))['commit'];manifest=json.loads((B/'manifest.json').read_text('utf8'))
    assert P.git('remote','get-url','origin')==EXPECTED and not P.git('status','--porcelain')and P.git('rev-parse','HEAD')==commit
    assert P.git('ls-remote','origin','refs/heads/main').split()[0]==manifest['baseCommit'],'Remote main changed, inspect before push'
    P.git('push','origin','main');assert P.git('ls-remote','origin','refs/heads/main').split()[0]==commit
    save('published.json',dict(repository=EXPECTED,branch='main',commit=commit,sourceOnly=True,apkBuilt=False,releaseCreated=False,artifactRedownloaded=False,remoteFilesVerified=False))
    print('SOURCE PUSH PASS',commit,flush=True)
if __name__=='__main__':
    assert len(sys.argv)==2 and sys.argv[1]in ['prepare','test','commit','push'];globals()[sys.argv[1]]()
