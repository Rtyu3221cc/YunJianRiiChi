"""Package the final 0.6.5 Unity screenshots; no synthetic UI renders."""
from pathlib import Path
R=Path(__file__).resolve().parent.parent
s=(R/'scripts/package-character-review-r5.py').read_text('utf8')
s=s.replace("S=R/'build/character-review-r5/render-quick'","S=R/'build/release-065/ui/render-quick'")
s=s.replace('2026-10-19-character-system-r5','2026-10-19-character-system-r6')
s=s.replace("'CHARACTER REVIEW R5 COMPLETE errors=0' in (R/'build/character-review-r5/quick.log')","'CHARACTER REVIEW R6 COMPLETE errors=0' in (R/'build/release-065/ui.log')")
s=s.replace("R/'build/character-review-r5/artifact-checks.json'","R/'build/release-065/ui/artifact-checks.json'")
s=s.replace('雀士弹窗修正 R5','雀士界面 0.6.5')
s=s.replace('使用中文字和数量数字各右移 2px。','使用中文字和数量数字各右移 2px；数量数字再下移 1px；随机滑块与轨道边缘贴合。')
s=s.replace('本轮只交付效果图，未编译 APK、未上传 Git。','0.6.5 发布前实际截图；最终安装包、单元测试和发布记录见本版交接文档。')
s=s.replace('apkBuilt=False,gitUploaded=False','releaseVersion="0.6.5"')
exec(compile(s,str(R/'scripts/package-character-review-r5.py'),'exec'),{'__file__':str(R/'scripts/package-character-review-r5.py'),'__name__':'__main__'})
