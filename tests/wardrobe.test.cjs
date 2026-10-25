const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
const catalog=JSON.parse(fs.readFileSync(path.join(__dirname,'../unity/Assets/Resources/Majsoul/Wardrobe/catalog.json'),'utf8'));
function ok(reply){assert.equal(reply.ok,true,reply.error);return reply;}
function send(r,token,op,extra={}){return r.dispatch({op,token,version:r.s.version,...extra});}
test('every category has two valid bundled choices and all resources resolve',()=>{
 const code=fs.readFileSync(path.join(__dirname,'../app/src/main/assets/engine.js'),'utf8'),registry=JSON.parse(code.match(/const WARDROBE_CATALOG=(\{[^\r\n]+\});/)[1]);
 const expected={};for(const item of catalog.items)(expected[item.category]??=[]).push(item.id);assert.deepEqual(registry,expected);
 const categories=new Map();for(const item of catalog.items){categories.set(item.category,(categories.get(item.category)||0)+1);
  for(const key of ['icon','texture','hand','table','preview','model','motion','audio','effect'])if(item[key]){
   const stem=path.join(__dirname,'../unity/Assets/Resources',item[key]);assert.ok(['.png','.jpg','.json','.mp3'].some(ext=>fs.existsSync(stem+ext)),`${item.id}: ${key}`);
  }
 }
 assert.equal(categories.size,11);for(const [category,count] of categories)assert.ok(count>=2,category);
 for(const clip of ['cat-hand','cat-discard','cat-riichi','cat-meld','cat-tsumo']){
  const root=path.join(__dirname,'../unity/Assets/Resources/Majsoul/Wardrobe/Motion',clip),j=JSON.parse(fs.readFileSync(root+'.json'));
  assert.equal(fs.statSync(root+'-frames.bytes').size,j.frames*2*j.vertices*3*4);
  assert.equal(j.tilePoints.length,j.frames*3);assert.ok(j.landTime>0&&j.landTime<=j.duration);
 }
});
test('each seat publishes only its validated loadout, frozen for a match and retained after restart',()=>{
 const r=new Room('wardrobe'),loadout={hand:'hand-cat',stick:'liqi_bone',riichiEffect:'riichi-ice',winEffect:'win-default',callout:'mpzs_22event'};
 const h=ok(r.dispatch({op:'create',host:'wardrobe',n:4,name:'host',character:'yiji',skin:400101,wardrobe:loadout})).token;
 const g=ok(r.dispatch({op:'join',pin:r.s.pin,name:'guest',character:'yiji',skin:400101,wardrobe:{hand:'hand-human'}})).token;
 assert.deepEqual(r.view(1).players[0].wardrobe,loadout);assert.equal(r.view(0).players[1].wardrobe.hand,'hand-human');
 ok(send(r,h,'fillBots'));ok(send(r,h,'start'));assert.deepEqual(r.view(1).players[0].wardrobe,loadout);
 r.s.players[0].wardrobe={hand:'hand-cat-orange'};assert.deepEqual(r.view(0).players[0].wardrobe,loadout);
 const restored=new Room('wardrobe',()=>{},JSON.stringify(r.s));assert.deepEqual(restored.view(0).players[0].wardrobe,loadout);
 ok(send(restored,g,'join',{pin:restored.s.pin,character:'yiji',skin:400101,wardrobe:{hand:'hand-cat-orange'}}));
 assert.equal(restored.view(0).players.find(p=>p.name==='guest').wardrobe.hand,'hand-human');
});
test('invalid or wrong-category identifiers are rejected without changing the room',()=>{
 const r=new Room('wardrobe'),h=ok(r.dispatch({op:'create',host:'wardrobe',n:4,name:'host'})).token;
 for(const wardrobe of [{hand:'missing'},{hand:'liqi_bone'},{hand:{path:'anything'}},[]]){
  const before=JSON.stringify(r.s);assert.equal(send(r,h,'character',{character:'yiji',skin:400101,wardrobe}).ok,false);assert.equal(JSON.stringify(r.s),before);
 }
 ok(send(r,h,'character',{character:'yiji',skin:400101,wardrobe:{hand:'hand-cat-orange'}}));assert.equal(r.view(0).players[0].wardrobe.hand,'hand-cat-orange');
 assert.deepEqual(r.view(0).players[0].wardrobe,{hand:'hand-cat-orange'});
});
