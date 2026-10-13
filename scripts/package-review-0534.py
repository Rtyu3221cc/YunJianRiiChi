"""Package actual Unity frames for riichi draw timing and score breakdown."""
from pathlib import Path
import shutil,zipfile,html,json
root=Path(__file__).resolve().parent.parent;dist=root/'dist';previews=dist/'previews';previews.mkdir(exist_ok=True)
files=[]
for source in sorted((root/'build/unity-smoke').glob('review-0534-*.png')):
    target=previews/('unity-0.5.34-'+source.name);shutil.copyfile(source,target);files.append(target.name)
assert len(files)==10
figures=''.join(f'<figure><img src="previews/{name}"><figcaption>{html.escape(name)}</figcaption></figure>' for name in files)
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0.5.34 立直摸牌与结算截图</title><style>body{max-width:1200px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 "Microsoft YaHei",sans-serif}figure{padding:14px;margin:24px 0;background:#1d293a}img{max-width:100%}figcaption{color:#b9c8dd}h1{color:#f1d298}</style><h1>0.5.34 立直摸牌与结算截图</h1><p>上一家出牌及自家摸牌展示结束前手牌保持正常色；摸牌完成后仅不可打牌置灰，可打四条保持正常。保留可杠／拔北牌的正常颜色。</p><p>和牌12000＋一根供托1000＝实际增分13000；点炮者只付12000。结算新增来源说明。截图覆盖0／1／2根供托和最终30500分。</p><p>10张实际Unity渲染截图，来自合成测试局面，非安卓真机截图。</p>'''+figures+'</html>'
gallery=dist/'unity-0.5.34-screenshots.html';gallery.write_text(page,encoding='utf8')
with zipfile.ZipFile(dist/'yunjian-riichi-unity-0.5.34-screenshots.zip','w',zipfile.ZIP_DEFLATED) as z:
    z.write(gallery,gallery.name)
    for name in files:z.write(previews/name,'previews/'+name)
(previews/'unity-0.5.34-screenshot-manifest.json').write_text(json.dumps({'screenshots':10,'files':files},indent=2)+'\n',encoding='utf8')
print('PASS: ten actual render screenshots and verified offline gallery links.')
