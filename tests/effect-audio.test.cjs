const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { sourceFile } = require('./wardrobe-source.cjs');

const root = path.resolve(__dirname, '..');
const resources = path.join(root, 'unity/Assets/Resources');
const read = file => JSON.parse(fs.readFileSync(file, 'utf8'));
const audit = read(path.join(root, 'docs/effect-audio-audit-2026-11-11.json'));
const catalog = read(path.join(resources, 'Majsoul/Wardrobe/catalog.json'));
const effectItems = catalog.items.filter(item => ['winEffect', 'riichiEffect'].includes(item.category));

test('every winning and riichi effect retains its source audio binding', () => {
  assert.deepEqual(audit.effects.map(row => row.id).sort(), effectItems.map(item => item.id).sort());
  for (const row of audit.effects) {
    const item = effectItems.find(item => item.id === row.id);
    assert.equal(item.cue || '', row.cue, row.id);
    if (row.audioId === 0) {
      assert.equal(row.id, 'riichi-blue');
      assert.equal(row.cue, '');
      continue;
    }
    assert.ok(row.cue, row.id);
    const file = sourceFile(path.join(resources, 'Majsoul/Audio', row.cue + '.mp3'));
    const bytes = fs.readFileSync(file);
    assert.ok(bytes.subarray(0, 3).equals(Buffer.from('ID3')) || bytes[0] === 255, row.id);
    assert.equal(bytes.length, row.bytes, row.id);
    assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'), row.sha256, row.id);
    assert.ok(fs.existsSync(file + '.meta'), row.id);
  }
  assert.equal(audit.effects.filter(row => row.cue).length, 119);
});

for (const platform of ['android', 'windows']) {
  test(platform + ' can resolve every effect sound through built-in assets or its pack dependencies', () => {
    const directory = read(path.join(resources, 'Majsoul/WardrobeDirectory/packs-' + platform + '.json'));
    const packs = new Map(directory.packs.map(pack => [pack.key, pack]));
    const owners = new Map(directory.items.map(item => [item.id, item.pack]));
    const addresses = new Map(directory.addresses.map(address => [address.path, address]));
    function closure(key) {
      const pack = packs.get(key);
      return new Set([key, ...pack.dependencies.flatMap(dependency => [...closure(dependency)])]);
    }
    for (const row of audit.effects.filter(row => row.cue)) {
      const address = 'Majsoul/Audio/' + row.cue;
      const asset = addresses.get(address);
      if (asset) {
        assert.ok(closure(owners.get(row.id)).has(asset.pack), row.id);
        assert.ok(fs.existsSync(path.join(root, 'unity', asset.source)), row.id);
      } else {
        assert.ok(fs.existsSync(path.join(resources, address + '.mp3')), row.id);
      }
    }
  });
}
