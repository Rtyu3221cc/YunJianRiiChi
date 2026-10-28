const {test}=require('node:test'),assert=require('node:assert/strict');
const {randomUUID}=require('node:crypto');
globalThis.Majiang=require('../app/src/main/assets/core.js');
require('../app/src/main/assets/ai.js');
const {Room,tiles,norm}=require('../app/src/main/assets/engine.js');

function send(room,token,op,extra={}){
 return room.dispatch({op,token,version:room.s?.version,id:randomUUID(),...extra});
}
function ok(reply){assert(reply.ok,reply.error);return reply;}
function game(n,humanCount=n,settings={}){
 const room=new Room('session-test-host');
 const host=ok(room.dispatch({op:'create',host:'session-test-host',name:'房主',n,rounds:2,red:false,lobbyFlow:true,category:'competition',...settings})).token;
 const humans=[host];
 for(let i=1;i<humanCount;i++){
  const token=ok(room.dispatch({op:'join',pin:room.s.pin,name:'牌友'+i})).token;
  humans.push(token);ok(send(room,token,'lobbyReady',{ready:true}));
 }
 if(humanCount<n)ok(send(room,host,'fillBots'));
 ok(send(room,host,'start'));
 return {room,host,humans};
}
function clock(run){
 const original=Date.now;let now=1000000;Date.now=()=>now;
 try{return run({get now(){return now;},advance(ms){now+=ms;}});}finally{Date.now=original;}
}
function heartbeat(room,tokens){for(const token of tokens)ok(room.dispatch({op:'state',token}));}
function physicalTiles(room){
 const s=room.s,all=[...s.wall,...s.rinshan,...s.indicators,...s.ura,...s.deadFill];
 for(let i=0;i<s.n;i++){
  const p=s.players[i],hand=room.h(i);all.push(...tiles(hand),...p.river.filter(t=>!t.called).map(t=>t.tile));
  for(const meld of hand._fulou)for(const d of meld.match(/\d/g))all.push(meld[0]+d);
  for(let j=0;j<p.nuki;j++)all.push('z4');
 }
 assert.equal(all.length,s.n===3?108:136);
 const counts={};for(const tile of all)counts[norm(tile)]=(counts[norm(tile)]||0)+1;
 for(const count of Object.values(counts))assert.equal(count,4);
}
function terminalFixture(room){
 // Independent final-hand fixture: a leader has reached the target, the final
 // dealer did not repeat. The original wall/hands remain a valid full deal.
 const s=room.s;s.round=s.rounds*s.n-1;s.dealer=0;
 s.players[1].score+=5000;s.players[2].score-=5000;
 room.finish({title:'终局回归样例',delta:s.players.map(()=>0),details:[],repeat:false,draw:false});
 assert(s.result.matchOver);
}

for(const n of [3,4]){
 test(`${n} players: final readiness starts exactly one match and preserves room settings and seats`,()=>{
  const {room,host,humans}=game(n);terminalFixture(room);
  const before=structuredClone(room.s),last=humans.at(-1);
  const retained={pin:before.pin,audioRoom:before.audioRoom,n,rounds:before.rounds,red:before.red,category:before.lobbyCategory,
   seats:before.players.map(p=>[p.token,p.name,p.avatar,!!p.bot])};
  for(const token of humans.slice(0,-1)){ok(send(room,token,'ready',{handSeq:before.handSeq}));assert.equal(room.s.phase,'result');}
  const request={op:'ready',token:last,version:-99,handSeq:before.handSeq,id:'final-ready-'+n};
  const reply=ok(room.dispatch(request));assert.equal(reply.state.phase,'play');
  assert.deepEqual({pin:room.s.pin,audioRoom:room.s.audioRoom,n:room.s.n,rounds:room.s.rounds,red:room.s.red,category:room.s.lobbyCategory,
   seats:room.s.players.map(p=>[p.token,p.name,p.avatar,!!p.bot])},retained);
  assert.equal(room.s.matchSeq,before.matchSeq+1);assert.equal(room.s.handSeq,before.handSeq+1);
  assert.equal(room.s.round,0);assert.equal(room.s.dealer,before.initialDealer);
  assert.equal(room.s.honba,0);assert.equal(room.s.sticks,0);assert(room.s.players.every(p=>p.score===(n===3?35000:25000)));
  assert(room.s.players.every(p=>!p.ready));physicalTiles(room);
  const continued=JSON.stringify(room.s);ok(room.dispatch(request));assert.equal(JSON.stringify(room.s),continued);
  assert(!send(room,host,'ready',{handSeq:before.handSeq}).ok);assert.equal(JSON.stringify(room.s),continued);
 });

 test(`${n} players: ordinary hand readiness advances one hand without restarting the match`,()=>{
  const {room,humans}=game(n),before=structuredClone(room.s);
  room.abort('九种九牌');assert(!room.s.result.matchOver);
  for(const token of humans)ok(send(room,token,'ready',{handSeq:before.handSeq}));
  assert.equal(room.s.phase,'play');assert.equal(room.s.matchSeq,before.matchSeq);assert.equal(room.s.handSeq,before.handSeq+1);
  assert.equal(room.s.dealer,before.dealer);assert.equal(room.s.honba,1);assert.equal(room.s.pin,before.pin);physicalTiles(room);
 });

 test(`${n} players: guest departure becomes AI, invalidates old seat, host departure clears saved room`,()=>{
  const {room,host,humans}=game(n),guest=humans[1],seat=room.s.players.findIndex(p=>p.token===guest);
  const player=structuredClone(room.s.players[seat]);let saved='';room.save=value=>saved=value;
  ok(send(room,guest,'leaveMatch',{version:-100}));
  const bot=room.s.players[seat];assert(bot.bot);assert.equal(bot.hand,player.hand);assert.equal(bot.score,player.score);
  assert.deepEqual(bot.river,player.river);assert.notEqual(bot.token,guest);assert.equal(bot.recovery,'');
  assert.equal(room.dispatch({op:'peek',token:guest}).state.phase,'join');assert(!send(room,guest,'resume',{room:room.s.audioRoom}).ok);
  assert(!room.dispatch({op:'join',pin:room.s.pin,recovery:player.recovery}).ok);
  const restored=new Room('session-test-host',null,saved);assert(restored.s.players[seat].bot);physicalTiles(restored);
  const formerPin=room.s.pin;ok(send(room,host,'leaveMatch',{version:-100}));assert.equal(room.s,null);assert.equal(saved,'');
  assert.equal(room.dispatch({op:'peek',token:humans[2]}).state.phase,'empty');assert(!room.dispatch({op:'discover',pin:formerPin}).ok);
  const next=ok(room.dispatch({op:'create',host:'session-test-host',n,name:'重新开房',lobbyFlow:true}));assert.equal(next.state.phase,'lobby');
 });

 test(`${n} players: peek cannot keep a disconnected human online, original token resumes without recovery code`,()=>clock(time=>{
  const {room,humans}=game(n),guest=humans[1],seat=room.s.players.findIndex(p=>p.token===guest);
  time.advance(6000);heartbeat(room,humans);room.tick();const lastOnline=time.now;
  const before=JSON.stringify(room.s),seen=room.seen[guest];
  for(let i=0;i<11;i++){time.advance(1000);ok(room.dispatch({op:'peek',token:guest}));}
  assert.equal(room.seen[guest],seen);assert.equal(JSON.stringify(room.s),before);
  time.advance(999);room.tick();assert(!room.s.players[seat].auto);
  time.advance(1);assert.equal(time.now-lastOnline,12000);room.tick();assert(!room.s.players[seat].auto);assert(!room.view(0).players[seat].online);
  time.advance(107999);heartbeat(room,humans.filter(t=>t!==guest));room.tick();assert(!room.s.players[seat].auto);
  time.advance(1);room.tick();assert.equal(room.s.players[seat].autoReason,'offline');
  heartbeat(room,[guest]);assert(room.s.players[seat].auto,'state heartbeat must not silently recover control');
  const saved=JSON.stringify(room.s),restored=new Room('session-test-host',null,saved),original=structuredClone(restored.s.players[seat]);
  const wrong=send(restored,guest,'resume',{room:'a-different-room'});assert(!wrong.ok);assert.equal(JSON.stringify(restored.s),saved);
  const reply=ok(send(restored,guest,'resume',{room:room.s.audioRoom,version:-100}));
  assert.equal(reply.state.you,seat);assert(!reply.state.players[seat].auto);assert(!restored.s.players[seat].away);
  assert.equal(restored.s.players[seat].token,guest);assert.equal(restored.s.players[seat].hand,original.hand);
  assert.equal(restored.s.players[seat].score,original.score);assert.equal(restored.s.pin,room.s.pin);physicalTiles(restored);
 }));

 test(`${n} players: 120 second idle timeout respects threshold and heartbeat never takes control back`,()=>clock(time=>{
  const {room,humans}=game(n);time.advance(6000);heartbeat(room,humans);room.tick();
  const seat=room.s.turn,token=room.s.players[seat].token,start=time.now;
  for(let i=0;i<11;i++){time.advance(10000);heartbeat(room,humans);room.tick();assert(!room.s.players[seat].auto);}
  time.advance(9999);heartbeat(room,humans);room.tick();assert.equal(time.now-start,119999);assert(!room.s.players[seat].auto);
  time.advance(1);heartbeat(room,humans);const step=room.tick();assert(step.idle);assert.equal(room.s.players[seat].autoReason,'timeout');
  assert(!room.s.players[seat].bot);assert(room.s.players[seat].auto);heartbeat(room,humans);assert(room.s.players[seat].auto);
  ok(send(room,token,'presence',{present:true}));assert(room.s.players[seat].auto,'foreground heartbeat preserves AI until explicit return');
  ok(send(room,token,'autoPlay',{enabled:false,version:-100}));assert(!room.s.players[seat].auto);
  const action=room.options(seat).find(a=>a.kind==='discard');assert(action);ok(send(room,token,'action',{action:action.id}));
  assert(!room.s.players[seat].auto);assert(room.s.players.filter((p,i)=>i!==seat).every(p=>p.autoReason==='timeout'));physicalTiles(room);
 }));

 test(`${n} players: manual AI takes a legal action, polling preserves it, explicit resume restores manual control`,()=>clock(time=>{
  const {room,humans}=game(n);time.advance(6000);heartbeat(room,humans);
  const seat=room.s.turn,token=room.s.players[seat].token,handSeq=room.s.handSeq;
  ok(send(room,token,'autoPlay',{enabled:true,version:-100}));
  heartbeat(room,humans);assert.equal(room.s.players[seat].autoReason,'manual');
  const first=room.tick();assert(first.waitMs>=700&&first.waitMs<=1000);
  time.advance(first.waitMs);const result=room.tick();assert(result.ok&&result.action,result.error);physicalTiles(room);
  heartbeat(room,humans);assert(room.s.players[seat].auto);
  const reply=ok(send(room,token,'resume',{room:room.s.audioRoom}));assert.equal(reply.state.you,seat);assert(!reply.state.players[seat].auto);
  assert.equal(room.s.handSeq,handSeq);
 }));

 test(`${n} players: late human inherits a real AI seat without taking a disconnected human seat`,()=>{
  const {room,humans}=game(n,2),guest=humans[1],guestSeat=room.s.players.findIndex(p=>p.token===guest);
  ok(send(room,guest,'presence',{present:false}));const reserved=structuredClone(room.s.players[guestSeat]);
  for(let i=0;i<n-2;i++){
   const aiSeat=room.s.players.findIndex((p,j)=>j>0&&p.bot),before=structuredClone(room.s.players[aiSeat]),dealer=room.s.dealer;
   const reply=ok(room.dispatch({op:'join',pin:room.s.pin,name:'新玩家'+i}));assert.equal(reply.state.you,aiSeat);
   const human=room.s.players[aiSeat];assert(!human.bot);assert(!human.auto);assert.equal(human.hand,before.hand);assert.equal(human.score,before.score);
   assert.deepEqual(human.river,before.river);assert.equal(room.s.dealer,dealer);assert.notEqual(human.token,before.token);
   assert.deepEqual(room.s.players[guestSeat],reserved);physicalTiles(room);
  }
  assert(!room.dispatch({op:'join',pin:room.s.pin,name:'超额玩家'}).ok);assert.deepEqual(room.s.players[guestSeat],reserved);
  const reply=ok(send(room,guest,'resume',{room:room.s.audioRoom}));assert.equal(reply.state.you,guestSeat);assert(!reply.state.players[guestSeat].auto);
 });
}

test('unattended terminal stays paused after resume and ready until host explicitly starts another match',()=>clock(time=>{
 const {room,host}=game(3,1);terminalFixture(room);ok(send(room,host,'presence',{present:false}));
 const handSeq=room.s.handSeq,matchSeq=room.s.matchSeq;
 for(let i=0;i<10;i++){time.advance(200000);room.tick();assert.equal(room.s.phase,'result');assert.equal(room.s.matchSeq,matchSeq);}
 ok(room.dispatch({op:'peek',token:host}));room.tick();assert.equal(room.s.phase,'result');
 ok(send(room,host,'resume',{room:room.s.audioRoom}));room.tick();assert.equal(room.s.phase,'result');
 ok(send(room,host,'ready',{handSeq}));assert.equal(room.s.phase,'result');assert.equal(room.s.matchSeq,matchSeq);
 ok(send(room,host,'next'));assert.equal(room.s.phase,'play');assert.equal(room.s.matchSeq,matchSeq+1);physicalTiles(room);
}));

test('failed session persistence rolls back AI mode, guest departure and host closure',()=>{
 for(const [kind,actor,extra] of [['autoPlay',0,{enabled:true}],['leaveMatch',1,{}],['leaveMatch',0,{}]]){
  const {room,humans}=game(4),token=humans[actor],before=JSON.stringify(room.s);room.save=()=>{throw Error('storage unavailable');};
  const reply=send(room,token,kind,extra);assert(!reply.ok);assert.equal(reply.error,'storage unavailable');assert.equal(JSON.stringify(room.s),before);
 }
});

test('a rejected illegal action does not extend the 120 second inactivity deadline',()=>clock(time=>{
 const {room,humans}=game(4);time.advance(6000);heartbeat(room,humans);room.tick();
 const seat=room.s.turn,token=room.s.players[seat].token;
 for(let i=0;i<11;i++){time.advance(10000);heartbeat(room,humans);room.tick();}
 time.advance(999);heartbeat(room,humans);const before=JSON.stringify(room.s);
 assert(!send(room,token,'action',{action:'discard:invalid-tile'}).ok);assert.equal(JSON.stringify(room.s),before);
 time.advance(9001);heartbeat(room,humans);room.tick();assert.equal(room.s.players[seat].autoReason,'timeout');
}));

test('a failed session save or action save preserves the original inactivity deadline',()=>{
 for(const op of ['autoPlay','resume','action'])clock(time=>{
  const {room,humans}=game(3);time.advance(6000);heartbeat(room,humans);room.tick();
  const seat=room.s.turn,token=room.s.players[seat].token;
  for(let i=0;i<11;i++){time.advance(10000);heartbeat(room,humans);room.tick();}
  const before=JSON.stringify(room.s);room.save=()=>{throw Error('disk full');};
  const extra=op==='autoPlay'?{enabled:true}:op==='resume'?{room:room.s.audioRoom}:{action:room.options(seat).find(a=>a.kind==='discard').id};
  assert(!send(room,token,op,extra).ok);assert.equal(JSON.stringify(room.s),before);room.save=()=>{};
  time.advance(10000);heartbeat(room,humans);room.tick();assert.equal(room.s.players[seat].autoReason,'timeout');
 });
});

test('cancelled rooms and revoked credentials are distinguishable from an active private seat',()=>{
 const {room,host,humans}=game(3),guest=humans[1],pin=room.s.pin;
 const stranger=room.dispatch({op:'peek',token:'unknown'});assert(stranger.ok);assert.equal(stranger.state.phase,'join');
 assert(!('players'in stranger.state));assert(!send(room,'unknown','resume',{room:room.s.audioRoom}).ok);
 ok(send(room,guest,'leaveMatch'));assert.equal(room.dispatch({op:'peek',token:guest}).state.phase,'join');
 ok(send(room,host,'leaveMatch'));assert.equal(room.dispatch({op:'peek',token:host}).state.phase,'empty');
 assert(!room.dispatch({op:'discover',pin}).ok);assert(!send(room,host,'resume').ok);
});

test('unlimited mode never starts idle takeover while connected, but still replaces a disconnected player',()=>clock(time=>{
 const {room,humans}=game(4,4,{idleSeconds:0});time.advance(6000);
 for(let i=0;i<61;i++){heartbeat(room,humans);room.tick();assert(room.s.players.every(p=>!p.auto));time.advance(10000);}
 assert.equal(room.view(0).idleSeconds,0);
 const seat=room.s.players.findIndex(p=>p.token===humans[1]);
 ok(send(room,humans[1],'presence',{present:false}));assert(!room.s.players[seat].auto);time.advance(119999);heartbeat(room,humans.filter(t=>t!==humans[1]));room.presence(time.now);assert(!room.s.players[seat].auto);time.advance(1);room.presence(time.now);assert.equal(room.s.players[seat].autoReason,'offline');assert(room.view(0).players[seat].takeoverMask);
}));

test('real input on another seat restarts its own idle clock, polling alone does not',()=>clock(time=>{
 const {room,humans}=game(4);time.advance(6000);const start=time.now,seat=(room.s.turn+1)%4,token=room.s.players[seat].token;
 for(let i=0;i<10;i++){time.advance(10000);heartbeat(room,humans);room.tick();}
 ok(room.dispatch({op:'state',token,userActive:true}));
 time.advance(20000);heartbeat(room,humans);room.tick();assert.equal(time.now-start,120000);
 assert(!room.s.players[seat].auto);assert(room.s.players.filter((p,i)=>i!==seat).every(p=>p.autoReason==='timeout'));
 for(let i=0;i<9;i++){time.advance(10000);heartbeat(room,humans);room.tick();assert(!room.s.players[seat].auto);}
 time.advance(10000);heartbeat(room,humans);room.presence(time.now);assert.equal(room.s.players[seat].autoReason,'timeout');
 assert(room.view(0).players[seat].takeoverMask);
 ok(room.dispatch({op:'state',token,userActive:true}));assert(!room.s.players[seat].auto);assert(!room.view(0).players[seat].takeoverMask);
}));

test('manual takeover is unmasked, disconnect masks it, and resume restores the same seat',()=>clock(time=>{
 const {room,humans}=game(3),token=humans[1],seat=room.s.players.findIndex(p=>p.token===token);
 ok(send(room,token,'autoPlay',{enabled:true}));assert(!room.view(0).players[seat].takeoverMask);
 time.advance(120000);heartbeat(room,humans.filter(t=>t!==token));room.presence(time.now);
 assert.equal(room.s.players[seat].autoReason,'offline');assert(room.view(0).players[seat].takeoverMask);
 const hand=room.s.players[seat].hand;ok(send(room,token,'resume',{room:room.s.audioRoom}));
 assert(!room.view(0).players[seat].takeoverMask);assert(!room.s.players[seat].auto);assert.equal(room.s.players[seat].hand,hand);
}));

test('guest departure shows the takeover mask until a new human inherits that bot seat',()=>{
 const {room,humans}=game(3),token=humans[1],seat=room.s.players.findIndex(p=>p.token===token);
 ok(send(room,token,'leaveMatch'));assert(room.view(0).players[seat].takeoverMask);
 const reply=ok(room.dispatch({op:'join',pin:room.s.pin,name:'接座玩家'}));assert.equal(reply.state.you,seat);
 assert(!reply.state.players[seat].takeoverMask);assert(!reply.state.players[seat].bot);physicalTiles(room);
});

test('whole-match and unlimited options survive a saved-room restart',()=>{
 const {room}=game(4,4,{rounds:4,idleSeconds:0});assert.equal(room.s.rounds,4);
 const restored=new Room('session-test-host',undefined,JSON.stringify(room.s));
 assert.equal(restored.view(0).rounds,4);assert.equal(restored.view(0).idleSeconds,0);physicalTiles(restored);
});

test('unattended ordinary settlement advances after its grace period, including older saved results',()=>clock(time=>{
 const {room,host}=game(3,1);ok(send(room,host,'presence',{present:false}));room.abort('九种九牌');
 const hand=room.s.handSeq;delete room.s.resultFinishedAt;room.tick();assert.equal(room.s.phase,'result');
 time.advance(119999);room.tick();assert.equal(room.s.phase,'result');
 time.advance(1);room.tick();assert.equal(room.s.phase,'play');assert.equal(room.s.handSeq,hand+1);physicalTiles(room);
}));

test('failed real-input persistence restores the previous idle clock and takeover mask',()=>clock(time=>{
 const {room,humans}=game(3);time.advance(126000);heartbeat(room,humans);room.presence(time.now);
 const token=humans[0],before=JSON.stringify(room.s);room.save=()=>{throw Error('disk full');};
 assert(!room.dispatch({op:'state',token,userActive:true}).ok);assert.equal(JSON.stringify(room.s),before);
 assert(room.view(0).players[0].takeoverMask);
}));

for(const n of [3,4])for(const final of [false,true]){
 test(`${n} players: online auto-play seats wait for presentation acknowledgements (${final?'final':'ordinary'})`,()=>clock(time=>{
  const {room,humans}=game(n,2);
  for(const token of humans)ok(send(room,token,'autoPlay',{enabled:true}));
  room.sound('tsumo',1,'s8',false,0,{from:1,kind:'draw'});
  if(final)terminalFixture(room);else room.finish({title:'托管结算确认回归',delta:room.s.players.map(()=>0),details:[],repeat:false,draw:false});
  const hand=room.s.handSeq,match=room.s.matchSeq,seq=room.s.audioSeq;
  for(let i=0;i<15;i++){time.advance(1000);heartbeat(room,humans);room.tick();assert.equal(room.s.phase,'result');assert.equal(room.s.handSeq,hand);}
  ok(send(room,humans[0],'ready',{handSeq:hand}));
  for(let i=0;i<3;i++){time.advance(1000);heartbeat(room,humans);room.tick();assert.equal(room.s.phase,'result');}
  ok(send(room,humans[1],'ready',{handSeq:hand}));
  if(final){assert.equal(room.s.phase,'result');assert.equal(room.s.handSeq,hand);assert.equal(room.s.matchSeq,match);ok(send(room,humans[0],'next'));}
  assert.equal(room.s.phase,'play');assert.equal(room.s.handSeq,hand+1);assert.equal(room.s.matchSeq,match+(final?1:0));
  assert.equal(room.s.audioEvents.length,0);assert(room.s.audioSeq>=seq);physicalTiles(room);
  const snapshot=JSON.stringify(room.s);assert(!send(room,humans[0],'ready',{handSeq:hand}).ok);assert.equal(JSON.stringify(room.s),snapshot);
 }));
}
