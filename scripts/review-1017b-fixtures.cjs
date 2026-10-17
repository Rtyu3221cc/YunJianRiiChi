// Disposable, engine-generated player views for visual rehearsal; no test runner.
const fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');
require('../app/src/main/assets/ai.js');
const {Room,tiles}=require('../app/src/main/assets/engine.js');
const output=path.join(__dirname,'../build/review-1017b/fixtures');fs.mkdirSync(output,{recursive:true});
function seed(hand,{n=4,turn=0,river=[]}={}){
 const room=new Room('visual-rehearsal-only');let reply=room.dispatch({op:'create',host:room.host,name:'演示玩家',n,rounds:4,red:false,bots:true,lobbyFlow:true});
 room.dispatch({op:'start',token:reply.token,version:reply.state.version,fillBots:true});
 const s=room.s;s.dealer=s.initialDealer=0;s.turn=turn;s.phase='play';s.round=0;s.matchStartedAt=Date.now()-30000;s.matchOpeningUntil=s.roundOpeningUntil=0;s.interrupted=true;s.audioEvents=[];s.audioSeq=0;
 const deck=[];for(const suit of ['m','p','s','z'])for(let i=1;i<=(suit==='z'?7:9);i++)if(n===4||suit!=='m'||i===1||i===9)for(let k=0;k<4;k++)deck.push(suit+i);
 const take=tile=>{const i=deck.indexOf(tile);if(i<0)throw Error('Preview exceeds physical tile count: '+tile);return deck.splice(i,1)[0];};
 s.players[0].hand=Majiang.Shoupai.fromString(hand).toString();tiles(room.h(0)).forEach(take);river.forEach(take);
 for(let i=0;i<n;i++){
  const p=s.players[i];p.river=i===0?river.map(tile=>({tile,called:false,riichi:false,tsumogiri:false})):[];p.bot=i!==0;p.auto=false;p.riichi=0;p.kans=p.nuki=0;p.ready=false;
  if(i>0)p.hand=new Majiang.Shoupai(deck.splice(0,13)).toString();p.dealt=tiles(room.h(i));
 }
 s.rinshan=deck.splice(0,4);s.indicators=deck.splice(0,5);s.ura=deck.splice(0,5);s.deadFill=[];s.wall=deck;s.last=null;s.pending=null;
 return room;
}
function save(name,view){fs.writeFileSync(path.join(output,name+'.json'),JSON.stringify(view));console.log(name,JSON.stringify({rounds:view.rounds,phase:view.phase,yakuman:view.yakumanIndicator,waits:view.waitDetails,discardCandidates:view.discardHints?.length}));}
let room=seed('m123456p234s226z1s7');save('before-discard',room.view(0));
room.act(0,'discard:z1');room.s.version++;save('after-discard',room.view(0));
room=seed('m999p3444s777z666',{turn:1,river:['p3']});save('yakuman-chance',room.view(0));
room=seed('m1999p444s777z666',{turn:1});save('yakuman-certain',room.view(0));
room=seed('m123456p234s226z1s7');room.s.phase='lobby';room.s.players.forEach(p=>p.hand='');save('ready-full',room.view(0));
for(const n of [3,4]){
 room=seed(n===3?'m19p123456s123z112':'m123456p234s226z1s7',{n});
 room.s.round=n*4-1;room.finish({title:'荒牌流局',delta:Array(n).fill(0),details:[],drawDetails:[],draw:true,repeat:false});
 console.log('Full match final wind',JSON.stringify({players:n,rounds:room.s.rounds,round:room.s.round,matchOver:room.s.result.matchOver}));
}
