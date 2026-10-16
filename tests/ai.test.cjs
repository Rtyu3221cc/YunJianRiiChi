const {test}=require('node:test'),assert=require('node:assert/strict');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const AI=require('../app/src/main/assets/ai.js'),{Room,tiles,norm}=require('../app/src/main/assets/engine.js');
function create(n=4,bots=true){const r=new Room('secret');const v=r.dispatch({op:'create',host:'secret',name:'Human',n,rounds:1,bots});assert(v.ok,v.error);return r}
// AI strategy fixtures skip the real-time presentation wait; its boundary is checked below.
function op(r,op,extra={}){const out=r.dispatch({op,token:r.s.players[0].token,version:r.s.version,...extra});if(op==='start'&&out.ok){r.s.matchOpeningUntil=0;r.s.roundOpeningUntil=0;}return out}
function fastTick(r){let now=Date.now();r._aiNow=()=>now;let out=r.tick();if(out.waitMs){now+=out.waitMs;out=r.tick();}return out;}
function checkTiles(r){const s=r.s,a=[...s.wall,...s.rinshan,...s.indicators,...s.ura,...s.deadFill];for(let i=0;i<s.n;i++){const p=s.players[i],h=r.h(i);a.push(...tiles(h));for(const m of h._fulou)for(const d of m.match(/\d/g))a.push(m[0]+d);a.push(...p.river.filter(x=>!x.called).map(x=>x.tile));for(let j=0;j<p.nuki;j++)a.push('z4')}assert.equal(a.length,s.n===3?108:136);const count={};for(const p of a)count[norm(p)]=(count[norm(p)]||0)+1;for(const n of Object.values(count))assert.equal(n,4);assert.equal(s.players.reduce((sum,p)=>sum+p.score,0)+s.sticks*1000,s.n===3?105000:100000)}
test('host can add/remove AI, guests cannot; bot credentials cannot be used',()=>{const r=create(4,false);const guest=r.dispatch({op:'join',pin:r.s.pin,name:'Guest'});assert(guest.ok);assert(!r.dispatch({op:'addBot',token:guest.token,version:r.s.version}).ok);assert(op(r,'addBot').ok);const bot=r.s.players[2];assert(!r.dispatch({op:'ready',token:bot.token,version:r.s.version}).ok);assert(!r.dispatch({op:'join',pin:r.s.pin,recovery:bot.recovery}).ok);assert(op(r,'removeBot',{seat:2}).ok);assert(!op(r,'removeBot',{seat:1}).ok);assert(op(r,'fillBots').ok);assert.equal(r.s.players.filter(p=>p.bot).length,2);assert(!op(r,'addBot').ok);assert(op(r,'start').ok);assert(!op(r,'removeBot',{seat:2}).ok)});
test('host recovery requires secret and restores only owner',()=>{const r=create();assert(!r.dispatch({op:'recoverHost',host:'wrong'}).ok);assert.equal(r.dispatch({op:'recoverHost',host:'secret'}).token,r.s.players[0].token)});
test('one human can start with AI filling remaining seats',()=>{for(const n of [3,4]){const r=create(n,false);assert(op(r,'start',{fillBots:true}).ok);assert.equal(r.s.players.filter(p=>p.bot).length,n-1);checkTiles(r)}});
test('tick never acts for a human and survives snapshot restoration',()=>{let r=create();assert(op(r,'start').ok);for(let i=0;i<100;i++){const step=fastTick(r);assert(step.ok,step.error);if(step.idle)break;}const before=JSON.stringify(r.s);assert(r.tick().idle);assert.equal(JSON.stringify(r.s),before);r=new Room('secret',null,before);assert(r.s.players.some(p=>p.bot));assert(r.tick().idle)});
test('AI takes winning action and only needs private player view',()=>{const r=create();assert(op(r,'start').ok);const v=r.view(r.s.turn);assert(!('wall'in v));assert(v.players.every(p=>!('hand'in p)));v.options=[{kind:'pass',id:'pass'},{kind:'ron',id:'ron'}];assert.equal(AI.choose(v),'ron');const copy=JSON.parse(JSON.stringify(v));assert.equal(AI.choose(copy),AI.choose(v))});
test('AI tick rolls back on storage failure',()=>{const r=create();assert(op(r,'start').ok);r.s.players.forEach(p=>p.bot=true);const before=JSON.stringify(r.s);r.save=()=>{throw Error('disk full')};assert(!fastTick(r).ok);assert.equal(JSON.stringify(r.s),before)});
test('AI completes 24 sanma/yonma hands, conserves tiles, waits for human confirmation',()=>{for(const n of [3,4])for(let run=0;run<12;run++){const r=create(n);assert(op(r,'start').ok);let count=0;while(r.s.phase!=='result'&&count++<600){const step=fastTick(r);assert(step.ok,step.error);if(step.idle){const view=r.view(0),a=AI.choose(view);assert(a,'No legal human action');const result=op(r,'action',{action:a});assert(result.ok,result.error)}checkTiles(r)}assert.equal(r.s.phase,'result');assert(r.s.players.filter(p=>p.bot).every(p=>p.ready));assert(!r.s.players[0].ready);assert(!op(r,'next').ok);const handSeq=r.s.handSeq;assert(op(r,'ready').ok);assert.equal(r.s.phase,'play');assert.equal(r.s.handSeq,handSeq+1);checkTiles(r)}});

test('AI reactions never skip a pending human ron choice',()=>{const r=create();assert(op(r,'start').ok);const s=r.s;s.dealer=0;s.turn=1;s.phase='play';s.interrupted=true;s.wall=Array(30).fill('p9');s.indicators=Array(5).fill('z4');s.ura=Array(5).fill('z4');for(const p of s.players){p.hand='m19p147s258z12345';p.river=[]}s.players[0].hand='m123p123s123z1122';r.react(1,'z2');assert(r.options(0).some(a=>a.kind==='ron'));for(let i=0;i<5;i++)assert(fastTick(r).ok);assert.equal(s.phase,'reaction');assert.equal(s.pending.replies[0],undefined);assert(r.options(0).some(a=>a.kind==='ron'));assert(op(r,'action',{action:'ron'}).ok);assert.equal(r.s.phase,'result')});

test('AI waits through fresh full-match introduction and deal',()=>{
 const clock=Date.now;let now=100000;Date.now=()=>now;
 try{const r=create();const first=r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version});assert(first.ok);r.s.players.forEach(p=>p.bot=true);const before=JSON.stringify(r.s);
  assert(r.tick().idle);now+=5999;assert(r.tick().idle);assert.equal(JSON.stringify(r.s),before);now++;const waiting=r.tick();assert(waiting.waitMs>=700&&waiting.waitMs<=1000);now+=waiting.waitMs;const step=r.tick();assert(step.ok&&step.action,step.error);checkTiles(r);
 }finally{Date.now=clock;}
});

test('every bot action family waits 700..1000ms without publishing an early action',()=>{
 const choose=globalThis.RiichiAI.choose;
 try{for(const action of ['discard:p1','ron','tsumo','chi:m123-','pon:p111=','kan:s2222','nuki','pass']){
  const r=create();assert(op(r,'start').ok);r.s.players.forEach(p=>p.bot=true);let now=1000;r._aiNow=()=>now;globalThis.RiichiAI.choose=()=>action;let acted=[];r.act=(seat,id)=>acted.push(id);const before=JSON.stringify(r.s);
  const first=r.tick();assert(first.waitMs>=700&&first.waitMs<=1000);assert.equal(JSON.stringify(r.s),before);now+=first.waitMs-1;assert(r.tick().idle);assert.deepEqual(acted,[]);now++;assert.equal(r.tick().action,action);assert.deepEqual(acted,[action]);
 }}finally{globalThis.RiichiAI.choose=choose;}
});
test('changed room version restarts bot thinking and consecutive actions get independent waits',()=>{
 const r=create();assert(op(r,'start').ok);r.s.players.forEach(p=>p.bot=true);let now=1000;r._aiNow=()=>now;let first=r.tick();now+=first.waitMs;r.s.version++;const second=r.tick();assert(second.idle&&second.waitMs>=700&&second.waitMs<=1000);now+=second.waitMs;assert(r.tick().action);const third=r.tick();if(third.waitMs)assert(third.waitMs>=700&&third.waitMs<=1000);
});
