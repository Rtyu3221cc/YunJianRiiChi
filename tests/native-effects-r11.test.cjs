const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
const root=path.join(__dirname,'../unity/Assets/Resources/Majsoul');
const read=file=>JSON.parse(fs.readFileSync(path.join(root,file),'utf8'));
const manifest=read('NativeReview/EffectsR11/manifest.json'),catalog=read('Wardrobe/catalog.json');

test('all 24 source outfits include native prefabs, original particle modules and usable source textures',()=>{
 assert.equal(manifest.effects.length,24);assert.equal(new Set(manifest.effects.map(e=>e.id)).size,24);
 assert.equal(manifest.effects.filter(e=>e.category==='winEffect').length,13);
 assert.equal(manifest.effects.filter(e=>e.category==='riichiEffect').length,11);
 for(const entry of manifest.effects){
  const item=catalog.items.find(i=>i.id===entry.id);assert.ok(item,entry.id);
  const wrapper=JSON.parse(fs.readFileSync(path.join(root,'../',item.effect+'.json'),'utf8'));assert.equal(wrapper.nativeEffect,entry.nativeEffect);
  for(const native of wrapper.nativeVariants??[wrapper.nativeEffect]){
   assert.ok(fs.existsSync(path.join(root,'NativeReview',native+'.prefab')),native);
   const source=read('NativeReview/'+native+'-source.json'),layers=source.nodes.filter(n=>n.particle);
   assert.equal(layers.length,entry.particleLayers);
   for(const node of layers){
    // EditorJsonUtility's serialized curves can contain nonfinite sentinels in
    // inactive curve branches. The imported files remain byte-for-byte intact;
    // normalize those scalar tokens only for this structural JSON scan.
    const serialized=fs.readFileSync(path.join(root,'NativeReview',node.particle),'utf8');
    const modules=JSON.parse(serialized.replace(/:\s*(?:NaN|-?Infinity)(?=\s*[,}])/g,': null')).ParticleSystem;
    for(const module of ['InitialModule','EmissionModule','ShapeModule','ColorModule','SizeModule','RotationModule','UVModule'])assert.ok(module in modules,entry.id+'/'+module);
    for(const material of node.materials){const image=fs.readFileSync(path.join(root,'NativeReview',material.texture+'.png'));assert.deepEqual([...image.subarray(0,8)],[137,80,78,71,13,10,26,10]);}
   }
  }
 }
});

test('model fading and the animated scythe mask survive original clip sampling',()=>{
 const curves=key=>read('NativeReview/EffectsR11/'+key+'-motion.json').tracks.flatMap(t=>t.curves);
 assert.equal(curves('ron_23ex_heidong').filter(c=>c.property==='_Alpha').length,3);
 assert.equal(curves('effect_liqi_23ex_xueqiu').filter(c=>c.property==='_Alpha').length,2);
 for(const key of ['effect_liqi_23ex_liandao','effect_liqi_23ex_liandao_2','effect_liqi_23ex_liandao_3','effect_liqi_23ex_liandao_4']){
  const mask=curves(key).find(c=>c.property==='_MaskTex_ST');assert.ok(mask,key);assert.ok(new Set(mask.values).size>4,key+' must move');
 }
});

test('new source outfit identifiers are accepted and preserved in frozen match loadouts',()=>{
 for(const entry of manifest.effects){
  const room=new Room('native-'+entry.id);const wardrobe={[entry.category]:entry.id};
  const host=room.dispatch({op:'create',host:'native-'+entry.id,n:4,name:'host',character:'yiji',skin:400101,wardrobe});assert.equal(host.ok,true,host.error);
  const send=op=>room.dispatch({op,token:host.token,version:room.s.version});assert.equal(send('fillBots').ok,true);assert.equal(send('start').ok,true);
  assert.equal(room.view(1).players[0].wardrobe[entry.category],entry.id);
  const restored=new Room('native-'+entry.id,()=>{},JSON.stringify(room.s));assert.equal(restored.view(1).players[0].wardrobe[entry.category],entry.id);
 }
});
