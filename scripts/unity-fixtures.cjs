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
