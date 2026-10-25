const fs=require('node:fs');const path=require('node:path');const assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
const roles=JSON.parse(fs.readFileSync(path.join(root,'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json'),'utf8')).roles;
const registry=Object.fromEntries(roles.map(r=>[r.key,r.skins.map(s=>s.id)]));
const main=path.join(root,'app/src/main/assets/engine.js');const current=fs.readFileSync(main,'utf8');
const regex=/const CHARACTER_SKINS\s*=\s*\{[^\r\n]+\};/;assert.ok(regex.test(current),'Engine character registry marker missing');
let updated=current.replace(regex,'const CHARACTER_SKINS='+JSON.stringify(registry)+';');
const emojis=Object.fromEntries(roles.map(r=>[r.key,r.emojis.map(p=>Number(p.split('/').pop())).sort((a,b)=>a-b)]));
assert.ok(Object.values(emojis).every(ids=>ids.every(Number.isInteger)&&new Set(ids).size===ids.length),'Invalid character emoji IDs');
const marker=/const CHARACTER_EMOJIS\s*=\s*\{[^\r\n]*\};/;assert.ok(marker.test(updated),'Engine emoji registry marker missing');
updated=updated.replace(marker,'const CHARACTER_EMOJIS='+JSON.stringify(emojis)+';');
if(process.argv.includes('--check'))assert.equal(current,updated,'Engine registry differs from published character metadata');
else {fs.writeFileSync(main,updated);fs.writeFileSync(path.join(root,'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js'),updated);}
console.log('CHARACTER REGISTRY PASS',roles.length,'roles',roles.reduce((n,r)=>n+r.skins.length,0),'skins');
