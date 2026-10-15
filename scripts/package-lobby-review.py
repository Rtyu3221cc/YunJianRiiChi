"""Make a local review gallery and signed APK review bundle, without publishing."""
from pathlib import Path
import hashlib,json,html,shutil
from zipfile import ZipFile,ZIP_DEFLATED
from importlib import import_module
R=Path(__file__).resolve().parent.parent;D=R/'dist';V='0.5.40'
images=R/'build/lobby-reference-1013/rendered';out=D/'lobby-0.5.40/previews';out.mkdir(parents=True,exist_ok=True)
groups=[('大厅','home-1920','home-2400','returned-home'),('场次入口','friend-1920','ranked','competition'),('创建房间','create-1920','advanced-1920'),('加入与说明','join-keypad','ranked-help','rules-four','rules-three','rules-detail'),('等待室','room-empty-three','room-empty-four','room-ready-three','room-guest-unready','room-guest-ready','room-kick-confirm'),('退出与开局','guest-leave-confirm','room-leave-confirm','existing-table-handoff')]
labels={'home-1920':'大厅 16:9','home-2400':'大厅宽屏','returned-home':'退出后的大厅','friend-1920':'友人场','ranked':'段位场','competition':'比赛场','create-1920':'创建表单','advanced-1920':'高级设置','join-keypad':'加入数字键盘','ranked-help':'感叹号说明','rules-four':'四人规则图文','rules-three':'三人规则图文','rules-detail':'实际规则说明','room-empty-three':'三人等待室','room-empty-four':'四人等待室','room-ready-three':'三人准备完成','room-guest-unready':'真人未准备','room-guest-ready':'真人准备完成','room-kick-confirm':'踢出确认','guest-leave-confirm':'真人退房确认','room-leave-confirm':'房主解散确认','existing-table-handoff':'交接原牌桌'}
cards=[]
for group,*names in groups:
 for name in names:
  src=images/(name+'.png');assert src.exists(),src
  shutil.copyfile(src,out/src.name)
  cards.append(f'<figure data-group="{group}"><a href="lobby-0.5.40/previews/{src.name}"><img loading="lazy" src="lobby-0.5.40/previews/{src.name}" alt="{html.escape(labels[name])}"></a><figcaption>{html.escape(labels[name])}</figcaption></figure>')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.5.40 大厅与房间</title><style>body{margin:0;background:#111a2c;color:#eee;font:16px/1.6 "Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:28px}h1{font-size:25px;color:#f0cf8c}p{color:#c0c9dc}nav{position:sticky;top:0;padding:12px 0;background:#111a2c;display:flex;gap:10px;flex-wrap:wrap}button{border:1px solid #8d7546;border-radius:6px;background:#1c2941;color:#f2dbb4;padding:9px 15px;cursor:pointer}button.active{background:#7c572b}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:20px}figure{margin:0;background:#1c2941;border-radius:8px;overflow:hidden}img{width:100%;display:block}figcaption{padding:10px 16px}a{color:#efce8c}</style><main><h1>云间日麻 0.5.40 大厅与房间</h1><p>实际 Unity 渲染。保持 0.5.39 牌桌和打牌逻辑；高级规则按确认保留实际默认值。99 项规则／协议测试、192 项大厅专项检查通过。安卓真机安装、触摸及多手机热点尚未测试。点击图片查看原图。</p><nav>'''
for group in ['全部']+[g[0]for g in groups]:page+=f'<button onclick="filter(this)"'+(' class="active"'if group=='全部'else '')+'>'+group+'</button>'
page+='</nav><div class="grid">'+''.join(cards)+'''</div></main><script>function filter(b){document.querySelectorAll('button').forEach(x=>x.classList.toggle('active',x===b));document.querySelectorAll('figure').forEach(x=>x.style.display=b.textContent==='全部'||x.dataset.group===b.textContent?'':'none')}</script></html>'''
gallery=D/'unity-0.5.40-lobby-review.html';gallery.write_text(page,encoding='utf8')
apk=D/f'yunjian-riichi-unity-{V}.apk';signature=import_module('verify-apk-signature').verify_signature(apk)
report=D/'lobby-0.5.40-verification.json';report.write_text(json.dumps(dict(apk=signature,engineTests=99,lobbyChecks=192,protectedFiles=1402,packagedUnchangedTextures=310,baseline='face364ea2688d1944f64809b3a3021524987d07',physicalAndroid=False),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
doc=D/'主界面与房间重构-1013.md';shutil.copyfile(R/'docs'/doc.name,doc)
package=D/f'yunjian-riichi-unity-{V}-lobby-review.zip'
with ZipFile(package,'w',ZIP_DEFLATED)as z:
 for file in [apk,gallery,report,doc,D/'SHA256-unity-0.5.40.txt',*sorted(out.glob('*.png'))]:z.write(file,file.relative_to(D).as_posix())
with ZipFile(package)as z:
 assert z.testzip()is None
 assert hashlib.sha256(z.read(apk.name)).hexdigest()==signature['sha256']
print('PASS: local review gallery, signed APK review ZIP, embedded APK hash identical')
print(package.name,package.stat().st_size)
