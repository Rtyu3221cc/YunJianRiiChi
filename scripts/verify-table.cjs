const assert=require('node:assert/strict'),fs=require('node:fs');
const path=require('node:path');
let playwright;
try { playwright=require('playwright'); }
catch { playwright=require(process.env.PLAYWRIGHT_MODULE || path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright')); }
const {chromium}=playwright;
globalThis.Majiang=require('../app/src/main/assets/core.js');require('../app/src/main/assets/ai.js');
const {Room}=require('../app/src/main/assets/engine.js');
(async()=>{
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
 page.on('pageerror',e=>errors.push(String(e)));
 page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url())});
 const r=new Room('test');r.dispatch({op:'create',host:'test',name:'布局测试',n:3,bots:true});r.dispatch({op:'start',token:r.s.players[0].token,version:r.s.version});
 r.s.turn=0;r.s.phase='play';r.s.pending=null;r.s.players[0].hand='p123456s123z1122z4';
 // Intentional synthetic fixture, used only to stress layout and click behavior.
 r.s.players[0].nuki=3;r.s.players[0].hand='p123s123z1122z4,p555+';
 r.s.players[1].riichi=1;r.s.sticks=1;
 r.s.players[1].river=Array.from({length:18},(_,i)=>({tile:'p'+(i%9+1),riichi:i===4}));
 r.s.players[0].river=Array.from({length:12},(_,i)=>({tile:'s'+(i%9+1),riichi:false}));
 const view=r.view(0);let submissions=[];
 await page.route('**/api',async route=>{
  const req=route.request().postDataJSON();if(req.op==='action')submissions.push(req.action);
  await route.fulfill({contentType:'application/json',body:JSON.stringify({ok:true,state:view})});
 });
 await page.goto('http://127.0.0.1:8876/#host=table-review');
 await page.evaluate(()=>{active=true;token='fixture';});await page.waitForSelector('.game');
 const tile=page.locator('#hand .playable').first();await tile.click();assert.equal(submissions.length,0);
 await page.waitForTimeout(1100);assert(await tile.evaluate(el=>el.classList.contains('selected')));
 await tile.click();assert.equal(submissions.length,1);
 const ts=page.locator('#hand .playable');if(await ts.count()>1){await ts.nth(0).click();await ts.nth(1).click();assert.equal(submissions.length,1);await ts.nth(1).click();assert.equal(submissions.length,2);}
 assert.equal(await page.locator('.riichi-deposit').count(),1);
 const decoded=await page.evaluate(async()=>{
  const ctx=new AudioContext(),results=[];
  for(const clip of MajsoulVoice.clips){const res=await fetch(clip.webPath);if(!res.ok)throw Error('Missing clip '+clip.webPath);const data=await ctx.decodeAudioData(await res.arrayBuffer());const samples=data.getChannelData(0);results.push({key:clip.key,duration:data.duration,nonSilent:samples.some(x=>Math.abs(x)>.001),source:clip.source});}
  await ctx.close();return results;
 });assert.equal(decoded.length,8);assert(decoded.every(x=>x.duration>0&&x.nonSilent&&x.source.includes('/audio/sound/yiji/')));
 await page.evaluate(()=>RiichiVoice.preview(.5));assert((await page.locator('#voice-output').getAttribute('src')).endsWith('yiji-ron.mp3'));
 // Validate actual transformed face bounds, not only the CSS slot dimensions.
 const overlaps=await page.locator('.river-row').evaluateAll(rows=>rows.flatMap(row=>{
  const tiles=[...row.querySelectorAll('.tile')].map(x=>x.getBoundingClientRect());
  return tiles.flatMap((a,i)=>tiles.slice(i+1).filter(b=>Math.min(a.right,b.right)-Math.max(a.left,b.left)>1&&Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)>1).map(()=>row.parentElement.parentElement.className));
 }));assert.deepEqual(overlaps,[]);
 fs.mkdirSync('dist/previews',{recursive:true});
 await page.screenshot({path:'dist/previews/table-0.5.0-1280.png'});
 for(const [width,height] of [[844,390],[720,360]]){
  await page.setViewportSize({width,height});await page.waitForTimeout(100);
  const out=await page.locator('#hand button').evaluateAll(bs=>bs.some(b=>{const r=b.getBoundingClientRect();return r.left<0||r.right>innerWidth||r.bottom>innerHeight}));assert.equal(out,false);
  await page.screenshot({path:`dist/previews/table-0.5.0-${width}.png`});
 }
 assert.deepEqual(errors,[]);console.log('PASS: slow second click, switch selection, riichi stick, river collision, three viewports, 8 decoded/non-silent Ichihime clips, no browser/asset errors. Synthetic layout fixture only.');
 fs.writeFileSync('dist/previews/audio-validation.json',JSON.stringify(decoded,null,2));
 const live=await browser.newPage({viewport:{width:1280,height:720}});
 await live.goto('http://127.0.0.1:8876/#host=table-review');await live.locator('#practice').click();
 await live.locator('select[name="n"]').selectOption('3');await live.locator('#entry button[type="submit"]').click();
 await live.waitForSelector('#hand button.playable',{timeout:15000});
 const before=await live.evaluate(()=>state.players[state.you].river.length);
 await live.locator('#hand button.playable').first().click();await live.waitForTimeout(1000);await live.locator('#hand button.selected').click();
 await live.waitForFunction(n=>state.players[state.you].river.length>n,before,{timeout:10000});
 await live.evaluate(()=>command('close'));await live.close();console.log('PASS: live server creates sanma practice with AI and commits a slow two-click discard.');
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
