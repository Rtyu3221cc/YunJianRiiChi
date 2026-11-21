const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');require('../app/src/main/assets/ai.js');
const {Room}=require('../app/src/main/assets/engine.js');
const root=path.resolve(__dirname,'..');
function fixture(){const r=new Room('secret');assert(r.dispatch({op:'create',host:'secret',name:'Host',n:4,bots:true}).ok);assert(r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version}).ok);r.s.phase='play';r.s.turn=0;r.s.interrupted=true;r.s.players[0].hand='m111p222s333z55s67';r.s.players[0].river=[];return r;}
test('voice wait han includes visible dora, excludes ura and does not change the room',()=>{
 const r=fixture();r.s.indicators=['m9'];r.s.ura=['m9'];
 const before=JSON.stringify(r.s),a=r.view(0).waitDetails;assert.equal(JSON.stringify(r.s),before);
 assert(a.length>0);assert(a.every(w=>Number.isInteger(w.ronHan)&&Number.isInteger(w.tsumoHan)));
 r.s.ura=['z4'];assert.deepEqual(r.view(0).waitDetails,a);
 r.s.indicators=['p3'];const b=r.view(0).waitDetails;
 assert(a.every((w,i)=>w.ronHan===b[i].ronHan+3&&w.tsumoHan===b[i].tsumoHan+3),'three visible dora reflected');
});
test('private wait han stays in viewer hints and out of public players and audio events',()=>{
 const r=fixture(),v=r.view(0);for(const p of v.players)for(const key of ['waitDetails','ronHan','tsumoHan','hand'])assert(!Object.hasOwn(p,key));
 assert(v.audioEvents.every(e=>!Object.hasOwn(e,'ronHan')&&!Object.hasOwn(e,'tsumoHan')));
 assert.equal(fs.readFileSync(path.join(root,'app/src/main/assets/engine.js'),'utf8'),fs.readFileSync(path.join(root,'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js'),'utf8'));
});
test('every character event resolves against its own original catalog path',()=>{
 const {roles}=JSON.parse(fs.readFileSync(path.join(root,'unity/Assets/Resources/Majsoul/Characters/voice-directory.json'),'utf8'));
 const types=['lobby_playerlogin','lobby_normal','lobby_room_in','lobby_room_ready','ingame_start','game_top','game_top_ron','game_top_big','gameend_tingpai','gameend_noting','ingame_lianda','ingame_baopai','ingame_remain10','ingame_yiman','ingame_beiman'];
 for(const role of roles)for(const type of types){const matches=role.lines.filter(x=>x.available&&x.type===type);assert(matches.length>0,`${role.key}/${type}`);assert(matches.every(x=>x.path&&!x.path.includes('..')));}
 const yiji=roles.find(x=>x.key==='yiji');assert.equal(yiji.lines.find(x=>x.type==='ingame_start').path,'extra2/ingame_start');assert.equal(yiji.lines.find(x=>x.type==='lobby_room_in').path,'extra2/lobby_room_in');
});
