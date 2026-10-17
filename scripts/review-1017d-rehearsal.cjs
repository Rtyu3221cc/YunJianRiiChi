// Disposable room rehearsal and public views. No unit-test runner or user save.
const fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');require('../app/src/main/assets/ai.js');
const {Room,tiles}=require('../app/src/main/assets/engine.js');
const out=path.join(__dirname,'../build/review-1017d/fixtures');fs.mkdirSync(out,{recursive:true});
let now=Date.now();Date.now=()=>now;
function room(n=4,idleSeconds=120){const r=new Room('isolated-rehearsal');let x=r.dispatch({op:'create',host:r.host,name:'演示玩家',n,rounds:1,idleSeconds,bots:true,lobbyFlow:true});x=r.dispatch({op:'start',token:x.token,version:x.state.version,fillBots:true});now+=7000;r.seen[r.s.players[0].token]=now;r.s.players[0].lastInputAt=now;return r;}
function save(name,v){fs.writeFileSync(path.join(out,name+'.json'),JSON.stringify(v));}
const report={activity:[],matches:[]};
let r=room(),token=r.s.players[0].token;
const status=label=>{const p=r.view(0).players[0];const item={label,auto:p.auto,reason:p.autoReason,mask:p.takeoverMask,idleSeconds:r.s.idleSeconds};report.activity.push(item);console.log(JSON.stringify(item));save(label,r.view(0));};
now+=119000;r.seen[token]=now;r.presence(now);status('idle-119');
now+=1001;r.seen[token]=now;r.presence(now);status('idle-taken');
r.dispatch({op:'resume',token});status('idle-returned');
r.dispatch({op:'autoPlay',token,enabled:true});status('manual-ai');
r.dispatch({op:'presence',token,present:false});status('offline-ai');r.dispatch({op:'resume',token});
now+=119000;r.dispatch({op:'state',token,userActive:true});now+=119000;r.seen[token]=now;r.presence(now);status('activity-kept-human');
r=room(4,0);token=r.s.players[0].token;now+=600000;r.seen[token]=now;r.presence(now);status('unlimited-human');
for(const [n,offline]of [[3,true],[4,false]]){
 r=room(n);token=r.s.players[0].token;const p=r.s.players[0];p.auto=true;p.autoReason=offline?'offline':'manual';p.away=offline;
 const history=[];let previousHand=0,steps=0;
 for(;steps<40000;steps++){
  now+=1000;if(!offline)r.seen[token]=now;
  if(r.s.phase==='result'){
   if(previousHand!==r.s.handSeq){history.push({hand:r.s.handSeq,round:r.s.round,honba:r.s.honba,title:r.s.result.title,matchOver:r.s.result.matchOver});previousHand=r.s.handSeq;}
   if(r.s.result.matchOver){
    const hand=r.s.handSeq,match=r.s.matchSeq;r.dispatch({op:'ready',token,handSeq:hand});now+=15000;r.tick();
    if(r.s.phase!=='result'||r.s.handSeq!==hand||r.s.matchSeq!==match)throw Error('AI terminal result restarted unexpectedly');
    save('ai-final-'+n,r.view(0));break;
   }
   if(!offline)r.dispatch({op:'ready',token,handSeq:r.s.handSeq});
  }
  const result=r.tick();if(!result.ok)throw Error(result.error);
 }
 if(steps===40000)throw Error('AI match did not reach its terminal result');
 const entry={players:n,offline,actions:steps,hands:history.length,terminal:r.s.result.matchOver,heldAfterReady:true,history};report.matches.push(entry);console.log('FULL AI MATCH',JSON.stringify({...entry,history:undefined}));
}
// Synthetic native-animation views, with real source hand clips.
const base=JSON.parse(fs.readFileSync(path.join(__dirname,'../build/unity-smoke/preview-4.json')));
base.phase='play';base.you=0;base.turn=2;base.audioRoom='riichi-depth-preview';base.audioEvents=[];base.audioSeq=0;base.options=[];base.result=null;base.round=0;base.honba=0;
base.players.forEach(p=>{p.river=[];p.melds=[];p.riichi=0;p.auto=false;p.takeoverMask=false;});
base.players[2].river=['m1','p2','s3','z1','p4','s6','m7','z4'].map((tile,i)=>({tile,called:false,riichi:i===7}));base.players[2].riichi=1;base.last={from:2,tile:'z4'};save('riichi-depth',base);
const response=JSON.parse(JSON.stringify(base));response.phase='reaction';response.turn=3;response.audioRoom='response-order-preview';response.players[2].river=[];response.players[2].riichi=0;response.players[3].river=['m1','p2','s3','z1','p4','s6','s5'].map(tile=>({tile,called:false,riichi:false}));response.pending={from:3,tile:'s5',kind:'discard',waiting:[0]};response.last={from:3,tile:'s5'};response.options=[{id:'pon:s555-',kind:'pon',meld:'s555-'},{id:'pass',kind:'pass'}];save('response-order',response);
// Legal four-concealed-triplets single wait: a natural double yakuman ron.
r=room();const s=r.s;s.dealer=1;s.initialDealer=2;s.round=3;s.interrupted=true;s.turn=2;s.phase='reaction';s.players.forEach((p,i)=>{p.hand='';p.river=[];p.riichi=0;p.nuki=p.kans=0;p.score=i===2?70000:10000;});
s.players[0].hand=Majiang.Shoupai.fromString('m111999p111s999z1').toString();
const deck=[];for(const suit of ['m','p','s','z'])for(let i=1;i<=(suit==='z'?7:9);i++)for(let k=0;k<4;k++)deck.push(suit+i);
const take=t=>{let i=deck.indexOf(t);if(i<0)throw Error('Too many fixture tiles');deck.splice(i,1);};tiles(r.h(0)).forEach(take);take('z1');
for(let i=1;i<4;i++)s.players[i].hand=new Majiang.Shoupai(deck.splice(0,13)).toString();
s.players[2].river=[{tile:'z1',called:false,riichi:false}];s.last={from:2,tile:'z1'};s.rinshan=deck.splice(0,4);s.indicators=deck.splice(0,5);s.ura=deck.splice(0,5);s.deadFill=[];s.wall=deck;s.audioEvents=[];s.audioSeq=0;r.win([0],2,'z1');
const double=r.view(0);double.audioEvents=[];if(double.result.details[0].score.damanguan!==2)throw Error('Fixture is not a natural double yakuman');save('double-yakuman',double);console.log('DOUBLE YAKUMAN',double.result.details[0].score.defen,double.result.details[0].limit);
const ready=JSON.parse(JSON.stringify(base));ready.phase='lobby';ready.pin='479009';ready.idleSeconds=120;save('ready',ready);
fs.writeFileSync(path.join(out,'rehearsal-report.json'),JSON.stringify(report,null,2));
