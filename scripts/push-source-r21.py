"""Publish only the reviewed tsumo-contact fix, handoff update and preview."""
from pathlib import Path
import importlib.util,json,shutil,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/source-push-r21';B.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('source_push',R/'scripts/push-source-r20.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old);old.B=B;P=old.P
FILES=['README.md','docs/handoff-lua-effects-2026-10-29.md','docs/自摸落牌时序修正-r21-2026-10-29.md','unity/Assets/Scripts/TableEffects.cs','unity/Assets/Scripts/SourcePlaybackContracts.cs','unity/Assets/Editor/TsumoContactR21Preview.cs','unity/Assets/Editor/TsumoContactR21Preview.cs.meta','scripts/prepare-original-input-stage.py','scripts/mux-recorded-cues.py','scripts/package-tsumo-r21.py','scripts/push-source-r21.py']
def save(name,value):(B/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def prepare():
    assert P.git('remote','get-url','origin')==old.EXPECTED and P.git('branch','--show-current')=='main'
    assert not P.git('status','--porcelain');base=P.git('rev-parse','HEAD');assert P.git('ls-remote','origin','refs/heads/main').split()[0]==base
    assert 'TSUMO R21 PASS 268 frames=498' in(R/'build/tsumo-r21/runtime-retry.log').read_text('utf8',errors='replace')
    rows=P.check_files([R/n for n in FILES]+[p for p in(R/'dist/previews/2026-10-29-tsumo-r21').rglob('*')if p.is_file()]);assert not any(Path(x['path']).suffix.lower()in {'.apk','.zip','.jks','.keystore','.log','.majset'}for x in rows)
    rows=[x for x in rows if not(P.PUBLISH/x['path']).exists()or P.sha256(P.PUBLISH/x['path'])!=x['sha256']]
    save('manifest.json',dict(baseCommit=base,sourceOnly=True,apkBuilt=False,credentialScanPassed=True,changedFiles=rows,totalBytes=sum(x['bytes']for x in rows)))
    for row in rows:
        src=R/row['path'];dst=P.PUBLISH/row['path'];assert dst.resolve().is_relative_to(P.PUBLISH.resolve())and not dst.is_symlink();dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dst)
    pathspec=B/'paths.bin';pathspec.write_bytes(b'\0'.join(b':(literal)'+x['path'].encode('utf8')for x in rows)+b'\0');P.git('add','-f','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul')
    staged=set(filter(None,P.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')));assert staged=={x['path']for x in rows}
    print('R21 CURATION PASS',len(rows),'files;',sum(x['bytes']for x in rows),'bytes',flush=True)
def commit():
    m=json.loads((B/'manifest.json').read_text('utf8'));log=(B/'publication-node-tests.log').read_text('utf8');assert 'pass 185'in log and 'fail 0'in log;assert P.git('rev-parse','HEAD')==m['baseCommit']
    for row in m['changedFiles']:assert P.sha256(P.PUBLISH/row['path'])==row['sha256']
    staged=set(filter(None,P.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')));assert staged=={x['path']for x in m['changedFiles']}
    P.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Fix tsumo slap contact and separate tile settling from hand release')
    sha=P.git('rev-parse','HEAD');save('commit.json',dict(commit=sha,sourceOnly=True));print('R21 COMMIT',sha,flush=True)
if __name__=='__main__':
    assert len(sys.argv)==2 and sys.argv[1]in ['prepare','test','commit','push'];{'prepare':prepare,'test':old.test,'commit':commit,'push':old.push}[sys.argv[1]]()
