"""Merge upstream/new-character records without deleting already published IDs."""
from pathlib import Path
import argparse, copy, json, re

TABLES=('item_definition/character','item_definition/skin')
def merge_config(original,patch):
    result=copy.deepcopy(original)
    for table in TABLES:
        rows={r['id']:r for r in result[table]}
        incoming=patch.get(table,[]);assert len({r['id']for r in incoming})==len(incoming),'Duplicate IDs in patch: '+table
        for row in incoming:
            old=rows.get(row['id'],{})
            if table.endswith('/character')and old:assert old['sound_folder']==row.get('sound_folder',old['sound_folder']),'Do not rename a published character key'
            if table.endswith('/skin')and old:assert old['character_id']==row.get('character_id',old['character_id']),'Do not transfer an existing skin ID'
            rows[row['id']]={**old,**row}
        result[table]=list(rows.values())
    characters={c['id']:c for c in result[TABLES[0]]};skins={s['id']:s for s in result[TABLES[1]]}
    assert len(characters)==len(result[TABLES[0]])and len(skins)==len(result[TABLES[1]]),'Duplicate IDs in source'
    assert len({c['sound_folder']for c in characters.values()})==len(characters),'Duplicate character keys'
    for c in characters.values():
        assert re.fullmatch(r'[a-z0-9_-]+',c['sound_folder']),'Unsafe character key'
        for field in ['init_skin','full_fetter_skin']:assert c[field]in skins and skins[c[field]]['character_id']==c['id'],'Default/bond skin owner mismatch'
    for s in skins.values():
        assert s['character_id']in characters,'Unknown character owner'
        assert s['path'].startswith('extendRes/charactor/')and'..'not in s['path'].split('/'),'Unsafe upstream asset path'
    return result

def main():
    root=Path(__file__).resolve().parent.parent
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--patch',type=Path,required=True);p.add_argument('--versions',type=Path);p.add_argument('--inputs',type=Path,default=root/'docs/character-resource-inputs');p.add_argument('--dry-run',action='store_true');args=p.parse_args()
    inputs=args.inputs.resolve();assert inputs.is_relative_to(root.resolve()),'Inputs must stay in this checkout'
    original=json.loads((inputs/'config.json').read_text('utf8'));patch=json.loads(args.patch.read_text('utf8'));merged=merge_config(original,patch)
    versions=json.loads((inputs/'versions.json').read_text('utf8'))
    if args.versions:versions['res'].update(json.loads(args.versions.read_text('utf8'))['res'])
    if not args.dry_run:
        for name,value in [('config.json',merged),('versions.json',versions)]:
            temp=inputs/(name+'.tmp');temp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8');temp.replace(inputs/name)
    print('INPUT MERGE PASS',len(merged[TABLES[0]]),'characters',len(merged[TABLES[1]]),'skins','dry-run'if args.dry_run else'written')
if __name__=='__main__':main()
