"""Reuse the actual-frame encoder for the native roleset reconstruction review."""
from pathlib import Path
import json,shutil
R=Path(__file__).resolve().parent.parent
source=(R/'scripts/package-character-review-r3.py').read_text('utf8').replace('character-review-r3','character-review-r4').replace('2026-10-19-character-system-r3','2026-10-19-character-system-r4').replace('walkthrough-r3','walkthrough-r4').replace('修正 R3','修正 R4')
groups=[
 ('宿舍／服饰',[('dorm-tabs','角色／装扮文字上移 3px'),('skins-description-top','服饰介绍顶部；移除左右箭头'),('skins-description-scrolled','手指向上拖动后的介绍内容'),('skins-swiped','保留卡片左右滑动切换')]),
 ('切换雀士 · 原版布局',[('switch-main','左侧当前雀士，右侧宿舍；原始比例与九宫格边框'),('switch-character','切换角色，左侧显示名称与初始形象说明'),('switch-skins','点击当前角色，头像下展开服饰'),('switch-equipped','选中服饰后立即应用'),('switch-favorites','星标过滤')]),
 ('指定／随机 · 功能开关',[('random-specified','指定模式：配置区域关闭'),('random-empty','切到随机后，从空列表开始配置'),('random-disabled','切回指定，隐藏配置但保留候选'),('random-restored','再切回随机，恢复候选列表')]),
 ('随机配置 · 雀士与服饰',[('random-skins','在角色头像下展开服饰'),('random-two-skins','同一雀士选择两套服饰，左侧合并并标记数量'),('random-candidates','多名雀士候选'),('random-skins-scrolled','服饰弹层横向拖动'),('random-three-characters','多个雀士／服饰组合'),('random-cleared','清空候选后保持随机模式')]),
 ('16:9 布局',[('switch-16x9','16:9 切换雀士'),('random-16x9','16:9 随机配置')])]
a=source.index('groups=[');b=source.index('\nfor _,shots in groups:',a);source=source[:a]+'groups='+repr(groups)+source[b:]
source=source.replace('拜访拖动、竖排文字、动态／静态大小对齐，以及大厅相机的还原、保存、隐藏与恢复。','宿舍页签文字上移、服饰介绍拖动及移除箭头；按原包和参考视频重做切换雀士／随机配置。')
source=source.replace('poster="photo-default.png"','poster="switch-main.png"')
source=source.replace('滚动与相机编辑由脚本按下／移动／抬起输入驱动共用的运行时手势路由和命中区域；页面导航使用验证入口。动态演示保留原始骨骼运动，因此与静态截图的姿态仍会自然不同。界面素材来自原 APK，新增相机、按钮与缩放条的来源见 photo-source.json。','服饰介绍和弹层滚动由脚本输入驱动共用的运行时手势路由；页面导航与选择使用验证入口。面板、边框、头像标记、按钮和弹层来自原 APK，来源见 roleset-source.json。指定／随机开关独立于候选列表；候选为空时沿用当前指定雀士，对局中的随机固定规则保持不变。')
source=source.replace("else'大厅 · 进入宿舍'","else'宿舍 · 角色与装扮文字上移'")
exec(compile(source,str(R/'scripts/package-character-review-r3.py'),'exec'),{'__file__':str(R/'scripts/package-character-review-r3.py'),'__name__':'__main__'})
O=R/'dist/previews/2026-10-19-character-system-r4';reference=R/'build/character-review-r4/reference-random-9.7.png';shutil.copyfile(reference,O/'original-random-reference.png')
p=O/'index.html';t=p.read_text('utf8');extra='<h2>原版随机配置参考</h2><figure><a target="_blank" href="original-random-reference.png"><img loading="lazy" src="original-random-reference.png"></a><figcaption>来自提供的“大厅随机出战角色功能.mp4”；角色与服饰数量按本工程已解锁内容展示。</figcaption></figure>';p.write_text(t.replace('<footer>',extra+'<footer>'),encoding='utf8')
print('R4 reference included',flush=True)
