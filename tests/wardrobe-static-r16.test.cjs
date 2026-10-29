const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),read=p=>JSON.parse(fs.readFileSync(path.join(root,p),'utf8'));
const catalog=read('unity/Assets/Resources/Majsoul/Wardrobe/catalog.json'),audit=read('unity/Assets/Resources/Majsoul/Wardrobe/provenance-r16.json');
globalThis.Majiang=require('../app/src/main/assets/core.js');const {Room}=require('../app/src/main/assets/engine.js');
test('static decoration source records reconcile without advertising incomplete resources',()=>{
 assert.equal(catalog.items.length,new Set(catalog.items.map(i=>i.id)).size);
 assert.equal(audit.mappings.length+audit.issues.length,317);
 assert.equal(new Set(audit.mappings.map(x=>x.itemId)).size,audit.mappings.length);
 for(const row of audit.mappings){const item=catalog.items.find(i=>i.id===row.runtimeId);assert.equal(item.category,row.category);}
 for(const row of audit.issues)assert.ok(!catalog.items.some(i=>i.sourceItemId===row.id),row.name);
 assert.deepEqual(audit.issues.map(x=>x.id).sort((a,b)=>a-b),[305805,305809,30580007].sort((a,b)=>a-b));
 for(const category of ['stick','cloth','tileBack','tileFace','headFrame','lobbyBackground','music']){
  for(const item of catalog.items.filter(i=>i.category===category)){
   const required={stick:['modelPrefab'],cloth:['texture','preview'],tileBack:['hand','table','preview'],tileFace:['hand','table','preview'],headFrame:['texture'],lobbyBackground:['texture'],music:['audio']}[category];
   for(const key of required){if(category==='stick'&&item.standardStick)continue;assert.ok(item[key],item.id+' '+key);}
  }
 }
});
test('all 22 background tracks and 17 riichi tracks resolve to local offline audio',()=>{
 const tracks=read('unity/Assets/Resources/Majsoul/Settings/music.json').tracks;assert.equal(tracks.length,22);assert.equal(new Set(tracks.map(t=>t.id)).size,22);
 assert.equal(tracks.filter(t=>t.group==='lobby').length,9);assert.equal(tracks.filter(t=>t.group==='mj').length,13);
 assert.equal(catalog.items.filter(i=>i.category==='music').length,17);
 for(const resource of [...tracks.map(t=>t.resource),...catalog.items.filter(i=>i.category==='music').map(i=>i.audio)]){
  const file=path.join(root,'unity/Assets/Resources',resource+'.mp3');assert.ok(fs.statSync(file).size>10000,file);
 }
});
test('new static loadouts survive match freezing, guest polling, recovery and identifier rejection',()=>{
 const r=new Room('r16-static');let reply=r.dispatch({op:'create',host:'r16-static',n:4,name:'host',wardrobe:{stick:'liqi_25yh',cloth:'tablecloth_25xila',tileBack:'mjp_tianrandaiyouling',headFrame:'headframe_25yh',lobbyBackground:'beijing_yuanlin',music:'music-305026'}});assert.equal(reply.ok,true,reply.error);const h=reply.token;
 const send=(op,extra={})=>r.dispatch({op,token:h,version:r.s.version,...extra});
 assert.equal(send('fillBots').ok,true);assert.equal(send('start').ok,true);
 const frozen={...r.view(1).players[0].wardrobe};r.s.players[0].wardrobe={stick:'stick-default'};
 assert.deepEqual(r.view(2).players[0].wardrobe,frozen);const restored=new Room('r16-static',()=>{},JSON.stringify(r.s));assert.deepEqual(restored.view(0).players[0].wardrobe,frozen);
 for(const bad of [{stick:'headframe_25yh'},{cloth:'../arbitrary'},{music:'https://example.invalid/a.mp3'}]){const before=JSON.stringify(r.s);assert.equal(send('character',{character:'yiji',skin:400101,wardrobe:bad}).ok,false);assert.equal(JSON.stringify(r.s),before);}
 assert.deepEqual(fs.readFileSync(path.join(root,'app/src/main/assets/engine.js')),fs.readFileSync(path.join(root,'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js')));
});
test('exported R16 resource bytes retain their recorded source hashes',()=>{
 const seen=new Map();for(const row of audit.records)seen.set(row.output,row);
 for(const [relative,row] of seen){const file=path.resolve(root,relative);assert.ok(file.startsWith(root+path.sep));assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),row.sha256,relative);}
});
