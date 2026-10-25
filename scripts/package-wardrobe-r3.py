"""Package Unity captures and both actual table loadout demonstrations."""
from pathlib import Path
import base64,html,json,shutil,sys,io
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/video-inspector'))
import av
source=R/'build/wardrobe-preview-r3/render'
out=R/'dist/previews/2026-10-25-wardrobe-r3'
out.mkdir(parents=True,exist_ok=True)
report=json.loads((source/'capture.json').read_text('utf8'));fps=report['fps']
def encode(name,start,end):
    file=out/(name+'.mp4')
    if '--pages-only' in sys.argv:return
    with av.open(str(file),'w',options={'movflags':'+faststart'}) as container:
        stream=container.add_stream('libx264',rate=fps);stream.width=1688;stream.height=768;stream.pix_fmt='yuv420p';stream.options={'crf':'19','preset':'medium'}
        for i in range(start,end):
            with Image.open(source/'frames'/f'{i:05d}.jpg') as image:
                frame=av.VideoFrame.from_image(image.convert('RGB'));frame.pts=i-start
                for packet in stream.encode(frame):container.mux(packet)
        for packet in stream.encode():container.mux(packet)
    print('Encoded',name,(end-start)/fps,'s',flush=True)
encode('wardrobe-full',0,report['frames'])
for section in report['sections']:encode(section['key'],section['start'],section['end'])
table=next(s for s in report['sections'] if s['key'].startswith('table-'))
encode('table-two-loadouts',table['start'],report['frames'])
for shot in report['shots']:shutil.copyfile(source/(shot['key']+'.png'),out/(shot['key']+'.png'))
shutil.copyfile(source/'capture.json',out/'capture.json')
shutil.copyfile(source/'gameplay-checks.txt',out/'gameplay-checks.txt')
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',26)
for name,reference,current in [('compare-ui','reference-config','01-overview'),('compare-cloth','reference-cloth','cloth-0')]:
    sheet=Image.new('RGB',(1688,1644),'#17151f');draw=ImageDraw.Draw(sheet)
    for row,(path,title) in enumerate([(R/'dist/previews/2026-10-25-wardrobe-r2'/(reference+'.png'),'原版视频参考'),(out/(current+'.png'),'本轮 Unity 实际画面')]):
        y=row*822;draw.text((20,y+10),title,font=font,fill='#efd2a0')
        with Image.open(path) as image:image=image.convert('RGB');image.thumbnail((1688,768));sheet.paste(image,((1688-image.width)//2,y+54))
    sheet.save(out/(name+'.jpg'),quality=94)
def embedded_image(path):
    with Image.open(path) as image:
        image=image.convert('RGB');image.thumbnail((1280,600));buffer=io.BytesIO();image.save(buffer,format='JPEG',quality=85)
    return 'data:image/jpeg;base64,'+base64.b64encode(buffer.getvalue()).decode()
def page(single):
    video='data:video/mp4;base64,'+base64.b64encode((out/'wardrobe-full.mp4').read_bytes()).decode() if single else 'wardrobe-full.mp4'
    nav=''.join('<button data-time="'+str(s['start']/fps)+'">'+html.escape(s['title'])+'</button>' for s in report['sections'])
    photos=''
    for shot in report['shots']:
        url=embedded_image(out/(shot['key']+'.png')) if single else shot['key']+'.png'
        photos+='<figure><img loading="lazy" src="'+url+'"><figcaption>'+html.escape(shot['title'])+'</figcaption></figure>'
    compare=''.join('<figure><img loading="lazy" src="'+(embedded_image(out/(key+'.jpg')) if single else key+'.jpg')+'"><figcaption>'+label+'</figcaption></figure>' for key,label in [('compare-ui','布局与按钮边距对比'),('compare-cloth','桌布预览对比')])
    audio=''
    for key,title in [('music-riichi','立直音乐原曲'),('music-8bit','八位音色')]:
        src=R/'unity/Assets/Resources/Majsoul/Wardrobe/Audio'/(key+'.mp3');shutil.copyfile(src,out/src.name)
        url='data:audio/mpeg;base64,'+base64.b64encode(src.read_bytes()).decode() if single else src.name
        audio+='<p>'+title+'</p><audio controls preload="none" src="'+url+'"></audio>'
    install='<p>APK 单独提供；请安装下载好的 0.6.11 测试包。</p>' if single else '<p><a class="link" href="../../yunjian-riichi-unity-0.6.11.apk">安装 0.6.11 APK</a></p>'
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.6.11 装扮预览</title>
<style>body{margin:0;background:#18161f;color:#eee1c9;font:16px/1.6 system-ui,sans-serif}main{max-width:1280px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:21px;margin-top:28px}video,img{width:100%;display:block;background:#242431;border-radius:8px}video{position:sticky;top:0;z-index:2}nav{display:flex;flex-wrap:wrap;gap:8px;margin:16px 0}button,a.link{border:1px solid #9b7a4c;border-radius:6px;background:#302a37;color:#f1d6a2;padding:9px 12px;cursor:pointer;text-decoration:none;font:inherit}figure{margin:14px 0}figcaption{padding:5px 0;color:#d1b484}p{margin:12px 0}summary{cursor:pointer;padding:12px;background:#292431;border-radius:6px}audio{max-width:100%}.note{color:#c1b7cb;font-size:14px}</style><main><h1>云间日麻 0.6.11 · 装扮实机测试版</h1><p>11 类装扮，24 款资源，每类至少两款。已修正透明预览、保存与关闭按钮边距，并接入实际牌桌动作与特效。</p><p>点击目录可跳到对应分类；末尾两段是进入牌桌后的两套装扮演示。</p><video id="player" controls playsinline preload="metadata" src="'''+video+'''"></video><nav>'''+nav+'''</nav><p class="note">画面来自 Unity 编辑器实际运行。视频不含录音；下方可分别试听两种立直音乐。手机安装、触控和局域网体验由本轮 APK 实机确认。</p><h2>手机切换测试</h2><p>进入“宿舍 → 装扮”，选中资源后点击“保存”，再进入新对局。本人立直后会切换到选定的立直音乐，手牌结束后恢复普通音乐。</p>'''+install+'''<details><summary>查看全部截图</summary>'''+photos+'''</details><details><summary>查看原版视频对比</summary>'''+compare+'''</details><details><summary>试听立直音乐</summary>'''+audio+'''</details><script>const player=document.querySelector('#player');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{player.currentTime=Number(b.dataset.time);player.play()})</script></main></html>'''
(out/'index.html').write_text(page(False),'utf8')
(out/'手机版完整预览.html').write_text(page(True),'utf8')
print('Packaged',report['frames']/fps,'seconds;',len(report['shots']),'screenshots',flush=True)
