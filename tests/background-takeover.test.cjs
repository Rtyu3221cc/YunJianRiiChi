const {test}=require('node:test'),assert=require('node:assert/strict');
globalThis.Majiang=require('../app/src/main/assets/core.js');const {Room}=require('../app/src/main/assets/engine.js');
function scenario(n,run){const original=Date.now;let now=1000000;Date.now=()=>now;try{const r=new Room('background-test');const host=r.dispatch({op:'create',host:'background-test',n,name:'host'});assert(host.ok);const humans=[host.token];for(let i=1;i<n;i++){const p=r.dispatch({op:'join',pin:r.s.pin,name:'guest'+i});assert(p.ok);humans.push(p.token)}const send=(token,op,extra={})=>{const q=r.dispatch({op,token,version:r.s.version,...extra});assert(q.ok,q.error);return q};send(host.token,'start');now+=6000;for(const token of humans)send(token,'state');r.tick();run({r,humans,send,advance:ms=>now+=ms,now:()=>now,others:token=>humans.filter(t=>t!==token).forEach(t=>send(t,'state'))});}finally{Date.now=original}}
for(const n of [3,4]){
 test(`${n} seats: background gets a fresh 120 seconds even after 110 seconds without input`,()=>scenario(n,({r,humans,send,advance,others})=>{
  const token=r.s.players[r.s.turn].token;advance(110000);humans.forEach(t=>send(t,'state'));r.presence(Date.now());assert(!r.s.players.find(p=>p.token===token).auto);send(token,'presence',{present:false});const p=r.s.players.find(p=>p.token===token),hand=p.hand;
  assert(!p.auto);advance(12000);others(token);r.tick();assert(!p.auto);assert.equal(p.hand,hand);
  advance(107999);others(token);r.tick();assert(!p.auto);advance(1);r.presence(Date.now());assert(p.auto);assert.equal(p.autoReason,'offline');
 }));
 test(`${n} seats: returning within the background grace keeps manual control and a new pause starts a new clock`,()=>scenario(n,({r,send,advance,others})=>{
  const token=r.s.players[r.s.turn].token,p=r.s.players.find(p=>p.token===token);send(token,'presence',{present:false});advance(119999);others(token);r.presence(Date.now());assert(!p.auto);send(token,'resume',{room:r.s.audioRoom});assert(!p.auto&&!p.away);advance(1);others(token);r.presence(Date.now());assert(!p.auto);
  send(token,'presence',{present:false});advance(119999);others(token);r.presence(Date.now());assert(!p.auto);advance(1);r.presence(Date.now());assert(p.auto);
 }));
 test(`${n} seats: background heartbeats and duplicate notifications cannot restart the saved grace period`,()=>scenario(n,({r,send,advance,others})=>{
  const token=r.s.players[r.s.turn].token;send(token,'presence',{present:false});const since=r.s.players.find(p=>p.token===token).awaySince;
  advance(60000);send(token,'state');send(token,'presence',{present:false});assert.equal(r.s.players.find(p=>p.token===token).awaySince,since);
  const restored=new Room('background-test',null,JSON.stringify(r.s));advance(59999);for(const t of r.s.players.filter(p=>!p.bot).map(p=>p.token))restored.dispatch({op:'state',token:t});restored.presence(Date.now());assert(!restored.s.players.find(p=>p.token===token).auto);advance(1);restored.presence(Date.now());assert.equal(restored.s.players.find(p=>p.token===token).autoReason,'offline');
 }));
 test(`${n} seats: manual takeover remains immediate`,()=>scenario(n,({r,send})=>{const token=r.s.players[r.s.turn].token;send(token,'autoPlay',{enabled:true});assert.equal(r.s.players.find(p=>p.token===token).autoReason,'manual');assert(r.controlled(r.s.players[r.s.turn]));}));
}
