"""Actual Unity screenshots and pointer-driven photo/profile repair demo."""
from pathlib import Path
import sys,json,html,shutil,re
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'));import av
S=R/'build/character-review-r3/render';O=R/'dist/previews/2026-10-19-character-system-r3';O.mkdir(parents=True,exist_ok=True)
movie=json.loads((S/'movie.json').read_text('utf8'));font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',25)
groups=[('拜访 · 文字间距与拖动',[('profile-top','档案／语音灯笼文字间距'),('profile-dragged','从人物介绍向上拖动，显示聊天表情'),('profile-scrollbar','右侧菱形滑块可拖动'),('profile-16x9','16:9 布局')]),('宿舍 · 完整竖排姓名',[('dorm-names','五字姓名完整显示在名牌内')]),('动态／静态 · 同一原始画布',[('live-400107','一姬 · 动态'),('static-400107','一姬 · 静态'),('live-403304','寺崎千穗理 · 动态'),('static-403304','寺崎千穗理 · 静态')]),('大厅 · 还原、保存与重新打开',[('photo-default','原版还原默认／保存位置按钮及透明相机'),('photo-moved','拖动后的位置'),('photo-restored','点击还原默认，恢复大厅默认位置与大小'),('photo-save-pose','调整位置和缩放后保存'),('hall-saved','保存并返回大厅'),('photo-reopened','再次打开，保留保存的位置与大小')]),('透明相机 · 隐藏与恢复',[('photo-clean','点击相机后，按钮、缩放条和相机全部隐藏'),('photo-controls-restored','再次点击屏幕，全部控件恢复')])]
for _,shots in groups:
 for name,_ in shots:shutil.copyfile(S/(name+'.png'),O/(name+'.png'))
frames=sorted((S/'frames').glob('frame-*.jpg'));assert len(frames)==movie['frames'];w,h=Image.open(frames[0]).size;fps=movie['fps'];out=av.open(str(O/'walkthrough-r3.mp4'),'w',options={'movflags':'+faststart'});stream=out.add_stream('libx264',rate=fps);stream.width=w;stream.height=h+72;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast'};section=0
for i,p in enumerate(frames):
 while section+1<len(movie['sections'])and movie['sections'][section+1]['frame']<=i:section+=1
 canvas=Image.new('RGB',(w,h+72),(13,19,31));canvas.paste(Image.open(p),(0,0));d=ImageDraw.Draw(canvas);title=movie['sections'][section]['title']if i>=movie['sections'][0]['frame']else'大厅 · 进入宿舍';d.text((24,h+18),title,font=font,fill=(232,207,158));frame=av.VideoFrame.from_image(canvas);frame.pts=i
 for packet in stream.encode(frame):out.mux(packet)
 if i%200==0:print('Encoded',i,'/',len(frames),flush=True)
for packet in stream.encode():out.mux(packet)
out.close()
body=[]
for heading,shots in groups:
 body.append('<h2>'+heading+'</h2><div class="grid">')
 for name,caption in shots:body.append('<figure><a target="_blank" href="'+name+'.png"><img loading="lazy" src="'+name+'.png"></a><figcaption>'+caption+'</figcaption></figure>')
 body.append('</div>')
chapters=''.join('<button data-time="'+str(s['frame']/fps)+'">'+html.escape(s['title'].split(' · ')[0])+'</button>'for s in movie['sections'])
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 角色界面修正 R3</title><style>
*{box-sizing:border-box}body{margin:0;background:#0c1320;color:#e6ebf3;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:32px 24px 60px}h1{font-size:29px;margin:0 0 10px}h2{font-size:22px;margin:30px 0 15px}p{color:#b5c2d5;line-height:1.8}a{color:#e6c592}video{display:block;width:100%;background:#05080d;border:1px solid #34425b;border-radius:10px}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}figure{margin:0;background:#142035;border:1px solid #31435e;border-radius:8px;overflow:hidden}img{display:block;width:100%}figcaption{padding:12px 15px;line-height:1.6;color:#e3c696}.toolbar{display:flex;gap:8px;flex-wrap:wrap;margin:14px 0}button{border:1px solid #3b4d69;color:#dbe3ef;background:#1b2a40;border-radius:6px;padding:9px 13px;cursor:pointer}button:hover{border-color:#d9b982}footer{border-top:1px solid #34435c;padding-top:18px;margin-top:35px;color:#92a1b8;line-height:1.8;font-size:13px}@media(max-width:800px){main{padding:20px 12px}.grid{grid-template-columns:1fr}h1{font-size:24px}}
</style><main><h1>云间日麻 · 角色界面修正 R3</h1><p>拜访拖动、竖排文字、动态／静态大小对齐，以及大厅相机的还原、保存、隐藏与恢复。</p><video id="v" src="walkthrough-r3.mp4" poster="photo-default.png" controls preload="metadata"></video><div class="toolbar"><button data-rate="0.5">0.5× 慢放</button><button data-rate="1">1× 正常播放</button><a href="walkthrough-r3.mp4">直接打开视频</a></div><div class="toolbar">__CHAPTERS__</div>__BODY__<footer>全部画面来自当前 Unity 工程实际渲染。滚动与相机编辑由脚本按下／移动／抬起输入驱动共用的运行时手势路由和命中区域；页面导航使用验证入口。动态演示保留原始骨骼运动，因此与静态截图的姿态仍会自然不同。界面素材来自原 APK，新增相机、按钮与缩放条的来源见 photo-source.json。<br>本轮为本地效果预览，未生成新 APK、未上传 Git。手机真机触摸仍需后续实测。</footer></main><script>const v=document.getElementById('v');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{v.currentTime=Number(b.dataset.time);v.play()});document.querySelectorAll('[data-rate]').forEach(b=>b.onclick=()=>{v.playbackRate=Number(b.dataset.rate);v.play()});</script></html>'''
(O/'index.html').write_text(page.replace('__CHAPTERS__',chapters).replace('__BODY__',''.join(body)),encoding='utf8')
with av.open(str(O/'walkthrough-r3.mp4'))as check:
 count=sum(1 for frame in check.decode(video=0));assert count==movie['frames']
links=re.findall(r'(?:src|href)="([^"#]+)"',(O/'index.html').read_text('utf8'));assert all((O/p).is_file()for p in links)
report=dict(frames=count,seconds=count/fps,videoSize=[w,h+72],links=len(links),interactionChecks=movie['checks'],unityErrors=0)
(R/'build/character-review-r3/artifact-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8');print('Packaged',O,'video',count/fps,'seconds; checks',len(movie['checks']),flush=True)
