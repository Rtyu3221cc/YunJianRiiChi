"""Encode actual Unity capture timestamps; package a local visual review."""
from pathlib import Path
import json, math, os, shutil, sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/video-inspector'))
import av

RELEASE = os.environ.get('RIICHI_REVIEW_RELEASE', '')
SOURCE = ROOT / ('build/ai-settlement-preview/render-0.5.43' if RELEASE else 'build/ai-settlement-preview/render-final')
OUTPUT = ROOT / ('dist/ai-settlement-review-' + RELEASE if RELEASE else 'dist/ai-settlement-review-20261016')
OUTPUT.mkdir(parents=True, exist_ok=True)
SCENARIOS = {
    'four-double-ron': '四人 · 一炮双响',
    'sanma-tsumo': '三人 · 自摸',
    'final-four-tsumo': '四人 · 终局倒计时',
    'four-draw': '四人 · 流局',
}
summaries = []
for scenario, title in SCENARIOS.items():
    directory = SOURCE / scenario
    rows = json.loads((directory / 'frames.json').read_text('utf8'))['frames']
    if RELEASE:
        assert not any(row.get('carriedVisible') and row.get('targetVisible') for row in rows), 'Duplicate carried/landed tile: ' + scenario
        if scenario.endswith('tsumo'):
            for row in rows:
                if row['phase'] == 'result' and row.get('carriedVisible'):
                    assert all(abs(row['carriedPosition'][axis] - row['targetPosition'][axis]) < .0001 for axis in ('x', 'z')), 'Self-draw leaves its reserved slot'
    fps = 24
    start = rows[0]['time']
    duration = rows[-1]['time'] - start + .2
    movie = OUTPUT / (scenario + '.mp4')
    container = av.open(str(movie), 'w', options={'movflags': '+faststart'})
    stream = container.add_stream('libx264', rate=fps)
    stream.width, stream.height = 1280, 720
    stream.pix_fmt = 'yuv420p'
    stream.options = {'crf': '19', 'preset': 'fast'}
    position, loaded, picture = 0, -1, None
    for index in range(math.ceil(duration * fps)):
        at = start + index / fps
        while position + 1 < len(rows) and rows[position + 1]['time'] <= at:
            position += 1
        if position != loaded:
            if picture is not None:
                picture.close()
            with Image.open(directory / rows[position]['file']) as image:
                picture = image.convert('RGB')
            loaded = position
        frame = av.VideoFrame.from_image(picture)
        frame.pts = index
        for packet in stream.encode(frame):
            container.mux(packet)
    for packet in stream.encode():
        container.mux(packet)
    container.close()
    picture.close()
    # Independently decode every frame before handing off the movie.
    with av.open(str(movie)) as check:
        decoded = sum(1 for frame in check.decode(video=0))
        actual_duration = check.duration / av.time_base
    stages = []
    for row in rows:
        if not stages or stages[-1]['stage'] != row['stage']:
            stages.append({'stage': row['stage'], 'at': round(row['time'], 2)})
    for stage in dict.fromkeys(row['stage'] for row in rows):
        candidates = [row for row in rows if row['stage'] == stage]
        choice = candidates[-2] if len(candidates) > 1 else candidates[0]
        if stage == 'final':
            choice = min(candidates, key=lambda row: abs(row['time'] - candidates[0]['time'] - 3))
        if stage == 'win':
            particles = [row for row in candidates if row['winParticles'] > 0]
            if particles:
                choice = max(particles, key=lambda row: row['winParticles'])
        shutil.copyfile(directory / choice['file'], OUTPUT / f'{scenario}-{stage}.png')
    # The review records the actual presentation boundary, including the last
    # cut-in and final sampled particle frame, rather than a guessed timer.
    last_particle = max((row['time'] for row in rows if row['winParticles'] > 0), default=None)
    details_at = next((row['time'] for row in rows if row['stage'].startswith('details')), None)
    next_at = next((row['time'] for row in rows if row['stage'] == 'next-hand'), None)
    summary = {'scenario': scenario, 'title': title, 'duration': round(actual_duration, 2),
               'decodedFrames': decoded, 'stages': stages,
               'lastVisibleParticleAt': last_particle, 'detailsAt': details_at,
               'nextHandAt': next_at,
               'newHandContainsOldWin': any(row['handSeq'] > rows[0]['handSeq'] and row['winEffects'] for row in rows)}
    summaries.append(summary)
    print(json.dumps(summary, ensure_ascii=True), flush=True)

shutil.copyfile(ROOT / 'build/ai-settlement-preview/host-timeline.json', OUTPUT / 'host-timeline.json')
(OUTPUT / 'presentation-timeline.json').write_text(json.dumps(summaries, ensure_ascii=False, indent=2), 'utf8')
sections = []
for item in summaries:
    scenario = item['scenario']
    explanation = {
        'four-double-ron': '两位和牌者依次提示与切入 → 共用点炮牌特效完整播放 → 第一位役种 → 第二位役种 → 分数结算 → 下一局。',
        'sanma-tsumo': '自摸提示、切入与手部动作完整结束，再展示役种与分数，托管自动确认后发下一局。',
        'final-four-tsumo': '和牌与分数结算结束后进入终局，保留“下一局(9)”至 0 的倒计时，再开始下一场。',
        'four-draw': '先展示流局信息与听牌摊牌，再结算分数，完成后自动进入下一局。',
    }[scenario]
    poster = scenario + ('-draw.png' if scenario == 'four-draw' else '-win.png')
    sections.append(f'<section><h2>{item["title"]}</h2><p>{explanation}</p>'
                    f'<video controls playsinline muted preload="metadata" poster="{poster}" src="{scenario}.mp4"></video>'
                    f'<p class="meta">实际速度 · 无声 · {item["duration"]:.1f} 秒</p></section>')

page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>全员托管结算修复 · 2026-10-16</title>
<style>body{margin:0;background:#111a2a;color:#eef1f8;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1280px;margin:auto;padding:24px}h1,h2,a{color:#eed2a0}h1{font-size:27px}h2{font-size:22px}p{color:#c8d3e6}.meta{font-size:14px;color:#a5b5cc}video,img{display:block;width:100%;border-radius:8px}section{margin:28px 0 42px}button{padding:9px 17px;margin:0 10px 10px 0;background:#263c5c;border:1px solid #ba995d;color:white;border-radius:5px;cursor:pointer}.grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0}figcaption{padding:9px}.note{padding:14px 18px;border-left:3px solid #d8b673;background:#1c2b41}@media(max-width:850px){.grid{grid-template-columns:1fr}main{padding:16px}}</style>
<main><h1>全员托管 · 和牌与结算流程修复</h1>
<p>电脑行动等待调整为随机 700–1000ms。在线托管玩家完成演出后再确认结算；换局清空上一局事件、动画、语音和音效。</p>
<p class="note">以下为实际 Unity 客户端连接隔离本地房主服务的录制。预置临近和牌／流局的演示牌面，之后由实际 AI、轮询、演出与确认流程推进；没有通过预览脚本点击跳过或冻结演出时钟。主视角为托管玩家，其他座位为电脑。未打包 APK、未上传 Git、未运行单元测试，尚未真机验证。视频无声，未验证手机音频体验。</p>
<button onclick="speed(1)">正常速度</button><button onclick="speed(.5)">0.5 倍慢放</button><button onclick="step()">暂停并逐帧</button>
''' + '\n'.join(sections) + '''
<section><h2>双响关键帧</h2><div class="grid">
<figure><img src="four-double-ron-win.png"><figcaption>最后一位切入后，点炮牌特效完整播放</figcaption></figure>
<figure><img src="four-double-ron-details-0.png"><figcaption>特效完成后展示第一位役种</figcaption></figure>
<figure><img src="four-double-ron-details-1.png"><figcaption>依次展示第二位役种</figcaption></figure>
<figure><img src="four-double-ron-next-hand.png"><figcaption>新局已开始，旧和牌提示与特效已清空</figcaption></figure>
</div></section><p><a href="presentation-timeline.json">客户端演出时间记录</a> · <a href="host-timeline.json">服务端结算确认记录</a> · <a href="../../docs/ai-settlement-review-20261016.md">修改说明</a></p>
</main><script>function speed(x){document.querySelectorAll('video').forEach(v=>v.playbackRate=x)}function step(){document.querySelectorAll('video').forEach(v=>{v.pause();v.currentTime+=1/24})}</script></html>'''
if RELEASE:
    page = page.replace('未打包 APK、未上传 Git、未运行单元测试，尚未真机验证。',
        '0.5.43 发布复核：自摸牌在预留牌位垂直起落；分数与名次演出结束后，暗色“确认(3)”倒计时到 0 自动确认；终局仍为 9 秒。规则测试与 Unity 专项回归记录随安装包提供，尚未真机验证。')
    page = page.replace('全员托管 · 和牌与结算流程修复', 'Unity ' + RELEASE + ' · 自摸与确认倒计时')
    page = page.replace('../../docs/ai-settlement-review-20261016.md', '../../docs/自摸与结算倒计时-0.5.43.md')
    for name in ('carried-1280.png', 'landed-1280.png', 'confirm-3.png', 'confirm-2.png', 'confirm-1.png', 'passed.txt'):
        shutil.copyfile(ROOT / 'build/win-settlement-0543' / name, OUTPUT / name)
    page = page.replace('</main>', '<section><h2>模型交接与暗色确认按钮专项截图</h2><p>以下为真实 Unity 渲染的合成验证牌面，倒计时时钟冻结用于核对按钮文字；上方流程视频使用正常运行时钟。</p><div class="grid">' +
        ''.join('<figure><img src="' + name + '"><figcaption>' + caption + '</figcaption></figure>' for name, caption in (
            ('carried-1280.png', '自摸动作末段：牌位与尺寸保持一致'), ('landed-1280.png', '交接后：只显示桌面牌'),
            ('confirm-3.png', '确认(3)'), ('confirm-2.png', '确认(2)'), ('confirm-1.png', '确认(1)'))) + '</div><p><a href="passed.txt">984 项专项断言</a></p></section></main>')
(OUTPUT / 'index.html').write_text(page, 'utf8')
print('Review page:', OUTPUT / 'index.html', flush=True)
