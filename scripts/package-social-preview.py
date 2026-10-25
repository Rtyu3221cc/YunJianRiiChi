"""Package actual Unity emote/preference screenshots for visual review."""
from pathlib import Path
import html,json,re
from PIL import Image,ImageDraw,ImageFont

R=Path(__file__).resolve().parent.parent
B=R/'build/social-preview-1022'
P=R/'dist/previews/2026-10-22-emotes-preferences'
P.mkdir(parents=True,exist_ok=True)
report=json.loads((B/'render/checks.json').read_text('utf8'))
assert 'SOCIAL PREFERENCES PREVIEW PASS ' in (B/'runtime.log').read_text('utf8',errors='replace')
assert 'RIICHI UNITY VALIDATION PASS' in (R/'build/unity-validate.log').read_text('utf8',errors='replace')
node=(B/'node-tests.log').read_text('utf8');assert re.search(r'pass\s+158',node) and re.search(r'fail\s+0',node)
assert (R/'app/src/main/assets/engine.js').read_bytes()==(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').read_bytes()
labels={
 'emotes-four-directions':'四个方向 · 四名雀士各自的角色表情',
 'emote-picker-ready':'表情面板 · 当前角色专属表情，支持滚动',
 'emotes-four-directions-cooldown':'冷却状态 · 面板灰显，禁止连续发送',
 'emote-picker-after-cooldown':'冷却结束 · 恢复彩色与发送操作',
 'preferences-bot-options':'新增偏好页 · 动态服饰、电脑形象选项',
 'preferences-live-off':'动态服饰关闭 · 使用原始静态立绘',
 'preferences-random-on':'电脑形象 · 从已下载雀士中随机',
 'room-random-bots':'添加电脑 · 从本机已有角色中选择不同形象',
 'preferences-16x9':'16:9 设置布局',
}
for kind,title in [('ron','和'),('tsumo','自摸')]:
    for direction,name in enumerate(['自家','右侧','对家','左侧']):labels[f'{kind}-direction-{direction}']=title+' · '+name
for name in labels:
    with Image.open(B/'render'/(name+'.png')) as source:source.convert('RGB').save(P/(name+'.jpg'),quality=91,optimize=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
for kind,title in [('ron','和'),('tsumo','自摸')]:
    sheet=Image.new('RGB',(1440,720),(22,24,34));draw=ImageDraw.Draw(sheet)
    for direction,name in enumerate(['自家','右侧','对家','左侧']):
        image=Image.open(B/'render'/f'{kind}-direction-{direction}.png').convert('RGB');image.thumbnail((710,323));x=direction%2*720;y=direction//2*360;sheet.paste(image,(x,y+32));draw.text((x+10,y+3),title+' · '+name,font=font,fill=(233,199,145))
    sheet.save(P/(kind+'-four-directions.jpg'),quality=93,optimize=True)
rows=[]
for kind in ['ron','tsumo']:
    words=[w for w in report['winWords'] if w['kind']==kind];assert len(words)==4
    assert max(w['width'] for w in words)-min(w['width'] for w in words)<.001
    assert max(w['height'] for w in words)-min(w['height'] for w in words)<.001
    native=315/(130 if kind=='ron' else 128)
    assert all(abs(w['ratio']-native)<.0001 for w in words)
    rows.append(f"<tr><td>{'和' if kind=='ron' else '自摸'}</td><td>{words[0]['width']:.3f} × {words[0]['height']:.3f}</td><td>四方向一致</td><td>{'315 × 130' if kind=='ron' else '315 × 128'}</td></tr>")
cards=[]
order=['emotes-four-directions','emotes-four-directions-cooldown','emote-picker-ready','preferences-bot-options','preferences-random-on','room-random-bots','ron-four-directions','tsumo-four-directions','emote-picker-after-cooldown','preferences-live-off','preferences-16x9']
titles={**labels,'ron-four-directions':'和 · 四个方向尺寸对照','tsumo-four-directions':'自摸 · 四个方向尺寸对照'}
for name in order:cards.append(f'<article><h2>{html.escape(titles[name])}</h2><a href="{name}.jpg"><img loading="lazy" src="{name}.jpg"></a></article>')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 表情与偏好开发预览</title><style>body{margin:0;background:#151722;color:#e8e8ee;font:17px/1.7 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1450px;margin:auto;padding:28px 20px}h1{color:#ebc98f}h2{font-size:21px;color:#ebd5b3}article{background:#222535;padding:16px;margin:24px 0;border-radius:12px}img{width:100%;display:block;border-radius:6px}table{border-collapse:collapse}td,th{padding:8px 15px;border:1px solid #52566a;text-align:left}p{max-width:1100px}a{color:#ebc98f}</style><main><h1>表情系统、偏好设置与和牌指示器</h1><p>2026-10-22 · 开发预览。画面来自实际 Unity 运行，表情发送经过本机房间 HTTP 接口；多人由测试请求模拟。</p><p>表情按本局角色校验和显示，五秒冷却，灰显期间无法继续发送。“偏好”页控制动态服饰及之后新增电脑的形象。随机电脑只从房主本机已安装的雀士中选择，默认使用各角色已选择的服饰。</p><p>158 项 Node 测试、571 项既有 Unity 断言以及本轮实际运行检查均通过。本轮按要求先提供效果图，供确认；手机与多手机热点尚未实测。</p><h2>和／自摸的比例与尺寸检查</h2><table><tr><th>指示器</th><th>当前预览设计宽高</th><th>方向检查</th><th>原始图片像素</th></tr>'''+''.join(rows)+'''</table><p>相同时间点采用同一缩放曲线；每张图保持自身原始比例，并统一显示高度。</p>'''+''.join(cards)+'</main></html>'
(P/'index.html').write_text(page,'utf8')
summary=dict(date='2026-10-22',stage='development-preview',nodeTests=158,existingUnityAssertions=571,runtimeChecks=len(report['checks']),androidDeviceTested=False,newApkBuilt=False,winWords=report['winWords'],preview='dist/previews/2026-10-22-emotes-preferences/index.html')
(P/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n','utf8')
notes=f'''# 表情系统与偏好设置：开发预览

日期：2026-10-22。用户要求“改完先看看效果图”，本轮完成代码与实际运行预览。当前客户端版本常量维持 0.6.8；既有 0.6.8 APK 保持上一轮内容。

## 本轮实现

- 对局表情：右侧原版面板、三列表情网格、滚动、四向头像气泡。使用参考视频 `D:/MyWorkSpace/video/1022/对局中的表情系统.mp4` 和雀魂 APK 的 `bg_chat_content`、`bg_func_big`、`bg_arrow`、`lifu_arrow`、`btn_emo`。
- 发送与显示：服务端由座位的本局冻结角色生成表情身份，校验该角色的表情 ID。客户端请求仅提供数字 ID；无法冒用其他角色。广播使用独立序列，避免影响出牌版本；重复响应不重播，旧表情五秒过期。
- 冷却：服务端每座位五秒冷却；面板使用真正的灰度纹理，冷却结束恢复彩色。面板及关闭手势保护牌桌输入，避免误出牌。
- 和／自摸：修正导入器的 NPOT 自动缩放，保留源图 315×130／315×128。各方向采用同一动画缩放和高度，宽度按原图比例计算。
- 设置：新增“偏好”页，包含动态服饰开关和电脑形象下拉框。动态服饰开关即时控制大厅、宿舍及欣赏页，并与宿舍 LIVE 按钮共用设置；关闭显示原始静态图。
- 电脑形象：默认初始一姬；开启后新增电脑从房主已安装角色中随机，采用各角色已选择的服饰，候选足够时优先避免电脑之间重复。只对之后新增的电脑生效，不改变已冻结的对局身份。
- 解析修正：将 JsonUtility 对 JSON null 生成的零值 resourceSetup 对象归一化为空，保留正 revision 的真实准备状态，防止牌桌控件被错误禁用。

## 核心入口

- `unity/Assets/Scripts/TableEmotes.cs`：气泡、面板、灰显、冷却和交互保护。
- `app/src/main/assets/engine.js`：角色表情白名单、广播、冷却、电脑形象选择。
- `scripts/sync-character-registry.cjs`：同步角色／皮肤与表情 ID 白名单，保持 Android 离线副本一致。
- `AppPreferences.cs`、`NativeSettingsUi.cs`、`CharacterIllustration.cs`：偏好与动态控制。
- `NativeAnnouncements.cs`、`unity/Assets/Editor/TableSocialAssetImport.cs`：和／自摸原始尺寸导入及等比例绘制。
- `RoomProtocol.cs`：公共表情 DTO 及空资源准备状态归一化。

## 实测与交付

Node 158 项通过；既有 Unity 571 项断言通过；本轮实际运行 {len(report['checks'])} 项检查通过，覆盖实际 HTTP 表情发送、四角色气泡、冷却禁止发送、灰度图、LIVE 开关、随机电脑、三／四人指示器比例及四方向大小、16:9 设置和结算关闭面板。

截图：`dist/previews/2026-10-22-emotes-preferences/index.html`。
原始 PNG、日志和测量：`build/social-preview-1022/`。
重跑：先按维护指南准备本地角色包缓存，再运行 `scripts/stage-portrait-review.py` 和 `scripts/run-social-preview.ps1`。后者使用独立测试状态并要求 8765 端口空闲，不会结束其他服务。

尚未进行 Android 真机、多手机热点和触摸滚动实测。
'''
(R/'docs/表情与偏好设置开发预览-2026-10-22.md').write_text(notes,'utf8')
refs=re.findall(r'(?:href|src)="([^"]+)"',page);assert all((P/ref).is_file() for ref in refs)
print(json.dumps(summary,ensure_ascii=False))
