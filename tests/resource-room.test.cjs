const test=require('node:test'),assert=require('node:assert/strict'),{randomUUID}=require('node:crypto');
globalThis.Majiang=require('../app/src/main/assets/core.js');const {Room}=require('../app/src/main/assets/engine.js');
function send(r,t,op,x={}){return r.dispatch({op,token:t,version:r.s?.version,id:randomUUID(),...x})}
function ok(x){assert(x.ok,x.error);return x}
function setup(){const r=new Room('resource-test');const h=ok(r.dispatch({op:'create',host:'resource-test',n:3,name:'甲',lobbyFlow:true,resourceProtocol:1,character:'bamuwei',skin:400701,randomCharacters:true,characterPool:[{character:'bamuwei',skin:400701},{character:'bamuwei',skin:400709}]})).token;const g=ok(r.dispatch({op:'join',name:'乙',pin:r.s.pin,resourceProtocol:1})).token;ok(send(r,h,'addBot'));ok(send(r,g,'lobbyReady',{ready:true}));return {r,h,g}}
function ack(r,t,state='ready',fallbackCharacters=[]){return send(r,t,'resourceAck',{setupRevision:r.s.resourceSetup.revision,resourceStatus:state,fallbackCharacters})}
test('resource-aware start requires a frozen preparation and every human report',()=>{const {r,h,g}=setup();assert(!send(r,h,'start').ok);const prepared=ok(send(r,h,'prepareResources')).state.resourceSetup;assert.equal(r.s.phase,'lobby');assert.equal(prepared.ready,false);ok(ack(r,h));assert(!send(r,h,'start',{setupRevision:prepared.revision}).ok);ok(ack(r,g,'downloading'));assert(!send(r,h,'start',{setupRevision:prepared.revision}).ok);ok(ack(r,g,'fallback',['bamuwei']));assert.equal(r.view(0).resourceSetup.ready,true);const chosen=r.s.players[0].matchSkin;ok(send(r,h,'start',{setupRevision:prepared.revision}));assert.equal(r.s.phase,'play');assert.equal(r.s.players[0].matchSkin,chosen);assert.equal(r.s.players[0].matchCharacter,'bamuwei')});
test('retries do not reroll the random character and do not expose private credentials',()=>{const {r,h}=setup();ok(send(r,h,'prepareResources'));const choice=JSON.stringify(r.s.resourceSetup.choices),revision=r.s.resourceSetup.revision;for(let i=0;i<10;i++){const v=ok(send(r,h,'prepareResources')).state;assert.equal(JSON.stringify(v.resourceSetup.choices),choice);assert.equal(v.resourceSetup.revision,revision);assert(!JSON.stringify(v).includes(r.s.players[1].token));assert(!JSON.stringify(v).includes(r.s.players[0].token));}assert.equal(r.view(0).players[0].skin,r.s.players[0].matchSkin)});
test('selection is locked while prepared and stale resource reports are rejected atomically',()=>{const {r,h,g}=setup();ok(send(r,h,'prepareResources'));const before=JSON.stringify(r.s);assert(!send(r,h,'character',{character:'yiji',skin:400101}).ok);assert.equal(JSON.stringify(r.s),before);assert(!send(r,g,'resourceAck',{setupRevision:0,resourceStatus:'ready'}).ok);assert.equal(JSON.stringify(r.s),before);assert(!ack(r,g,'fallback',['unknown']).ok);assert.equal(JSON.stringify(r.s),before);ok(send(r,h,'cancelResourceSetup'));assert(!r.s.resourceSetup);ok(send(r,h,'character',{character:'yiji',skin:400101}));});
test('guest cannot cancel another preparation; leaving or unreadying removes the barrier',()=>{for(const action of ['leaveLobby','lobbyReady']){const {r,h,g}=setup();ok(send(r,h,'prepareResources'));assert(!send(r,g,'cancelResourceSetup').ok);ok(send(r,g,action,{ready:false}));assert(!r.s.resourceSetup);assert.equal(r.s.phase,'lobby')}});
test('legacy clients and bots do not create impossible acknowledgement waits',()=>{const {r,h,g}=setup();r.s.players[1].resourceProtocol=0;ok(send(r,h,'prepareResources'));ok(ack(r,h));assert(r.resourcesReady());assert.equal(r.view(0).resourceSetup.players[1].state,'ready')});
test('LAN offers accept only bounded public resource capabilities, not arbitrary URLs',()=>{const {r,h,g}=setup();const hash='a'.repeat(64),ticket='b'.repeat(32);assert(!send(r,g,'resourceOffer',{peerAddress:'8.8.8.8',resourceShare:{port:8767,ticket,hashes:[hash]}}).ok);assert(!send(r,g,'resourceOffer',{resourceShare:{port:80,ticket,hashes:[hash]}}).ok);assert(!send(r,g,'resourceOffer',{resourceShare:{port:8767,ticket,hashes:['../key']}}).ok);ok(send(r,g,'resourceOffer',{peerAddress:'192.168.43.2',resourceShare:{port:8767,ticket,hashes:[hash,hash]}}));const peers=r.view(0).resourcePeers;assert.equal(peers.length,1);assert.equal(peers[0].address,'192.168.43.2');assert.deepEqual(peers[0].hashes,[hash]);assert(!JSON.stringify(peers).includes(g))});
test('resource state survives a room snapshot without changing frozen identities',()=>{const {r,h,g}=setup();ok(send(r,h,'prepareResources'));ok(ack(r,h));const saved=JSON.stringify(r.s);const recovered=new Room('resource-test',null,saved);assert.deepEqual(recovered.s.resourceSetup.choices,r.s.resourceSetup.choices);assert.equal(recovered.s.players[0].matchSkin,r.s.players[0].matchSkin)});
test('host recovery resets acknowledgements while retaining frozen choices',()=>{const {r,h,g}=setup();ok(send(r,h,'prepareResources'));ok(ack(r,h));ok(ack(r,g));const old=r.s.resourceSetup.revision;const recovered=new Room('resource-test',null,JSON.stringify(r.s));assert(recovered.s.resourceSetup.revision>old);assert(!recovered.resourcesReady());assert(!send(recovered,h,'resourceAck',{setupRevision:old,resourceStatus:'ready'}).ok);assert.deepEqual(recovered.s.resourceSetup.choices,r.s.resourceSetup.choices)});
test('an abandoned resource preparation expires without dealing a hand',()=>{const {r,h}=setup();ok(send(r,h,'prepareResources'));r.s.resourceSetup.createdAt=Date.now()-180001;r.tick();assert(!r.s.resourceSetup);assert.equal(r.s.phase,'lobby');assert(!r.s.players[0].hand)});

test('preparation freezes wardrobes and validates per-client basic-outfit fallback',()=>{
 const {r,h,g}=setup(),wardrobe={winEffect:'ron-26dj',cloth:'tablecloth_25xila'};
 ok(send(r,h,'character',{character:'bamuwei',skin:400701,wardrobe}));
 const prepared=ok(send(r,h,'prepareResources')).state.resourceSetup;
 assert.deepEqual(prepared.wardrobes[0],wardrobe);
 const before=JSON.stringify(r.s);
 assert.equal(send(r,g,'resourceAck',{setupRevision:prepared.revision,resourceStatus:'fallback',fallbackWardrobes:['ron-flame']}).ok,false);
 assert.equal(JSON.stringify(r.s),before);
 ok(send(r,g,'resourceAck',{setupRevision:prepared.revision,resourceStatus:'fallback',fallbackWardrobes:['ron-26dj','ron-26dj']}));
 assert.deepEqual(r.view(0).resourceSetup.players[1].fallbackWardrobes,['ron-26dj']);
 ok(ack(r,h));ok(send(r,h,'start',{setupRevision:prepared.revision}));
 assert.equal(r.s.players[0].matchWardrobe.winEffect,'ron-26dj');
});

test('download heartbeats retain preparation while abandoned downloads still expire',()=>{
 const {r,h}=setup();ok(send(r,h,'prepareResources'));r.s.resourceSetup.createdAt=Date.now()-240000;
 ok(ack(r,h,'downloading'));r.tick();assert.ok(r.s.resourceSetup);
 r.s.resourceSetup.updatedAt=Date.now()-180001;r.tick();assert.ok(!r.s.resourceSetup);
});

test('LAN capability accommodates wardrobe dependencies with a finite hash limit',()=>{
 const {r,g}=setup(),hashes=Array.from({length:1024},(_,i)=>i.toString(16).padStart(64,'0'));
 const share={port:8767,ticket:'c'.repeat(32),hashes};
 ok(send(r,g,'resourceOffer',{peerAddress:'192.168.43.2',resourceShare:share}));
 assert.equal(r.view(0).resourcePeers[0].hashes.length,1024);
 const before=JSON.stringify(r.s);
 assert.equal(send(r,g,'resourceOffer',{resourceShare:{...share,hashes:[...hashes,'f'.repeat(64)]}}).ok,false);
 assert.equal(JSON.stringify(r.s),before);
});

test('a random character is rolled on entering the room and kept for the match',()=>{const seen=new Set();for(let k=0;k<12;k++){const {r,h}=setup();const lobby=r.view(0).players[0],pool=[400701,400709];assert(pool.includes(lobby.skin),'room shows a pool skin, not the fixed battle skin: '+lobby.skin);assert.equal(lobby.character,'bamuwei');ok(send(r,h,'prepareResources'));assert.equal(r.s.players[0].matchSkin,lobby.skin);assert.equal(r.view(0).players[0].skin,lobby.skin);seen.add(lobby.skin)}assert(seen.has(400709),'the room never showed a rolled pool skin')});
test('AI players can wear random downloaded outfits and every client is asked to prepare them',()=>{
 const pools=[{category:'winEffect',ids:['ron-flame','ron-wind','not-a-real-id']},{category:'stick',ids:['liqi_fish']},{category:'cloth',ids:['tablecloth_green']},{category:'hand',ids:['hand-cat-orange']}];
 const r=new Room('bot-wear');const h=ok(r.dispatch({op:'create',host:'bot-wear',n:3,name:'甲',lobbyFlow:true,resourceProtocol:1,randomBotWardrobe:true,botWardrobes:pools})).token;
 const wear={randomBotWardrobe:true,botWardrobes:pools};ok(send(r,h,'addBot',wear));ok(send(r,h,'addBot',wear));
 for(const bot of r.s.players.filter(p=>p.bot)){assert(['ron-flame','ron-wind'].includes(bot.wardrobe.winEffect),JSON.stringify(bot.wardrobe));assert.equal(bot.wardrobe.stick,'liqi_fish');assert(!('cloth' in bot.wardrobe),'viewer-level cloth must not be assigned');assert(!('hand' in bot.wardrobe),'AI players keep the default hand')}
 assert(r.view(0).players.filter(p=>p.bot).every(p=>p.wardrobe.winEffect));
 ok(send(r,h,'prepareResources'));const wanted=r.view(0).resourceSetup.wardrobes;assert(wanted.some(w=>w.stick==='liqi_fish'),'bot outfits enter the shared preparation');
 const plain=new Room('bot-plain');const p=ok(plain.dispatch({op:'create',host:'bot-plain',n:3,name:'乙',botWardrobes:pools})).token;ok(send(plain,p,'addBot'));assert.deepEqual(plain.s.players[1].wardrobe,{});
 assert(!new Room('bot-bad').dispatch({op:'create',host:'bot-bad',n:3,name:'丙',randomBotWardrobe:true,botWardrobes:'x'}).ok);
});