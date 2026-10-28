const fs=require('node:fs');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
const r=new Room('wardrobe-r6');let s;
const send=q=>{const a=r.dispatch(q);if(!a.ok)throw Error(a.error);s=r.s;return a};
send({op:'create',host:'wardrobe-r6',n:4,name:'动作预览',rounds:1});
for(let i=1;i<4;i++)send({op:'join',pin:s.pin,name:'预览'+i});
send({op:'start',token:s.players[0].token,version:s.version});
s.dealer=0;s.turn=0;s.interrupted=true;s.drawKind='normal';s.wall=Array(30).fill('p9');s.indicators=Array(5).fill('z4');s.ura=Array(5).fill('z4');s.phase='play';s.pending=null;
for(const p of s.players){p.hand='m19p147s258z12345';p.river=[{tile:'p2'}];}
s.players[0].hand='m123p123s123z1112z4';s.players[1].hand='m19p147s258z12344';
const out='build/wardrobe-r6-study';fs.mkdirSync(out,{recursive:true});
const snapshot=name=>fs.writeFileSync(out+'/riichi-'+name+'.json',JSON.stringify(r.view(0)));
snapshot('before');send({op:'action',token:s.players[0].token,version:s.version,action:'riichi:z4_',id:'r6-declare'});snapshot('declared');
let n=0;while(s.phase==='reaction'){
 const seat=Object.keys(s.pending.options).find(i=>!s.pending.replies[i]);
 if(seat===undefined)throw Error('No pending responder');
 send({op:'action',token:s.players[seat].token,version:s.version,action:'pass',id:'r6-pass-'+n++});
}
snapshot('committed');
const events=r.view(0).audioEvents;for(const kind of ['discard','riichi','riichi-stick'])if(!events.some(e=>e.kind===kind))throw Error('Missing '+kind);
if(r.view(0).players.some(p=>p.hand))throw Error('Opponent private hands leaked');
console.log('R6 legal public declaration fixtures PASS',events.map(e=>e.kind).join(', '));
