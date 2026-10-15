const test=require('node:test'),assert=require('node:assert/strict');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
const HOST='lobby-test';
function create(n=4,category='friend'){
 const room=new Room(HOST);const r=room.dispatch({op:'create',host:HOST,name:'房主',n,rounds:2,red:false,lobbyFlow:true,category});assert(r.ok,r.error);
 return {room,token:r.token};
}
function command(room,token,op,extra={}){return room.dispatch({op,token,version:room.s?.version,id:require('node:crypto').randomUUID(),...extra});}
function join(room,name){const reply=room.dispatch({op:'join',pin:room.s.pin,name});assert(reply.ok,reply.error);return reply.token;}
test('new categories retain standard rules and public lobby readbacks',()=>{
 for(const n of [3,4])for(const category of ['ranked','competition','friend']){
  const {room}=create(n,category);const v=room.view(0);assert.deepEqual(v.lobby,{category,red:false,managed:true});assert(v.players[0].ready);assert.equal(v.pin.length,6);
  assert.equal(v.n,n);assert.equal(v.rounds,2);assert.equal(v.players[0].score,0);assert.deepEqual(v.hand,[]);
  assert(!JSON.stringify(v).includes(HOST));assert(!JSON.stringify(v).includes(room.s.players[0].token));
 }
});
test('creating again cannot silently reuse or discard a saved room',()=>{
 const {room}=create();const before=JSON.stringify(room.s);const reply=room.dispatch({op:'create',host:HOST,name:'替代',n:3,lobbyFlow:true});assert(!reply.ok);assert.equal(JSON.stringify(room.s),before);
});
test('host must wait for a human readiness and all occupied seats',()=>{
 const {room,token}=create(3);const guest=join(room,'牌友');assert.equal(room.view(0).players[1].ready,false);
 assert(command(room,token,'addBot').ok);assert(!command(room,token,'start').ok);assert.equal(room.s.phase,'lobby');
 assert(command(room,guest,'lobbyReady',{ready:true}).ok);assert(command(room,guest,'lobbyReady',{ready:false}).ok);assert(!command(room,token,'start').ok);
 assert(command(room,guest,'lobbyReady',{ready:true}).ok);assert(command(room,token,'start').ok);assert.equal(room.s.phase,'play');
 assert(room.s.players.every(p=>p.score===35000));assert(!('lobby'in room.view(0)));
});
test('bot removal and guest leaving release seats without moving host ownership',()=>{
 const {room,token}=create();const a=join(room,'甲'),b=join(room,'乙');assert(command(room,token,'addBot').ok);
 assert(command(room,a,'leaveLobby').ok);assert.equal(room.s.players.length,3);assert.equal(room.s.players[0].token,token);
 assert.equal(room.view(1).host,false);assert.equal(room.s.players[1].token,b);assert(command(room,token,'kick',{seat:2}).ok);assert.equal(room.s.players.length,2);
 assert.equal(room.dispatch({op:'state',token:a}).state.phase,'join');assert(command(room,token,'addBot').ok);
});
test('kicked human loses token and recovery access, then can join a new seat',()=>{
 const {room,token}=create();const guest=join(room,'牌友');const recovery=room.view(1).recovery;
 assert(command(room,token,'kick',{seat:1}).ok);assert.equal(room.dispatch({op:'state',token:guest}).state.phase,'join');
 assert(!room.dispatch({op:'join',pin:room.s.pin,recovery}).ok);const replacement=join(room,'牌友');assert.notEqual(replacement,guest);
 assert.equal(room.view(1).players[1].ready,false);
});
test('guest cannot kick, close a room or mutate another player readiness',()=>{
 const {room,token}=create();const guest=join(room,'牌友');const before=JSON.stringify(room.s);
 for(const req of [{op:'kick',seat:0},{op:'kick',seat:1},{op:'closeLobby'}])assert(!command(room,guest,req.op,req).ok);
 assert(!command(room,token,'lobbyReady',{ready:true}).ok);assert.equal(JSON.stringify(room.s),before);
});
test('stale kick and duplicate readiness are safely rejected or deduplicated',()=>{
 const {room,token}=create();const guest=join(room,'牌友');const version=room.s.version;assert(command(room,guest,'leaveLobby').ok);join(room,'后来者');
 assert(!room.dispatch({op:'kick',token,seat:1,version,id:'stale'}).ok);assert.equal(room.s.players[1].name,'后来者');
 const req={op:'lobbyReady',ready:true,token:room.s.players[1].token,version:room.s.version,id:'ready-once'};
 assert(room.dispatch(req).ok);const after=JSON.stringify(room.s);assert(room.dispatch(req).ok);assert.equal(JSON.stringify(room.s),after);
});
test('host leaving dissolves only a waiting room and permits a clean next create',()=>{
 const {room,token}=create();const guest=join(room,'牌友');const r=command(room,token,'closeLobby');assert(r.ok);assert.equal(r.state.phase,'empty');assert.equal(room.s,null);
 assert.equal(room.dispatch({op:'state',token:guest}).state.phase,'empty');assert(room.dispatch({op:'create',host:HOST,name:'房主',n:3,lobbyFlow:true}).ok);
});
test('saved lobby preserves category and readiness; recover is explicit',()=>{
 const {room,token}=create(3,'competition');const guest=join(room,'牌友');assert(command(room,guest,'lobbyReady',{ready:true}).ok);
 const restored=new Room(HOST,null,JSON.stringify(room.s));const reply=restored.dispatch({op:'recoverHost',host:HOST});assert(reply.ok);assert.equal(reply.token,token);assert.equal(reply.state.lobby.category,'competition');assert(reply.state.players[1].ready);
});
test('new waiting-room commands cannot alter a started game',()=>{
 const {room,token}=create(3);command(room,token,'fillBots');assert(command(room,token,'start').ok);const before=JSON.stringify(room.s);
 for(const op of ['kick','leaveLobby','closeLobby','lobbyReady'])assert(!command(room,token,op,{seat:1,ready:true}).ok);
 assert.equal(JSON.stringify(room.s),before);assert(!('lobby'in room.view(0)));
});
test('legacy rooms keep existing startup behavior without requiring new readiness',()=>{
 const room=new Room(HOST);const host=room.dispatch({op:'create',host:HOST,n:3,name:'旧房主'});join(room,'旧客户端');command(room,host.token,'addBot');assert(command(room,host.token,'start').ok);
});
