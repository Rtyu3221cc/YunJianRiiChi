const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..'), dir=path.join(root,'vendor/package/lib');
let modules='';
for(const f of fs.readdirSync(dir).filter(f=>f.endsWith('.js'))){
 let src=fs.readFileSync(path.join(dir,f),'utf8');
 // Keep upstream intact. This one extension adds sanma extracted-North dora
 // AFTER yaku detection, so nuki never makes a no-yaku hand legal.
 if(f==='hule.js') src=src.replace('    let max;','    let max;').replace('    for (let mianzi of hule_mianzi(shoupai, rongpai)) {',"    if (param.nuki) post_hupai.push({name:'拔北宝牌', fanshu:param.nuki});\n    for (let mianzi of hule_mianzi(shoupai, rongpai)) {");
 modules+=JSON.stringify('./'+f.slice(0,-3))+':function(module,exports,require){\n'+src+'\n},\n';
}
const out=`/* Bundled @kobalab/majiang-core 1.4.1, MIT, Satoshi Kobayashi. See LICENSE-core.txt. */\n(function(g){const modules={${modules}},cache={};function req(id){if(cache[id])return cache[id].exports;const m={exports:{}};cache[id]=m;modules[id](m,m.exports,req);return m.exports;}g.Majiang=req('./index');if(typeof module!=='undefined')module.exports=g.Majiang;})(globalThis);\n`;
fs.writeFileSync(path.join(root,'app/src/main/assets/core.js'),out);
fs.copyFileSync(path.join(root,'vendor/package/LICENSE'),path.join(root,'app/src/main/assets/LICENSE-core.txt'));
console.log('Bundled core:',Buffer.byteLength(out),'bytes');
