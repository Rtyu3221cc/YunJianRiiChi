from pathlib import Path
import shutil,zipfile,json
root=Path(__file__).resolve().parent.parent;dist=root/'dist';previews=dist/'previews';files=[]
for source in sorted((root/'build/unity-smoke').glob('review-0536-*.png')):
 target=previews/('unity-0.5.36-'+source.name);shutil.copyfile(source,target);files.append(target.name)
assert len(files)==5
labels=['吃走宣言牌后的横置弃牌','碰走宣言牌后的横置弃牌','明杠拿走宣言牌后的横置弃牌','普通出牌手部动画搬运横置牌','横置牌落地']
figures=''.join(f'<figure><img src="previews/{name}"><figcaption>{label}</figcaption></figure>' for name,label in zip(files,labels))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0.5.36立直标记顺延</title><style>body{max-width:1250px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 "Microsoft YaHei",sans-serif}figure{padding:14px;background:#1d293a}img{max-width:100%}h1{color:#f1d298}</style><h1>0.5.36 立直横置标记顺延</h1><p>宣言牌被拿走后，下一张弃牌横置；随后普通弃牌正常方向。若替代横置牌再次被拿走，标记继续顺延。不会重复立直宣言、扣分或放棒。</p><p>五张实际Unity渲染截图，合成测试局面，非安卓真机截图。</p>'''+figures+'</html>'
gallery=dist/'unity-0.5.36-screenshots.html';gallery.write_text(page,encoding='utf8')
with zipfile.ZipFile(dist/'yunjian-riichi-unity-0.5.36-screenshots.zip','w',zipfile.ZIP_DEFLATED) as z:
 z.write(gallery,gallery.name)
 for name in files:assert(previews/name).exists();z.write(previews/name,'previews/'+name)
(previews/'unity-0.5.36-screenshot-manifest.json').write_text(json.dumps({'actualUnityScreenshots':5,'files':files},indent=2)+'\n',encoding='utf8')
print('PASS: five real screenshots and offline confirmation gallery.')
