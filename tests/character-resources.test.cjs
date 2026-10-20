const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const directory = 'unity/Assets/Resources/Majsoul/CharacterDirectory/';
const read = file => JSON.parse(fs.readFileSync(path.join(root, file), 'utf8'));
const roles = read(directory + 'catalog.json').roles;
const targets = new Set(read('docs/character-resource-inputs/recommended.json').keys);
const inputs=read('docs/character-resource-inputs/config.json');
const catalogs = ['android','windows'].map(platform => read(directory + `packs-${platform}.json`));

test('resource recommendation matches the declared popular-character preset', () => {
  for (const catalog of catalogs) {
    assert.deepEqual(new Set(catalog.packs.filter(p => p.recommended).map(p => p.key)), targets);
    assert.ok(catalog.packs.filter(p => p.recommended).every(p => p.available));
  }
  const ids=new Set(inputs['item_definition/character'].filter(r=>targets.has(r.sound_folder)).map(r=>r.id));
  assert.equal(roles.filter(r => targets.has(r.key)).reduce((n,r) => n+r.skins.length,0),inputs['item_definition/skin'].filter(s=>ids.has(s.character_id)).length);
});
test('only Ichihime and Miki are protected offline defaults', () => {
  for (const c of catalogs) assert.deepEqual(c.packs.filter(p=>p.builtin).map(p=>p.key).sort(), ['erjietang','yiji']);
  for(const key of ['yiji','erjietang']) for(const s of roles.find(r=>r.key===key).skins)
    for(const kind of ['full','bighead','smallhead']) assert.ok(fs.existsSync(path.join(root,`unity/Assets/Resources/Majsoul/Characters/Skins/${s.id}/${kind}.png`)));
});
test('available catalogs agree on outfit ownership and include a valid default', () => {
  for (const c of catalogs) for(const pack of c.packs.filter(p=>p.available)) {
    const role=roles.find(r=>r.key===pack.key);assert.ok(role);
    assert.deepEqual([...pack.skins].sort(),role.skins.map(s=>s.id).sort());
    assert.ok(pack.skins.includes(role.defaultSkin));
    for(const id of pack.skins) assert.equal(c.packs.filter(p=>p.skins.includes(id)).length,1);
  }
});
test('published optional packs use fixed platform-specific HTTPS files and trusted hashes', () => {
  for(const c of catalogs) for(const p of c.packs.filter(p=>p.available&&!p.builtin)) {
    assert.equal(c.schema,1);assert.equal(c.protocol,1);
    assert.match(p.sha256,/^[0-9a-f]{64}$/);assert.ok(p.bytes>1024);
    assert.ok(Number.isInteger(p.version)&&p.version>=1);
    assert.equal(p.file,`${p.key}-v${p.version}-${c.platform}.bundle`);
    const url=new URL(p.url);assert.equal(url.origin,'https://github.com');
    assert.match(url.pathname,new RegExp(`^/Rtyu3221cc/YunJianRiiChi/releases/download/[a-zA-Z0-9._-]+/${p.file.replaceAll('.','\\.')}$`));
  }
});
test('platform catalogs expose the same roles and skins while keeping platform binaries separate', () => {
  const [a,w]=catalogs;
  assert.equal(a.platform,'android');assert.equal(w.platform,'windows');
  assert.deepEqual(a.packs.map(p=>[p.key,p.available,p.skins]),w.packs.map(p=>[p.key,p.available,p.skins]));
  for(const p of a.packs.filter(p=>p.available&&!p.builtin)) assert.notEqual(p.sha256,w.packs.find(q=>q.key===p.key).sha256);
});
test('every character retains a lightweight offline avatar before download', () => {
  for(const role of roles) {
    const image=fs.readFileSync(path.join(root,directory,`Avatars/${role.id}.png`));
    assert.equal(image.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
    assert.ok(image.readUInt32BE(16)<=160&&image.readUInt32BE(20)<=160);
  }
});
test('dynamic outfits have safe explicit skeleton parts and original scene framing', () => {
  const frames=read('unity/Assets/Resources/Majsoul/Characters/framing.json').skins;
  for(const role of roles) for(const skin of role.skins) {
    const frame=frames.find(f=>f.id===skin.id);assert.ok(frame);
    assert.ok(frame.full.every(Number.isFinite));
    if(skin.dynamic){assert.ok(skin.parts.length>0);for(const part of skin.parts)assert.match(part,new RegExp(`^Spine/${skin.id}/[a-zA-Z0-9_-]+$`));}
  }
});
test('full roster exposes every declared character and outfit with no unavailable entries',()=>{
  const characterIds=new Set(inputs['item_definition/character'].map(r=>r.id));
  assert.deepEqual(new Set(roles.map(r=>r.id)),characterIds);
  // Upstream also declares two generic gender mannequins with character_id=0.
  const playable=inputs['item_definition/skin'].filter(s=>s.character_id!==0);
  assert.ok(playable.every(s=>characterIds.has(s.character_id)));
  assert.deepEqual(new Set(roles.flatMap(r=>r.skins.map(s=>s.id))),new Set(playable.map(s=>s.id)));
  assert.ok(roles.every(r=>r.emojis.length>0),'Every playable role has an expression bank, including language-base resources');
  assert.ok(roles.every(r=>new Set(r.emojis).size===r.emojis.length),'Localized subfolders cannot duplicate the same expression');
  for(const catalog of catalogs)assert.ok(catalog.packs.every(p=>p.available));
});
test('adding an outfit preserves old IDs and versions unchanged packs immutably',()=>{
  const old=read('docs/character-resource-inputs/previous-packs-android.json');
  for(const before of old.packs.filter(p=>p.available)){
    const after=catalogs[0].packs.find(p=>p.key===before.key);assert.ok(after);
    assert.ok(before.skins.every(s=>after.skins.includes(s)));
    if(after.skins.length!==before.skins.length)assert.ok(after.version>before.version);
    else {assert.equal(after.version,before.version);assert.equal(after.url,before.url);assert.equal(after.sha256,before.sha256);}
  }
});
test('room engine accepts exactly the published character and outfit registry',()=>{
  const engine=fs.readFileSync(path.join(root,'app/src/main/assets/engine.js'),'utf8');
  const text=engine.match(/const CHARACTER_SKINS\s*=\s*(\{[^\r\n]+\});/);assert.ok(text);
  assert.deepEqual(JSON.parse(text[1]),Object.fromEntries(roles.map(r=>[r.key,r.skins.map(s=>s.id)])));
});
test('unpublished directory entries cannot accidentally queue zero-byte packs', () => {
  for(const c of catalogs) for(const p of c.packs.filter(p=>!p.available)) {
    assert.equal(p.builtin,false);assert.equal(p.bytes,0);assert.equal(p.url,'');assert.equal(p.sha256,'');
  }
});
