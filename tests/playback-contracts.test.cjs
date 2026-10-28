const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const root=path.join(__dirname,'../unity/Assets/Resources/Majsoul'),read=(p)=>JSON.parse(fs.readFileSync(path.join(root,p),'utf8'));
test('tile mount follows the source front and glyph UV up, not positive local Z',()=>{
 const f=read('Actions/source-tile-frame.json'),q=f.mount,rotate=(v)=>{
  const [x,y,z,w]=q,[a,b,c]=v;return [(1-2*y*y-2*z*z)*a+(2*x*y-2*z*w)*b+(2*x*z+2*y*w)*c,(2*x*y+2*z*w)*a+(1-2*x*x-2*z*z)*b+(2*y*z-2*x*w)*c,(2*x*z-2*y*w)*a+(2*y*z+2*x*w)*b+(1-2*x*x-2*y*y)*c];
 };
 for(const [from,to]of [[f.sourceFaceNormal,[0,1,0]],[f.sourceUTangent,[1,0,0]],[f.sourceVTangent,[0,0,1]]])assert.ok(rotate(from).every((v,i)=>Math.abs(v-to[i])<1e-6));
 assert.ok(Math.abs(f.sourceWidth-.029754)<1e-7);
});
test('ground and tile-bound declarations use the two original animation branches and exact event keys',()=>{
 const c=read('Actions/playback-contracts.json'),human=(action)=>c.rows.find(r=>r.type==='hand_human'&&r.action===action);
 assert.equal(c.bindings.find(b=>b.id==='riichi-feather').action,'Dapai');assert.equal(c.bindings.find(b=>b.id==='riichi-afterimage').action,'LiQiPai');
 assert.ok(Math.abs(human('Dapai').cue-.319/.94)<1e-7);assert.ok(Math.abs(human('Dapai').release-.319/.94)<1e-7);
 assert.equal(human('LiQiPai').cue,.275);assert.equal(human('LiQiPai').release,.65);assert.equal(human('LiQiPai').lifetime,.8075);
});
test('unverified adapted river effects stay identifiable but out of the default selector',()=>{
 const items=read('Wardrobe/catalog.json').items;const trials=items.filter(i=>i.category==='callout'&&i.trialStyle);assert.equal(trials.length,4);assert.ok(trials.every(i=>i.hiddenFromSelection));assert.equal(items.filter(i=>i.category==='callout'&&!i.hiddenFromSelection).length,4);
});
