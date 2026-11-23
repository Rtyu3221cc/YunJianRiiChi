const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
const root=path.resolve(__dirname,'..');
const roles=JSON.parse(fs.readFileSync(path.join(root,'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json'),'utf8')).roles;
const keys=['lingxing','fulilian','feilun','xiutaerke','youbeier'];
const loadout={stick:'liqi_26fll',winEffect:'ron_26fll',riichiEffect:'effect_liqi_26fll',cloth:'tablecloth_26fll',tileBack:'mjp_26fll',headFrame:'headframe_26fll'};
test('new native character IDs and all collaboration cosmetics survive room start and recovery',()=>{
 for(const key of keys)for(const skin of roles.find(r=>r.key===key).skins){
  const r=new Room('nov-'+skin.id);const created=r.dispatch({op:'create',host:'nov-'+skin.id,n:4,name:key,character:key,skin:skin.id,wardrobe:loadout});assert.equal(created.ok,true,created.error);
  for(const op of ['fillBots','start']){const reply=r.dispatch({op,token:created.token,version:r.s.version});assert.equal(reply.ok,true,reply.error);}
  const v=r.view(0).players[0];assert.equal(v.character,key);assert.equal(v.skin,skin.id);assert.deepEqual(v.wardrobe,loadout);
  const recovered=new Room('nov-'+skin.id,()=>{},JSON.stringify(r.s)).view(0).players[0];assert.equal(recovered.character,key);assert.equal(recovered.skin,skin.id);assert.deepEqual(recovered.wardrobe,loadout);
 }
});
test('both platform directories include every new character and preserve every previous pack byte contract',()=>{
 for(const platform of ['android','windows']){
  const read=name=>JSON.parse(fs.readFileSync(path.join(root,name),'utf8'));
  const now=read(`unity/Assets/Resources/Majsoul/CharacterDirectory/packs-${platform}.json`);
  for(const key of keys){const p=now.packs.find(p=>p.key===key);assert.ok(p?.available&&p.bytes>0);assert.deepEqual(p.skins,roles.find(r=>r.key===key).skins.map(s=>s.id));}
  assert.deepEqual(new Set(now.packs.map(p=>p.key)),new Set(roles.map(r=>r.key)));
  const before=read(`docs/reference/november-2026/previous-characters-${platform}.json`);
  for(const p of before.packs){const next=now.packs.find(n=>n.key===p.key);assert.ok(next);if(p.available&&p.skins.length===next.skins.length)for(const field of ['version','bytes','sha256','url','file'])assert.equal(next[field],p[field],p.key+' '+field);}
 }
});
