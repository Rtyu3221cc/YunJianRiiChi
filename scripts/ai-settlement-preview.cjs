// Disposable loopback host for the Unity visual review. No test runner or user save.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const root=path.resolve(__dirname,'..'),out=path.join(root,'build/ai-settlement-preview');
fs.mkdirSync(out,{recursive:true});
globalThis.Majiang=require(root+'/app/src/main/assets/core.js');
require(root+'/app/src/main/assets/ai.js');
const {Room,tiles}=require(root+'/app/src/main/assets/engine.js');
let room,scenario,armed=0,forced=false,last='',trace=[];
function record(kind,data={}){const row={at:Date.now(),scenario,kind,...data};trace.push(row);fs.writeFileSync(path.join(out,'host-timeline.json'),JSON.stringify(trace,null,2));console.log(JSON.stringify(row));}
function seed(name){
 scenario=name;armed=0;forced=false;last='';room=new Room('ai-settlement-preview-only');
 const n=name==='sanma-tsumo'?3:4;
 let reply=room.dispatch({op:'create',host:room.host,name:'全员托管演示',n,rounds:1,red:false,bots:true});
 room.dispatch({op:'start',token:reply.token,version:room.s.version});
 const s=room.s;s.dealer=0;s.initialDealer=0;s.turn=name==='four-double-ron'?0:1;s.interrupted=true;
 s.round=name==='final-four-tsumo'?3:0;
 s.matchStartedAt=Date.now()-20000;s.matchOpeningUntil=0;s.roundOpeningUntil=Date.now()+3500;
 s.players.forEach((p,i)=>{p.name=i?'电脑 · '+i:'全员托管演示';p.ready=false;p.river=[];});
 // A deterministic near-win deal, with all 108/136 physical tiles accounted for.
 const deck=[];for(const suit of ['m','p','s','z'])for(let j=1;j<=(suit==='z'?7:9);j++){
  if(n===3&&suit==='m'&&j!==1&&j!==9)continue;for(let k=0;k<4;k++)deck.push(suit+j);
 }
 const take=t=>{const i=deck.indexOf(t);if(i<0)throw Error('Fixture tile exhausted: '+t);deck.splice(i,1);return t;};
 const fixed=name==='four-double-ron'?['m789p789s123z1122z4','m123p456s789z5554','m456p123s456z6664']:[];
 if(!fixed.length)fixed[1]=n===3?'p234456s23467z22s8':'m234456p234s2267s8';
 for(let i=0;i<n;i++)if(fixed[i]){s.players[i].hand=Majiang.Shoupai.fromString(fixed[i]).toString();tiles(room.h(i)).forEach(take);}
 // Shuffle the remaining tiles before filling the other concealed hands.
 for(let i=deck.length-1;i>0;i--){const j=Math.floor(Math.random()*(i+1));[deck[i],deck[j]]=[deck[j],deck[i]];}
 for(let i=0;i<n;i++)if(!fixed[i])s.players[i].hand=new Majiang.Shoupai(deck.splice(0,13)).toString();
 s.players.forEach(p=>p.dealt=tiles(Majiang.Shoupai.fromString(p.hand)));
 s.rinshan=deck.splice(0,4);s.indicators=deck.splice(0,5);s.ura=deck.splice(0,5);s.deadFill=[];s.wall=deck;
 s.audioEvents=[];s.audioSeq=0;s.version++;
 if(name==='final-four-tsumo'){s.players[1].score=42000;s.players[0].score=8000;}
 record('seed',{n,handSeq:s.handSeq,turn:s.turn});
}
const server=http.createServer(async(req,res)=>{
 try{
  if(req.url.startsWith('/preview/seed?')){seed(new URL(req.url,'http://localhost').searchParams.get('scenario'));res.end('seeded');return;}
  if(req.url==='/preview/stop'){res.end('stopped');clearInterval(timer);server.close();return;}
  if(req.url!=='/api'||!room){res.writeHead(404);res.end();return;}
  let body='';for await(const part of req)body+=part;
  const request=JSON.parse(body),before=room.s?.handSeq,reply=room.dispatch(request);
  if(request.op==='autoPlay'&&reply.ok){armed=Date.now();record('auto-enabled');}
  if(request.op==='ready')record('presentation-ready',{handSeq:request.handSeq,ok:reply.ok,phase:room.s.phase,nextHand:room.s.handSeq});
  if(room.s?.handSeq!==before)record('next-hand',{handSeq:room.s.handSeq,oldCalls:room.s.audioEvents.length});
  res.setHeader('Content-Type','application/json');res.end(JSON.stringify(reply));
 }catch(e){res.writeHead(500);res.end(String(e));console.error(e);}
});
const started=Date.now();
const timer=setInterval(()=>{
 if(Date.now()-started>600000){clearInterval(timer);server.close();return;}
 if(!room||!armed)return;
 if(!forced&&Date.now()>=room.s.roundOpeningUntil){
  forced=true;
  if(scenario==='four-double-ron'){room.act(0,'discard:z4_');room.s.version++;record('staged-discard');}
  if(scenario==='four-draw'){room.exhaust();room.s.version++;}
 }
 const result=room.tick();if(!result.ok)record('engine-error',{error:result.error});
 const s=room.s,key=[s.phase,s.handSeq,s.audioSeq].join(':');
 if(key!==last){record('state',{phase:s.phase,handSeq:s.handSeq,audioSeq:s.audioSeq,ready:s.players.map(p=>p.ready),result:s.result?.title,matchOver:s.result?.matchOver});last=key;}
},50);
server.listen(8765,'127.0.0.1',()=>console.log('Unity auto settlement preview host on loopback:8765'));
