"""One-shot protocol migration. Do not rerun after the markers exist."""
from pathlib import Path
import json
R=Path(__file__).resolve().parent.parent;p=R/'app/src/main/assets/engine.js';s=p.read_text('utf8');assert 'prepareResources'not in s
cfg=json.loads((R/'build/characters-1018/config.json').read_text('utf8'));registry={r['sound_folder']:[x['id']for x in cfg['item_definition/skin']if x['character_id']==r['id']]for r in cfg['item_definition/character']}
a=s.index('const CHARACTER_SKINS=');b=s.index('\n',a);s=s[:a]+'const CHARACTER_SKINS='+json.dumps(registry,separators=(',',':'))+';'+s[b:];s=s.replace('pool.length<=21','pool.length<=1024')
s=s.replace('function publicCharacter(p,phase){','function publicCharacter(p,phase,prepared=false){').replace("phase==='lobby'?(p.character||'yiji')","phase==='lobby'&&!prepared?(p.character||'yiji')").replace("phase==='lobby'?(p.skin||400101)","phase==='lobby'&&!prepared?(p.skin||400101)")
s=s.replace('publicCharacter(p,s.phase)','publicCharacter(p,s.phase,!!s.resourceSetup)')
s=s.replace("lobbyFlow:req.lobbyFlow===true,","resourceProtocol:req.resourceProtocol===1?1:0,resourceRevision:0,lobbyFlow:req.lobbyFlow===true,")
s=s.replace("const p=player(String(req.name||'房主').slice(0,12));","const p=player(String(req.name||'房主').slice(0,12));p.resourceProtocol=req.resourceProtocol===1?1:0;")
s=s.replace("this.save(JSON.stringify(s));this.seen[s.players[i].token]=Date.now();return {ok:true,token:s.players[i].token,state:this.view(i)};","s.players[i].resourceProtocol=req.resourceProtocol===1?1:0;if(characterNewSeat)delete s.resourceSetup;this.save(JSON.stringify(s));this.seen[s.players[i].token]=Date.now();return {ok:true,token:s.players[i].token,state:this.view(i)};")
s=s.replace("if(req.op!=='profile'&&req.op!=='ready'&&req.op!=='character')","if(!['resourceAck','resourceOffer','cancelResourceSetup','profile','ready','character'].includes(req.op))")
insert='''   // Resource reports are public cosmetics only, independent of turn versions.
   if(req.op==='resourceOffer'){
    const offer=req.resourceShare||{};check(Number.isInteger(offer.port)&&offer.port>=1024&&offer.port<=65535&&/^[a-f0-9]{32}$/.test(offer.ticket),'资源分享信息无效');check(Array.isArray(offer.hashes)&&offer.hashes.length<=200&&offer.hashes.every(h=>/^[a-f0-9]{64}$/.test(h)),'资源清单无效');
    const address=String(req.peerAddress||'127.0.0.1').replace(/^::ffff:/,'');check(/^(127\\.|10\\.|192\\.168\\.|172\\.(1[6-9]|2[0-9]|3[01])\\.)/.test(address),'资源分享地址无效');s.players[i].resourceShare={address,port:offer.port,ticket:offer.ticket,hashes:[...new Set(offer.hashes)]};this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   if(req.op==='resourceAck'){
    check(s.phase==='lobby'&&s.resourceSetup&&req.setupRevision===s.resourceSetup.revision,'资源准备已更新');check(['pending','downloading','ready','fallback'].includes(req.resourceStatus),'资源确认状态无效');const keys=req.fallbackCharacters||[];check(Array.isArray(keys)&&keys.every(k=>s.resourceSetup.choices.some(c=>c.character===k)),'替代角色无效');s.resourceSetup.confirmations[req.token]={state:req.resourceStatus,fallback:keys};this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   if(req.op==='cancelResourceSetup'){check(i===0&&s.phase==='lobby','仅房主可取消开局准备');delete s.resourceSetup;this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};}
   if(req.op==='prepareResources'){
    check(i===0&&s.phase==='lobby','仅房主可准备开局');if(req.fillBots===true)while(s.players.length<s.n)this.addBot();check(s.players.length===s.n,'还有空座位');check(!s.lobbyFlow||s.players.every((p,j)=>p.bot||j===0||p.lobbyReady),'请等待所有玩家准备');
    if(!s.resourceSetup){s.players.forEach(freezeCharacter);s.resourceRevision=(s.resourceRevision||0)+1;s.resourceSetup={revision:s.resourceRevision,createdAt:Date.now(),choices:s.players.map(p=>({character:p.matchCharacter,skin:p.matchSkin})),confirmations:{}};}
    this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
'''
needle="   // Waiting-room operations terminate here.";assert needle in s;s=s.replace(needle,insert+needle)
s=s.replace("if(req.op==='lobbyReady'){check", "if(req.op==='lobbyReady'){if(req.ready!==true)delete s.resourceSetup;check")
s=s.replace("else if(req.op==='kick'){","else if(req.op==='kick'){delete s.resourceSetup;")
s=s.replace("}else if(req.op==='leaveLobby'){","}else if(req.op==='leaveLobby'){delete s.resourceSetup;")
s=s.replace("if(s.phase==='lobby')s.players.splice(i,1);","if(s.phase==='lobby'){s.players.splice(i,1);delete s.resourceSetup;}")
s=s.replace("if(req.op==='character'){check(s.phase==='lobby',","if(req.op==='character'){check(!s.resourceSetup,'请先取消开局准备再切换雀士');check(s.phase==='lobby',")
s=s.replace("else if(req.op==='addBot'||req.op==='fillBots'){check", "else if(req.op==='addBot'||req.op==='fillBots'){delete s.resourceSetup;check")
s=s.replace("else if(req.op==='removeBot'){check", "else if(req.op==='removeBot'){delete s.resourceSetup;check")
needle="else if(req.op==='start'){check(i===0&&s.phase==='lobby','仅房主可开局');";assert needle in s
s=s.replace(needle,needle+"const prepared=s.resourceSetup;if(s.resourceProtocol===1){check(prepared&&req.setupRevision===prepared.revision,'请先完成资源准备');check(this.resourcesReady(),'请等待资源下载或选择一姬替代');}")
s=s.replace("p.lastInputAt=s.matchOpeningUntil;freezeCharacter(p);","p.lastInputAt=s.matchOpeningUntil;if(!prepared)freezeCharacter(p);")
s=s.replace("s.dealer=parseInt(randomHex(2),16)%s.n;","delete s.resourceSetup;s.dealer=parseInt(randomHex(2),16)%s.n;")
needle=' controlled(p){return !!(p.bot||p.auto)}';assert needle in s
s=s.replace(needle,needle+'''
 resourcesReady(){const s=this.s;if(!s?.resourceSetup)return false;return s.players.every(p=>p.bot||p.resourceProtocol!==1||['ready','fallback'].includes(s.resourceSetup.confirmations[p.token]?.state));}
 resourceView(){const s=this.s;if(!s?.resourceSetup)return null;const x=s.resourceSetup;return {revision:x.revision,createdAt:x.createdAt,ready:this.resourcesReady(),choices:x.choices,players:s.players.map((p,seat)=>({seat,state:p.bot||p.resourceProtocol!==1?'ready':x.confirmations[p.token]?.state||'pending'}))};}
''')
s=s.replace("return {phase:s.phase,version:s.version,","return {resourceProtocol:s.resourceProtocol||0,resourceSetup:this.resourceView(),resourcePeers:s.players.filter(p=>!p.bot&&p.resourceShare).map(p=>({...p.resourceShare,name:p.name})),phase:s.phase,version:s.version,")
# Resource preparations survive retries; expired preparations never pin a room forever.
s=s.replace("presence(now){\n  const s=this.s;let changed=false;","presence(now){\n  const s=this.s;let changed=false;if(s.phase==='lobby'&&s.resourceSetup&&now-s.resourceSetup.createdAt>180000){delete s.resourceSetup;changed=true;}")
p.write_text(s,encoding='utf8');print('Resource preparation protocol installed')
