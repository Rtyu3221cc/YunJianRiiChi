from pathlib import Path
import re,json,sys,urllib.request
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'));import av
O=R/'dist/previews/2026-10-19-character-system-r2';S=R/'build/character-review-r2/render';t=(O/'index.html').read_text('utf8');links=re.findall(r'(?:src|href)="([^"#]+)"',t);missing=[p for p in links if not(O/p).is_file()];assert not missing,missing
m=json.loads((S/'movie.json').read_text('utf8'));c=av.open(str(O/'walkthrough-r2.mp4'));v=c.streams.video[0];count=sum(1 for _ in c.decode(video=0));assert count==m['frames'];assert(v.width,v.height)==(1688,840);assert len(list(S.glob('skin-audit-*.png')))==21;assert len(m['checks'])==7;assert(S/'render-notes.txt').read_text().strip().endswith('no APK;');assert 'COMPLETE errors=0'in(R/'build/character-review-r2/recording.log').read_text('utf8')
report=dict(links=len(links),missing=missing,videoFrames=count,videoSeconds=count/m['fps'],videoSize=[v.width,v.height],skinCards=21,interactionChecks=m['checks'],unityErrors=0,apkBuilt=False)
(R/'build/character-review-r2/artifact-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(report,ensure_ascii=False))
