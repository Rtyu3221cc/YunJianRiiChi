const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const directory = 'unity/Assets/Resources/Majsoul/CharacterDirectory/';
const read = file => JSON.parse(fs.readFileSync(path.join(root, file), 'utf8'));
const roles = read(directory + 'catalog.json').roles;
const targets = new Set(['yiji','kawei','fuji','bamuwei','huiyeji','xiangyuanwu','qingluan','beijianshahezi','xiaoniaoyouchutian','erjietang','wangcilang']);
const catalogs = ['android','windows'].map(platform => read(directory + `packs-${platform}.json`));

test('approved resource recommendation contains exactly the requested eleven roles', () => {
  for (const catalog of catalogs) {
    assert.deepEqual(new Set(catalog.packs.filter(p => p.recommended).map(p => p.key)), targets);
    assert.ok(catalog.packs.filter(p => p.recommended).every(p => p.available));
  }
  assert.equal(roles.filter(r => targets.has(r.key)).reduce((n,r) => n+r.skins.length,0),71);
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
    assert.equal(p.file,`${p.key}-v1-${c.platform}.bundle`);
    assert.equal(p.url,`https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/character-resources-v1/${p.file}`);
  }
});
test('platform catalogs expose the same roles and skins while keeping platform binaries separate', () => {
  const [a,w]=catalogs;
  assert.equal(a.platform,'android');assert.equal(w.platform,'windows');
  assert.deepEqual(a.packs.map(p=>[p.key,p.available,p.skins]),w.packs.map(p=>[p.key,p.available,p.skins]));
  for(const p of a.packs.filter(p=>p.available&&!p.builtin)) assert.notEqual(p.sha256,w.packs.find(q=>q.key===p.key).sha256);
});
test('all target roles retain a lightweight offline avatar before download', () => {
  for(const role of roles.filter(r=>targets.has(r.key))) {
    const image=fs.readFileSync(path.join(root,directory,`Avatars/${role.id}.png`));
    assert.equal(image.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
    assert.ok(image.readUInt32BE(16)<=160&&image.readUInt32BE(20)<=160);
  }
});
test('dynamic outfits have safe explicit skeleton parts and original scene framing', () => {
  const frames=read('unity/Assets/Resources/Majsoul/Characters/framing.json').skins;
  for(const role of roles.filter(r=>targets.has(r.key))) for(const skin of role.skins) {
    const frame=frames.find(f=>f.id===skin.id);assert.ok(frame);
    assert.ok(frame.full.every(Number.isFinite));
    if(skin.dynamic){assert.ok(skin.parts.length>0);for(const part of skin.parts)assert.match(part,new RegExp(`^Spine/${skin.id}/[a-zA-Z0-9_-]+$`));}
  }
});
test('unpublished directory entries cannot accidentally queue zero-byte packs', () => {
  for(const c of catalogs) for(const p of c.packs.filter(p=>!p.available)) {
    assert.equal(p.builtin,false);assert.equal(p.bytes,0);assert.equal(p.url,'');assert.equal(p.sha256,'');
  }
});
