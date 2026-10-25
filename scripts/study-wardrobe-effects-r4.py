from pathlib import Path
import hashlib,json,urllib.request,urllib.parse
R=Path(__file__).resolve().parent.parent
OUT=R/'build/wardrobe-r4-study';OUT.mkdir(exist_ok=True)
INDEX=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
def fetch(path):
    cached=R/'build/wardrobe-reference-r2/cdn'/(hashlib.sha256(path.encode()).hexdigest()[:16]+Path(path).suffix)
    dest=OUT/'source'/path;dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists():
        if cached.exists():dest.write_bytes(cached.read_bytes())
        else:
            for prefix in dict.fromkeys([INDEX[path]['prefix'],'v0.10.1.w']):
                url='https://game.maj-soul.com/1/'+prefix+'/'+urllib.parse.quote(path)
                try:dest.write_bytes(urllib.request.urlopen(url,timeout=40).read());break
                except urllib.error.HTTPError as error:
                    if error.code!=404:raise
            else:raise RuntimeError('Missing source '+path)
    return dest.read_bytes()
if __name__=='__main__':
    for key in ['ron_huolongjuan','effect_hupai_fire','effect_liqi_ice','effect_liqi_fire','mpzs_shandian']:
        j=json.loads(fetch('scene/'+key+'.lh'));lines=[]
        def walk(n,depth=0):
            c=n.get('customProps',{});lines.append('  '*depth+n['type']+' '+str(n.get('props',{}))+' '+json.dumps(c,ensure_ascii=False))
            for v in n.get('child',[]):walk(v,depth+1)
        walk(j);(OUT/(key+'-graph.txt')).write_text('\n'.join(lines),'utf8')
        print(key,len(lines),'nodes',flush=True)
