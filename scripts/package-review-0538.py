from pathlib import Path
import hashlib, html, json, shutil
from zipfile import ZipFile, ZIP_DEFLATED

root=Path(__file__).resolve().parent.parent
dist=root/'dist';previews=dist/'previews';files=[];pairs=[]
for width in (1688,1280):
    for count in range(1,5):
        before=root/f'dist/previews/unity-0.5.37-review-0537-after-meld-{count}-{width}.png'
        after=root/f'build/unity-smoke/review-0538-own-{count}-{width}.png'
        names=[]
        for source,kind in ((before,'before'),(after,'after')):
            assert source.exists()
            target=previews/f'unity-0.5.38-{kind}-meld-{count}-{width}.png'
            shutil.copyfile(source,target);files.append(target.name);names.append(target.name)
        pairs.append(f'<h2>{count}组副露，{width}px画面</h2><div class="pair">'+''.join(f'<figure><a href="previews/{name}"><img loading="lazy" src="previews/{name}"></a><figcaption>{label}</figcaption></figure>' for name,label in zip(names,('0.5.37','0.5.38 贴靠')) )+'</div>')
for source in sorted((root/'build/unity-smoke').glob('review-0538-*.png')):
    if '-own-' in source.name:continue
    target=previews/('unity-0.5.38-'+source.name);shutil.copyfile(source,target);files.append(target.name)
assert len(files)==19
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0.5.38副露贴靠对照</title><style>body{max-width:1600px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 sans-serif}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}figure{margin:10px 0;padding:10px;background:#1d293a}img{width:100%;height:auto}h1{color:#f1d298}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style><h1>0.5.38 副露贴靠</h1><p>同一副露组内和组间按可见边缘贴靠，不再预留额外空隙。保留0.5.37的牌尺寸、镜头和区域对齐。19张实际Unity渲染截图：8张0.5.37对照，11张本次结果；均为合成测试局面，非真机截图。点击可查看完整尺寸。</p>'''+''.join(pairs)
for name in files:
    if 'review-0538-' in name:page+=f'<figure><a href="previews/{name}"><img loading="lazy" src="previews/{name}"></a><figcaption>{html.escape(name)}</figcaption></figure>'
gallery=dist/'unity-0.5.38-screenshots.html';gallery.write_text(page+'</html>',encoding='utf8')
manifest=previews/'unity-0.5.38-screenshot-manifest.json';manifest.write_text(json.dumps({'actualUnityScreenshots':19,'baselineCommit':'31e30f97b420792ce0bc5267c997150be686ef20','files':{name:hashlib.sha256((previews/name).read_bytes()).hexdigest() for name in files}},indent=2)+'\n',encoding='utf8')
with ZipFile(dist/'yunjian-riichi-unity-0.5.38-screenshots.zip','w',ZIP_DEFLATED) as z:
    z.write(gallery,gallery.name);z.write(manifest,'previews/'+manifest.name)
    for name in files:z.write(previews/name,'previews/'+name)
print('PASS: 19 actual screenshots, before/after gallery and checksum manifest.')
