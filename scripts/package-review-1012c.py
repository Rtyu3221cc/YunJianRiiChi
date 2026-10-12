"""Build an offline, zoomable review gallery from actual Unity screenshots."""
from pathlib import Path
import sys,shutil,json,html,zipfile
R=Path(__file__).resolve().parent.parent;D=R/'dist';P=D/'previews';P.mkdir(exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));from PIL import Image,ImageDraw,ImageFont
items=[
 ('排名依次出现','积分计算结束后，隐藏旧名次，按第4→3→2→1位依次播放 APK 原版缩放曲线；三人模式按3→2→1。','01-rank-1'),
 ('九万宝牌判定','视频同时有一万和九万指示牌。三麻中一万指示九万，因此九万发光正确；仅有九万指示牌时九万不发光。','02-one-and-nine-indicators'),
 ('负数分数','原负号贴图为69×27，数字为69×108；保持相同像素比例，积分结算和终局分数均显示正常短横。','03-negative-final'),
 ('开局名牌','增加 APK 原版雀士图标、段位底图和星级；统一显示暂无称号，调整姓名字号、位置及顶部称号留白。','04-intro-badges'),
 ('立直提示同步','立直提示与宣言出牌手部动作在同一事件开始，后续确认事件不再重复播放提示。','05-riichi-simultaneous'),
 ('立直棒随手放置','立直棒排队期间保持隐藏，搬运时只显示手中立直棒，落点交接时才显示桌面立直棒。','06-stick-carried'),
 ('自摸牌位置与朝向','自摸牌按和牌者摊开手牌的实际右端定位，保留间隔；搬运及落地的朝向、尺寸与该家的摊牌一致。','07-tsumo-landed-seat-0'),
 ('立直后的可操作牌','可参与暗杠的同类牌、可拔的北牌及杠／拔北后新摸牌保持正常颜色，其余不能打出的旧牌变暗。','08-riichi-kan'),
 ('等待确认文字','已确认，等待其他玩家字号由17增至22，扩宽文字区域；在936×435与1688×780下检查。','09-waiting-936'),
 ('自家副露布局','以1012-2原版为参考扩大副露，并将其往左、往上留出边距；通过移动手牌让位，保持手牌原大小。','10-melds-1-936'),
 ('胡牌横幅转场','接入 APK UI_CutIn 底图、边框、遮罩布局及1.2秒动画曲线，统一使用一姬；横幅覆盖HUD后再接自摸动作。','11-tsumo-cutin-0.45')]
screens=sorted((R/'build/unity-smoke').glob('review-1012c-*.png'));assert len(screens)>=41
copied=[]
for source in screens:
 name='unity-0.5.30-'+source.name;target=P/name;shutil.copyfile(source,target);copied.append(name)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
sheet=Image.new('RGB',(1440,6*388),(13,20,30));draw=ImageDraw.Draw(sheet)
for i,(title,description,suffix) in enumerate(items):
 x=i%2*720;y=i//2*388;draw.text((x+16,y+12),f'{i+1:02d}  {title}',font=font,fill=(244,210,135))
 source=P/f'unity-0.5.30-review-1012c-{suffix}.png';assert source.exists()
 im=Image.open(source);im.thumbnail((704,330));sheet.paste(im,(x+8+(704-im.width)//2,y+48+(330-im.height)//2))
sheet.save(P/'unity-0.5.30-review-overview.jpg',quality=94)
sections=[]
for i,(title,description,_) in enumerate(items,1):
 files=[n for n in copied if f'-{i:02d}-' in n]
 figures=''.join(f'<button class="thumb" data-src="previews/{n}"><img loading="lazy" src="previews/{n}" alt="第{i}项截图"><span>{html.escape(n.replace("unity-0.5.30-review-1012c-", "").replace(".png", ""))}</span></button>'for n in files)
 sections.append(f'<section id="p{i}"><h2>{i:02d}　{title}</h2><p>{description}</p><div class="grid">{figures}</div></section>')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.5.30 · 11项截图确认</title>
<style>*{box-sizing:border-box}body{margin:0;background:#101722;color:#e7edf5;font:16px/1.7 "Microsoft YaHei",sans-serif}header,main{max-width:1240px;margin:auto;padding:22px}h1{font-size:27px;color:#f1d298}h2{font-size:23px;color:#f1d298}nav{position:sticky;top:0;background:#1c2736;padding:10px;display:flex;justify-content:center;gap:9px;z-index:1}nav a{color:#f1d298;text-decoration:none;padding:2px 9px;border:1px solid #536072;border-radius:5px}section{scroll-margin-top:80px;border-top:1px solid #374357;padding:20px 0}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(285px,1fr));gap:16px}.thumb{padding:0;background:#1b2637;border:1px solid #465269;color:#dee7f2;cursor:zoom-in;border-radius:8px;overflow:hidden}.thumb img{width:100%;display:block}.thumb span{display:block;padding:8px;font-size:13px}.note{color:#b4c3d6}dialog{max-width:98vw;max-height:98vh;background:#0c111a;color:white;border:1px solid #8591a4;padding:12px}dialog::backdrop{background:#000d}dialog img{max-width:94vw;max-height:83vh;display:block;object-fit:contain}dialog button{font-size:18px;color:white;background:#36445a;border:0;padding:8px 18px;cursor:pointer;margin:8px}.bar{display:flex;justify-content:space-between;align-items:center}a{color:#c9e0ff}</style>
<header><h1>云间日麻 Unity 0.5.30 · 11项截图确认</h1><p class="note">2026-10-13　实际 Unity 渲染 · 合成测试局面 · 非安卓真机录像</p><p>点击任意截图查看原图；左右按钮或方向键可切换连续帧。截图集随安装包一同发布。</p><a href="https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.30/yunjian-riichi-unity-0.5.30.apk">安装 APK</a>　<a href="https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/unity-0.5.30/yunjian-riichi-unity-0.5.30-screenshots.zip">下载全部截图</a></header><nav>'''+''.join(f'<a href="#p{i}">{i}</a>'for i in range(1,12))+'''</nav><main>'''+''.join(sections)+'''</main><dialog id="viewer"><div class="bar"><button id="prev">← 上一张</button><span id="counter"></span><button id="next">下一张 →</button><button id="close">关闭</button></div><img id="full" alt="原图"></dialog><script>
const all=[...document.querySelectorAll('.thumb')],v=document.querySelector('#viewer'),full=document.querySelector('#full'),counter=document.querySelector('#counter');let at=0;function show(n){at=(n+all.length)%all.length;full.src=all[at].dataset.src;counter.textContent=(at+1)+' / '+all.length}all.forEach((b,i)=>b.onclick=()=>{show(i);v.showModal()});document.querySelector('#prev').onclick=()=>show(at-1);document.querySelector('#next').onclick=()=>show(at+1);document.querySelector('#close').onclick=()=>v.close();document.onkeydown=e=>{if(v.open&&e.key==='ArrowLeft')show(at-1);if(v.open&&e.key==='ArrowRight')show(at+1)};v.onclick=e=>{if(e.target===v)v.close()};</script></html>'''
gallery=D/'unity-0.5.30-screenshots.html';gallery.write_text(page,encoding='utf8')
with zipfile.ZipFile(D/'yunjian-riichi-unity-0.5.30-screenshots.zip','w',zipfile.ZIP_DEFLATED)as archive:
 archive.write(gallery,gallery.name)
 for name in copied+['unity-0.5.30-review-overview.jpg']:archive.write(P/name,'previews/'+name)
assert all((P/n).is_file()for n in copied)
(D/'previews/unity-0.5.30-screenshot-manifest.json').write_text(json.dumps({'source':'actual Unity rendering of synthetic test scenes','count':len(copied),'files':copied},indent=2)+'\n',encoding='utf8')
print('Packaged',len(copied),'actual screenshots; offline gallery and ZIP ready.')
