const fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');const {Room}=require('../app/src/main/assets/engine.js');
const out=path.resolve(__dirname,'../build/original-input-r18/fixtures');fs.mkdirSync(out,{recursive:true});
const savedRandom=Math.random;let seed=181029;Math.random=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
function make(){const secret='source-input-r18',r=new Room(secret);const ok=q=>{const v=r.dispatch(q);if(!v.ok)throw Error(v.error);return v;};ok({op:'create',host:secret,n:4,name:'原版流程演示',rounds:1});for(let i=1;i<4;i++)ok({op:'join',pin:r.s.pin,name:'牌友'+i});ok({op:'start',token:r.s.players[0].token,version:r.s.version});return r;}
for(const kind of ['held','drawn']){
 const r=make(),seat=r.s.turn,before=JSON.parse(JSON.stringify(r.view(seat)));const choice=before.options.find(o=>o.kind==='discard'&&o.id.endsWith('_')===(kind==='drawn'));if(!choice)throw Error('No legal '+kind+' discard');
 const presence=r.dispatch({op:'presence',token:r.s.players[seat].token,room:before.audioRoom,present:true});if(!presence.ok)throw Error(presence.error);const poll=JSON.parse(JSON.stringify(r.view(seat)));
 const reply=r.dispatch({op:'action',token:r.s.players[seat].token,version:r.s.version,action:choice.id,id:'r18-'+kind});if(!reply.ok)throw Error(reply.error);
 const after=r.view(seat);if(after.players[seat].river.length!==1||after.players[seat].river[0].tsumogiri!==(kind==='drawn'))throw Error('Incorrect engine discard');
 if(before.players[seat].river.length!==0||poll.players[seat].river.length!==0)throw Error('Before snapshots changed after engine action');
 fs.writeFileSync(path.join(out,kind+'-before.json'),JSON.stringify(before));fs.writeFileSync(path.join(out,kind+'-poll.json'),JSON.stringify(poll));fs.writeFileSync(path.join(out,kind+'-after.json'),JSON.stringify(after));fs.writeFileSync(path.join(out,kind+'-action.json'),JSON.stringify(choice));
}
Math.random=savedRandom;console.log('Actual engine public before/after fixtures: held discard and tsumogiri PASS');
