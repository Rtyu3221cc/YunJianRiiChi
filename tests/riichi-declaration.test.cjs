const {test}=require('node:test'),assert=require('node:assert/strict');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
function make(n=4){const r=new Room('host');let a=r.dispatch({op:'create',host:'host',n,name:'A',rounds:1});assert(a.ok,a.error);for(let i=1;i<n;i++)assert(r.dispatch({op:'join',pin:r.s.pin,name:'P'+i}).ok);assert(r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version}).ok);return r}
function fixture(n=4){const r=make(n),s=r.s;s.dealer=0;s.turn=0;s.interrupted=true;s.drawKind='normal';s.wall=Array(30).fill('p9');s.indicators=Array(5).fill('z4');s.ura=Array(5).fill('z4');s.phase='play';s.pending=null;for(const p of s.players){p.hand='m19p147s258z12345';p.river=[{tile:'p2'}];}return r}
function action(r,i,id){const ret=r.dispatch({op:'action',token:r.s.players[i].token,version:r.s.version,action:id,id:require('crypto').randomBytes(8).toString('hex')});assert(ret.ok,ret.error+' / '+id);return ret}
// The Unity client plays the original sideways LiQiPai clip for a declaration and the
// plain discard clip otherwise, so the committed event has to carry the distinction.
test('a riichi declaration discard carries the flag that selects the sideways clip',()=>{
 const r=fixture();r.s.players[0].hand='m123p123s123z1112z4';
 action(r,0,'riichi:z4_');
 const events=r.view(0).audioEvents.filter(e=>e.kind==='discard');
 assert.equal(events.length,1);
 assert.equal(events[0].seat,0);assert.equal(events[0].tile,'z4');assert.equal(events[0].riichi,true);
});
test('an ordinary discard is not flagged as a declaration',()=>{
 const r=fixture();r.s.players[0].hand='m123p123s123z1112z4';
 action(r,0,r.options(0).find(a=>a.kind==='discard').id);
 const event=r.view(0).audioEvents.filter(e=>e.kind==='discard').pop();
 assert.equal(event.riichi,false);
});
// The response marker is drawn from the player's own options, so the engine must keep
// creating them for calling seats only; a lone 'pass' is never a call.
test('response options are created only for seats that can actually call',()=>{
 const r=fixture();r.s.players[1].hand='m33p147s258z12345';r.s.players[2].hand='m19p147s258z12345';r.s.players[3].hand='m19p147s258z12345';
 r.react(0,'m3');
 assert.equal(r.s.phase,'reaction');
 const q=r.s.pending;
 assert(q.options[1].some(a=>a.kind==='pon'));
 assert(q.options[1].some(a=>a.kind==='pass'));
 assert.equal(q.options[2],undefined);assert.equal(q.options[3],undefined);
 assert(r.view(1).options.some(a=>a.kind!=='pass'));
 assert.deepEqual(r.view(2).options,[]);assert.deepEqual(r.view(3).options,[]);
});
test('a discard nobody can call opens no reaction window at all',()=>{
 const r=fixture();r.s.players.forEach(p=>p.hand='m19p147s258z12345');
 r.react(0,'z7');
 assert.equal(r.s.pending,null);assert.equal(r.s.phase,'play');
 for(let i=0;i<4;i++)assert.deepEqual(r.view(i).options.filter(a=>a.kind!=='discard'&&a.kind!=='riichi'),[]);
});
