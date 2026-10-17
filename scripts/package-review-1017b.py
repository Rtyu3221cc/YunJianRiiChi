"""Publish actual local Unity stills and two short particle rehearsals."""
from pathlib import Path
import json,shutil,sys
R=Path(__file__).resolve().parent.parent;S=R/'build/review-1017b/render';O=R/'dist/preview-20261017'
O.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(R/'tools/video-inspector'))
import av
from PIL import Image
labels={
    'create-full-match':'局数新增整庄，按东、南、西、北四圈进行',
    'wait-button-before-selection':'有可听牌的舍牌时显示原按钮',
    'wait-selected-discard':'选中手牌后自动显示原听牌面板，保持居中',
    'wait-after-discard-hidden':'出牌后本次听牌面板收起，按钮保留',
    'yaku-confirm-3':'役种展示完成后：确认(3)',
    'yaku-confirm-2':'一秒后：确认(2)',
    'yaku-confirm-1':'两秒后：确认(1)',
    'yaku-countdown-to-scores':'三秒后自动进入分数结算',
    'invite-qr-large-1688':'二维码与邀请弹窗均放大 25%',
    'invite-qr-large-1280':'1280 × 720 下的放大效果',
}
for name in labels:shutil.copyfile(S/(name+'.png'),O/(name+'.png'))
for name in ['yakuman-chance','yakuman-certain']:
    shutil.copyfile(S/(name+'.png'),O/(name+'.png'));movie=O/(name+'.mp4');frames=sorted((S/name).glob('frame-*.png'))
    if len(frames)!=36:raise RuntimeError('Incomplete particle rehearsal: '+name)
    container=av.open(str(movie),'w',options={'movflags':'+faststart'});stream=container.add_stream('libx264',rate=20)
    stream.width,stream.height=1688,780;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast'}
    for i,file in enumerate(frames):
        with Image.open(file)as pic:frame=av.VideoFrame.from_image(pic.convert('RGB'))
        frame.pts=i
        for packet in stream.encode(frame):container.mux(packet)
    for packet in stream.encode():container.mux(packet)
    container.close()
music=R/'unity/Assets/Resources/Majsoul/Lobby/Audio/lobby_autumn.mp3';shutil.copyfile(music,O/'lobby_autumn.mp3')
figures=''.join('<figure><a href="'+name+'.png" target="_blank"><img loading="lazy" src="'+name+'.png"></a><figcaption>'+label+'</figcaption></figure>'for name,label in labels.items())
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 本次修改预览</title><style>body{margin:0;background:#121c2d;color:#e8edf5;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:26px}h1,h2,a{color:#efd0a0}h1{font-size:27px}h2{font-size:21px}p{color:#bbc8de}.note{padding:12px 18px;background:#1e2d45;border-left:3px solid #cfaa6b}.grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0;background:#1a2a40;border-radius:6px;overflow:hidden}img,video{display:block;width:100%}figcaption{padding:10px 15px}audio{width:100%;max-width:700px}a:hover{opacity:.9}@media(max-width:850px){.grid{grid-template-columns:1fr}main{padding:16px}}</style><main><h1>云间日麻 · 本次修改预览</h1><p class="note">以下为 Unity 实际渲染的演示牌面。本轮仅编译及界面演练，没有运行单元测试、生成 APK、提交或推送 Git。</p><h2>役满指示特效</h2><div class="grid"><figure><video controls playsinline loop muted preload="metadata" poster="yakuman-chance.png" src="yakuman-chance.mp4"></video><figcaption>役满机会：二、三、五筒中，仅三筒成役满</figcaption></figure><figure><video controls playsinline loop muted preload="metadata" poster="yakuman-certain.png" src="yakuman-certain.mp4"></video><figcaption>役满确定：一万单骑，各种和牌方式均成役满</figcaption></figure></div><p>使用雀魂 APK 原始简体中文粒子图、材质及完整发射参数。APK Lua 仍为加密内容；触发判断接入本项目计分引擎，只读取本人的手牌。</p><h2>大厅音乐：金秋送爽</h2><p>已按你确认的秋季 BGM 接入大厅循环播放，沿用音乐开关及音量设置。</p><audio controls preload="metadata" src="lobby_autumn.mp3"></audio><h2>听牌、整庄、结算与二维码</h2><p>听牌面板保留之前的牌面与剩余张数样式，不显示舍牌建议。加入房间失败提示已改为确认与房主连接同一 Wi-Fi 或同一热点。</p><div class="grid">'''+figures+'''</div></main></html>'''
(O/'index.html').write_text(page,'utf8')
(O/'review-1017b.json').write_text(json.dumps({'unitTestsRun':False,'apkBuilt':False,'gitCommitted':False,'musicConfirmedByUser':True,'qrScale':1.25,'screenshots':list(labels),'particleClips':2,'particleFramesPerClip':36,'apkLuaDecrypted':False},ensure_ascii=False,indent=2)+'\n','utf8')
print(O/'index.html')
