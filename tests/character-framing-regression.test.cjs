const test=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'..');
const read=file=>JSON.parse(fs.readFileSync(path.join(root,file),'utf8'));
const bank=read('unity/Assets/Resources/Majsoul/Characters/framing.json');
const roles=read('unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').roles;
const pose=(id,scene,index=0)=>bank.skins.find(s=>s.id===id).placements.find(p=>p.scene===scene&&p.index===index);
const near=(actual,expected)=>{assert.equal(actual.length,expected.length);actual.forEach((n,i)=>assert.ok(Math.abs(n-expected[i])<.001,`${actual} != ${expected}`));};

test('Ichihime match intro uses the actual APK parent offsets, never the web parent',()=>{
  // APK default TextAsset pathID 4331372021175156098, UI_FightBegin seat 0.
  near(pose(400101,'I0').rect,[40.0084,-157,989.2,1504.8]);
  near(pose(400101,'J0').rect,[-516.5265252,164,825.982,1256.508]);
  const native=read('docs/character-resource-inputs/framing-native-default.json').default;
  assert.equal(native.I0[0].offset.y,-536.5);
  assert.equal(native.J0[0].offset.y,0);
  assert.notDeepEqual(native.I0,read('docs/character-resource-inputs/framing-default.json').default.I0);
});
test('Mirai formal outfit retains its previously correct APK framing',()=>{
  near(pose(406903,'I0').rect,[-125.8978,-118.55404,1224.4,1484.4]);
  near(pose(406903,'J0').rect,[429.557466,196.226324,1022.374,1239.474]);
  near(pose(406903,'F0').rect,[-212.466311,2.634826,604.450633,732.805063]);
});
test('every outfit supports all used scenes or has usable native legacy head/crop coordinates',()=>{
  assert.equal(bank.skins.length,roles.reduce((count,role)=>count+role.skins.length,0));
  for(const role of roles)for(const skin of role.skins){
    const frame=bank.skins.find(f=>f.id===skin.id);assert.ok(frame);
    if(frame.placements.length){
      for(const scene of ['A0','B0','B1','B2','F0','K0','I0','J0'])
        for(let index=0;index<(scene==='I0'||scene==='J0'?4:1);index++)assert.ok(pose(skin.id,scene,index),`${skin.id} ${scene}/${index}`);
    }else {assert.ok(frame.full[2]>0&&frame.full[3]>0&&frame.head[2]>0,`${skin.id} legacy crop`);}
    const folder=['yiji','erjietang'].includes(role.key)?'unity/Assets/Resources/Majsoul/Characters':`unity/Assets/CharacterPacks/Source/${role.key}`;
    // File dimensions are optional in a source-only checkout, metadata is required.
    assert.ok(skin.artWidth>0&&skin.artHeight>0,`${skin.id} offline layout dimensions`);
    const file=path.join(root,folder,`Skins/${skin.id}/full.png`);
    if(fs.existsSync(file)){const header=fs.readFileSync(file).subarray(16,24);assert.equal(header.readUInt32BE(0),skin.artWidth);assert.equal(header.readUInt32BE(4),skin.artHeight);assert.equal(skin.hasWaitingroom,fs.existsSync(path.join(path.dirname(file),'waitingroom.png')));}
  }
});
