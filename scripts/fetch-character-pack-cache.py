"""Explicitly restore selected public packs for editor validation; not a release verification step."""
from pathlib import Path
import argparse, hashlib, json, urllib.request
R=Path(__file__).resolve().parent.parent
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--roles',required=True,help='Comma separated keys, or all');p.add_argument('--platform',choices=['windows','android'],default='windows');args=p.parse_args()
catalog=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+args.platform+'.json')).read_text('utf8'))
keys=None if args.roles=='all'else set(args.roles.split(','))
if keys:assert keys<={r['key']for r in catalog['packs']},'Unknown role key'
def digest(file):
 h=hashlib.sha256()
 with file.open('rb')as stream:
  for block in iter(lambda:stream.read(1048576),b''):h.update(block)
 return h.hexdigest()
for row in catalog['packs']:
 if row['builtin']or not row['available']or keys is not None and row['key']not in keys:continue
 assert row['url'].startswith('https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/')
 target=R/'build/character-packs-cache'/args.platform/row['file'];target.parent.mkdir(parents=True,exist_ok=True)
 if target.exists()and target.stat().st_size==row['bytes']and digest(target)==row['sha256']:print('Cached',row['key']);continue
 part=target.with_suffix(target.suffix+'.part');start=part.stat().st_size if part.exists()else 0
 if start>=row['bytes']:part.unlink();start=0
 request=urllib.request.Request(row['url'],headers={'Range':'bytes='+str(start)+'-'}if start else{})
 with urllib.request.urlopen(request,timeout=600)as response:
  if start and response.status!=206:start=0
  with part.open('ab'if start else'wb')as output:
   while block:=response.read(1048576):output.write(block)
 assert part.stat().st_size==row['bytes']and digest(part)==row['sha256'],'Pack failed integrity check: '+row['key']
 part.replace(target);print('Restored editor pack',row['key'],row['bytes'],flush=True)
