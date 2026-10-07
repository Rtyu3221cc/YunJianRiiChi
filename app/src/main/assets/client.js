'use strict';
const $=s=>document.querySelector(s),esc=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const native=!!window.NativeApp,localServer='http://127.0.0.1:8765';
let config=native?JSON.parse(NativeApp.config()):{},host=config.host||new URLSearchParams(location.hash.slice(1)).get('host')||sessionStorage.getItem('riichi-host')||'';
if(host&&!native){sessionStorage.setItem('riichi-host',host);history.replaceState(null,'',location.pathname);}
let server=native?(localStorage.getItem('riichi-server')||localServer):location.origin,token='',state=null,busy=false,selected=null,selectedIndex=-1,riichiMode=false,active=false,generation=0,info={addresses:config.addresses||[]},toastTimer,lastRendered='',mode='home';
const names={m:'萬',p:'筒',s:'索'},honors=['','東','南','西','北','白','發','中'],winds=['东','南','西','北'];
const pending=new Map();let requestNo=0;
window.nativeResponse=(id,json)=>{const p=pending.get(id);if(!p)return;pending.delete(id);clearTimeout(p.timer);try{p.resolve(JSON.parse(json))}catch(e){p.reject(e)}};

let avatarDraft='';
const personalAvatar=()=>localStorage.getItem('riichi-avatar');
const avatarPayload=()=>personalAvatar()===null?{}:{avatar:personalAvatar()};
const portraitSrc=p=>p?.avatar||('portraits/'+(p?.bot?'yiji':'erjietang')+'.png');
async function syncAvatar(){if(active&&state?.players&&personalAvatar()!==null&&state.players[state.you].avatar!==personalAvatar())await command('profile',avatarPayload());}
window.avatarPicked=(data,error)=>{if(error){toast(error);return;}if(data&&$('#avatar-preview')){avatarDraft=data;$('#avatar-preview').src=data;}};
async function browserAvatar(file){
 if(!file)return;if(!file.type.startsWith('image/')||file.size>20*1024*1024){toast('请选择 20 MB 以内的图片');return;}
 let bitmap;try{bitmap=await createImageBitmap(file);const canvas=document.createElement('canvas');canvas.width=canvas.height=128;const ctx=canvas.getContext('2d');ctx.fillStyle='#ffffff';ctx.fillRect(0,0,128,128);const edge=Math.min(bitmap.width,bitmap.height);ctx.drawImage(bitmap,(bitmap.width-edge)/2,(bitmap.height-edge)/2,edge,edge,0,0,128,128);let data;for(const q of [.85,.7,.55,.4,.25]){data=canvas.toDataURL('image/jpeg',q);if(data.length<=12288)break;}if(data.length>12288)throw Error();window.avatarPicked(data,'');}catch(e){toast('无法读取这张图片，请换一张 JPG 或 PNG');}finally{bitmap?.close();}
}
function preferences(){
 avatarDraft=personalAvatar()??state?.players?.[state.you]?.avatar??'';
 modal('<div class="eyebrow">PERSONAL SETTINGS</div><h2>头像与语音</h2><div class="profile-editor"><img id="avatar-preview" alt="我的头像" src="'+esc(avatarDraft||'portraits/erjietang.png')+'"><div><button id="choose-avatar">从相册选择</button><button id="reset-avatar" class="quiet">恢复默认</button><p class="note">图片居中裁成方形，并同步给本桌牌友。</p></div></div><input id="avatar-file" type="file" accept="image/*" hidden><label class="voice-switch"><input id="voice-enabled" type="checkbox" '+(RiichiVoice.enabled?'checked':'')+'> 开启出牌与和牌语音</label><label class="volume-label">语音音量 <input id="voice-volume" type="range" min="0" max="100" value="'+Math.round(RiichiVoice.volume*100)+'"></label><p class="note">中文报牌 · 荣和 / 自摸 · 无需联网</p><div class="row"><button id="voice-preview">试听荣和</button><button id="save-profile" class="primary">保存设置</button></div>');
 $('#choose-avatar').onclick=()=>native?NativeApp.pickAvatar():$('#avatar-file').click();$('#avatar-file').onchange=e=>browserAvatar(e.target.files[0]);
 $('#reset-avatar').onclick=()=>{avatarDraft='';$('#avatar-preview').src='portraits/erjietang.png';};
 $('#voice-preview').onclick=()=>RiichiVoice.preview(Number($('#voice-volume').value)/100);
 $('#save-profile').onclick=async()=>{const button=$('#save-profile');if(button.disabled)return;button.disabled=true;const enabled=$('#voice-enabled').checked,volume=Number($('#voice-volume').value)/100;
  try{if(active&&state?.players){if(!await command('profile',{avatar:avatarDraft}))return;}localStorage.setItem('riichi-avatar',avatarDraft);localStorage.setItem('riichi-voice',enabled?'on':'off');localStorage.setItem('riichi-volume',String(volume));RiichiVoice.configure(enabled,volume);$('#modal').classList.add('hidden');toast('设置已保存');}catch(e){toast('设置未保存，请重试');}finally{button.disabled=false;}
 };
}

function seatKey(){return 'riichi-seat:'+server}
function selectServer(value){generation++;active=false;server=value;token=localStorage.getItem(seatKey())||(!native?localStorage.getItem('riichi-seat'):'')||'';state=null;lastRendered='';if(native)localStorage.setItem('riichi-server',server);}
selectServer(server);
async function request(data){
 if(native)return new Promise((resolve,reject)=>{const id=String(++requestNo),timer=setTimeout(()=>{pending.delete(id);reject(Error('连接超时'))},18000);pending.set(id,{resolve,reject,timer});NativeApp.request(id,server,JSON.stringify(data));});
 const controller=new AbortController(),timer=setTimeout(()=>controller.abort(),15000);try{const r=await fetch('/api',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data),signal:controller.signal});if(!r.ok)throw Error('连接失败');return await r.json()}finally{clearTimeout(timer)}
}
function network(ok){document.body.classList.toggle('offline',!ok);$('#connection').textContent=ok?'● 已连接 · 无需互联网':'● 连接中断 · 正在重连';$('#connection').classList.toggle('offline',!ok)}
function toast(t){clearTimeout(toastTimer);$('#toast').textContent=t;$('#toast').classList.add('visible');toastTimer=setTimeout(()=>$('#toast').classList.remove('visible'),4000)}
function modal(html){$('#modal-content').innerHTML=html;$('#modal').classList.remove('hidden')}
$('#dismiss').onclick=()=>$('#modal').classList.add('hidden');
$('#home').onclick=()=>{if(busy)return;showHome()};
window.showHome=function(){RiichiVoice.reset();generation++;active=false;mode='home';$('#modal').classList.add('hidden');document.body.classList.remove('playing');renderHome();};
function help(){modal('<div class="eyebrow">HOW TO PLAY</div><h2>把牌桌，带上云端。</h2><p>所有手机提前安装 APK。一台手机开热点，其他手机连接这个热点。房主“创建房间”，朋友在 App 内“加入房间”，输入房主地址和六位房间码即可。</p><p>没有牌友？“单人练习”会自动补齐 AI 并开局。多人房间也能添加或移除 AI。AI 使用自己的手牌和公开牌面，不读取对手暗牌；采用基础牌效与防守策略。</p><h3>操作提示</h3><p>单击手牌选中，快速双击同一张牌打出。先点“立直”，再双击高亮手牌立直打出。吃碰杠、荣和出现时需选择操作或“过”，真人不会自动超时。结算后真人确认，房主开始下一局。</p><h3>规则约定</h3><p>四麻 25000 点、三麻 35000 点。食断、后付、可选赤宝牌，双响、三家和、四风连打、四家立直、四杠散了、九种九牌和流局满贯。三麻去二至八万、无吃、拔北、自摸损；拔北从活牌山补牌，仅国士可抢北。支持东风和半庄。</p><h3>断线恢复</h3><p>牌局自动保存在房主手机。重进 App 点击“继续上次牌桌”；地址变化时，输入新地址、房间码和个人恢复码回座。恢复码在牌桌右上角房间信息中。AI 自动确认结算，真人操作始终由本人完成。</p><p class="note">房主保持 App 亮屏。仅在航司允许使用 Wi-Fi 时开启热点，并提前做一次无互联网联机测试。</p>');}
$('#help').onclick=help;$('#preferences').onclick=preferences;
async function command(op,extra={}){
 if(busy)return false;busy=true;const gen=generation;
 try{const r=await request({op,token,version:state?.version,id:Date.now().toString(36)+'-'+Math.random().toString(36).slice(2),...extra});if(gen!==generation)return false;if(r.networkError)throw Error(r.error);network(true);if(!r.ok){toast(r.error);return false;}
 if(r.token){token=r.token;localStorage.setItem(seatKey(),token)}
 if(op==='close'){token='';localStorage.removeItem(seatKey());active=false;showHome();return true;}
 selected=null;riichiMode=false;active=true;accept(r.state,true);return true;
 }catch(e){network(false);toast('无法连接房主，请检查热点和地址；原牌局仍保留。');return false;}finally{busy=false}
}
function accept(s,force=false){RiichiVoice.accept(s);state=s;const key=JSON.stringify(s);if(force||key!==lastRendered){lastRendered=key;render()}}
async function poll(){const gen=generation;try{if(active&&!busy){const r=await request({op:'state',token});if(gen===generation&&!busy&&active){if(r.networkError)throw Error();network(true);if(r.ok&&(!state||r.state.version==null||state.version==null||r.state.version>=state.version))accept(r.state)}}}catch(e){if(gen===generation)network(false)}finally{setTimeout(poll,650)}}
if(!native)fetch('/info').then(r=>r.json()).then(x=>info=x).catch(()=>{});
function render(){if(!state)return;document.body.classList.toggle('playing',['play','reaction'].includes(state.phase));if(['empty','join'].includes(state.phase)){active=false;renderEntry(state.phase==='empty'&&isOwn()?'create':'join');}else if(state.phase==='lobby')renderLobby();else if(state.phase==='result')renderResult();else renderTable()}
function isOwn(){return !!host&&(!native||server===localServer)}
function renderHome(){
 $('#app').innerHTML=`<section class="home-screen"><div class="home-art"><div class="orbit orbit-one"></div><div class="orbit orbit-two"></div><div class="art-tiles">${tile('s1')}${tile('z1')}${tile('m0')}</div><div class="seal">一局一会</div><div class="art-caption">风起云间 · 好友同席</div></div><div class="home-content"><div class="eyebrow">YOUR POCKET MAHJONG CLUB</div><h1>云间相聚<br>随时<span>开局。</span></h1><p class="lead">一张牌桌，三两好友。<br>没有网络，也能尽兴。</p><div class="home-buttons">${native||host?'<button id="practice" class="primary wide">单人练习 <small>AI 即刻入座</small><span>→</span></button><button id="create" class="wide">创建房间 <small>与好友一起打牌</small><span>＋</span></button>':''}<button id="join" class="wide">加入房间 <small>连接同一热点</small><span>↗</span></button>${token?'<button id="resume" class="quiet">继续上次牌桌 →</button>':''}</div><div class="home-foot"><span>✧ 完全离线</span><span>三麻 / 四麻</span><span>v0.4.0</span></div></div></section>`;
 if($('#practice'))$('#practice').onclick=()=>ownEntry('practice');if($('#create'))$('#create').onclick=()=>ownEntry('create');$('#join').onclick=()=>{active=false;renderEntry('join')};if($('#resume'))$('#resume').onclick=async()=>{active=true;if(await command('state'))await syncAvatar();};
}
async function ownEntry(next){if(busy)return;selectServer(native?localServer:location.origin);mode=next;busy=true;try{const gen=generation,r=await request({op:'state',token});if(gen!==generation)return;if(r.networkError)throw Error();network(true);if(r.state.phase!=='empty'){const recovered=await request({op:'recoverHost',host});if(recovered.ok){token=recovered.token;localStorage.setItem(seatKey(),token);active=true;accept(recovered.state,true);setTimeout(syncAvatar,0);toast('已回到本机现有牌桌；结束牌桌后可新建。');return}}renderEntry(next)}catch(e){toast('本机房间服务正在启动，请稍后再试。')}finally{busy=false}}
function renderEntry(next){mode=next;document.body.classList.remove('playing');const create=next!=='join';$('#app').innerHTML=`<section class="entry-screen"><div class="entry-copy"><div class="eyebrow">${create?'MAKE YOUR TABLE':'JOIN YOUR FRIENDS'}</div><h1>${next==='practice'?'一个人，<br>也能练一局。':create?'好牌友，<br>云间见。':'牌桌已备好，<br>就等你了。'}</h1><p>${create?'三麻与四麻，自由选择。<br>空位可以随时添加 AI。':'连上房主的热点，<br>输入地址和房间码即可入座。'}</p>${native?'<button id="wifi" class="quiet">热点 / Wi-Fi 设置 ↗</button>':''}</div><form id="entry" class="card fields"><h2>${next==='practice'?'单人练习':create?'创建房间':'加入房间'}</h2><label>你的昵称<input name="name" maxlength="12" value="${esc(localStorage.getItem('riichi-name')||'旅人')}" required></label>${create?'<div class="form-row"><label>玩法<select name="n"><option value="4">四人日麻</option><option value="3">三人日麻</option></select></label><label>对局<select name="rounds"><option value="1">东风战</option><option value="2">半庄战</option></select></label></div><label>赤宝牌<select name="red"><option value="1">有赤宝牌</option><option value="0">无赤宝牌</option></select></label>':`${native?'<label>房主局域网地址<input name="address" placeholder="例如 192.168.43.1" inputmode="url" required value="'+esc(server!==localServer?server:'')+'"></label>':''}<label>六位房间码<input name="pin" inputmode="numeric" pattern="[0-9]{6}" maxlength="6" required placeholder="向房主获取"></label><details><summary>恢复已有座位</summary><input name="recovery" placeholder="个人恢复码，首次加入留空" maxlength="12"></details>`}<button class="primary" type="submit">${next==='practice'?'与 AI 开始练习':create?'创建牌桌':'入座'} →</button><button type="button" class="quiet" id="back-home">返回大厅</button></form></section>`;
 if($('#wifi'))$('#wifi').onclick=()=>NativeApp.settings();$('#back-home').onclick=showHome;
 $('#entry').onsubmit=async e=>{e.preventDefault();if(busy)return;const f=new FormData(e.target),name=f.get('name');localStorage.setItem('riichi-name',name);if(!create&&native){let addr=f.get('address').trim().replace(/\/$/,'');if(!addr.startsWith('http://'))addr='http://'+addr;if(!/:\d+$/.test(addr))addr+=':8765';if(!/^http:\/\/(?:\d{1,3}\.){3}\d{1,3}:8765$/.test(addr)){toast('请输入房主显示的 IPv4 地址');return}selectServer(addr)}
 const ok=await command(create?'create':'join',create?{...avatarPayload(),host,name,n:+f.get('n'),rounds:+f.get('rounds'),red:f.get('red')==='1',bots:next==='practice'}:{...avatarPayload(),name,pin:f.get('pin'),recovery:f.get('recovery')});if(ok&&next==='practice')await command('start');};
}
function roomInfo(){if(native)info=JSON.parse(NativeApp.config());modal(`<div class="eyebrow">ROOM INFORMATION</div><h2>邀请朋友入座</h2><div class="large-code">${esc(state.pin)}</div><p>朋友连接同一热点，在 App 内“加入房间”</p>${(isOwn()?info.addresses:[server]).map(a=>`<div class="address">${esc(a)}</div>`).join('')||'<p>开启热点后重新打开此窗口查看地址。</p>'}<h3>我的个人恢复码</h3><div class="recovery">${esc(state.recovery)}</div><p class="note">只供自己保管，用于地址变化或重新安装后的回座。</p>${native?'<button id="wifi">热点 / Wi-Fi 设置</button>':''}${state.host?'<button id="close-in-info" class="danger">结束当前牌桌</button>':''}`);if($('#wifi'))$('#wifi').onclick=()=>NativeApp.settings();if(state.host)$('#close-in-info').onclick=closeRoom}
function closeRoom(){modal('<h2>结束当前牌桌？</h2><p>这将清除当前牌局，所有玩家回到加入页面。</p><button id="confirm-close" class="danger">确认结束牌桌</button>');$('#confirm-close').onclick=()=>{$('#modal').classList.add('hidden');command('close')}}
function renderLobby(){const s=state;$('#app').innerHTML=`<section class="lobby"><div class="row spread"><div><div class="eyebrow">${s.n===3?'SANMA':'YONMA'} · ${s.rounds===1?'东风战':'半庄战'}</div><h1>落座，开一局。</h1></div><button id="room-info">邀请好友 <strong>${esc(s.pin)}</strong> ↗</button></div><div class="seats">${Array.from({length:s.n},(_,i)=>{const p=s.players[i];return p?`<div class="seat ${p.bot?'bot':''}"><div class="avatar"><img src="${esc(portraitSrc(p))}" alt=""></div><h3>${esc(p.name)}</h3><p>${i===s.you?'你 · ':''}${i===0?'房主':p.bot?'电脑牌友':'已入座'}</p>${p.bot&&s.host?`<button class="quiet" data-remove="${i}">移除 AI</button>`:'<span class="seat-check">✓ 准备就绪</span>'}</div>`:`<div class="seat empty"><div class="avatar">＋</div><h3>虚位以待</h3><p>邀请好友，或添加电脑</p>${s.host?'<button class="add-ai">添加 AI</button>':''}</div>`}).join('')}</div><div class="lobby-bottom"><p class="note">AI 无需网络 · 真人不足也能开局<br>开局前可移除 AI，为好友留出座位。</p>${s.host?`<button id="start" class="primary">${s.players.length<s.n?'补齐 AI 并开局':'开始对局'} →</button>`:'<p>等待房主开始对局…</p>'}</div></section>`;$('#room-info').onclick=roomInfo;document.querySelectorAll('.add-ai').forEach(b=>b.onclick=()=>command('addBot'));document.querySelectorAll('[data-remove]').forEach(b=>b.onclick=()=>command('removeBot',{seat:+b.dataset.remove}));if(s.host)$('#start').onclick=()=>command('start',{fillBots:true});}
function tileName(p){if(!p)return '';return p[0]==='z'?honors[+p[1]]:(p[1]==='0'?'赤5':p[1])+names[p[0]]}
function face(p){return '<img class="tile-face" src="tiles/'+p.slice(0,2)+'.png" alt="" draggable="false">'}
function tile(p,small=false,extra=''){return `<span class="tile ${p[0]} ${p[1]==='0'||p==='z7'?'red':''} ${small?'small':''} ${extra}" title="${esc(tileName(p))}">${face(p)}</span>`}
function meld(m){
 const digits=m.match(/\d/g)||[],mark=m.search(/[+=-]/),dir=mark>=0?m[mark]:null;
 if(!dir)return '<span class="meld">'+digits.map((n,i)=>i===0||i===3?'<span class="tile small back"></span>':tile(m[0]+n,true)).join('')+'</span>';
 const calledIndex=(m.slice(1,mark).match(/\d/g)||[]).length-1,called=digits.splice(calledIndex,1)[0];
 const added=digits.length===3&&mark<m.length-1&&/\d$/.test(m)?digits.pop():null;
 const slot=dir==='-'?0:dir==='='?1:digits.length;
 const pieces=digits.map(n=>tile(m[0]+n,true));
 pieces.splice(slot,0,'<span class="meld-turned'+(added?' meld-stack':'')+'">'+tile(m[0]+called,true)+(added?tile(m[0]+added,true,'added-tile'):'')+'</span>');
 return '<span class="meld">'+pieces.join('')+'</span>';
}
function handFromString(h){return (h.split(',')[0].match(/[mpsz]\d+/g)||[]).flatMap(s=>s.slice(1).split('').map(n=>s[0]+n));}
function seatAt(i){const d=(i-state.you+state.n)%state.n;return d===0?'bottom':d===1?'right':d===state.n-1?'left':'top'}
function playerCard(i){
 const s=state,p=s.players[i],pos=seatAt(i),title=p.bot?'电脑牌友':i===s.you?'你':i===0?'房主':'牌友';
 return `<div class="player-card ${pos} ${s.turn===i?'current':''}"><div class="portrait portrait-${p.wind}"><img src="${esc(portraitSrc(p))}" alt="" draggable="false"><span class="portrait-wind">${winds[p.wind]}</span></div><div class="player-name" title="${esc(p.name)}">${esc(p.name)}</div><div class="player-caption">${title}${p.nuki?' · 拔北 '+p.nuki:''}${p.riichi?' · 立直':''}</div></div>`;
}
function opponentHand(i){if(i===state.you)return '';const p=state.players[i];return `<div class="opponent-hand ${seatAt(i)}" aria-label="${esc(p.name)}的暗牌 ${p.count} 张">${Array.from({length:p.count},()=>'<span class="tile back"></span>').join('')}</div>`;}
function exposed(i){const p=state.players[i];return `<div class="table-melds ${seatAt(i)}">${p.melds.map(meld).join('')}${p.nuki?'<span class="north-counter">'+tile('z4',true)+'<b>×'+p.nuki+'</b></span>':''}</div>`;}
function river(i){const p=state.players[i];return `<div class="river-area ${seatAt(i)}"><div class="river ${p.river.length>18?'long':''}">${p.river.map((r,j)=>`<span class="river-slot ${r.riichi?'declaration':''}">${tile(r.tile,true,(r.called?'called ':'')+(r.riichi?'riichi ':'')+(state.last?.from===i&&j===p.river.length-1?'last-discard':''))}</span>`).join('')}</div></div>`;}
function scoreRing(){const s=state;return s.players.map((p,i)=>`<div class="seat-score ${seatAt(i)} ${s.turn===i?'active':''}"><span class="score-wind ${p.riichi?'declared':''}">${winds[p.wind]}</span><span class="score-number">${p.score}</span><span class="turn-light"></span></div>`).join('');}
function fitTable(){const game=$('.game'),stage=$('.table-stage');if(!game||!stage)return;const scale=Math.min(game.clientHeight/720,game.clientWidth/1280),width=Math.max(1280,game.clientWidth/scale);stage.style.width=width+'px';stage.style.transform='translate(-50%,-50%) scale('+scale+')';}
window.addEventListener('resize',fitTable);
function renderTable(){const s=state;let status;if(s.pending)status=s.options.length?'请选择操作':'等待其他玩家响应';else status=s.turn===s.you?'轮到你出牌':s.players[s.turn].name+' 正在思考';
 $('#app').innerHTML=`<div class="game"><div class="table-stage"><div class="table-surface"></div><div class="table-inset"></div><div class="table-seams"></div><div class="game-plaque"><div class="dora">${Array.from({length:5},(_,i)=>s.indicators[i]?tile(s.indicators[i],true):'<span class="tile small back"></span>').join('')}</div><div class="table-counters"><span><i class="riichi-stick"></i> × ${s.sticks}</span><span><i class="honba-stick"></i> × ${s.honba}</span></div><div class="table-mode">好友场 · ${s.n===3?'三人':'四人'}${s.players.some(p=>p.bot)?' / AI':''}</div></div><div class="table-tools"><button id="table-preferences" aria-label="头像与语音设置">设置</button><button id="table-help" aria-label="查看玩法">?</button><button id="room-info" aria-label="查看房间信息">房间</button><button id="table-home" aria-label="返回大厅">↗</button></div><div class="center-board"><div class="center-core"><div class="round">${winds[Math.floor(s.round/s.n)]}${s.round%s.n+1}局</div><div class="remaining">余 <strong>${s.remaining}</strong></div></div>${scoreRing()}</div>${s.players.map((p,i)=>opponentHand(i)+river(i)+exposed(i)+playerCard(i)).join('')}<div class="table-watermark">云间<span>日麻</span><small>OFFLINE RIICHI</small></div><button id="log" class="table-log">牌局记录</button><div class="table-status ${s.turn===s.you?'your-turn':''}"><span class="status-dot"></span>${esc(status)}${s.pending?tile(s.pending.tile,true):''}</div><div id="actions" class="actions"></div><section class="hand-zone"><div class="hand-caption"><span>${riichiMode?'双击高亮手牌，立直打出':s.furiten?'振听 · 暂不可荣和':s.waits.length?'听 '+s.waits.map(tileName).join(' / '):'单击选牌 · 双击打出'}</span><span>${s.players[s.you].riichi?'立直中':''}</span></div><div class="hand" id="hand"></div></section><div class="table-connection"><i></i><i></i><i></i><span>LAN</span></div></div></div>`;
 $('#table-preferences').onclick=preferences;$('#table-help').onclick=help;$('#table-home').onclick=showHome;$('#room-info').onclick=roomInfo;$('#log').onclick=()=>modal('<h2>本局记录</h2>'+s.log.slice().reverse().map(x=>`<p>${esc(x.replace(/[mpsz][0-9]/g,tileName))}</p>`).join(''));renderHand();fitTable();
}

function renderHand(){const s=state,hand=s.hand.slice();if(s.drawn){const k=hand.lastIndexOf(s.drawn);if(k>=0){hand.splice(k,1);hand.push(s.drawn)}}
 const choices=s.options.filter(a=>a.kind===(riichiMode?'riichi':'discard'));if(!choices.some(a=>a.id===selected))selected=null;
 $('#hand').innerHTML=hand.map((p,i)=>{const drawn=!!s.drawn&&i===hand.length-1,a=choices.find(a=>a.tile===p&&a.id.endsWith('_')===drawn);return `<button class="tile ${p[0]} ${p[1]==='0'||p==='z7'?'red':''} ${drawn?'drawn':''} ${selected&&selected===a?.id&&selectedIndex===i?'selected':''} ${a?'playable':''}" data-index="${i}" ${a?'':'disabled'} aria-label="${esc(tileName(p))}${drawn?' 摸牌':''}">${face(p)}</button>`}).join('');
 let lastTap=null;
 $('#hand').querySelectorAll('button').forEach((b,i)=>b.onclick=()=>{
  if(busy)return;const drawn=!!s.drawn&&i===hand.length-1,a=choices.find(a=>a.tile===hand[i]&&a.id.endsWith('_')===drawn);if(!a)return;
  const now=performance.now(),doubleTap=lastTap&&lastTap.index===i&&lastTap.action===a.id&&now-lastTap.time<=450;
  selectedIndex=i;selected=a.id;
  $('#hand').querySelectorAll('button').forEach((el,j)=>el.classList.toggle('selected',j===i));
  if(doubleTap){lastTap=null;command('action',{action:a.id});}else lastTap={index:i,action:a.id,time:now};
 });
 const actions=s.options.filter(a=>!['discard','riichi'].includes(a.kind));let html='';
 if(s.options.some(a=>a.kind==='riichi'))html+=`<button id="toggle-riichi" class="riichi-button">${riichiMode?'取消立直':'立直'}</button>`;
 const labels={tsumo:'自摸',ron:'荣和',kan:'杠',chi:'吃',pon:'碰',minkan:'明杠',nuki:'拔北',abort9:'九种九牌',pass:'过'};
 html+=actions.map((a,i)=>`<button data-action="${i}" class="${['ron','tsumo'].includes(a.kind)?'primary win':''}">${labels[a.kind]}${a.meld?meld(a.meld):''}</button>`).join('');$('#actions').innerHTML=html||'<span class="waiting">静候好牌<br><small>轮到你时显示操作</small></span>';
 if($('#toggle-riichi'))$('#toggle-riichi').onclick=()=>{riichiMode=!riichiMode;selected=null;selectedIndex=-1;renderTable()};$('#actions').querySelectorAll('[data-action]').forEach(b=>b.onclick=()=>command('action',{action:actions[+b.dataset.action].id}));
}
const yakuNames={'門前清自摸和':'门前清自摸和','立直':'立直','ダブル立直':'两立直','一発':'一发','断幺九':'断幺九','平和':'平和','ドラ':'宝牌','赤ドラ':'赤宝牌','裏ドラ':'里宝牌','嶺上開花':'岭上开花','槍槓':'抢杠','海底摸月':'海底捞月','河底撈魚':'河底捞鱼','七対子':'七对子','対々和':'对对和','混一色':'混一色','清一色':'清一色','国士無双':'国士无双','国士無双十三面':'国士无双十三面','四暗刻単騎':'四暗刻单骑'};
function renderResult(){const s=state,r=s.result;$('#app').innerHTML=`<section class="result"><div class="eyebrow">${r.matchOver?'FINAL RESULT · 对局结束':'HAND RESULT · 本局结算'}</div><h1>${esc(r.title)}</h1>${r.details.map(d=>`<div class="card"><div class="row spread"><h2>${esc(d.name)}</h2><b>${d.score.damanguan?d.score.damanguan+' 倍役满':d.score.fanshu+' 番 '+d.score.fu+' 符'}</b></div><div class="result-hand">${handFromString(d.hand).map(p=>tile(p,true)).join('')}${d.ron?tile(d.tile,true):''}${d.hand.split(',').slice(1).filter(Boolean).map(meld).join('')}</div><div class="yaku">${d.score.hupai.map(y=>`<span>${esc(yakuNames[y.name]||y.name)} ${typeof y.fanshu==='number'?y.fanshu+' 番':esc(y.fanshu)}</span>`).join('')}</div>${d.ura.length?`<p>里宝牌指示 ${d.ura.map(p=>tile(p,true)).join('')}</p>`:''}</div>`).join('')}<div class="card"><div class="score-list">${s.players.map((p,i)=>`<div class="score-item"><span>${esc(p.name)}${r.tenpai?' · '+(r.tenpai[i]?'听牌':'未听'):''}${r.nagashi?.includes(i)?' · 流满':''}</span><span><b class="${r.delta[i]>=0?'plus':'minus'}">${r.delta[i]>0?'+':''}${r.delta[i]}</b> → ${p.score.toLocaleString()}</span></div>${r.revealed?.[i]?`<div class="result-hand">${handFromString(r.revealed[i]).map(p=>tile(p,true)).join('')}${r.revealed[i].split(',').slice(1).filter(Boolean).map(meld).join('')}</div>`:''}`).join('')}</div><p>${r.matchOver?'本场已结束；全员确认后可再来一场。':r.repeat?'庄家连庄。':'下一家坐庄。'} ${s.sticks?'供托保留 '+s.sticks+' 根。':''}</p><div class="row"><button id="ready" class="primary" ${s.players[s.you].ready?'disabled':''}>${s.players[s.you].ready?'已确认':'确认结算'}</button>${s.host?`<button id="next" ${s.players.every(p=>p.ready)?'':'disabled'}>${r.matchOver?'再来一场':'下一局'}</button><button class="quiet danger" id="close-room">结束牌桌</button>`:''}</div><p class="muted">${s.players.filter(p=>!p.ready).map(p=>esc(p.name)).join('、')||'全员'} ${s.players.every(p=>p.ready)?'已确认':'尚未确认'}</p></div></section>`;$('#ready').onclick=()=>command('ready');if(s.host){$('#next').onclick=()=>command('next');$('#close-room').onclick=closeRoom}}


showHome();poll();


