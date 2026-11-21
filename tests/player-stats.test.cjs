const {test}=require('node:test'),assert=require('node:assert/strict');
globalThis.Majiang=require('../app/src/main/assets/core.js');
require('../app/src/main/assets/ai.js');
const {Room}=require('../app/src/main/assets/engine.js');

function make(n=4,extra={}){
 const r=new Room('host');const a=r.dispatch({op:'create',host:'host',n,name:'A',rounds:2,category:'ranked',profile:{rank4:4,rank3:1,title:'starter'},...extra});assert(a.ok,a.error);
 for(let i=1;i<n;i++)assert(r.dispatch({op:'join',pin:r.s.pin,name:'P'+i}).ok);
 assert(r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version}).ok);return r;
}
function fixture(n=4){const r=make(n),s=r.s;s.dealer=0;s.turn=0;s.interrupted=true;s.drawKind='normal';s.wall=Array(30).fill('p9');s.indicators=Array(5).fill('z4');s.ura=Array(5).fill('z4');s.phase='play';s.pending=null;for(const p of s.players){p.hand='m19p147s258z12345';p.river=[{tile:'p2'}];}return r}
function action(r,i,id){const ret=r.dispatch({op:'action',token:r.s.players[i].token,version:r.s.version,action:id,id:require('crypto').randomBytes(8).toString('hex')});assert(ret.ok,ret.error+' / '+id);return ret}
function resolvePass(r){while(r.s.phase==='reaction'){const q=r.s.pending;const i=Object.keys(q.options).find(i=>!q.replies[i]);action(r,+i,'pass')}}
function lastHand(r,scores){const s=r.s;s.round=s.rounds*s.n-1;s.dealer=(s.initialDealer+s.n-1)%s.n;scores.forEach((v,i)=>s.players[i].score=v);}

test('ranked standings: places, tie order from the first dealer, deposits and rules',()=>{
 for(const n of [3,4]){
  const r=make(n),s=r.s;s.initialDealer=1;
  const scores=n===4?[30000,30000,25000,15000]:[40000,40000,25000];lastHand(r,scores);s.sticks=1;
  r.finish({title:'test',delta:s.players.map(()=>0),details:[],repeat:false,draw:false});
  const res=s.result;assert(res.matchOver);
  // Seat 1 is the first dealer, so it wins the tie at the top and receives the deposit.
  assert.equal(res.standings[1].place,1);assert.equal(res.standings[0].place,2);
  assert.equal(res.standings[1].score,scores[1]+1000);assert.equal(s.sticks,0);
  assert.deepEqual(res.standings.map(x=>x.place).sort(),Array.from({length:n},(_,k)=>k+1));
  assert.deepEqual(res.rules,{n,rounds:2,start:n===3?35000:25000,category:'ranked',initialDealer:1});
  for(const x of res.standings){assert.equal(typeof x.stats.hands,'number');assert.equal(x.stats.hands,1);}
 }
});

test('a hand that does not end the match carries no standings',()=>{
 const r=make();const s=r.s;s.round=0;r.finish({title:'test',delta:s.players.map(()=>0),details:[],repeat:true,draw:true});
 assert(!s.result.matchOver);assert.equal(s.result.standings,undefined);assert.equal(s.matchStats[0].hands,1);
});

test('statistics: riichi, ron, deal-in, win turn and best hand',()=>{
 const r=fixture();r.s.players[0].hand='m123p123s123z1112z4';r.s.players[1].hand='m123p123s123z3334';r.s.players[1].river=[];
 action(r,0,'riichi:z4_');action(r,1,'ron');resolvePass(r);
 const st=r.s.matchStats;assert.equal(r.s.phase,'result');
 assert.equal(st[0].riichi,1);assert.equal(st[0].dealIns,1);assert.equal(st[1].wins,1);assert.equal(st[1].tsumo,0);
 assert.equal(st[1].winTurns,0);assert(st[1].winPoints>0);assert(st[1].best);assert.equal(st[1].best.tile,'z4');assert(st[1].best.ron);
 assert.equal(st[2].wins,0);assert.equal(st[2].dealIns,0);for(const x of st)assert.equal(x.hands,1);
});

test('a better hand replaces the best hand; a weaker one does not',()=>{
 const r=make(),x=r.s.matchStats[0];
 const hand=(fanshu,damanguan=0)=>({seat:0,from:1,ron:true,tiles:['m1'],melds:[],tile:'m1',limit:'',yaku:[],score:{fanshu,fu:30,damanguan,defen:1000}});
 const fin=d=>{r.s.phase='play';r.finish({title:'t',delta:r.s.players.map(()=>0),details:[d],repeat:true,draw:false});};
 fin(hand(3));assert.equal(x.best.fanshu,3);fin(hand(2));assert.equal(x.best.fanshu,3);fin(hand(0,1));assert.equal(x.best.damanguan,1);assert.equal(x.highlights.yakuman,1);
});

test('published profiles: sanitized for humans, never for bots',()=>{
 const r=new Room('host');const a=r.dispatch({op:'create',host:'host',n:4,name:'A',rounds:1,lobbyFlow:true,profile:{rank4:99,rank3:3,title:'x'.repeat(80)}});assert(a.ok,a.error);
 assert(r.dispatch({op:'fillBots',token:a.token,version:r.s.version}).ok);
 const v=r.view(0);assert.deepEqual(v.players[0].profile,{set:true,rank4:0,rank3:3,title:'x'.repeat(32)});
 for(const p of v.players.slice(1))assert.equal(p.profile,null);
 assert(r.dispatch({op:'profile',token:a.token,version:r.s.version,profile:{rank4:7,rank3:2,title:'t'}}).ok);
 assert.equal(r.view(0).players[0].profile.rank4,7);
});

test('a full AI match ends with complete standings and consistent statistics',()=>{
 const original=Date.now;let now=original();Date.now=()=>now;
 try{
  for(const n of [3,4]){
   const r=new Room('stats-ai');let x=r.dispatch({op:'create',host:r.host,name:'A',n,rounds:1,bots:true,lobbyFlow:true,category:'ranked'});
   x=r.dispatch({op:'start',token:x.token,version:x.state.version,fillBots:true});assert(x.ok,x.error);
   const p=r.s.players[0];p.auto=true;p.autoReason='manual';p.away=true;let steps=0;
   for(;steps<40000&&!(r.s.phase==='result'&&r.s.result.matchOver);steps++){now+=1000;const t=r.tick();assert(t.ok,t.error);}
   assert(steps<40000,'AI match did not finish');
   const res=r.s.result,hands=r.s.handSeq;
   assert.deepEqual(res.standings.map(x=>x.place).sort(),Array.from({length:n},(_,k)=>k+1));
   const byPlace=[...res.standings].sort((a,b)=>a.place-b.place);for(let k=1;k<n;k++)assert(byPlace[k-1].score>=byPlace[k].score);
   assert.equal(res.standings.reduce((t,x)=>t+x.score,0),n===3?105000:100000);
   const wins=res.standings.reduce((t,x)=>t+x.stats.wins,0),dealIns=res.standings.reduce((t,x)=>t+x.stats.dealIns,0);
   for(const x of res.standings){assert.equal(x.stats.hands,hands);assert(x.stats.tsumo<=x.stats.wins);assert(x.stats.wins===0||x.stats.best);}
   assert(dealIns<=wins);
  }
 }finally{Date.now=original;}
});

test('a rematch resets match statistics',()=>{
 const r=make(),s=r.s;lastHand(r,[40000,30000,20000,10000]);r.finish({title:'t',delta:s.players.map(()=>0),details:[],repeat:false,draw:false});
 assert(s.result.matchOver);for(const p of s.players)p.ready=true;r.advanceResult();
 assert(s.matchStats.every(x=>x.hands===0&&x.best===null));
});
