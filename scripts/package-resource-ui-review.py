"""Package the actual Unity resource UI review frames."""
from pathlib import Path
import html, json, re, shutil
R=Path(__file__).resolve().parent.parent
B=R/'build/resource-ui-r2';S=B/'render';D=R/'dist/previews/2026-10-20-resource-ui-r2'
report=json.loads((S/'checks.json').read_text('utf8'))
assert len(report['checks'])==31 and 'RESOURCE UI REVIEW PASS 31'in(B/'review.log').read_text('utf8',errors='replace')
assert not(S/'failed.txt').exists()
D.mkdir(parents=True,exist_ok=True)
groups=[('字体与表情边框',[
    ('dorm-unavailable-font','“未下载”使用原版毛笔字体'),
    ('profile-thin-emoji-frames','聊天表情恢复原版3px切片边框'),
    ('dorm-loaded-check','宿舍中的可用角色显示绿色对勾')]),
    ('独立资源窗口',[
    ('fuji-download-confirm','点击宿舍头像确认下载大小'),
    ('standalone-focused-progress','开始下载后自动定位并高亮该角色；按钮等宽'),
    ('standalone-loaded-check','加载完成后头像下方显示绿色对勾，独立窗口和关闭按钮已放大')]),
    ('设置中的资源列表',[
    ('settings-equal-buttons','“部分下载”、等宽按钮与“热门角色”文案'),
    ('settings-focused-progress','从设置下载时保持在设置页面，并自动定位目标条目'),
    ('settings-loaded-check','成功加载后的绿色对勾'),
    ('settings-marker-after-delete','删除后对勾消失，清理出战、星标和随机候选')]),
    ('加载失败与屏幕比例',[
    ('standalone-load-failure','模拟损坏资源包：移除安装标记与对勾，提示重新下载'),
    ('standalone-16x9','16:9比例下的独立窗口')])]
body=[]
for title,items in groups:
    body.append('<h2>'+html.escape(title)+'</h2><div class="grid">')
    for name,caption in items:
        source=S/(name+'.png');assert source.is_file();shutil.copyfile(source,D/source.name)
        body.append(f'<figure><a href="{source.name}" target="_blank"><img loading="lazy" src="{source.name}" alt="{html.escape(caption)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>')
    body.append('</div>')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 资源界面修正</title><style>*{box-sizing:border-box}body{margin:0;background:#101726;color:#eee;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1550px;margin:auto;padding:26px 24px 60px}h1{font-size:29px}h2{font-size:22px;margin-top:30px}p,footer{color:#afbed4;line-height:1.85}a{color:#efcd92}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}figure{margin:0;border:1px solid #3b4961;background:#19253b;border-radius:7px;overflow:hidden}img{display:block;width:100%}figcaption{padding:13px 16px;color:#e9c992;line-height:1.6}footer{margin-top:28px;border-top:1px solid #3b4961;padding-top:18px}@media(max-width:850px){main{padding:18px 12px}.grid{grid-template-columns:1fr}}</style><main><h1>云间日麻 · 资源界面修正</h1><p>统一“未下载”字体、修复表情粗边框，“推荐下载”改为“部分下载”。资源窗口增大、五个按钮等宽、条目文字下移5px，文案使用“热门角色”。下载后自动定位并高亮目标；资源成功加载才显示对勾，删除或加载失败后移除。</p><p>本次通过31项针对性检查，包括下载定位、加载成功、重启后恢复标记、删除清理、损坏包拒绝与当帧安全回退。下方12张图片均由实际Unity工程运行生成，可点击查看原图。</p>__BODY__<footer>本轮更新为源码与实际效果图，尚未重新编译APK或发布GitHub。下载与故障检查使用本机真实HTTP资源服务和隔离存储；损坏包截图为主动构造的故障测试。尚未进行Android真机触摸验证。</footer></main></html>'''.replace('__BODY__',''.join(body))
(D/'index.html').write_text(page,'utf8');shutil.copyfile(S/'checks.json',D/'checks.json')
assert all((D/link).is_file()for link in re.findall(r'(?:href|src)="([^"#]+)"',page))
print('RESOURCE UI GALLERY PASS',len(list(D.glob('*.png'))),'images;',len(report['checks']),'checks;',D.resolve())
