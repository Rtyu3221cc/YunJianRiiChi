/* Import an explicitly mapped local Ichihime voice pack; never guess a character. */
'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const mapFile=process.argv[2];if(!mapFile)throw Error('Usage: node scripts/import-yiji.cjs <mapping.json>');
const map=JSON.parse(fs.readFileSync(mapFile,'utf8'));if(map.role!=='yiji')throw Error('mapping.role must be yiji');
const source=path.resolve(path.dirname(mapFile),map.directory||'.'),root=path.resolve(__dirname,'../app/src/main/assets');
const entries=Object.entries(map.clips||{}),required=['ron','tsumo','riichi','chi','pon','kan','nuki'];
for(const key of required)if(!map.clips[key])throw Error('Missing Ichihime clip: '+key);
const clips=entries.map(([key,file])=>{
 if(!/^(?:[mps][0-9]|z[1-7]|ron|tsumo|riichi|double-riichi|chi|pon|kan|nuki)$/.test(key))throw Error('Unknown event: '+key);
 const filename=path.resolve(source,file),relative=path.relative(source,filename);
 if(relative.startsWith('..')||path.isAbsolute(relative)||!filename.toLowerCase().endsWith('.wav'))throw Error('Clip must be a local WAV under directory');
 const bytes=fs.readFileSync(filename);if(bytes.length<44||bytes.toString('ascii',0,4)!=='RIFF'||bytes.toString('ascii',8,12)!=='WAVE')throw Error('Invalid WAV: '+filename);
 return {key,bytes,resource:'Majsoul/Voice/yiji-'+key,webPath:'voice/yiji-'+key+'.wav',source:filename,sha256:crypto.createHash('sha256').update(bytes).digest('hex')};
});
// Validate all sources before changing the active voice pack.
for(const c of clips)fs.writeFileSync(path.join(root,c.webPath),c.bytes);
const manifest={provider:'yiji',role:'一姬',clips:clips.map(({bytes,...e})=>e)};
fs.writeFileSync(path.join(root,'voice-manifest.json'),JSON.stringify(manifest,null,2));
fs.writeFileSync(path.join(root,'voice-manifest.js'),'globalThis.MajsoulVoice='+JSON.stringify(manifest)+';\n');
console.log('Imported '+clips.length+' mapped Ichihime clips. Run scripts/sync-unity.ps1 and rebuild the APK.');
