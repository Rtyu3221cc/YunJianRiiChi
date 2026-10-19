/* Public-state integration checks for per-match character selection. */
'use strict';
const assert=require('node:assert/strict'),crypto=require('node:crypto'),fs=require('node:fs');
globalThis.Majiang=require('../app/src/main/assets/core.js');const {Room}=require('../app/src/main/assets/engine.js');
const report={checks:[],randomSelections:[]};
function verify(name,fn){fn();report.checks.push(name);}
function command(room,token,op,extra={}){return room.dispatch({op,token,version:room.s.version,id:crypto.randomUUID(),...extra});}
function create(extra={}){const room=new Room('character-check');const r=room.dispatch({op:'create',host:'character-check',name:'房主',n:4,rounds:2,...extra});assert(r.ok,r.error);return {room,token:r.token};}
verify('every viewer receives the same public character and skin',()=>{
 const {room,token}=create({character:'weilai',skin:406903});
 const a=room.dispatch({op:'join',pin:room.s.pin,name:'甲',character:'siqiqiansuili',skin:403304});assert(a.ok,a.error);
 const b=room.dispatch({op:'join',pin:room.s.pin,name:'乙',character:'erjietang',skin:400207});assert(b.ok,b.error);
 assert(command(room,token,'addBot').ok);assert(command(room,token,'start').ok);
 const expected=room.view(0).players.map(p=>[p.character,p.skin]);for(let i=1;i<4;i++)assert.deepEqual(room.view(i).players.map(p=>[p.character,p.skin]),expected);
 assert(expected.some(p=>p[0]==='weilai'&&p[1]===406903));assert(expected.some(p=>p[0]==='siqiqiansuili'&&p[1]===403304));
 const wire=JSON.stringify(room.view(1));assert(!wire.includes('characterPool'));assert(!wire.includes(token));assert(!wire.includes(a.token));
});
verify('dorm and active-selection edits cannot alter a match already started',()=>{
 const {room,token}=create({character:'erjietang',skin:400201});assert(command(room,token,'start',{fillBots:true}).ok);const before=JSON.stringify(room.s);
 assert(!command(room,token,'character',{character:'weilai',skin:406901}).ok);assert.equal(JSON.stringify(room.s),before);
 assert.equal(room.view(0).players[0].character,'erjietang');
});
verify('random choices are fixed across hands, polling, rejoin and reload',()=>{
 const pool=[{character:'weilai',skin:406903},{character:'weilai',skin:406901},{character:'yiji',skin:400107}];
 for(let i=0;i<24;i++){
  const {room,token}=create({character:'erjietang',skin:400201,randomCharacters:true,characterPool:pool});assert(command(room,token,'start',{fillBots:true}).ok);
  const p=room.s.players[0],snapshot=[p.matchCharacter,p.matchSkin];assert(pool.some(c=>c.character===snapshot[0]&&c.skin===snapshot[1]));report.randomSelections.push(snapshot);
  for(let h=0;h<3;h++){room.newHand();assert.deepEqual([p.matchCharacter,p.matchSkin],snapshot);assert.deepEqual([room.view(0).players[0].character,room.view(0).players[0].skin],snapshot);}
  const recovered=new Room('character-check',()=>{},JSON.stringify(room.s));assert.deepEqual([recovered.view(0).players[0].character,recovered.view(0).players[0].skin],snapshot);
  const joined=recovered.dispatch({op:'join',token,pin:recovered.s.pin,character:'siqiqiansuili',skin:403301});assert(joined.ok,joined.error);assert.deepEqual([recovered.view(0).players[0].character,recovered.view(0).players[0].skin],snapshot);
 }
});
verify('invalid character/skin, cross-character skins and empty random pools roll back',()=>{
 for(const bad of [{character:'missing',skin:400101},{character:'weilai',skin:400101},{character:'weilai',skin:406901,randomCharacters:true,characterPool:[]},{character:'weilai',skin:406901,characterPool:[{character:'yiji',skin:406903}]}]){
  const {room,token}=create();const before=JSON.stringify(room.s);assert(!command(room,token,'character',bad).ok);assert.equal(JSON.stringify(room.s),before);
 }
});
verify('changing the waiting-room choice is reflected for other viewers',()=>{
 const {room,token}=create();assert(room.dispatch({op:'join',pin:room.s.pin,name:'牌友'}).ok);assert(command(room,token,'character',{character:'siqiqiansuili',skin:403303}).ok);assert.equal(room.view(1).players[0].skin,403303);
});
fs.mkdirSync('build/characters-1018',{recursive:true});fs.writeFileSync('build/characters-1018/protocol-checks.json',JSON.stringify(report,null,2));console.log('Character public-state checks passed:',report.checks.length);
