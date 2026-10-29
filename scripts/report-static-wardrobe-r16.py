"""Package the actual R16 Unity captures and scoped verification evidence."""
from pathlib import Path
import collections,hashlib,html,json,re
R=Path(__file__).resolve().parent.parent
B=R/'build/wardrobe-r16';O=R/'dist/previews/2026-10-28-wardrobe-r16'
audit=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/provenance-r16.json').read_text('utf8'))
catalog=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'))
before=json.loads((B/'catalog-before.json').read_text('utf8'))
counts=collections.Counter(x['category'] for x in catalog['items'] if not x.get('hiddenFromSelection'))
old=collections.Counter(x['category'] for x in before['items'] if not x.get('hiddenFromSelection'))
checks=(O/'capture-checks.txt').read_text('utf8').splitlines()
assert checks and all(x.startswith('PASS ') for x in checks)
assert 'fail 0' in (B/'node-tests-final.log').read_text('utf8')
native=(B/'native-import-checks.txt').read_text('utf8').splitlines()
carryover=(B/'r15-regression/capture-checks.txt').read_text('utf8').splitlines()
assert len(carryover)==58 and all(x.startswith('PASS ') for x in carryover)
assert 'RIICHI UNITY VALIDATION PASS' in (B/'unity-validation.log').read_text('utf8')
labels={'stick':'立直棒','cloth':'静态桌布','tileBack':'牌背','tileFace':'牌面','headFrame':'头像框','lobbyBackground':'大厅背景','music':'立直音乐'}
rows=[dict(category=key,name=name,before=old[key],after=counts[key],added=counts[key]-old[key]) for key,name in labels.items()]
shots=[('01-stick-list','立直棒列表'),('02-goldfish-stick','金龙鱼立直棒预览'),('03-glasses-stick','志村新八立直棒预览'),('03b-fireworks-stick','绚烂烟花立直棒预览'),('04-cloth-preview','桌布预览'),('05-tile-back-preview','天然呆幽灵牌背'),('06-frame-preview','伊丽莎白头像框'),('07-background-preview','听竹别苑背景预览'),('08-riichi-music','立直音乐选择与试听入口'),('09-lobby-background','实际大厅背景'),('10-equipped-table','实际牌桌：桌布、牌背、牌面和四人不同立直棒'),('11-mangan-banner','满贯：保留原版角色横幅'),('12-ordinary-yaku','满贯以下：跳过横幅，进入役种展示'),('13-mangan-yaku','满贯：横幅后的役种展示'),('14-table-music-scrolled','对局音乐：可滚动至末尾曲目')]
for key,_ in shots:assert (O/(key+'.png')).exists(),key
summary=dict(date='2026-10-28',base='Unity 0.6.16',newSelectableItems=sum(r['added'] for r in rows),categories=rows,sourceDefinitions=317,mappedDefinitions=len(audit['mappings']),excluded=audit['issues'],unityRuntimeChecks=len(checks),unityUnitChecks=571,unityCarryoverChecks=len(carryover),nativeImportChecks=native,nodeTests=185,pythonTests=11,newApk=False,published=False)
assets=[]
for folder in ['unity/Assets/Resources/Majsoul/Wardrobe/R16','unity/Assets/Resources/Majsoul/NativeReview/StaticR16']:
    for file in sorted((R/folder).rglob('*')):
        if file.is_file():assets.append(dict(path=file.relative_to(R).as_posix(),bytes=file.stat().st_size,sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
(O/'asset-hashes.json').write_text(json.dumps(assets,ensure_ascii=False,indent=1)+'\n','utf8')
summary['assetHashEntries']=len(assets)
(O/'verification.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n','utf8')
table='| 类别 | 原可选数 | 当前可选数 | 新增 |\n|---|---:|---:|---:|\n'+'\n'.join(f"| {x['name']} | {x['before']} | {x['after']} | {x['added']} |" for x in rows)
excluded='\n'.join(f"- {x['name']}（{x['id']}）：原生多层动态桌布，需要专用渲染流程，本轮静态资源批次未接入。" for x in audit['issues'])
doc=f'''# 云间日麻 · 非特效装扮与结算横幅 R16

日期：2026-10-28。维护工作区：`{R}`。基于交接版 0.6.16，当前为源代码和实际 Unity 预览，未生成新 APK，未发布 GitHub，也没有修改旧发布附件。

## 完成内容

装扮增加 **{summary['newSelectableItems']} 个可选项**，均有实际运行资源；另将已有设置中的大厅／对局音乐由 10 首补齐到 22 首（大厅 9、对局 13），对局音乐列表增加裁切和触摸／滚轮滚动。

{table}

头像框 24 个独立素材覆盖官方公开配置中的 90 条定义；重复赛季奖励共享资源，未伪装成 90 种不同外观。57 条牌背定义映射为 56 个外观。默认立直棒另计，61 条立直棒定义均已接入。

资源优先使用本机 `dist/Majsoul_4.0.16_MC.apk`。该基础 APK 缺少的桌布、背景和较新条目使用已经固定版本的官方原生资源清单 4.0.51／0.16.289 补齐；音乐和少量缺失图片使用已缓存的官方公开资源清单。逐项 item ID → 资源键 → 运行时 ID、图像尺寸、原始 URL、文件 SHA-256 保存在 `unity/Assets/Resources/Majsoul/Wardrobe/provenance-r16.json`。

立直棒使用原始网格、材质贴图、粒子模块和可用的编译动画采样，保留独立批次，不重新生成已确认的和牌／立直／鸣牌特效。新装扮的材质兼容实现属于源参数还原，尚未逐项进行原版录像对照，不宣称所有像素与原版一致。手部仍只显示默认手部和橘猫手，保留历史 ID 兼容。

## 横幅条件

- 满贯以下不播放角色横幅或其音效，也不保留原先横幅占用的等待时间，继续进入报役种页面。
- 满贯、跳满、倍满、三倍满、累计役满和役满保留横幅。按番符和役满倍数判定，不用加了本场棒／供托／庄家倍数的总得点判断。
- 验证了 3 番 60 符、4 番 30 符不触发，3 番 70 符、4 番 40 符和 5 番以上触发；双响按每位和牌者单独判断。
- 原有和牌牌面特效保留。没有增加普通和牌的横幅空等。

牌桌的桌布、牌背和牌面按自己的本局冻结配置读取；修改本地选项、轮询和恢复对局不会替换已经冻结的装扮。四位玩家可分别使用不同的立直棒和头像框。

## 明确未接入

317 条本批定义中，314 条完成映射，剩余 3 条均为多层动态桌布，没有用一张局部贴图冒充完成：

{excluded}

## 验证与交接

- Node：185 项通过，包含新增静态资源来源核对、离线音频、服务器白名单、每局冻结、恢复和未知 ID 拒绝；引擎与 Android 插件副本一致。
- Python：角色输入 8 项、已批准特效的导入兼容门禁 3 项通过。
- Unity 本批运行检查：{len(checks)} 项通过。真实截图见同目录 `index.html`；使用合成公开局面，未读取真实房间或修改手机存档。
- Unity 原有单元检查：62＋87＋385＋37＝571 项通过；听牌／三四人头像框／列表滚动等原有运行检查 58 项通过，新证据单独保存在 `build/wardrobe-r16/r15-regression/`，没有覆盖历史 R15 截图。
- 原生导入记录：`build/wardrobe-r16/native-import-checks.txt`；运行检查：预览目录 `capture-checks.txt`。
- 本轮新资源（含编译后的 prefab、材质、网格和 GUID 文件）逐文件摘要保存在预览目录 `asset-hashes.json`，共 {len(assets)} 项。
- 未生成新 APK，未验证手机触控、GPU、音频播放、热点联机或峰值内存。历史拔北间距基线问题未改动，未将本批检查描述为完整对局全通过。

主要文件：`WardrobeCatalog.cs`、`WardrobeModelVisual.cs`、`WardrobeMotionPreview.cs`、`TableRenderer.cs`、`NativeSettingsUi.cs`、`WinCutIn.cs`、`TableEffects.cs`；新增资源位于 `Wardrobe/R16` 和 `NativeReview/StaticR16`。新材质使用 `YunJian/StaticDecoration` 和 `YunJian/WardrobeParticle`，保留在 `ProjectSetup.cs` 配置中。

可复现命令：

```powershell
python -X utf8 scripts/import-static-wardrobe-r16.py
# 定向补充，避免覆盖其他批次：
python -X utf8 scripts/import-static-wardrobe-r16.py --only liqi_25summer
# Unity -executeMethod：
YunJian.Riichi.Editor.NativeEffectR11Import.RunWardrobeR16
YunJian.Riichi.Editor.WardrobeR16Preview.Run
```

完整导入需要本地 APK、已缓存的配置和 UnityPy 依赖；原始源包仍只作本地参考。Unity 运行时只加载已随项目导入的资源，不在线请求这些 URL。发布时继续沿用 0.6.16 交接约定：源码 ZIP 仅留本地，用户明确要求生成／发布后再做 APK 签名及分发核验。
'''
(R/'docs/handoff-static-wardrobe-r16.md').write_text(doc,'utf8');(O/'说明.md').write_text(doc,'utf8')
figures=''.join(f'<figure><figcaption>{html.escape(title)}</figcaption><a href="{key}.png"><img src="{key}.png" alt="{html.escape(title)}" loading="lazy"></a></figure>' for key,title in shots)
trs=''.join(f"<tr><td>{html.escape(x['name'])}</td><td>{x['after']}</td><td>+{x['added']}</td></tr>" for x in rows)
page=f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · R16 装扮与横幅</title><style>body{{background:#171a22;color:#efe8dc;margin:0;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}}main{{max-width:1400px;padding:30px;margin:auto}}a{{color:#ecc887}}h1{{font-size:28px}}figure{{margin:30px 0}}figcaption{{font-size:20px;margin-bottom:8px}}img{{width:100%;height:auto;border-radius:8px}}.note{{background:#252a35;padding:18px 24px;border-radius:8px}}td,th{{text-align:left;padding:6px 25px;border-bottom:1px solid #45494f}}table{{border-collapse:collapse}}</style><main><h1>云间日麻 · 非特效装扮 R16</h1><p class="note">新增 {summary['newSelectableItems']} 个可选装扮；满贯以下跳过角色横幅，满贯及以上保留。本页均为实际 Unity 游戏截图。185 项引擎、11 项 Python、{len(checks)} 项本批 Unity 运行检查通过；未生成新 APK。</p><p><a href="说明.md">实现范围与后续交接</a> · <a href="verification.json">核验摘要</a> · <a href="capture-checks.txt">Unity 检查记录</a></p><table><tr><th>类别</th><th>当前可选</th><th>新增</th></tr>{trs}</table><p>3 款多层动态桌布尚未接入：捞金鱼、紫霞海岸、星间飞行。新装扮为源参数还原，尚未逐项做原版录像对照。</p>{figures}</main></html>'''
(O/'index.html').write_text(page,'utf8')
print('R16 gallery ready:',len(shots),'actual screenshots;',len(checks),'Unity checks')
