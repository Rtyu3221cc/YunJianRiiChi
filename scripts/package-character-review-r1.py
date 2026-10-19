"""A review page for the seven marked corrections; actual Unity renders only."""
from pathlib import Path
import sys,json,html,shutil,math
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'))
import av
from PIL import Image,ImageDraw,ImageFont
S=R/'build/character-review-r1/render';O=R/'dist/previews/2026-10-18-character-system-r1';O.mkdir(exist_ok=True,parents=True)
M=json.loads((S/'movie.json').read_text('utf8'));fps=M['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',26);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',19)
groups=[
 ('1 · 宿舍',[('dorm-main','标题、页签、头像外发光与契约背景'),('dorm-quick-star','直接点击选中头像下的菱形星标')]),
 ('2 · 拜访',[('profile-main','半透明面板、灯笼、原版字体与标题底框'),('profile-emojis','保留全解锁聊天表情'),('profile-equipped','换装后返回档案')]),
 ('3 / 4 · 跳转与服饰',[('skins-current','初始、契约及其余服饰保持固定顺序'),('skins-first','第一套服饰不显示左箭头'),('skins-swiped','滑动后选中契约服饰'),('skins-equipped','已装备：灰色“选择服饰”按钮与完整“使用中”印章')]),
 ('5 · 立绘细看',[('appreciate-default','原版 MAX／MIN、金色滑条与手柄'),('appreciate-zoomed','拖动与缩放；移除多余的加减按钮'),('dorm-after-appreciate','退出鉴赏，恢复宿舍构图')]),
 ('6 · Live2D / 静态切换',[('live-on','三枚按钮均为原版 88×88 资源'),('live-static','OFF：实际显示静态立绘'),('live-restored','LIVE：恢复原始骨骼动画')]),
 ('7 · 雀士排序',[('sort-main','全宽弹窗、星标／非星标分组及底部提示'),('dorm-sorted','长按拖动并确认后的顺序')]),
 ('8 / 9 · 樱花与大厅',[('home-default','大厅默认立绘与宿舍使用相同的实际高度'),('home-edit','相机按钮打开立绘位置／大小设置'),('home-edit-moved','调整位置和大小'),('home-saved','保存并返回大厅'),('home-edit-reopened','再次打开，保留刚才保存的设置')]),
 ('16:9 布局',[('dorm-16x9','16:9 宿舍'),('profile-16x9','16:9 拜访')])]
for _,shots in groups:
 for name,_ in shots:shutil.copy2(S/(name+'.png'),O/(name+'.png'))
frames=sorted((S/'frames').glob('frame-*.jpg'));size=Image.open(frames[0]).size;frames=[p for p in frames if Image.open(p).size==size]
if frames:
 w,h=size;out=av.open(str(O/'walkthrough-r1.mp4'),'w',options={'movflags':'+faststart'});stream=out.add_stream('libx264',rate=fps);stream.width=w;stream.height=h+72;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast'};section=0
 for i,path in enumerate(frames):
  while section+1<len(M['sections'])and M['sections'][section+1]['frame']<=i:section+=1
  canvas=Image.new('RGB',(w,h+72),(13,19,31));canvas.paste(Image.open(path),(0,0));draw=ImageDraw.Draw(canvas);draw.text((24,h+18),M['sections'][section]['title'],font=font,fill=(232,207,158));vf=av.VideoFrame.from_image(canvas);vf.pts=i
  for packet in stream.encode(vf):out.mux(packet)
  if i%150==0:print('Encoded',i,'/',len(frames),flush=True)
 for packet in stream.encode():out.mux(packet)
 out.close()
# A small sequence makes the 0.25-0.333 second transitions inspectable without
# turning isolated preview poses into alleged manual recordings.
strips=[]
for title,start in [('进入宿舍',84),('宿舍进入拜访',148),('档案进入服饰',225)]:
 canvas=Image.new('RGB',(1500,206),(13,19,31));d=ImageDraw.Draw(canvas)
 for j,offset in enumerate([0,3,6,10,15]):
  image=Image.open(frames[min(start+offset,len(frames)-1)]).convert('RGB');image.thumbnail((300,150));canvas.paste(image,(j*300,30));d.text((j*300+12,183),f'{offset/fps:.2f} s',font=small,fill=(210,215,225))
 d.text((12,2),title,font=small,fill=(232,207,158));name='transition-'+str(start)+'.jpg';canvas.save(O/name,quality=94);strips.append((name,title))
body=[]
for heading,shots in groups:
 body.append('<h2>'+html.escape(heading)+'</h2><div class="grid">')
 for name,caption in shots:body.append('<figure><a href="'+name+'.png" target="_blank"><img loading="lazy" src="'+name+'.png" alt="'+html.escape(caption)+'"></a><figcaption>'+html.escape(caption)+'</figcaption></figure>')
 body.append('</div>')
chapters=''.join('<button data-time="'+str(s['frame']/fps)+'">'+html.escape(s['title'].split(' · ')[0])+'</button>'for s in M['sections'])
transitions=''.join('<figure class="strip"><a href="'+name+'" target="_blank"><img src="'+name+'" alt="'+title+'"></a><figcaption>'+title+' · 真实录制帧</figcaption></figure>'for name,title in strips)
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 界面修正 R1</title><style>
*{box-sizing:border-box}body{margin:0;background:#0c1320;color:#e6ebf3;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1480px;margin:auto;padding:32px 28px 65px}h1{font-size:30px;font-weight:600;margin:0 0 10px}h2{font-size:22px;margin:32px 0 15px}p{color:#aebcd1;line-height:1.8}a{color:#e4c28d}video{width:100%;background:#03070e;border:1px solid #34425b;border-radius:10px;display:block}.toolbar{display:flex;gap:8px;flex-wrap:wrap;margin:14px 0}button{background:#1b2a40;color:#dbe3ef;border:1px solid #3b4d69;border-radius:6px;padding:9px 13px;cursor:pointer}button:hover{border-color:#d9b982;color:#e9c995}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}figure{margin:0;background:#152137;border:1px solid #2c3d58;border-radius:8px;overflow:hidden}img{display:block;width:100%}figcaption{padding:12px 15px;color:#e0c397;line-height:1.6}.strip{margin:15px 0}.badge{display:inline-block;border:1px solid #42536c;border-radius:5px;padding:6px 10px;margin:3px 6px 13px 0;color:#d7bb8b;font-size:14px}footer{border-top:1px solid #34435c;padding-top:18px;margin-top:32px;font-size:13px;color:#92a1b8;line-height:1.9}@media(max-width:800px){main{padding:20px 12px}.grid{grid-template-columns:1fr}h1{font-size:25px}}
</style><main><h1>云间日麻 · 界面修正 R1</h1><p>按本轮七张标注图修正。下面是当前工程的实际 Unity 截图与交互演示。</p><span class="badge">宿舍 / 拜访 / 服饰 / 鉴赏 / 排序</span><span class="badge">非牌桌樱花</span><span class="badge">大厅立绘设置与保存</span><video id="video" src="walkthrough-r1.mp4" poster="dorm-main.png" controls preload="metadata"></video><div class="toolbar"><button data-rate="0.5">0.5× 慢放动画</button><button data-rate="1">1× 正常播放</button><button data-rate="2">2× 快速浏览</button><a href="walkthrough-r1.mp4">直接打开视频</a></div><div class="toolbar">__CHAPTERS__</div><h2>页面转场逐帧查看</h2>__TRANSITIONS____BODY__<footer>保留原有出战切换面板与牌桌，本轮没有继续调整这两部分。语音灯笼保留外观，功能仍按前次范围暂不开放。立绘、按钮、灯笼、木牌翻面帧、滑条、字体和樱花图集来自所提供的雀魂 APK；宿舍淡入／淡出与面板横移采用原包动画曲线。<br>本次仅生成本地效果预览，没有生成 APK、发布 GitHub或改写用户个人选择。视频由实际 Unity 帧组成，录制脚本驱动页面操作；不冒称手机真机触摸录屏。</footer></main><script>const v=document.getElementById('video');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{v.currentTime=Number(b.dataset.time);v.play()});document.querySelectorAll('[data-rate]').forEach(b=>b.onclick=()=>{v.playbackRate=Number(b.dataset.rate);v.play()});</script></html>'''
(O/'index.html').write_text(page.replace('__CHAPTERS__',chapters).replace('__TRANSITIONS__',transitions).replace('__BODY__',''.join(body)),encoding='utf8')
print('Packaged',O,'frames',len(frames),'seconds',len(frames)/fps,flush=True)
