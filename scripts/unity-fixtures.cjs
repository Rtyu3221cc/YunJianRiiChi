const fs=require('node:fs');
globalThis.Majiang=require('../app/src/main/assets/core.js');require('../app/src/main/assets/ai.js');
const {Room}=require('../app/src/main/assets/engine.js');
fs.mkdirSync('build/unity-smoke',{recursive:true});
fs.writeFileSync('build/unity-smoke/host.txt','unity-local-validation');
// Representative previews are legal, engine-generated public player views.
for(const n of [3,4]) {
 let preview;
 for(let attempt=0;attempt<20&&!preview;attempt++) {
  const room=new Room('preview');
  room.dispatch({op:'create',host:'preview',name:'旅人',n,rounds:1,bots:true});
  const token=room.s.players[0].token;
  room.dispatch({op:'start',token,version:room.s.version});
  // Synthetic preview generation runs after the presentation interval.
  room.s.matchOpeningUntil=0;
  for(let step=0;step<600&&room.s.phase!=='result';step++) {
   const view=room.view(0);
   if(view.phase==='play' && view.options.some(a=>a.kind==='discard') && view.players.reduce((total,p)=>total+p.river.length,0)>=n*8) {preview=view;break;}
   const result=room.tick();if(!result.ok)throw Error(result.error);
   if(result.idle) {
    const action=globalThis.RiichiAI.choose(room.view(0));
    if(!action)throw Error('No human preview action');
    const accepted=room.dispatch({op:'action',token,version:room.s.version,action});
    if(!accepted.ok)throw Error(accepted.error);
   }
  }
 }
 if(!preview)throw Error('Could not generate an in-progress preview');
 fs.writeFileSync(`build/unity-smoke/preview-${n}.json`,JSON.stringify(preview));
}
for(const n of [3,4]){
 const room=new Room('fixture');room.dispatch({op:'create',host:'fixture',name:'Unity布局测试',n,bots:true});
 room.dispatch({op:'start',token:room.s.players[0].token,version:room.s.version});
 const view=room.view(0);view.pending=null;view.turn=0;view.phase='play';view.sticks=1;
 view.players[1].riichi=1;
 view.players.forEach((p,j)=>{p.river=Array.from({length:18},(_,i)=>({tile:'p'+(i%9+1),riichi:j===1&&i===4,called:false}));});
 view.players[0].melds=['p555+','s1111','m333+3','z777-'];view.players[0].nuki=3;
 view.hand=view.hand.slice(0,2);view.drawn=view.hand[view.hand.length-1];
 view.players[1].score=-1200;view.players[n-1].score=125000;
 // Synthetic stress fixture, not a legal hand or replay.
 fs.writeFileSync(`build/unity-smoke/fixture-${n}.json`,JSON.stringify(view));
}
// Explicit synthetic scene for call UI, all four hand directions and exposed sets.
const effects=JSON.parse(fs.readFileSync('build/unity-smoke/preview-4.json'));
effects.phase='reaction';effects.turn=3;effects.pending={from:3,tile:'p5',kind:'discard',waiting:[0]};
effects.last={from:3,tile:'p5'};
effects.players.forEach((p,i)=>{
 p.melds=[i%2?'s123-':'p555+'];p.count=10;p.nuki=0;
 p.river=[{tile:'z1'},{tile:'p5',called:true},{tile:'p3'},{tile:'s6'},{tile:'p7'},{tile:'z5'},{tile:'p5'}];
});
effects.hand=effects.hand.slice(0,10);effects.drawn=null;effects.players[0].nuki=2;
effects.options=[{id:'chi:p345-',kind:'chi',meld:'p345-'},{id:'chi:p456-',kind:'chi',meld:'p456-'},{id:'pon:p555-',kind:'pon',meld:'p555-'},{id:'pass',kind:'pass'}];
fs.writeFileSync('build/unity-smoke/effects.json',JSON.stringify(effects));

// Controlled legal win hands; synthetic presentation views, not recorded games.
function winRoom(n){
 const r=new Room('win');r.dispatch({op:'create',host:'win',name:'动画测试',n,rounds:1});
 for(let i=1;i<n;i++)r.dispatch({op:'join',pin:r.s.pin,name:'玩家'+i});
 r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version});
 r.s.interrupted=true;r.s.wall=Array(30).fill('p9');r.s.indicators=Array(5).fill('z4');r.s.ura=Array(5).fill('z4');
 r.s.players.forEach(p=>{p.hand='m19p147s258z12345';p.river=[{tile:'p2'}];});return r;
}
for(const n of [3,4])for(const kind of ['ron','tsumo'])for(let winner=0;winner<n;winner++){
 const r=winRoom(n),from=(winner+1)%n;r.s.turn=kind==='tsumo'?winner:from;
 r.s.players[winner].hand='p123456s123z1112'+(kind==='tsumo'?'z2':'');r.s.players[winner].river=[];
 if(kind==='ron'){r.s.players[from].river.push({tile:'z2',called:false});r.s.last={from,tile:'z2'};}
 const before=r.s.audioSeq;r.win([winner],kind==='ron'?from:null,kind==='ron'?'z2':null);
 for(let you=0;you<2;you++){const v=r.view(you);v.audioEvents=v.audioEvents.filter(e=>e.id>before);fs.writeFileSync(`build/unity-smoke/win-${n}-${kind}-${winner}-${you}.json`,JSON.stringify(v));}
}
const doubled=winRoom(4);for(const i of [1,2]){doubled.s.players[i].hand='p123456s123z1112';doubled.s.players[i].river=[];}
doubled.s.players[0].river.push({tile:'z2',called:false});doubled.s.last={from:0,tile:'z2'};
const seq=doubled.s.audioSeq;doubled.win([1,2],0,'z2');const doubledView=doubled.view(0);doubledView.audioEvents=doubledView.audioEvents.filter(e=>e.id>seq);
fs.writeFileSync('build/unity-smoke/win-double-ron.json',JSON.stringify(doubledView));

// Authoritative waits/no-yaku/furiten and draw DTO scenes, not game replays.
for(const open of [false,true]){
 const r=winRoom(4);r.s.phase='play';r.s.turn=0;r.s.players[0].hand=open?'p23467s789z22p9,m111-':'m111p23467s789z22p9';r.s.players[0].river=[];
 r.s.players[0].bot=false;const v=r.view(0);v.audioRoom='hint-'+open;fs.writeFileSync('build/unity-smoke/hint-'+(open?'open':'closed')+'.json',JSON.stringify(v));
 r.s.players[0].river=[{tile:'p5'}];fs.writeFileSync('build/unity-smoke/hint-furiten.json',JSON.stringify(r.view(0)));
}
for(const n of [3,4])for(let k=0;k<=n;k++){
 const r=winRoom(n);r.s.audioRoom='draw-'+n+'-'+k;r.s.wall=[];
 r.s.players.forEach((p,i)=>{p.hand=i<k?(i===1?'m123456p123s78z22':'p123456s123z1112'):'m19p147s258z12345';p.river=[{tile:'p2'}];});r.exhaust();
 fs.writeFileSync('build/unity-smoke/draw-'+n+'-'+k+'.json',JSON.stringify(r.view(0)));
}
const thirteen=winRoom(3);thirteen.s.phase='play';thirteen.s.turn=1;thirteen.s.players[0].hand='m19p19s19z1234567';thirteen.s.players[0].river=[];
fs.writeFileSync('build/unity-smoke/hint-thirteen.json',JSON.stringify(thirteen.view(0)));

// Abort declaration publishes only the declaring player's hand.
for(const n of [3,4])for(let seat=0;seat<n;seat++){
 const r=winRoom(n);r.s.turn=seat;r.s.interrupted=false;r.s.players[seat].river=[];r.s.players[seat].hand='m19p19s19z1234567z1';
 r.act(seat,'abort9');fs.writeFileSync('build/unity-smoke/nine-'+n+'-'+seat+'.json',JSON.stringify(r.view(0)));
}
