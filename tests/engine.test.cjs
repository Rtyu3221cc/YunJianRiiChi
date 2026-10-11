const {test}=require('node:test'),assert=require('node:assert/strict');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const M=globalThis.Majiang,{Room,tiles,RULE,norm}=require('../app/src/main/assets/engine.js');
function make(n=4){const r=new Room('host');let a=r.dispatch({op:'create',host:'host',n,name:'A',rounds:1});assert(a.ok,a.error);for(let i=1;i<n;i++)assert(r.dispatch({op:'join',pin:r.s.pin,name:'P'+i}).ok);assert(r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version}).ok);return r}
function fixture(n=4){const r=make(n),s=r.s;s.dealer=0;s.turn=0;s.interrupted=true;s.drawKind='normal';s.wall=Array(30).fill('p9');s.indicators=Array(5).fill('z4');s.ura=Array(5).fill('z4');s.phase='play';s.pending=null;for(const p of s.players){p.hand='m19p147s258z12345';p.river=[{tile:'p2'}];}return r}
function action(r,i,id){const ret=r.dispatch({op:'action',token:r.s.players[i].token,version:r.s.version,action:id,id:require('crypto').randomBytes(8).toString('hex')});assert(ret.ok,ret.error+' / '+id);return ret}
function resolvePass(r){while(r.s.phase==='reaction'){const q=r.s.pending;const i=Object.keys(q.options).find(i=>!q.replies[i]);assert.notEqual(i,undefined);action(r,+i,'pass')}}
function conservation(r){const s=r.s,a=[...s.wall,...s.rinshan,...s.indicators,...s.ura,...s.deadFill];for(let i=0;i<s.n;i++){const p=s.players[i],h=r.h(i);a.push(...tiles(h));for(const m of h._fulou)for(const d of m.match(/\d/g))a.push(m[0]+d);a.push(...p.river.filter(x=>!x.called).map(x=>x.tile));for(let j=0;j<p.nuki;j++)a.push('z4');}assert.equal(a.length,s.n===3?108:136);const c={};for(const p of a)c[norm(p)]=(c[norm(p)]||0)+1;for(const count of Object.values(c))assert.equal(count,4);assert.equal(s.players.reduce((t,p)=>t+p.score,0)+s.sticks*1000,s.n===3?105000:100000)}
test('four/sanma walls, red fives and tile conservation',()=>{for(const n of [3,4]){const r=make(n);conservation(r);assert.equal(r.remain(),n===3?54:69);if(n===3)assert(!r.s.wall.some(p=>/^m[2-8]$/.test(p)));}});
test('private views never contain opponents hands, wall, ura or credentials',()=>{const r=make();for(let i=0;i<4;i++){const v=r.view(i);assert.equal(v.hand.length,i===r.s.dealer?14:13);assert(!('wall'in v));assert(!('ura'in v));for(const p of v.players){assert(!('hand'in p));assert(!('token'in p));assert(!('recovery'in p));}}assert.deepEqual(Object.keys(r.view(-1)).sort(),['count','n','phase','started']);});
test('unauthorized, stale, out-of-turn requests are atomic',()=>{const r=make(),before=JSON.stringify(r.s);assert(!r.dispatch({op:'action',token:'bad',version:r.s.version,action:'tsumo'}).ok);assert.equal(JSON.stringify(r.s),before);assert(!r.dispatch({op:'action',token:r.s.players[(r.s.turn+1)%4].token,version:r.s.version,action:'discard:m1'}).ok);assert.equal(JSON.stringify(r.s),before);assert(!r.dispatch({op:'close',token:r.s.players[0].token,version:-1}).ok);});
test('recovery restores existing seat and snapshot reload preserves secret state',()=>{const r=make(3),p=r.s.players[1],res=r.dispatch({op:'join',pin:r.s.pin,recovery:p.recovery});assert.equal(res.token,p.token);assert.equal(r.s.players.length,3);const restored=new Room('host',null,JSON.stringify(r.s));assert.deepEqual(restored.s,r.s);assert.equal(restored.view(1).hand.length,r.view(1).hand.length);});
test('save failure rolls back action',()=>{const r=make();const before=JSON.stringify(r.s);r.save=()=>{throw Error('disk full')};const i=r.s.turn;const a=r.options(i).find(a=>a.kind==='discard');assert(!r.dispatch({op:'action',token:r.s.players[i].token,version:r.s.version,action:a.id}).ok);assert.equal(JSON.stringify(r.s),before);});
test('duplicate request is idempotent',()=>{const r=make(),i=r.s.turn,req={op:'action',token:r.s.players[i].token,version:r.s.version,action:r.options(i).find(a=>a.kind==='discard').id,id:'one'};assert(r.dispatch(req).ok);const after=JSON.stringify(r.s);assert(r.dispatch(req).ok);assert.equal(JSON.stringify(r.s),after)});
test('riichi requires closed tenpai and deposits only after discard survives',()=>{const r=fixture();r.s.players[0].hand='m147p258s369z12345';assert(!r.options(0).some(a=>a.kind==='riichi'));r.s.players[0].hand='m123p123s123z1112z4';assert(r.options(0).some(a=>a.id==='riichi:z4_'));action(r,0,'riichi:z4_');resolvePass(r);assert.equal(r.s.players[0].score,24000);assert.equal(r.s.sticks,1);assert.equal(r.s.players[0].riichi,1)});
test('riichi discard restriction and score floor',()=>{const r=fixture();r.s.players[0].hand='m123p123s123z1112z4*';r.s.players[0].riichi=1;const a=r.options(0).filter(a=>a.kind==='discard');assert.deepEqual(a.map(a=>a.id),['discard:z4_']);r.s.players[0].riichi=0;r.s.players[0].hand=r.s.players[0].hand.replace('*','');r.s.players[0].score=900;assert(!r.options(0).some(a=>a.kind==='riichi'))});
test('dora and nuki alone do not form a yaku',()=>{const r=fixture(3);r.s.players[1].hand='p123s456p78z22,s123-';r.s.players[1].nuki=4;assert.equal(M.Util.xiangting(r.h(1).clone().zimo('p9')),-1);assert.equal(r.score(1,0,'p9'),null)});
test('ordinary no-yaku complete hand cannot ron',()=>{const r=fixture();r.s.players[1].hand='m123p456s78z22,s123-';assert.equal(r.score(1,0,'s9'),null)});
test('pinfu tsumo uses 20 fu, seven pairs uses 25',()=>{const r=fixture();r.s.turn=1;r.s.players[1].hand='m234456p234s2267s8';let v=r.score(1);assert.equal(v.fu,20);assert(v.hupai.some(y=>y.name==='平和'));r.s.players[1].hand='m1122p3344s5566z7z7';v=r.score(1);assert.equal(v.fu,25);assert(v.hupai.some(y=>y.name==='七対子'))});
test('full-set discard furiten blocks all waits, self draw still allowed',()=>{const r=fixture();r.s.players[1].hand='m123p123s123z1122';r.s.players[1].river=[{tile:'z1'}];assert(r.furiten(1));assert.equal(r.score(1,0,'z2'),null);r.put(1,r.h(1).zimo('z2'));r.s.turn=1;assert(r.score(1));});
test('temporary and riichi furiten on missed winning shape',()=>{const r=fixture();r.s.players[1].hand='m123p123s123z1122';r.s.players[1].river=[];r.react(0,'z2');assert(r.s.pending.options[1].some(a=>a.kind==='ron'));action(r,1,'pass');resolvePass(r);assert(!r.s.players[1].temp); // immediately drew next turn
 const q=fixture();q.s.players[2].hand='m123p123s123z1122*';q.s.players[2].riichi=1;q.react(0,'z2');action(q,2,'pass');resolvePass(q);assert(q.s.players[2].permanent);q.draw(2);assert(q.s.players[2].permanent)});
test('riichi declaration ron does not take the 1000 deposit',()=>{const r=fixture();r.s.players[0].hand='m123p123s123z1112z4';r.s.players[1].hand='m123p123s123z3334';r.s.players[1].river=[];action(r,0,'riichi:z4_');action(r,1,'ron');resolvePass(r);assert.equal(r.s.phase,'result');assert.equal(r.s.sticks,0);assert.equal(r.s.players.reduce((x,p)=>x+p.score,0),100000)});
test('double ron waits for all choices; deposit goes to nearest winner',()=>{const r=fixture();r.s.sticks=1;r.s.players[0].score-=1000;for(const i of [1,2]){r.s.players[i].hand='m123p123s123z3334';r.s.players[i].river=[];}r.react(0,'z4');action(r,2,'ron');assert.equal(r.s.phase,'reaction');action(r,1,'ron');resolvePass(r);assert.equal(r.s.result.details.length,2);assert.equal(r.s.result.details[0].seat,1);assert.equal(r.s.sticks,0);assert.equal(r.s.players.reduce((x,p)=>x+p.score,0),100000)});
test('triple ron aborts without paying winners',()=>{const r=fixture();for(const i of [1,2,3]){r.s.players[i].hand='m123p123s123z3334';r.s.players[i].river=[];}r.react(0,'z4');for(const i of [1,2,3])action(r,i,'ron');assert(r.s.result.title.includes('三家和'));assert(r.s.result.delta.every(x=>x===0))});
test('chi only from upstream, and never in sanma; pon outranks chi',()=>{const r=fixture();r.s.players[1].hand='m12456p123s123z11';r.s.players[2].hand='m33p123456s123z11';r.react(0,'m3');assert(r.options(1).some(a=>a.kind==='chi'));assert(!r.options(2).some(a=>a.kind==='chi'));action(r,1,r.options(1).find(a=>a.kind==='chi').id);action(r,2,r.options(2).find(a=>a.kind==='pon').id);resolvePass(r);assert.equal(r.s.turn,2);assert.equal(r.h(2)._bingpai.m[3],0);const q=fixture(3);q.s.players[1].hand='p12456s123789z11';q.react(0,'p3');assert(!q.options(1).some(a=>a.kind==='chi'))});
test('kuikae cannot discard called tile after pon',()=>{const r=fixture();r.s.players[1].hand='m333p123s123z1122';r.react(0,'m3');action(r,1,r.options(1).find(a=>a.kind==='pon').id);resolvePass(r);assert(!r.options(1).some(a=>a.kind==='discard'&&a.tile==='m3'))});
test('ankan gives rinshan, extra dora, and cancels ippatsu',()=>{const r=fixture();r.s.players[0].hand='m111p123s123z1122m1';r.s.players[1].ippatsu=true;const remain=r.remain(),dora=r.view(0).indicators.length;action(r,0,'kan:m1111');resolvePass(r);assert.equal(r.s.drawKind,'kan');assert.equal(r.s.players[0].kans,1);assert.equal(r.remain(),remain-1);assert.equal(r.view(0).indicators.length,dora+1);assert(!r.s.players[1].ippatsu);assert(r.h(0)._zimo)});
test('kakan can be robbed; original pon retained',()=>{const r=fixture();r.s.players[0].hand='p123s123z1122m3,m333+';r.s.players[1].hand='m12p123s123z11222';r.s.players[1].river=[];action(r,0,'kan:m333+3');assert(r.options(1).some(a=>a.kind==='ron'));action(r,1,'ron');resolvePass(r);assert(r.s.result.details[0].score.hupai.some(y=>y.name==='槍槓'));assert.equal(r.s.players[0].kans,0)});
test('kokushi may rob ankan; ordinary hands cannot',()=>{const r=fixture();r.s.players[0].hand='m111p123s123z1122m1';r.s.players[1].hand='m9p19s19z11234567';r.s.players[1].river=[];action(r,0,'kan:m1111');assert(r.options(1).some(a=>a.kind==='ron'));action(r,1,'ron');resolvePass(r);assert(r.s.result.details[0].score.damanguan)});
test('sanma North extraction, dora multiplicity and riichi restriction',()=>{const r=fixture(3);r.s.players[0].hand='p123456s123z1122z4';r.s.players[1].ippatsu=true;const n=r.remain();action(r,0,'nuki');resolvePass(r);assert.equal(r.s.players[0].nuki,1);assert.equal(r.remain(),n-1);assert.equal(r.s.drawKind,'nuki');assert(r.h(0).menqian);assert(!r.s.players[1].ippatsu);const q=fixture(3);q.s.turn=1;q.s.players[1].hand='p123456s123z44p78p9*';q.s.players[1].riichi=1;assert(!q.options(1).some(a=>a.kind==='nuki'));});
test('committed North event carries public tile and extraction index once',()=>{
 const r=fixture(3);r.s.players[0].hand='p123456s123z1122z4';r.s.players[0].nuki=1;
 const before=r.s.audioSeq;action(r,0,'nuki');resolvePass(r);
 const events=r.view(1).audioEvents.filter(e=>e.id>before&&e.kind==='nuki');
 assert.equal(events.length,1);assert.equal(events[0].tile,'z4');assert.equal(events[0].seat,0);assert.equal(events[0].nuki,2);
 assert.deepEqual(r.view(2).audioEvents.filter(e=>e.kind==='nuki'),r.view(1).audioEvents.filter(e=>e.kind==='nuki'));

});
test('sanma extracted North contributes bonus but preserves natural yaku',()=>{const r=fixture(3);r.s.turn=1;r.s.players[1].hand='p123456s123z1112z2';r.s.players[1].nuki=2;r.s.indicators[0]='z3';const score=r.score(1);assert.equal(score.hupai.find(y=>y.name==='拔北宝牌').fanshu,4);assert.equal(score.delta.reduce((a,b)=>a+b),0)});
test('sanma one-man indicator means nine-man dora',()=>{const r=fixture(3);r.s.turn=1;r.s.players[1].hand='m99p123456s123z11z1';r.s.indicators[0]='m1';assert.equal(r.score(1).hupai.find(y=>y.name==='ドラ').fanshu,2)});
test('sanma tsumo-loss removes north payment, ron remains full',()=>{const r=fixture(3);r.s.turn=1;r.s.players[1].hand='p123456s123z1112z2';const san=r.score(1);const full=M.Util.hule(r.h(1),null,M.Util.hule_param({menfeng:1,baopai:['z4']}));assert.equal(san.delta[0],full.fenpei[0]);assert.equal(san.delta[2],full.fenpei[2]);assert.equal(san.delta[1],-san.delta[0]-san.delta[2]);});
test('nine-terminals abort only first uninterrupted turn',()=>{const r=fixture();r.s.interrupted=false;r.s.players[0].river=[];r.s.players[0].hand='m19p19s19z1234567z1';assert(r.options(0).some(a=>a.kind==='abort9'));action(r,0,'abort9');assert(r.s.result.abort);assert(r.s.result.repeat)});
test('four wind first discards abort, three winds do not',()=>{const r=fixture();r.s.interrupted=false;r.s.players.forEach(p=>p.river=[{tile:'z1'}]);r.react(3,'z1');resolvePass(r);assert.equal(r.s.result.title,'四风连打');const q=fixture(3);q.s.interrupted=false;q.s.players.forEach(p=>p.river=[{tile:'z1'}]);q.react(2,'z1');resolvePass(q);assert.equal(q.s.phase,'play')});
test('four riichi abort; three riichi in sanma continues',()=>{for(const n of [3,4]){const r=fixture(n);r.s.players.forEach(p=>p.riichi=1);r.react(n-1,'z7','discard',null,true);resolvePass(r);assert.equal(r.s.phase,n===4?'result':'play');if(n===4)assert.equal(r.s.result.title,'四家立直')}});
test('four kans abort after discard except four by same player',()=>{const r=fixture();r.s.players[0].kans=3;r.s.players[1].kans=1;r.react(0,'z7');resolvePass(r);assert.equal(r.s.result.title,'四杠散了');const q=fixture();q.s.players[0].kans=4;q.react(0,'z7');resolvePass(q);assert.equal(q.s.phase,'play');assert(!q.options(q.s.turn).some(a=>a.kind==='kan'))});
test('exhaustive draw tenpai penalties: 3000 four players / 2000 three',()=>{for(const n of [3,4]){const r=fixture(n);r.s.players[0].hand='p123456s123z1112';r.exhaust();assert.equal(r.s.result.delta[0],n===3?2000:3000);assert(r.s.result.repeat);assert.equal(r.s.result.delta.reduce((a,b)=>a+b),0);assert.equal(r.s.result.revealed[1],null)}});
test('nagashi mangan and called discard invalidation',()=>{const r=fixture();r.s.players[1].river=[{tile:'m1'},{tile:'z7'}];r.exhaust();assert.equal(r.s.result.title,'流局满贯');assert.deepEqual(r.s.result.delta,[-4000,8000,-2000,-2000]);const q=fixture();q.s.players[1].river=[{tile:'z7',called:true}];q.exhaust();assert.equal(q.s.result.title,'荒牌流局')});
test('negative score ends match; all players must acknowledge next hand',()=>{const r=fixture();r.s.players[2].score=-100;r.abort('test');assert(r.s.result.matchOver);assert(!r.dispatch({op:'next',token:r.s.players[0].token,version:r.s.version}).ok);for(let i=0;i<4;i++)assert(r.dispatch({op:'ready',token:r.s.players[i].token,version:r.s.version}).ok);assert(r.dispatch({op:'next',token:r.s.players[0].token,version:r.s.version}).ok);assert(r.s.players.every(p=>p.score===25000));conservation(r)});
test('riichi kan cannot change wait set',()=>{const r=fixture();r.s.players[0].hand='m111p123s123z1122m1*';r.s.players[0].riichi=1;assert(r.options(0).some(a=>a.id==='kan:m1111'));const before=r.waits(r.h(0).clone().dapai('m1'));action(r,0,'kan:m1111');resolvePass(r);const after=r.waits(r.h(0).clone().dapai(r.h(0)._zimo));assert.deepEqual(before,after)});
test('random playable full hands preserve physical tiles and total points',()=>{
 for(const n of [3,4])for(let game=0;game<12;game++){
  const r=make(n);let steps=0;
  while(['play','reaction'].includes(r.s.phase)&&steps++<650){
   conservation(r);let i,opts;
   if(r.s.phase==='reaction'){i=+Object.keys(r.s.pending.options).find(k=>!r.s.pending.replies[k]);opts=r.options(i)}else{i=r.s.turn;opts=r.options(i)}
   assert(opts.length,'no legal action');let a=opts.find(a=>['ron','tsumo'].includes(a.kind));
   if(!a&&r.s.phase==='reaction')a=game%3===0?opts.find(a=>['minkan','pon','chi'].includes(a.kind)):null;
   if(!a&&r.s.phase==='reaction')a=opts.find(a=>a.kind==='pass');
   if(!a)a=opts.find(a=>['nuki','kan','riichi'].includes(a.kind));
   if(!a){const ds=opts.filter(a=>a.kind==='discard');ds.sort((a,b)=>M.Util.xiangting(r.h(i).clone().dapai(a.id.slice(8)))-M.Util.xiangting(r.h(i).clone().dapai(b.id.slice(8))));a=ds[0];}
   action(r,i,a.id);
  }
  assert(steps<650,'hand did not finish');conservation(r);assert.equal(r.s.phase,'result');
 }
});

test('tsumo event carries public winning tile and source for every peer',()=>{
 for(const n of [3,4]){const r=fixture(n);r.s.turn=1;r.s.players[1].hand='p123456s123z1112z2';const seq=r.s.audioSeq;action(r,1,'tsumo');
 const e=r.view(0).audioEvents.filter(e=>e.id>seq);assert.equal(e.length,1);assert.deepEqual([e[0].kind,e[0].seat,e[0].tile,e[0].from,e[0].winSource],['tsumo',1,'z2',1,'tsumo']);
 for(let i=0;i<n;i++)assert.deepEqual(r.view(i).audioEvents,r.view(0).audioEvents);assert.equal(r.s.result.details[0].tile,'z2');}
});
test('double ron emits each winner on the same source tile; triple ron emits no win',()=>{
 for(const count of [2,3]){const r=fixture();for(let i=1;i<=count;i++){r.s.players[i].hand='p123456s123z1112';r.s.players[i].river=[];}const seq=r.s.audioSeq;r.react(0,'z2');for(let i=1;i<=count;i++)action(r,i,'ron');resolvePass(r);
 const e=r.view(0).audioEvents.filter(e=>e.id>seq && e.kind==='ron');assert.equal(e.length,count===3?0:2);for(const event of e)assert.deepEqual([event.tile,event.from,event.winSource],['z2',0,'discard']);}
});
test('robbed kakan event targets added tile rather than previous discard or whole meld',()=>{
 const r=fixture();r.s.players[0].hand='p123s123z1122m3,m333+';r.s.players[1].hand='m12p123s123z11222';r.s.players[1].river=[];r.s.last={from:2,tile:'p9'};const seq=r.s.audioSeq;
 action(r,0,'kan:m333+3');action(r,1,'ron');resolvePass(r);const e=r.view(2).audioEvents.filter(e=>e.id>seq&&e.kind==='ron');assert.equal(e.length,1);assert.deepEqual([e[0].tile,e[0].from,e[0].winSource],['m3',0,'kakan']);assert.equal(r.s.result.details[0].winSource,'kakan');
});
test('ordinary ron publishes exact tile and source without exposing private wall',()=>{
 const r=fixture();r.s.players[1].hand='m123p123s123z3334';r.s.players[1].river=[];r.react(0,'z4');action(r,1,'ron');resolvePass(r);const e=r.view(0).audioEvents.find(e=>e.kind==='ron');assert.deepEqual([e.tile,e.from,e.winSource],['z4',0,'discard']);assert(!('wall' in r.view(0)));assert.equal(r.s.result.details[0].from,0);
});

test('settlement publishes winning tiles, melds and typed yaku only after a win',()=>{
 for(const ron of [false,true]){const r=fixture(3);r.s.turn=1;r.s.players[1].hand=ron?'p123456s123z1112':'p123456s123z1112z2';r.s.players[1].nuki=2;r.s.players[1].riichi=1;
 assert.equal(r.view(0).result,null);r.win([1],ron?0:null,ron?'z2':null);const d=r.view(0).result.details[0];
 assert.equal(d.tiles.length,13);assert.equal(d.tile,'z2');assert.deepEqual(d.melds,[]);assert.equal(d.nuki,2);assert.equal(d.ura.length,1);
 assert(d.yaku.every(y=>typeof y.han==='number'&&typeof y.yakuman==='number'));assert(d.yaku.some(y=>y.name==='拔北宝牌'));assert.deepEqual(r.view(2).result.details[0],d);
 }
});
test('yakuman yaku stars become an integer multiplier for native settlement',()=>{
 const r=fixture();r.s.turn=1;r.s.players[1].hand='m119p19s19z1234567';r.win([1]);const d=r.view(0).result.details[0];assert(d.score.damanguan>0);assert(d.yaku.some(y=>y.yakuman>0&&y.han===0));assert(d.limit.includes('役满'));
});
test('double ron settlement retains independent public winning hands and scores',()=>{
 const r=fixture();for(const i of [1,2]){r.s.players[i].hand='m123p123s123z3334';r.s.players[i].river=[];}r.win([1,2],0,'z4');
 for(const d of r.s.result.details){assert.equal(d.tiles.length,13);assert.equal(d.tile,'z4');assert(d.yaku.length>0);assert(d.score.defen>0);}assert.equal(r.s.result.details.length,2);
});

test('discard wait hints retain colored tile identities and distinguish open no-yaku waits',()=>{
 const r=fixture();r.s.players[0].hand='p23467s789z22p9,m111-';r.s.players[0].river=[];
 const before=JSON.stringify(r.s),v=r.view(0),hint=v.discardHints.find(x=>v.options.find(a=>a.id===x.id)?.tile==='p9');
 assert(hint);assert.deepEqual(hint.waits.map(w=>w.tile),['p5','p8']);assert(hint.waits.every(w=>!w.ron&&!w.tsumo));assert(!hint.furiten);assert.equal(JSON.stringify(r.s),before);
});
test('closed no-yaku ron waits allow self draw, and riichi discard hints add the yaku',()=>{
 const r=fixture();r.s.players[0].hand='m111p23467s789z22p9';r.s.players[0].river=[];
 const v=r.view(0),regular=v.discardHints.find(h=>h.id.startsWith('discard:p9')),declared=v.discardHints.find(h=>h.id.startsWith('riichi:p9'));
 assert(regular&&declared);assert(regular.waits.every(w=>!w.ron&&w.tsumo));assert(declared.waits.every(w=>w.ron&&w.tsumo));
});
test('furiten badge survives a draw and previews a discard into any wait',()=>{
 const r=fixture();r.s.players[0].hand='m111p23467s789z22p9';r.s.players[0].river=[{tile:'p5'}];
 assert(r.view(0).furiten);const v=r.view(0),hint=v.discardHints.find(h=>h.id.startsWith('discard:p9'));assert(hint.furiten);
 r.s.players[0].river=[];r.s.players[0].permanent=true;assert(r.view(0).furiten);
 r.s.players[0].permanent=false;r.s.players[0].hand='m111p23467s789z22p5';const next=r.view(0).discardHints.find(h=>h.id.startsWith('discard:p5'));assert(next.furiten);
});
test('temporary furiten clears on the next draw, while riichi permanent furiten remains',()=>{
 const r=fixture();r.s.players[0].hand='p123456s123z1112';r.s.players[0].temp=true;r.s.wall=['p9'];assert(r.view(0).furiten);
 r.draw(0);assert(!r.view(0).furiten);r.s.players[0].permanent=true;assert(r.view(0).furiten);
});
test('wait metadata exposes only the requesting hand and never depends on opponents concealed tiles',()=>{
 const r=fixture();r.s.players[0].hand='m111p23467s789z22p9';const pick=v=>({waitDetails:v.waitDetails,discardHints:v.discardHints,furiten:v.furiten});
 const a=pick(r.view(0));r.s.players[1].hand='m123456789p111z2';assert.deepEqual(pick(r.view(0)),a);
 assert(!JSON.stringify(a).includes(r.s.players[1].hand));assert(!r.view(1).discardHints.length);
});
test('exhaustive draw reveals waits/tiles only for tenpai seats and transfers the existing noten pool',()=>{
 for(const n of [3,4])for(let k=0;k<=n;k++){
  const r=fixture(n);r.s.wall=[];r.s.players.forEach((p,i)=>{p.hand=i<k?'p123456s123z1112':'m19p147s258z12345';p.river=[{tile:'p2'}];});r.exhaust();
  const result=r.view(0).result;assert.equal(result.title,'荒牌流局');assert.equal(result.drawDetails.length,n);assert.equal(result.delta.reduce((a,b)=>a+b,0),0);
  result.drawDetails.forEach((d,i)=>{assert.equal(d.seat,i);assert.equal(d.tenpai,i<k);assert.equal(d.tiles.length,i<k?13:0);assert.equal(d.waits.length,i<k?1:0);if(i<k)assert.deepEqual(d.waits,['z2']);else assert.equal(result.revealed[i],null);});
  if(k&&k<n){const pool=n===3?2000:3000;result.delta.forEach((d,i)=>assert.equal(d,i<k?pool/k:-pool/(n-k)));}
  else assert(result.delta.every(d=>d===0));
 }
});
test('sanma waits include all thirteen kokushi sides and exclude removed manzu ranks',()=>{
 const r=fixture(3);r.s.players[0].hand='m19p19s19z1234567';r.s.players[0].river=[];
 const v=r.view(0);assert.equal(v.waitDetails.length,13);assert(v.waitDetails.every(w=>w.ron&&w.tsumo));assert(!v.waitDetails.some(w=>/^m[2-8]$/.test(w.tile)));
});

test('nine terminals reveals only the declaring hand in all public views',()=>{
 for(const n of [3,4])for(let seat=0;seat<n;seat++){
  const r=fixture(n);r.s.turn=seat;r.s.interrupted=false;r.s.players[seat].river=[];r.s.players[seat].hand='m19p19s19z1234567z1';
  const expected=tiles(r.h(seat));action(r,seat,'abort9');
  for(let viewer=0;viewer<n;viewer++){const v=r.view(viewer);assert.deepEqual(v.result.abortHand,{seat,tiles:expected});assert.equal(v.result.delta.reduce((a,b)=>a+b),0);assert(v.players.every(p=>!('hand'in p)));assert.equal(v.result.drawDetails,undefined);}
 }
 const r=fixture();r.abort('四风连打');assert.equal(r.view(0).result.abortHand,undefined);
});
test('discard events identify the committed river slot without private hand data',()=>{
 const r=fixture();r.s.players[0].hand='p123456s123z1112p9';const prior=r.s.players[0].river.length;
 action(r,0,'discard:p9_');const e=r.view(1).audioEvents.findLast(e=>e.kind==='discard');assert.equal(e.riverIndex,prior+1);assert.equal(e.tile,'p9');assert(!('hand'in e));
});

test('opening order is viewer-private and preserves actual deal including dealer draw',()=>{
 for(const n of [3,4]){const r=make(n);for(let i=0;i<n;i++){
  const v=r.view(i);assert.equal(v.handSeq,1);assert(v.dealtAt>0);
  assert.deepEqual(v.openingHand.slice().sort(),v.hand.slice().sort());
  assert.equal(v.openingHand.length,i===r.s.dealer?14:13);
  if(i===r.s.dealer)assert.equal(v.openingHand.at(-1),v.drawn);
  for(const p of v.players)assert(!('dealt' in p)&&!('openingHand' in p));
  const saved=v.openingHand.slice();v.openingHand.length=0;assert.deepEqual(r.view(i).openingHand,saved);
 }}
});
test('opening sequence changes on new hands and survives host snapshot restore',()=>{
 const r=make(3),seq=r.view(0).handSeq,at=r.view(0).dealtAt;r.newHand();
 assert.equal(r.view(0).handSeq,seq+1);assert(r.view(0).dealtAt>=at);
 const restored=new Room('host',null,JSON.stringify(r.s));assert.deepEqual(restored.view(1).openingHand,r.view(1).openingHand);
 assert.equal(restored.view(1).handSeq,seq+1);conservation(r);
});

test('wait remaining counts own hand and public faces without hidden information',()=>{
 const r=fixture();r.s.players.forEach(p=>{p.river=[];p.nuki=0});r.s.indicators=['z7','z6','m1','m9','p9'];
 r.s.players[0].hand='m123p123s123z1122m5';r.s.players[0].river=[{tile:'z1'}];r.s.players[1].river=[{tile:'z1'}];
 const hint=r.view(0).discardHints.find(h=>h.id==='discard:m5_');assert(hint);assert.equal(hint.waits.find(w=>w.tile==='z1').remaining,0);assert.equal(hint.waits.find(w=>w.tile==='z2').remaining,2);
 const before=hint.waits;r.s.wall=Array(30).fill('z2');r.s.players[1].hand='z1111222233334';r.s.players[2].hand='z1111222233334';
 assert.deepEqual(r.view(0).discardHints.find(h=>h.id==='discard:m5_').waits,before);
});
test('unseen counts normalize red fives and do not double count called discards',()=>{
 const r=fixture();r.s.players.forEach(p=>{p.river=[];p.nuki=0});r.s.indicators=['z7','z6','m1','m9','p9'];
 r.s.players[0].hand='m123p123s123z1122';r.s.players[1].hand='m19p147s258z12345,p055+';
 r.s.players[1].river=[{tile:'p0',called:true}];assert.equal(r.unseen(0,'p5'),1);assert.equal(r.unseen(0,'p0'),1);
 r.s.players[2].river=[{tile:'p5'}];assert.equal(r.unseen(0,'p5'),0);
 r.s.players[1].hand='m19p147s258z12345,p5555';assert.equal(r.unseen(0,'p5'),0);
 r.s.players[1].nuki=3;assert.equal(r.unseen(0,'z4'),1);r.s.indicators[0]='z4';assert.equal(r.unseen(0,'z4'),0);
 const q=fixture();q.s.indicators=['z7','z6','m1','m9','p9'];q.s.players[1].hand='m19p147s258z12345,p555+';
 assert.equal(q.unseen(0,'p5'),1);q.s.pending={from:1,tile:'p0',kind:'kakan'};assert.equal(q.unseen(0,'p5'),0);
 q.s.players[1].hand='m19p147s258z12345';q.s.pending={from:1,tile:'p5',kind:'ankan'};assert.equal(q.unseen(0,'p5'),0);
 q.s.players[1].nuki=1;q.s.pending={from:1,tile:'z4',kind:'nuki'};assert.equal(q.unseen(0,'z4'),1);
 const s=fixture(3);assert.equal(s.unseen(0,'m2'),0);
});

test('full-match identity differs from hand identity and survives recovery',()=>{
 const r=make(3),first=r.view(0);assert.equal(first.matchSeq,1);assert(first.matchStartedAt>0);r.newHand();assert.equal(r.view(0).matchSeq,1);assert.equal(r.view(0).matchStartedAt,first.matchStartedAt);
 r.s.phase='result';r.s.result={matchOver:true};r.s.players.forEach(p=>p.ready=true);const next=r.dispatch({op:'next',token:r.s.players[0].token,version:r.s.version});assert(next.ok,next.error);assert.equal(next.state.matchSeq,2);assert(next.state.matchStartedAt>=first.matchStartedAt);
 assert.equal(new Room('host',null,JSON.stringify(r.s)).view(1).matchSeq,2);assert(!('matchOpeningUntil'in next.state));assert(next.state.players.every(p=>!('hand'in p)));conservation(r);
});
