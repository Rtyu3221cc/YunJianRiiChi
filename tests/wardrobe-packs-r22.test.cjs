const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..'),read=p=>JSON.parse(fs.readFileSync(path.join(root,p),'utf8'));
const wardrobe=read('unity/Assets/Resources/Majsoul/Wardrobe/catalog.json');
const catalogs=['android','windows'].map(platform=>read(`unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-${platform}.json`));
const layout=read('docs/wardrobe-resource-layout-r22.json');
const {sourceFile}=require('./wardrobe-source.cjs');

test('R22 base keeps two to four selectable outfits per category, with complete offline defaults',()=>{
 for(const c of catalogs){
  assert.equal(c.schema,1);assert.equal(c.protocol,1);assert.equal(c.items.length,354);
  const packs=new Map(c.packs.map(p=>[p.key,p]));assert.equal(packs.size,c.packs.length);
  for(const category of new Set(wardrobe.items.map(i=>i.category))){
   const items=wardrobe.items.filter(i=>i.category===category&&!i.hiddenFromSelection);
   const bundled=items.filter(i=>packs.get(c.items.find(x=>x.id===i.id).pack).builtin);
   assert.ok(bundled.length>=2&&bundled.length<=4,`${category}: ${bundled.length}`);
   const fallback=c.defaults.find(d=>d.category===category);assert.ok(bundled.some(i=>i.id===fallback.id));
   for(const i of items)assert.equal(i.builtin,packs.get(c.items.find(x=>x.id===i.id).pack).builtin,i.id);
  }
  assert.deepEqual(c.items.filter(i=>i.category==='winEffect'&&packs.get(i.pack).builtin).map(i=>i.id).sort(),['ron-fate-hf','ron-flame','win-default']);
 }
});

test('download packs form a finite verified dependency graph on both platforms',()=>{
 for(const c of catalogs){
  const byKey=new Map(c.packs.map(p=>[p.key,p])),seen=new Set();
  function visit(key,stack=new Set()){
   assert.ok(byKey.has(key),key);assert.ok(!stack.has(key),'Dependency cycle: '+key);if(seen.has(key))return;
   stack.add(key);for(const child of byKey.get(key).dependencies)visit(child,stack);stack.delete(key);seen.add(key);
  }
  for(const p of c.packs){visit(p.key);assert.match(p.key,/^deco-[a-z0-9_-]+$/);
   if(p.builtin)continue;
   assert.equal(p.available,true);assert.match(p.sha256,/^[a-f0-9]{64}$/);assert.ok(p.bytes>0);assert.ok(p.assets.length>0);
   assert.equal(p.file,`${p.key}-v${p.version}-${c.platform}-${p.sha256.slice(0,12)}.bundle`);
   assert.equal(p.url,`https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/wardrobe-resources-v1/${p.file}`);
  }
  assert.equal(new Set(c.addresses.map(a=>a.path.toLowerCase())).size,c.addresses.length);
  for(const a of c.addresses){assert.ok(byKey.get(a.pack).assets.includes(a.path),a.path);assert.ok(fs.existsSync(path.join(root,'unity',a.source)),a.source);assert.ok(a.source.startsWith('Assets/WardrobePacks/'),a.source);}
 }
 assert.deepEqual(catalogs[0].items,catalogs[1].items);
 assert.deepEqual(catalogs[0].addresses,catalogs[1].addresses);
});

test('optional heavy files physically leave Resources while original file GUIDs remain',()=>{
 assert.ok(layout.moves.length>10000);
 for(const row of layout.moves){
  assert.ok(row.from.startsWith('Assets/Resources/'));assert.ok(row.to.startsWith('Assets/WardrobePacks/'));
  assert.equal(fs.existsSync(path.join(root,'unity',row.from)),false,row.from);
  assert.ok(fs.existsSync(path.join(root,'unity',row.to)),row.to);
  assert.match(fs.readFileSync(path.join(root,'unity',row.to+'.meta'),'utf8'),/^guid: [a-f0-9]{32}$/m,row.to);
 }
});

test('all mapped win sources are selectable and old scythe transforms remain intact',()=>{
 const wins=wardrobe.items.filter(i=>i.category==='winEffect');assert.equal(wins.length,60);
 const audit=read('unity/Assets/Resources/Majsoul/Wardrobe/provenance-r22-wins.json');assert.equal(audit.records.length,45);
 for(const row of audit.records){const item=wins.find(i=>i.id===row.id);assert.equal(item.sourceItemId,row.sourceId);const config=JSON.parse(fs.readFileSync(sourceFile('unity/Assets/Resources/'+item.effect+'.json'),'utf8'));assert.equal(config.playback.sourceKey,row.sourceKey);assert.equal(config.playback.lifetime,6);}
 const scythe=wardrobe.items.find(i=>i.nativeEffect==='EffectsR11/effect_liqi_23ex_liandao'),config=JSON.parse(fs.readFileSync(sourceFile('unity/Assets/Resources/'+scythe.effect+'.json'),'utf8'));
 assert.deepEqual(config.nativeWorldEuler,[0,180,0]);assert.equal(config.nativeVariants.length,4);
 const pointer=read('docs/wardrobe-pointer-curve-proof-r22.json');assert.equal(pointer.probeSlot,0);assert.equal(pointer.sourceSlot,1);assert.equal(pointer.node,25);assert.ok(Math.abs(pointer.keys[1][0]-.2)<1e-6);
});
