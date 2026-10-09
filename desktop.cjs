// Desktop test harness. The APK uses RoomService.java instead; no Node needed on phones.
const fs=require('node:fs'),http=require('node:http'),path=require('node:path'),os=require('node:os');
globalThis.Majiang=require('./app/src/main/assets/core.js');
require('./app/src/main/assets/ai.js');
const {Room}=require('./app/src/main/assets/engine.js'),dir=path.join(__dirname,'app/src/main/assets'),saveFile=path.join(__dirname,process.env.RIICHI_STATE_FILE||'.desktop-state.json');
const secret=process.env.RIICHI_HOST_SECRET||require('node:crypto').randomBytes(24).toString('hex');
const room=new Room(secret,s=>fs.writeFileSync(saveFile,s),fs.existsSync(saveFile)?fs.readFileSync(saveFile,'utf8')||null:null);
setInterval(()=>{const r=room.tick();if(!r.ok)console.error('AI:',r.error)},850).unref();
const addresses=()=>Object.values(os.networkInterfaces()).flat().filter(x=>x.family==='IPv4'&&!x.internal).map(x=>'http://'+x.address+':8765');
http.createServer(async(req,res)=>{res.setHeader('Cache-Control','no-store');res.setHeader('X-Content-Type-Options','nosniff');res.setHeader('Content-Security-Policy',"default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; media-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'");
 try{if(req.headers.origin&&req.headers.origin!=='http://'+req.headers.host){res.writeHead(403);return res.end()}
 if(req.url==='/api'&&req.method==='POST'){let b='';for await(const chunk of req){b+=chunk;if(b.length>16384){res.writeHead(413);return res.end()}}res.setHeader('Content-Type','application/json');res.end(JSON.stringify(room.dispatch(JSON.parse(b))));return}
 if(req.url==='/info'){res.setHeader('Content-Type','application/json');return res.end(JSON.stringify({addresses:addresses()}))}
 const voiceEntry=/^\/voice\/(?:[mps][0-9]|z[1-7]|ron|tsumo|yiji-[a-z0-9_-]+)\.(?:wav|mp3)$/.test(req.url)?[req.url.slice(1),req.url.endsWith('.mp3')?'audio/mpeg':'audio/wav']:null;
 const extraPaths={'/table-layout.js':['table-layout.js','text/javascript'],'/voice-manifest.js':['voice-manifest.js','text/javascript'],'/table/cloth.png':['table/cloth.png','image/png'],'/table/score-hub.png':['table/score-hub.png','image/png']};
 const paths={'/':['index.html','text/html'],'/voice.js':['voice.js','text/javascript'],'/client.js':['client.js','text/javascript'],'/style.css':['style.css','text/css'],'/table.css':['table.css','text/css'],'/felt.svg':['felt.svg','image/svg+xml'],'/portraits/yiji.png':['portraits/yiji.png','image/png'],'/portraits/erjietang.png':['portraits/erjietang.png','image/png'],'/LICENSE-core.txt':['LICENSE-core.txt','text/plain']},entry=paths[req.url]||extraPaths[req.url]||voiceEntry||(/^\/tiles\/(?:[mps][0-9]|z[1-7])\.png$/.test(req.url)?[req.url.slice(1),'image/png']:null);if(!entry){res.writeHead(404);return res.end('Not found')}
 res.setHeader('Content-Type',entry[1]+'; charset=utf-8');res.end(fs.readFileSync(path.join(dir,entry[0])));
 }catch(e){res.writeHead(400);res.end('Invalid request')}
}).listen(Number(process.env.RIICHI_PORT)||8765,'0.0.0.0',()=>console.log('Local test host: http://127.0.0.1:'+(Number(process.env.RIICHI_PORT)||8765)+'/#host='+secret+'\nLAN: '+addresses().join(', ')));

