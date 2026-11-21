"""Reconcile current runtime choices with pinned, already recovered source lists."""
from pathlib import Path
from collections import Counter
import json
R=Path(__file__).resolve().parent.parent
def read(path):return json.loads((R/path).read_text('utf8'))
items=read('unity/Assets/Resources/Majsoul/Wardrobe/catalog.json')['items']
effects=read('docs/effects-restoration-index-r20.json')
bindings=read('unity/Assets/Resources/Majsoul/Actions/playback-contracts.json')['bindings']
static=read('unity/Assets/Resources/Majsoul/Wardrobe/provenance-r16.json')
wins=read('build/wardrobe-r22/source-inventory.json')
riichi_ids={b['sourceId']for b in bindings}
riichi=[dict(sourceId=d['sourceId'],name=d['name'],sourceKey=d['sourceKey'],implemented=d['sourceId']in riichi_ids,
             luaController='Block_QiPai',sourceView=d['sourceView'])for d in effects['definitions']if d['category']=='riichiEffect']
hidden=[dict(id=i['id'],category=i['category'],name=i['name'],trialStyle=i.get('trialStyle',False))for i in items if i.get('hiddenFromSelection')]
native_keys={i.get('nativeEffect','').split('/')[-1]for i in items if i['category']=='winEffect'}
win_rows=[dict(sourceId=d['id'],name=d['name'],sourceKey=d['sourceKey'],expired=d.get('expired',False),
               located=d.get('sourceKey')in native_keys)for d in wins['definitions']]
report=dict(scope=dict(apk='Majsoul_4.0.16_MC.apk',publicTable='0.11.252.w',nativeManifest='4.0.51 / 0.16.289',
                       latestOnlineCatalogQueried=False,allOriginalOutfitsComplete=False),
            selectable=dict(Counter(i['category']for i in items if not i.get('hiddenFromSelection'))),
            hiddenCompatibilityOrTrialItems=hidden,
            staticDefinitionMappings=len(static['mappings']),staticUnimplemented=static['issues'],
            riichiPublicDefinitions=len(riichi),riichiImplemented=sum(x['implemented']for x in riichi),
            riichiMissing=[x for x in riichi if not x['implemented']],riichiDefinitions=riichi,
            winDefinitions=win_rows,nativeOnlyCandidates=effects['nativeOnlyCandidates'],
            voiceAudit='122 characters, 10980 decoded source clips and 10980 successful runtime lookups',
            luaAudit=dict(newlyRecovered=['block_qipai.lua','actionliqi.lua'],
                          implementedRiichi='11 legacy definitions; carried model mounting and four-second lifetime corrected',
                          remainingRiichi='Includes legacy special branches and new root_rot/root_move/root_maque/root_ani controls; not substituted with generic particle mounting',
                          callout='3 source outfit definitions plus the normal guide; four old trial styles remain hidden',
                          visualParity='Parameter/script restoration and actual runtime preview; original-client pixel parity and Android device behavior remain unverified'))
current_native={i.get('nativeEffect','').split('/')[-1]:i['id']for i in items if i.get('nativeEffect')}
report['nativeOnlyCandidates']=[dict(row,currentlyMapped=row['sourceKey']in current_native,runtimeId=current_native.get(row['sourceKey']),status='mapped-runtime-group'if row['sourceKey']in current_native else 'unmapped-group-may-be-alias-or-unimplemented')for row in effects['nativeOnlyCandidates']]
report['nativeOnlyEntriesAreNotEqualToMissingChoices']=True
out=R/'build/review-r25/wardrobe-coverage.json';out.write_text(json.dumps(report,ensure_ascii=False,indent=1)+'\n','utf8')
print(json.dumps(dict(selectable=report['selectable'],staticMapped=report['staticDefinitionMappings'],staticMissing=len(report['staticUnimplemented']),riichiImplemented=report['riichiImplemented'],riichiDefinitions=report['riichiPublicDefinitions'],riichiMissing=len(report['riichiMissing']),allComplete=False),ensure_ascii=False))
