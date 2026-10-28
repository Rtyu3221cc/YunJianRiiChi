from pathlib import Path
import hashlib,json,re,subprocess,sys
R=Path(__file__).resolve().parent.parent;O=R/'dist/previews/2026-10-27-wardrobe-r7'
sys.path.insert(0,str(R/'tools/video-inspector'));import av
capture=json.loads((O/'capture.json').read_text('utf8'));assert capture['checks']==184 and capture['frames']==2568 and capture['fps']==30 and not capture['apkBuilt']and not capture['githubUploaded']
seed_records=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/provenance-r7-particles.json').read_text('utf8'))['records']
for record in seed_records:
    assert hashlib.sha256((R/record['source']).read_bytes()).hexdigest()==record['sourceSHA256']
    assert hashlib.sha256((R/'unity/Assets/Resources/Majsoul/Actions'/record['output']).read_bytes()).hexdigest()==record['outputSHA256']
sheet_samples=re.findall(r'actualRow=(\d+) sourceRow=(\d+)',(R/'build/wardrobe-r7-study/sheet-samples.txt').read_text('utf8'))
assert len(sheet_samples)==11 and all(actual==source for actual,source in sheet_samples)
items=json.loads((O/'media.json').read_text('utf8'))
for item in items:
    for key in ['file','close']:
        if item.get(key):assert (O/item[key]).is_file(),item[key]
videos=[]
for path in sorted(O.glob('*.mp4')):
    with av.open(str(path))as c:
        assert c.streams.video;frames=sum(1 for f in c.decode(video=0));assert frames>0,path.name
    videos.append(dict(name=path.name,frames=frames,bytes=path.stat().st_size))
print('R7 complete decode PASS',len(videos),'videos',flush=True)
node=Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
for name in ['index.html','手机版效果对照.html']:
    page=(O/name).read_text('utf8');scripts=re.findall(r'<script>(.*?)</script>',page,re.S);assert len(scripts)==1
    script=R/'build/wardrobe-r7-study'/('phone-script.js'if name!='index.html'else'index-script.js');script.write_text(scripts[0],'utf8');subprocess.run([str(node),'--check',str(script)],check=True,capture_output=True)
    for link in re.findall(r'(?:src|href)="([^"#]+)"',page):
        if not link.startswith(('http:','https:','data:')):assert (O/link).exists(),link
previous=R/'dist/yunjian-riichi-unity-0.6.13.apk';sha=hashlib.sha256(previous.read_bytes()).hexdigest();assert sha=='b076818a12efcfc949e81f832d0eff1db827babcb2e9026f365fae9913e13674'
(O/'validation.json').write_text(json.dumps(dict(runtimeChecks=capture['checks'],capturedFrames=2568,fps=30,videos=videos,nodeTests=10,sourceFidelityAssertions=144,emissionSeedLayers=sum(r['layers']for r in seed_records),sheetRowSamples=len(sheet_samples),pageScriptsPassed=True,localLinksPassed=True,browserAutomationRun=False,apkBuilt=False,githubUploaded=False,previousApkUnchanged=True,previousApkSHA256=sha),ensure_ascii=False,indent=2)+'\n','utf8')
print('R7 media and page verification PASS',flush=True)
