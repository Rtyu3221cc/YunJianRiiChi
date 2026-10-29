"""Recover selected protected Lua scripts from the pinned local reference APK.

The AES parameters are derived from pinned IL2CPP metadata fields, never copied
into game resources. This tool does not execute Lua or contact the game service.
"""
from pathlib import Path
from zipfile import ZipFile
import argparse,hashlib,json,runpy,subprocess

R=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--profile',choices=['input','fate-r19'],default='input')
parser.add_argument('--script',action='append',help='Exact APK TextAsset path; repeat to select several scripts')
parser.add_argument('--output-dir',help='Local decrypted output directory, constrained to this workspace build tree')
args=parser.parse_args()
O=R/('build/lua-reverse-20261028' if args.profile=='input' else 'build/fate-r19/lua');D=O/'decrypted'
if args.output_dir:
    D=Path(args.output_dir);D=D if D.is_absolute()else R/D;D=D.resolve()
    assert D.is_relative_to((R/'build').resolve()),'Decoded research must stay inside the local build tree'
    O=D.parent
if args.script and not args.output_dir:parser.error('--script requires an explicit --output-dir')
D.mkdir(parents=True,exist_ok=True)
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
names=['hand','handpai3d','handpaiplane','mjpai','viewplayer_me']
paths=['luabyte/lua/game/mj/'+n+'.lua.bytes' for n in names]
if args.profile=='fate-r19':
    paths=['luabyte/lua/'+p+'.lua.bytes' for p in ['desktopmgr','game/mj/actions/actionhule','game/mj/viewplayer','game/mj/viewplayer_other','game/mj/modelanimationcontroller','ui/ui_huleshow','game/scene_mj','tools','gamemgr','cameraratiolimitareamgr']]
if args.script:
    paths=args.script
    assert all(p.startswith('luabyte/lua/') and p.endswith('.lua.bytes') and '..'not in p for p in paths),'Expected exact protected Lua asset paths'
    assert len({p.rsplit('/',1)[-1]for p in paths})==len(paths),'Choose distinct output basenames or recover conflicting names separately'
with ZipFile(R/'dist/Majsoul_4.0.16_MC.apk') as z:
    metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
assert hashlib.sha256(metadata).hexdigest()=='ab1e5303842daa26b6c52ce87fe3416d08210dff24c5e64c1f6e20a9860f43fb','Reference APK metadata changed'
fields={
    'key':(0x5c3240,'551E297AC3FB5841E7F15CEB36A7162195B8DA1953CE263BB8A7BA234FBB6067'),
    'iv':(0x5bdcc8,'17B1474EF93FA5668B5A54CF1DA44E0048A15500DFAF74A02A44E1123ED5115C')}
config=dict(output=str(D),files=[])
for label,(offset,fingerprint) in fields.items():
    data=metadata[offset:offset+16]
    assert hashlib.sha256(data).hexdigest().upper()==fingerprint,'Unexpected IL2CPP array field '+label
    config[label]=data.hex()
for path in paths:
    obj=S['obj'](path,'TextAsset');raw=S['raw'](obj)
    config['files'].append(dict(name=path.rsplit('/',1)[-1].removesuffix('.bytes'),base64=raw.decode('ascii'),ciphertextSHA256=hashlib.sha256(raw).hexdigest(),sourcePath=path))
js=r'''
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const c=JSON.parse(fs.readFileSync(0,'utf8')),key=Buffer.from(c.key,'hex'),iv=Buffer.from(c.iv,'hex'),rows=[];
for(const f of c.files){
 const ciphertext=Buffer.from(f.base64.trim(),'base64');const decipher=crypto.createDecipheriv('aes-128-cbc',key,iv);
 const originalPlain=Buffer.concat([decipher.update(ciphertext),decipher.final()]);
 const enc=crypto.createCipheriv('aes-128-cbc',key,iv),roundtrip=Buffer.concat([enc.update(originalPlain),enc.final()]);
 if(!roundtrip.equals(ciphertext))throw Error('Ciphertext round trip failed '+f.name);
 let plain=originalPlain;if(plain.subarray(0,3).equals(Buffer.from([239,187,191])))plain=plain.subarray(3);
 while(plain.length&&plain[plain.length-1]<32&&![9,10,13].includes(plain[plain.length-1]))plain=plain.subarray(0,plain.length-1);
 const text=new TextDecoder('utf-8',{fatal:true}).decode(plain);if(!/\b(local|function|return|require)\b/.test(text))throw Error('No Lua tokens '+f.name);
 fs.writeFileSync(path.join(c.output,f.name),plain);
 rows.push({file:f.name,sourcePath:f.sourcePath,encryptedSHA256:f.ciphertextSHA256,bytes:plain.length,sha256:crypto.createHash('sha256').update(plain).digest('hex'),validUtf8:true,pkcs7Valid:true,ciphertextRoundtripExact:true});
}
process.stdout.write(JSON.stringify(rows,null,2));
'''
result=subprocess.run(['C:/Users/Rtyu3221/AppData/Local/Programs/node/node.exe','-e',js],input=json.dumps(config),text=True,capture_output=True,encoding='utf8',check=True)
rows=json.loads(result.stdout)
(D/'decryption-checks.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n','utf8')
evidence=dict(referenceApk='dist/Majsoul_4.0.16_MC.apk',metadataSHA256=hashlib.sha256(metadata).hexdigest(),
    className='StringObfuscator',decryptRva='0x21546bc',initialiserRva='0x2155b28',sanitizerRva='0x215511c',
    algorithm='Base64 -> AES-128-CBC / PKCS7 -> UTF-8, optional BOM/control-tail sanitation',
    verifiedMetadataFields={k:dict(offset=hex(v[0]),sha256=v[1].lower(),bytes=16)for k,v in fields.items()},
    readableLuaRecovered=True,files=rows,gameSourceModified=False,gameServiceContacted=False)
(O/'crypto-evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2)+'\n','utf8')
for row in rows:print('PASS',row['file'],row['bytes'],'bytes; PKCS7, UTF-8, exact ciphertext round trip')
