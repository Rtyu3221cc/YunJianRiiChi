"""Package the actual Unity resource layout review frames."""
from pathlib import Path
import html, json, re, shutil, sys
R=Path(__file__).resolve().parent.parent
B=R/'build/resource-ui-r3';S=B/'render';revision=sys.argv[1]if len(sys.argv)>1 else'r5';assert revision in ['r3','r4','r5'];D=R/('dist/previews/2026-10-20-resource-ui-'+revision)
report=json.loads((S/'checks.json').read_text('utf8'))
assert len(report['checks'])==15 and 'RESOURCE LAYOUT REVIEW PASS 15'in(B/'review.log').read_text('utf8',errors='replace')
assert not(S/'failed.txt').exists()
D.mkdir(parents=True,exist_ok=True)
groups=[('资源窗口：定位描边与退出后清除',[
    ('standalone-highlight','对勾已去除，恢复头像与条目间距；当前定位描边加粗，右侧为滚动条预留独立间距。列表完整显示五行角色。'),
    ('standalone-reopened','关闭后重新打开：滚动位置保留，定位描边已清除。')]),
    ('宿舍：去除下载对勾',[
    ('dorm-no-checks','已下载及内置角色不再显示额外对勾；保留原本的角色边框和星标。')]),
    ('设置页：完整显示四行角色',[
    ('settings-highlight','说明行已删除，按钮上移，完整显示四行角色；头像和文字大小不变。'),
    ('settings-reopened','退出设置后重新进入：此前的定位描边已清除。')]),
    ('16:9屏幕：压缩底部留白',[
    ('standalone-16x9-highlight','说明文字与关闭按钮共用底栏，列表占用剩余高度。'),
    ('standalone-16x9-reopened','同一界面退出重开后不保留描边。')])]
body=[]
for title,items in groups:
    body.append('<h2>'+html.escape(title)+'</h2><div class="grid'+(' single'if len(items)==1 else'')+'">')
    for name,caption in items:
        p=S/(name+'.png');assert p.is_file();shutil.copyfile(p,D/p.name)
        body.append(f'<figure><a href="{p.name}" target="_blank"><img loading="lazy" src="{p.name}" alt="{html.escape(caption)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>')
    body.append('</div>')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 资源窗口布局修正</title><style>*{box-sizing:border-box}body{margin:0;background:#101726;color:#eee;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1550px;margin:auto;padding:26px 24px 60px}h1{font-size:29px}h2{font-size:22px;margin-top:30px}p,footer{color:#afbed4;line-height:1.85}a{color:#efcd92}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.single{grid-template-columns:1fr;max-width:1100px}figure{margin:0;border:1px solid #3b4961;background:#19253b;border-radius:7px;overflow:hidden}img{display:block;width:100%}figcaption{padding:13px 16px;color:#e9c992;line-height:1.6}footer{margin-top:28px;border-top:1px solid #3b4961;padding-top:18px}@media(max-width:850px){main{padding:18px 12px}.grid{grid-template-columns:1fr}}</style><main><h1>云间日麻 · 资源窗口布局修正</h1><p>已删除资源标题下方的说明，按钮上移，设置页完整显示四行角色。条目、按钮及定位描边的右侧已缩进，滚动条使用独立间距，避免描边与滚动条重叠。宿舍和资源列表不再显示下载对勾，头像间距已恢复；离开界面后清除描边。说明与关闭按钮共用底栏，列表向下扩展。</p><p>下方7张图片均为当前Unity工程实际运行截图。已完成15项相关检查，包含独立窗口及设置页的退出、重开，以及两种屏幕比例。点击图片查看原图。</p>__BODY__<footer>本轮为源码与界面预览更新，尚未重新编译APK或上传GitHub。角色状态由隔离存储中的已验证资源包提供；没有修改已有资源附件或个人安装状态。</footer></main></html>'''.replace('__BODY__',''.join(body))
(D/'index.html').write_text(page,'utf8');shutil.copyfile(S/'checks.json',D/'checks.json')
assert all((D/link).is_file()for link in re.findall(r'(?:href|src)="([^"#]+)"',page))
print('RESOURCE LAYOUT GALLERY PASS',len(list(D.glob('*.png'))),'images;',len(report['checks']),'checks;',D.resolve())
