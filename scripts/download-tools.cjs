// Optional developer setup. Players only need the already-built APK.
const fs=require('node:fs'),crypto=require('node:crypto'),path=require('node:path');
const dir=path.resolve(__dirname,'../tools');fs.mkdirSync(dir,{recursive:true});
const jobs=[['build-tools_r35_windows.zip','af059bb67cf7786f45ee0db85e2d24985df1b4b6'],['platform-35_r02.zip','0bb560a90a7a2cbd0dd8348224d518b638fe7949']];
Promise.all(jobs.map(async([name,hash])=>{const file=path.join(dir,name);let b;
 if(fs.existsSync(file))b=fs.readFileSync(file);else {const r=await fetch('https://dl.google.com/android/repository/'+name);if(!r.ok)throw Error('HTTP '+r.status);b=Buffer.from(await r.arrayBuffer());}
 if(crypto.createHash('sha1').update(b).digest('hex')!==hash)throw Error('Google SDK checksum mismatch: '+name);
 fs.writeFileSync(file,b);console.log('Verified '+name);
})).catch(e=>{console.error(e);process.exitCode=1});
