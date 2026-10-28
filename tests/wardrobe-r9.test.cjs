const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');
const {Room}=require('../app/src/main/assets/engine.js');
const root=path.join(__dirname,'../unity/Assets/Resources/Majsoul/Actions');

test('sakura river trial retains the complete authored layer and texture set',()=>{
 const base=JSON.parse(fs.readFileSync(path.join(root,'sakura-win-effect.json'))),trial=JSON.parse(fs.readFileSync(path.join(root,'callout-sakura.json')));
 assert.equal(base.layers.length,8);
 assert.deepEqual(trial.layers.map(l=>[l.node,l.name,l.texture]),base.layers.map(l=>[l.node,l.name,l.texture]));
 assert.deepEqual(trial.nodes,base.nodes);
});

test('both new effect loadouts survive room publication and the match snapshot',()=>{
 const a={callout:'callout-frost',riichiEffect:'riichi-afterimage',winEffect:'ron-blackfire'},b={callout:'callout-vortex',riichiEffect:'riichi-feather',winEffect:'ron-reactor'};
 const r=new Room('wardrobe-r9');
 const host=r.dispatch({op:'create',host:'wardrobe-r9',n:4,name:'host',character:'yiji',skin:400101,wardrobe:a});assert.equal(host.ok,true,host.error);
 const guest=r.dispatch({op:'join',pin:r.s.pin,name:'guest',character:'yiji',skin:400101,wardrobe:b});assert.equal(guest.ok,true,guest.error);
 const send=(op)=>{const reply=r.dispatch({op,token:host.token,version:r.s.version});assert.equal(reply.ok,true,reply.error);};
 send('fillBots');send('start');
 assert.deepEqual(r.view(1).players.find(p=>p.name==='host').wardrobe,a);assert.deepEqual(r.view(0).players.find(p=>p.name==='guest').wardrobe,b);
 const restored=new Room('wardrobe-r9',()=>{},JSON.stringify(r.s));assert.deepEqual(restored.view(1).players.find(p=>p.name==='host').wardrobe,a);assert.deepEqual(restored.view(0).players.find(p=>p.name==='guest').wardrobe,b);
});
