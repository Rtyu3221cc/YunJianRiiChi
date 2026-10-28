"""Fetch public bundles actually observed while playing the reference previews."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib, json, re, sys, time, urllib.parse, urllib.request, warnings

ROOT = Path(__file__).resolve().parent.parent
REF = ROOT / 'dist/previews/2026-10-27-effects-reference'
SOURCE = REF / 'source/bundles'
SOURCE.mkdir(parents=True, exist_ok=True)
sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'
warnings.filterwarnings('ignore')


def fetch(entry):
    url = entry['url']
    parsed = urllib.parse.urlparse(url)
    if parsed.netloc != 'game.maj-soul.com' or not parsed.path.startswith(('/assetbundles/DXT/', '/assetbundles/clientBundleSettings/', '/assetbundles/warehouseSettings/')) or (parsed.query and not re.fullmatch(r'randv=[a-f0-9]{32}', parsed.query)):
        raise ValueError('Expected an observed public WebGL bundle URL')
    path = SOURCE / (Path(parsed.path).name if parsed.path.endswith(('.majset', '.txt')) else parsed.path.split('/')[2] + '.json')
    if not path.exists():
        request = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        for attempt in range(3):
            try:
                with urllib.request.urlopen(request, timeout=25) as response:
                    raw = response.read()
                break
            except (OSError, urllib.error.URLError) as error:
                if attempt == 2:
                    return dict(file=path.name, url=url, error=str(error))
                time.sleep(1)
        if path.suffix == '.json':
            json.loads(raw)
        elif path.suffix == '.majset' and not raw.startswith(b'UnityFS'):
            raise ValueError('Not a Unity bundle: ' + path.name)
        path.write_bytes(raw)
    return dict(file=path.name, url=url, bytes=path.stat().st_size,
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    session = json.loads((REF / 'source-session.json').read_text('utf8'))
    entries = {v['url']: v for v in session['bundles']}
    with ThreadPoolExecutor(max_workers=4) as pool:
        rows = list(pool.map(fetch, entries.values()))
    for row in rows:
        if 'error' in row:
            row['paths'], row['objects'] = [], []
            continue
        if not row['file'].endswith('.majset'):
            row['paths'] = []
            row['objects'] = []
            continue
        env = UnityPy.load(str(SOURCE / row['file']))
        row['paths'] = list(env.container)
        row['objects'] = [dict(id=o.path_id, type=o.type.name, name=o.peek_name())
                          for o in env.objects if o.type.name in ['GameObject','Material','Shader','AnimationClip','TextAsset']]
    (REF / 'source/bundle-index.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2)+'\n', 'utf8')
    print(json.dumps(dict(bundles=len(rows), failed=sum('error' in r for r in rows), bytes=sum(v.get('bytes',0) for v in rows),
                          prefabs=[p for r in rows for p in r['paths'] if p.endswith('.prefab')]), ensure_ascii=True), flush=True)


if __name__ == '__main__':
    main()
