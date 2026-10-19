"""Package actual Unity captures, original speech samples, and source statistics."""
from pathlib import Path
import sys,json,shutil,math,hashlib,html
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'))
import av,numpy as np
from PIL import Image,ImageDraw,ImageFont
S=R/'build/characters-1018/render';O=R/'dist/previews/2026-10-18-character-system';O.mkdir(exist_ok=True,parents=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',27);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
def audio_samples(path):
 c=av.open(str(path));res=av.AudioResampler(format='fltp',layout='stereo',rate=48000);parts=[]
 for frame in c.decode(audio=0):
  for f in res.resample(frame):parts.append(f.to_ndarray())
 for f in res.resample(None):parts.append(f.to_ndarray())
 c.close();return np.concatenate(parts,axis=1)if parts else np.zeros((2,0),np.float32)
def encode(frames,dest,sections,fps=20):
 first=Image.open(frames[0]).convert('RGB');w,h=first.size;vh=h+72;vh+=vh%2
 audio=np.zeros((2,math.ceil(len(frames)/fps*48000)),np.float32)
 for section in sections:
  if not section.get('voice'):continue
  p=R/'unity/Assets/Resources/Majsoul/Characters/Voice'/section['character']/(section['voice']+'.mp3')
  speech=audio_samples(p);at=int(section['frame']/fps*48000);n=min(speech.shape[1],audio.shape[1]-at)
  if n>0:audio[:,at:at+n]+=speech[:,:n]*.85
 out=av.open(str(dest),'w',options={'movflags':'+faststart'});video=out.add_stream('libx264',rate=fps);video.width=w;video.height=vh;video.pix_fmt='yuv420p';video.options={'crf':'19','preset':'fast'}
 speech=out.add_stream('aac',rate=48000);speech.layout='stereo';speech.bit_rate=160000
 section_index=0;audio_cursor=0
 for i,p in enumerate(frames):
  while section_index+1<len(sections)and sections[section_index+1]['frame']<=i:section_index+=1
  canvas=Image.new('RGB',(w,vh),(15,19,31));canvas.paste(Image.open(p).convert('RGB'),(0,0));d=ImageDraw.Draw(canvas);title=sections[section_index]['title']if sections else'云间日麻 · 角色系统';d.text((28,h+18),title,font=font,fill=(232,208,161));d.text((w-250,h+25),'Unity 实际画面',font=small,fill=(143,157,179))
  vf=av.VideoFrame.from_image(canvas);vf.pts=i
  for packet in video.encode(vf):out.mux(packet)
  end=min(audio.shape[1],round((i+1)*48000/fps))
  while audio_cursor+1024<=end:
   af=av.AudioFrame.from_ndarray(audio[:,audio_cursor:audio_cursor+1024],format='fltp',layout='stereo');af.sample_rate=48000;af.pts=audio_cursor
   for packet in speech.encode(af):out.mux(packet)
   audio_cursor+=1024
  if i%100==0:print(dest.name,i,'/',len(frames),flush=True)
 if audio_cursor<audio.shape[1]:
  af=av.AudioFrame.from_ndarray(audio[:,audio_cursor:],format='fltp',layout='stereo');af.sample_rate=48000;af.pts=audio_cursor
  for packet in speech.encode(af):out.mux(packet)
 for stream in [video,speech]:
  for packet in stream.encode():out.mux(packet)
 out.close()
def make_site(movie):
 stat=json.loads((R/'build/characters-1018/character-statistics.json').read_text('utf8'));catalog=json.loads((R/'unity/Assets/Resources/Majsoul/Characters/catalog.json').read_text('utf8'))
 shots=[('dorm-mirai','宿舍 · 未来'),('dorm-yiji','宿舍 · 一姬'),('dorm-erjietang','宿舍 · 二阶堂美树'),('dorm-siqiqiansuili','宿舍 · 寺崎千穗理'),('profile-mirai','角色档案 · 五星好感'),('skins-mirai','服饰选择与介绍'),('appreciate-zoom','皮肤鉴赏 · 自由缩放'),('favorites-sort','雀士星标与排序'),('battle-selection','大厅 · 切换角色与服饰'),('battle-random','随机出战列表'),('live-yiji-1','动态皮肤 · 校园微风'),('live-chihori-1','动态皮肤 · 玩转夏日'),('four-characters-table','四名角色同桌'),('call-seat-0','未来 · 立直'),('call-seat-1','寺崎千穗理 · 立直'),('call-seat-2','二阶堂美树 · 立直'),('call-seat-3','一姬 · 立直'),('dorm-16x9','16:9 适配'),('profile-16x9','16:9 档案')]
 for extra in [('profile-expressions','聊天表情 · 全部解锁'),('expressions-yiji','聊天表情 · 一姬'),('expressions-erjietang','聊天表情 · 二阶堂美树'),('expressions-siqiqiansuili','聊天表情 · 寺崎千穗理'),('dorm-reordered','拖动排序后 · 宿舍排列'),('battle-favorites','大厅 · 只看星标雀士'),('match-introduction','开局介绍 · 四名出战雀士')]:
  if (S/(extra[0]+'.png')).exists():shots.insert(5,extra)
 for key,title in shots:shutil.copy2(S/(key+'.png'),O/(key+'.png'))
 shutil.copy2(R/'build/characters-1018/character-statistics.csv',O/'character-statistics.csv')
 sections=''.join('<button data-time="'+str(s['frame']/20)+'">'+html.escape(s['title'])+'</button>'for s in movie['sections'])
 cards=''.join('<figure><a href="'+key+'.png" target="_blank"><img loading="lazy" src="'+key+'.png" alt="'+html.escape(title)+'"></a><figcaption>'+html.escape(title)+'</figcaption></figure>'for key,title in shots)
 limited='、'.join(x['name']for x in stat['limited'])
 skin_rows=''.join('<tr><td>'+r['name']+'</td><td>'+str(len(r['skins']))+'</td><td>'+('、'.join(s['name']for s in r['skins']if s['dynamic'])or'—')+'</td></tr>'for r in catalog['roles'])
 page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 雀士系统预览</title><style>
*{box-sizing:border-box}body{margin:0;background:#0d1420;color:#e4e9f0;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1440px;margin:auto;padding:36px 32px 70px}h1{font-size:30px;font-weight:600;margin:0 0 10px}h2{font-size:22px;margin:35px 0 16px}p{color:#aab8cd;line-height:1.8}a{color:#e6c78e}video{display:block;width:100%;background:#060b12;border-radius:12px;border:1px solid #344155}.tags{display:flex;gap:10px;flex-wrap:wrap;margin:20px 0}.tags span{padding:7px 12px;border:1px solid #43506a;border-radius:7px;font-size:14px;color:#e6c78e}.chapters{display:flex;gap:8px;flex-wrap:wrap;margin:16px 0}.chapters button{background:#1b283d;color:#c7d3e6;border:1px solid #354865;border-radius:6px;padding:9px 12px;cursor:pointer}button:hover{color:#eed7a7;border-color:#c1a363}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}figure{margin:0;background:#172236;border:1px solid #2e3d56;border-radius:8px;overflow:hidden}figure img{display:block;width:100%}figcaption{padding:13px 16px;color:#dfc596}.stats{display:flex;gap:20px;flex-wrap:wrap}.stat{background:#172236;padding:18px 26px;border-radius:9px;min-width:160px}.stat b{font-size:32px;color:#e2c791;display:block}.stat span{color:#aab8cd;font-size:14px}details{padding:16px;background:#172236;border-radius:8px;margin:15px 0}summary{cursor:pointer;color:#e3c99b}table{border-collapse:collapse;width:100%;margin:18px 0}th,td{text-align:left;padding:12px;border-bottom:1px solid #34435b}small{color:#8f9db4}footer{margin-top:35px;padding-top:15px;border-top:1px solid #34435b;font-size:13px;color:#8f9db4;line-height:1.9}@media(max-width:800px){main{padding:20px 14px}.grid{grid-template-columns:1fr}h1{font-size:25px}}
</style><main><h1>云间日麻 · 雀士系统</h1><p>宿舍、换装、档案、鉴赏、星标排序与大厅出战选择。以下均为当前工程的 Unity 实际渲染。</p><div class="tags"><span>四名雀士</span><span>21 套服饰</span><span>2 套原始 Spine 动画</span><span>626 条原版语音</span><span>每局固定出战角色</span></div><video id="mainvideo" controls preload="metadata" poster="dorm-mirai.png" src="walkthrough.mp4"></video><div class="chapters">__CHAPTERS__</div><p>视频末段含四名角色各自的原版立直语音。点击章节按钮可跳转；点击效果图可查看原尺寸。</p><h2>效果图</h2><div class="grid">__CARDS__</div><h2>素材与角色统计</h2><table><thead><tr><th>雀士</th><th>APK 内服饰</th><th>动态服饰</th></tr></thead><tbody>__SKINS__</tbody></table><div class="stats"><div class="stat"><b>86</b><span>官方配置中的女角色</span></div><div class="stat"><b>36</b><span>官方配置中的男角色</span></div><div class="stat"><b>114</b><span>APK 可核对默认立绘的角色</span></div></div><p>统计使用官方 v0.11.252.w 配置快照，共 122 名。以这个 APK 的默认立绘交叉核对，可确认 114 名（女 80、男 34）；另有 8 名仅在该配置中确认。不是实时运营名单。</p><details><summary>APK 可核对角色中的限定标记：48 名</summary><p>__LIMITED__</p><small>依据配置的 limited 字段；联动分类和限定标记单独保留，不能据此保证当前仍可获取。</small></details><p><a href="character-statistics.csv">查看完整逐人统计（CSV）</a></p><footer>立绘、皮肤、贴图、UI 与骨骼来自用户提供的 Majsoul_4.0.16_MC.apk。角色语音经本轮授权从官方资源服务器补齐。APK 缺少寺崎千穗理「共沐夜色」，因此未用其他图片替代。人物与皮肤介绍使用官方同名配置。未恢复的加密 Lua 保留为取证信息，界面流程与局域网适配由本工程实现。原始骨骼由匹配的 Spine 4.2 运行时评估。<br>本次是编辑器效果预览；未生成 APK，未发布 GitHub，也未把本地演示称为多手机实测。</footer></main><script>document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{const v=document.getElementById('mainvideo');v.currentTime=Number(b.dataset.time);v.play()});</script></html>'''
 page=page.replace('__CHAPTERS__',sections).replace('__CARDS__',cards).replace('__SKINS__',skin_rows).replace('__LIMITED__',html.escape(limited));page=page.replace('<span>626 条原版语音</span>','<span>626 条原版语音</span><span>71 张聊天表情</span>').replace('角色语音经本轮授权从官方资源服务器补齐。','角色语音与完整聊天表情经本轮分别授权从官方资源服务器补齐。');(O/'index.html').write_text(page,encoding='utf8')
if __name__=='__main__':
 movie=json.loads((S/'movie.json').read_text('utf8'));frames=sorted((S/'frames').glob('frame-*.jpg'));assert frames,'No recorded Unity frames';encode(frames,O/'walkthrough.mp4',movie['sections']);make_site(movie)
 print('Packaged',O,flush=True)
