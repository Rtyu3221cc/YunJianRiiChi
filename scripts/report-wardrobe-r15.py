"""Write an auditable offline report from the official resource snapshots."""
from pathlib import Path
from collections import Counter
import hashlib, html, json

R = Path(__file__).resolve().parent.parent
B = R / 'build/wardrobe-r14'
O = R / 'dist/previews/2026-10-28-wardrobe-r15'
def read(path):
    return json.loads(path.read_text(encoding='utf-8'))
tables = read(B / 'current-tables.json')
items = [r for r in tables['item_definition/item'] if r.get('category') == 5]
labels = {0:'立直棒',1:'和牌特效',2:'立直特效',3:'手部配置',4:'立直音乐',5:'头像框',6:'桌布',7:'牌背',8:'大厅背景',9:'大厅／对局背景音乐',10:'鸣牌指示',13:'牌面'}
counts = Counter(r['type'] for r in items)
assert set(counts) == set(labels)
rows = [dict(type=k,category=v,count=counts[k],expiredNameCount=sum('已过期' in r.get('name_chs','') for r in items if r['type']==k)) for k,v in labels.items()]
sizes = read(B / 'size-inputs.json')
bgm = read(B / 'bgm-sizes.json')
assert len(bgm)==22 and all(r.get('bytes',0)>0 and 'error' not in r for r in bgm)
ratio = sizes['referenceApkBytes']/sizes['referenceNativeBytes']
baseline = (R / 'dist/yunjian-riichi-unity-0.6.15.apk').stat().st_size
audio = sum(r['bytes'] for r in sizes['audio']) + sum(r['bytes'] for r in bgm)
calibrated = baseline+sizes['allBundleBytes']*ratio-sizes['referenceApkBytes']+audio
manifest=read(B/'manifest.json')
audit = dict(date='2026-10-28',scope='Official public config records, not current store availability; no extra default placeholders added',
    webVersion=read(B/'web-version.json'), nativeResourceSnapshot=dict(client='4.0.51',resource='0.16.289',bundles=len(manifest['bundleInfos']),assets=len(manifest['assetInfos'])),
    sourceUrls=['https://game.maj-soul.com/1/version.json','https://game.maj-soul.com/1/v0.11.252.w/res/config/lqc.lqbin','https://game.maj-soul.com/assetbundles/DXT/bundle_info_so.majset'],
    sourceSha256={name:hashlib.sha256((B/name).read_bytes()).hexdigest() for name in ['web-version.json','lqc-latest.lqbin','manifest.majset']},
    categories=rows,total=sum(counts.values()),items=[dict(id=r['id'],type=r['type'],name=r.get('name_chs',''),view=r.get('view')) for r in items],
    sizes=dict(baselineApkBytes=baseline,nativeDxtBundleBytes=sizes['allBundleBytes'],riichiAudioBytes=sum(r['bytes'] for r in sizes['audio']),backgroundAudioBytes=sum(r['bytes'] for r in bgm),
        effectNativeReferenceBytes=sizes['referenceNativeBytes'],effectApkReferenceBytes=sizes['referenceApkBytes'],calibrationFactor=ratio,calibratedTotalBytes=round(calibrated),estimatedFinalApkBytes=[650000000,1000000000],
        excluded=['Additional character illustrations/skins/voices/emojis','Loading CG collection'], limitations=['DXT/LZ4 resource size is not Android APK size','Current sample is effects, not a full category import','Some newer native assets have no mapping in the public lqc config','Shared dependencies, atlas-only frames and baked hand motion affect final size']))
O.mkdir(parents=True,exist_ok=True)
(O/'decoration-audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
table='| 类别 | 配置条数 |\n|---|---:|\n'+'\n'.join(f"| {r['category']} | {r['count']} |" for r in rows)
report=f'''# 装扮与结算修正 R15 · 2026-10-28

本轮交付实际 Unity 运行截图，不生成视频或 APK。当前手机上的 0.6.15 不包含本轮源代码修正。

## 修改结果

- 手部选择只保留“默认手部”和“橘猫手”，删除角色性别和当前角色解释；默认手仍随角色性别解析。
- 装扮右侧图标列表在框内裁切并支持手指拖动、滚轮与滑块滚动。
- 点数结算、终局排名按每位玩家的本局装扮绘制头像框；默认框与猫耳框混合使用，已核对三人和四人界面。
- 对照用户的“我们的软件在有胡牌按钮的时候点击听牌指示会自动关闭.mp4”，修正反应阶段轮询清空听牌开关的问题。同一次荣和／自摸决策中，展开、收起和未结束的点击均可跨轮询保留；局面变更、结果页和失去焦点会清除提示。
- 切后台后先等待 120 秒再离线托管；12 秒的在线状态检查不再触发提前接管，后台通知也不再被客户端 Busy 状态丢弃。

Unity 定向检查 58 项通过；后台托管与游戏规则等 Node 检查 181 项通过。引擎源文件与 Android 插件副本 SHA-256 一致。此处未宣称新 APK 或手机安装验证通过。

## 官方公开配置数量

统计日期为 2026-10-28；来自当日核验的官方公开配置 `0.11.252.w`，统计 item_definition/item 的 category=5。以下为定义记录，包含默认、历史、角色专属和可能不可获得的条目，不等于当前寻觅池或商店可购数量；未另行加上未装备占位项。

{table}

合计 **{sum(counts.values())} 条**。其中手部 27 条有 22 条名称标记“已过期”，含男性／女性默认、角色和联动手等内部配置；另外 5 条名称为橘猫爪、夜月猫爪、蓝猫爪、橘色虎爪、立直型强袭猫爪。不能据此理解为界面应列出 27 种普通手部。本项目仍只保留本轮要求的两项。

背景音乐 22 条中，大厅 9 条、对局 13 条；立直音乐 17 条单列。原生资源清单 4.0.51／0.16.289 另有尚未与公开配置映射的条目，因此不把目录数直接当作正式装扮数，也不把本表称为当前原生客户端的完整可获取清单。完整逐条 ID、名称、分类和证据哈希见 decoration-audit.json。

## 全部内置的 APK 大小估算

预计最终 APK 总大小约 **650 MB～1 GB**，以当前资源导入方式估算，尚未全量导入或构建。

- 当前 0.6.15：{baseline:,} 字节，约 {baseline/1e6:.1f} MB／{baseline/1048576:.2f} MiB。
- 相关原生视觉资源去重后约 {sizes['allBundleBytes']/1048576:.2f} MiB；简体中文与公共资源保留，其他语言重复变体去除。包括棒、和牌／立直／鸣牌特效、手、框、桌布、牌背、牌面、大厅背景与背景特效；这部分包含清单中的较新资源，不仅是上述 486 条记录。
- 17 段立直音乐约 {sum(r['bytes'] for r in sizes['audio'])/1048576:.2f} MiB；22 段大厅／对局背景音乐约 {sum(r['bytes'] for r in bgm)/1048576:.2f} MiB，仅通过 HEAD 查询体积，未批量下载音频。
- 已完成的 24 种特效样本，原始相关包 {sizes['referenceNativeBytes']/1048576:.2f} MiB，对应 APK 内资源 {sizes['referenceApkBytes']/1048576:.2f} MiB，约 {ratio:.2f} 倍。用该样本外推、扣除已内置特效后，校准值约 {calibrated/1e6:.0f} MB。
- Android 纹理压缩、图集中的其余头像框、共享依赖和手部逐帧网格烘焙均会改变结果，因此采用 650 MB～1 GB 的区间；650 MB 并非实测全量构建值。没有把 DXT 下载包体积直接当成 APK 体积。

该估算不包含额外全角色立绘、皮肤、语音、表情和全部加载 CG。若也要全部内置，需另算。

官方证据：[版本入口](https://game.maj-soul.com/1/version.json)、[公开配置](https://game.maj-soul.com/1/v0.11.252.w/res/config/lqc.lqbin)、[原生资源清单](https://game.maj-soul.com/assetbundles/DXT/bundle_info_so.majset)。

## 截图

![手部选择](01-hand-options.png)
![列表滚动](02-effects-scrolled.png)
![四人点数结算](03-score-4.png)
![四人终局](04-final-4.png)
![荣和听牌指示](05-waits-ron.png)
![自摸听牌指示](05-waits-tsumo.png)
'''
doc=R/'docs/装扮结算听牌修正与数量估算-r15-2026-10-28.md'
doc.write_text(report,encoding='utf-8');(O/'说明.md').write_text(report,encoding='utf-8')
shots=[('01-hand-options','手部选择：删除多余解释'),('02-effects-scrolled','右侧列表在框内滚动'),('03-score-4','四人点数结算：本局头像框'),('04-final-4','四人终局：本局头像框'),('05-waits-ron','荣和：听牌展开保持到轮询之后'),('05-waits-tsumo','自摸：听牌展开保持到轮询之后'),('03-score-3','三人点数结算'),('04-final-3','三人终局')]
figures=''.join(f'<figure><figcaption>{html.escape(title)}</figcaption><a href="{key}.png"><img src="{key}.png" alt="{html.escape(title)}" loading="lazy"></a></figure>' for key,title in shots)
tr=''.join(f"<tr><td>{html.escape(r['category'])}</td><td>{r['count']}</td></tr>" for r in rows)
page=f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 R15 截图与装扮统计</title><style>body{{margin:0;background:#171a22;color:#efe8dc;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}}main{{max-width:1350px;margin:auto;padding:30px}}h1{{font-size:28px}}a{{color:#ecc887}}img{{display:block;width:100%;height:auto;border-radius:7px}}figure{{margin:26px 0}}figcaption{{font-size:19px;margin-bottom:9px}}table{{border-collapse:collapse;min-width:400px}}td,th{{padding:7px 20px;border-bottom:1px solid #45494f;text-align:left}}.note{{padding:16px 22px;background:#252a35;border-radius:8px}}p{{max-width:1000px}}</style><main><h1>云间日麻 · R15 修改截图</h1><p class="note">真实 Unity 运行截图。58 项定向检查、181 项引擎检查通过。本轮没有视频或新 APK；手机 0.6.15 尚不含本轮改动。</p><p><a href="说明.md">完整说明与统计口径</a> · <a href="decoration-audit.json">逐条配置与估算证据</a> · <a href="capture-checks.txt">截图与交互检查</a></p>{figures}<h2>装扮数量</h2><p>官方公开配置 0.11.252.w 的定义记录，含默认和历史条目。手部 27 条中有 22 条标记“已过期”，不代表 27 种普通可选手部。背景音乐含大厅 9 条、对局 13 条。原生清单的较新未映射条目不计入本表。</p><table><tr><th>类别</th><th>配置条数</th></tr>{tr}</table><p>全部装扮内置后，APK 总大小估计约 <strong>650 MB～1 GB</strong>，当前约 215 MB。没有执行全量构建；不含额外全角色立绘／皮肤／语音／表情和全部加载 CG。</p></main></html>'''
(O/'index.html').write_text(page,encoding='utf-8')
print('R15 report PASS:',sum(counts.values()),'records; calibrated estimate',round(calibrated/1e6),'MB; 8 PNGs')
