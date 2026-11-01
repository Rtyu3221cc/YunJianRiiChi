"""Publish the verified 0.6.18 APK/ZIP; local source ZIP is never uploaded."""
from pathlib import Path
import hashlib,importlib.util,json,urllib.error,urllib.parse,urllib.request
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';TAG='unity-0.6.18'
spec=importlib.util.spec_from_file_location('github_r22',R/'scripts/github-release-0545.py');g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)
def sha(path):
 h=hashlib.sha256()
 with path.open('rb')as f:
  for block in iter(lambda:f.read(1048576),b''):h.update(block)
 return h.hexdigest()
def main():
 v=json.loads((R/'dist/unity-0.6.18-verification.json').read_text('utf8'));package=json.loads((B/'release-package.json').read_text('utf8'));assert package['zipApkMatches']and v['apk']['bytes']<=300000000
 commit=json.loads((R/'build/source-push-r22/published.json').read_text('utf8'))['commit'];assert g.prepare.git('ls-remote','origin','refs/heads/main').split()[0]==commit
 assert json.loads((B/'published.json').read_text('utf8'))['assets']
 g.authenticate();api=g.api
 try:release=api(g.BASE+'/releases/tags/'+TAG)
 except urllib.error.HTTPError as error:
  if error.code!=404:raise
  release=api(g.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish=commit,name='云间日麻 Unity 0.6.18 · 装扮下载与和牌特效',draft=True,prerelease=True,body=f"基础 APK 约 {v['apk']['bytes']/1000000:.2f} MB。基础每类 3 款，手部 2 款可选；其他装扮按需下载，支持资源页按类别批量管理、装扮页单件下载、续传校验和局域网接收／中转。接入 59 款已定位原生和牌特效＋默认款，按原版 Lua 处理动画牌、层级、结算等待与回收。\n\n装扮资源：https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/wardrobe-resources-v1\n\n192 项 Node、480 个四座位荣和／自摸场景、48 个补充 Lua 场景及实际下载流程通过；原包名、原证书、ARM64／ARMv7、v1／v2、独立 API23 v1 和 16KiB 对齐已核验。Android 真机 GPU、触摸、覆盖安装和多手机热点仍待实测。源码 ZIP 只作本地备份，不作为附件发布。"))
 files=[R/'dist/yunjian-riichi-unity-0.6.18.apk',R/'dist/yunjian-riichi-unity-release-0.6.18.zip',R/'dist/SHA256-unity-0.6.18.txt',R/'dist/unity-0.6.18-verification.json',R/'dist/README-unity-0.6.18.md',R/'dist/previews/2026-11-01-wardrobe-r22/all-wins-r22-2x.mp4']
 existing={a['name']:a for a in api(release['url'])['assets']};rows=[]
 for p in files:
  digest=sha(p);item=existing.get(p.name)
  if item and item['state']!='uploaded':assert release['draft'];api(item['url'],'DELETE');item=None
  if item is None:
   assert release['draft'];print('Uploading release asset',p.name,p.stat().st_size,flush=True)
   def chunks():
    with p.open('rb')as f:
     while block:=f.read(1048576):yield block
   request=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':p.name}),data=chunks(),method='POST',headers=dict(g.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(p.stat().st_size)}))
   with urllib.request.urlopen(request,timeout=600)as response:item=json.load(response)
  assert item['state']=='uploaded'and item['size']==p.stat().st_size and item['digest']=='sha256:'+digest,p.name
  rows.append(dict(name=p.name,bytes=item['size'],sha256=digest,url=item['browser_download_url']))
 if release['draft']:release=api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='true'))
 assert not release['draft'];(B/'apk-published.json').write_text(json.dumps(dict(url=release['html_url'],commit=commit,assets=rows,artifactRedownloaded=False),ensure_ascii=False,indent=1)+'\n','utf8');print('R22 APK PUBLISH PASS',release['html_url'],flush=True)
if __name__=='__main__':main()
