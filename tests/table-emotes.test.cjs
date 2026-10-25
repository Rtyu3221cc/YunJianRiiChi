const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
function ok(r){assert.equal(r.ok,true,r.error);return r;}
function send(r,token,op,extra={}){return r.dispatch({op,token,version:r.s.version,...extra});}
function table(extra={}){
 const r=new Room('emote-test'),h=ok(r.dispatch({op:'create',host:'emote-test',name:'甲',n:4,character:'kawei',skin:401004,...extra})).token;
 const guests=[['乙','yiji',400101],['丙','weilai',406903],['丁','fuji',403805]].map(([name,character,skin])=>ok(r.dispatch({op:'join',pin:r.s.pin,name,character,skin})).token);
 ok(send(r,h,'start'));return {r,h,guests};
}
test('table emoji registry matches every character-specific source ID, including non-contiguous IDs',()=>{
 const root=path.resolve(__dirname,'..'),roles=JSON.parse(fs.readFileSync(path.join(root,'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json'),'utf8')).roles;
 const code=fs.readFileSync(path.join(root,'app/src/main/assets/engine.js'),'utf8'),registry=JSON.parse(code.match(/const CHARACTER_EMOJIS=(\{[^\r\n]+\});/)[1]);
 for(const role of roles)assert.deepEqual(registry[role.key],role.emojis.map(e=>Number(e.split('/').pop())).sort((a,b)=>a-b));
 assert.equal(Object.keys(registry).length,roles.length);assert.ok(registry.kawei.includes(965));assert.ok(!registry.yiji.includes(965));
});
test('emoji sender identity comes from the frozen seat, and foreign/invalid IDs are rejected',()=>{
 const {r,h,guests}=table();const result=ok(send(r,h,'emote',{emoji:965,character:'fuji',skin:403805}));
 assert.equal(result.state.emotes[0].character,'kawei');assert.equal(result.state.emotes[0].skin,401004);assert.equal(result.state.emotes[0].seat,0);
 for(const emoji of [965,9,-1,'0',1.2,999999])assert.equal(send(r,guests[0],'emote',{emoji,character:'kawei'}).ok,false);
 assert.equal(r.dispatch({op:'emote',emoji:0,token:'invalid'}).ok,false);
});
test('emotes have independent versions, per-seat cooldown, and idempotent retries',()=>{
 const {r,h,guests}=table(),version=r.s.version;
 ok(r.dispatch({op:'emote',token:h,version:-1,id:'one',emoji:0}));assert.equal(r.s.version,version);
 ok(r.dispatch({op:'emote',token:h,id:'one',emoji:0}));assert.equal(r.s.emoteSeq,1);
 assert.equal(send(r,h,'emote',{emoji:1,id:'two'}).ok,false);
 ok(send(r,guests[0],'emote',{emoji:1}));assert.equal(r.s.emotes.length,2);
 assert.ok(r.view(0).emoteCooldownMs>0);r.s.players[0].lastEmoteAt=Date.now()-5001;ok(send(r,h,'emote',{emoji:1}));
 assert.equal(r.s.version,version);assert.equal(r.s.emoteSeq,3);
});
test('emotes use the match-selected role and old events expire for reconnecting viewers',()=>{
 const {r,h}=table({character:'yiji',skin:400101,randomCharacters:true,characterPool:[{character:'fuji',skin:403805}]});
 const view=ok(send(r,h,'emote',{emoji:974})).state;assert.equal(view.emotes[0].character,'fuji');assert.equal(view.players[0].character,'fuji');
 r.s.emotes[0].at=Date.now()-5001;assert.equal(r.view(0).emotes.length,0);
 r.s.phase='lobby';assert.equal(send(r,h,'emote',{emoji:0}).ok,false);
});
test('bot images default to Ichihime and random selection stays inside the supplied installed pool',()=>{
 const r=new Room('bots'),h=ok(r.dispatch({op:'create',host:'bots',n:4,name:'房主'})).token;
 ok(send(r,h,'addBot'));assert.deepEqual([r.s.players[1].character,r.s.players[1].skin],['yiji',400101]);
 const pool=[{character:'weilai',skin:406903},{character:'fuji',skin:403805}];
 ok(send(r,h,'fillBots',{randomBotAppearance:true,botCharacters:pool}));
 const bots=r.s.players.slice(2);assert.deepEqual(new Set(bots.map(p=>p.character)),new Set(['weilai','fuji']));
 assert.ok(bots.every(p=>pool.some(c=>c.character===p.character&&c.skin===p.skin)));
 ok(send(r,h,'start'));assert.ok(r.view(0).players.filter(p=>p.bot).every(p=>p.character&&p.skin));
});
test('random bot setup validates ownership, handles a one-role pool, and cannot be changed by guests',()=>{
 const r=new Room('bots'),h=ok(r.dispatch({op:'create',host:'bots',n:4,name:'房主'})).token,g=ok(r.dispatch({op:'join',pin:r.s.pin,name:'客人'})).token;
 const before=JSON.stringify(r.s);assert.equal(send(r,g,'addBot',{randomBotAppearance:true,botCharacters:[{character:'fuji',skin:403805}]}).ok,false);assert.equal(JSON.stringify(r.s),before);
 for(const botCharacters of [[],[{character:'unknown',skin:400101}],[{character:'weilai',skin:403805}]])assert.equal(send(r,h,'addBot',{randomBotAppearance:true,botCharacters}).ok,false);
 ok(send(r,h,'fillBots',{randomBotAppearance:true,botCharacters:[{character:'fuji',skin:403805}]}));assert.ok(r.s.players.filter(p=>p.bot).every(p=>p.character==='fuji'));
});

function botTable(){
 const r=new Room('replies'),h=ok(r.dispatch({op:'create',host:'replies',n:4,name:'房主',bots:true,randomBotAppearance:true,botCharacters:[{character:'yiji',skin:400101},{character:'weilai',skin:406903},{character:'fuji',skin:403805}]})).token;
 ok(send(r,h,'start'));return {r,h};
}
function simulatedReply(random,run){const originalNow=Date.now,originalRandom=Math.random,clock={now:10000000};try{Date.now=()=>clock.now;Math.random=random;run(clock);}finally{Date.now=originalNow;Math.random=originalRandom;}}
test('AI replies wait at least 1200ms, use each frozen character and never trigger a reply chain',()=>{
 simulatedReply(()=>0,clock=>{const {r,h}=botTable(),version=r.s.version;ok(send(r,h,'emote',{emoji:0}));assert.equal(r.s.botEmoteReplies.length,3);
  clock.now+=1199;ok(r.tick());assert.equal(r.s.emoteSeq,1);clock.now++;ok(r.tick());assert.equal(r.s.emoteSeq,4);assert.equal(r.s.version,version);assert.equal(r.s.botEmoteReplies.length,0);
  for(const event of r.s.emotes.slice(1)){const p=r.s.players[event.seat];assert.ok(p.bot);assert.equal(event.character,p.matchCharacter);assert.equal(event.skin,p.matchSkin);assert.equal(event.at,clock.now);}
  clock.now+=2000;ok(r.tick());assert.equal(r.s.emoteSeq,4);assert.equal(r.s.botEmoteReplies.length,0);
 });
});
test('AI replies may be skipped and their randomized delay includes the requested 1700ms bound',()=>{
 simulatedReply(()=>.99,clock=>{const {r,h}=botTable();ok(send(r,h,'emote',{emoji:0}));assert.equal(r.s.botEmoteReplies.length,0);clock.now+=1700;ok(r.tick());assert.equal(r.s.emoteSeq,1);});
 let sample=0;simulatedReply(()=>[0,.999999,.5][sample++%3],clock=>{const {r,h}=botTable();ok(send(r,h,'emote',{emoji:0}));assert.ok(r.s.botEmoteReplies.every(e=>e.dueAt-clock.now===1700));clock.now+=1699;ok(r.tick());assert.equal(r.s.emoteSeq,1);clock.now++;ok(r.tick());assert.equal(r.s.emoteSeq,4);});
});
test('scheduled cosmetic replies survive a room restart and a failed save does not duplicate them',()=>{
 simulatedReply(()=>0,clock=>{const {r,h}=botTable();ok(send(r,h,'emote',{emoji:0}));const restored=new Room('replies',()=>{throw Error('disk full');},JSON.stringify(r.s));clock.now+=1200;
  assert.equal(restored.tick().ok,false);assert.equal(restored.s.emoteSeq,1);assert.equal(restored.s.botEmoteReplies.length,3);restored.save=()=>{};ok(restored.tick());assert.equal(restored.s.emoteSeq,4);ok(restored.tick());assert.equal(restored.s.emoteSeq,4);
 });
});
test('AI replies are discarded across settlement, a changed hand or a stale suspended host',()=>{
 for(const scenario of ['result','new-hand','expired'])simulatedReply(()=>0,clock=>{const {r,h}=botTable();ok(send(r,h,'emote',{emoji:0}));
  if(scenario==='result')r.s.phase='result';if(scenario==='new-hand')r.s.handSeq++;clock.now+=scenario==='expired'?4000:1200;
  if(scenario==='result'){assert.equal(r.tickEmotes(clock.now),true);}else ok(r.tick());assert.equal(r.s.emoteSeq,1);assert.equal(r.s.botEmoteReplies.length,0);
 });
});
