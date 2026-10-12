/* LAN Riichi room controller. Runs only on the host, never on guest pages. */
(function(g){
'use strict';
const M=g.Majiang, copy=x=>JSON.parse(JSON.stringify(x)), norm=p=>p.slice(0,2).replace('0','5');
const RULE=M.rule({'カンドラ後乗せ':false,'リーチ後暗槓許可レベル':1});
function check(ok,msg){if(!ok)throw Error(msg)}
function randomHex(n){const b=new Uint8Array(n);g.crypto.getRandomValues(b);return Array.from(b,x=>x.toString(16).padStart(2,'0')).join('')}
function shuffle(a){for(let i=a.length-1;i>0;i--){const b=new Uint32Array(1);let max=Math.floor(4294967296/(i+1))*(i+1);do{g.crypto.getRandomValues(b)}while(b[0]>=max);const j=b[0]%(i+1);[a[i],a[j]]=[a[j],a[i]];}return a}
function tiles(h){const a=[];for(const s of ['m','p','s','z'])for(let n=1;n<h._bingpai[s].length;n++)for(let k=0;k<h._bingpai[s][n];k++)a.push(s+(n===5&&k<h._bingpai[s][0]?0:n));return a}
function avatar(value){check(typeof value==='string'&&value.length<=12288&&(value===''||/^data:image\/jpeg;base64,\/9j\/[A-Za-z0-9+/]*={0,2}$/.test(value)),'头像格式或大小不正确');return value}
function player(name,token){return {name,avatar:"",token:token||randomHex(18),recovery:randomHex(6),score:0,hand:'',river:[],riichi:0,ippatsu:false,nuki:0,temp:false,permanent:false,kans:0,ready:false,requests:[]}}
class Room{
 constructor(host,save,snapshot){this.host=host;this.save=save||(()=>{});this.s=snapshot?JSON.parse(snapshot):null;this.seen={};}
 sound(kind,seat,tile,riichi,nuki,win,meld){const s=this.s;s.audioSeq=(s.audioSeq||0)+1;s.audioEvents=(s.audioEvents||[]).concat({id:s.audioSeq,kind,seat,tile:tile||null,riichi:!!riichi,...(kind==='discard'?{riverIndex:s.players[seat].river.length}:{}),...(nuki?{nuki}:{}),...(win?{from:win.from,winSource:win.kind}:{}),...(meld?{meld:meld.value,meldKind:meld.kind,meldIndex:meld.index,from:meld.from}:{}),at:Date.now()}).slice(-24);}
 addBot(){const s=this.s;check(s.players.length<s.n,'座位已满');let n=1;while(s.players.some(p=>p.name==='AI · '+n))n++;const p=player('AI · '+n);p.bot=true;s.players.push(p);}
 tick(){
  const s=this.s;if(!s||!g.RiichiAI||!['play','reaction'].includes(s.phase)||Date.now()<(s.matchOpeningUntil||0))return {ok:true,idle:true};
  const i=s.phase==='play'?s.turn:s.players.findIndex((p,j)=>p.bot&&this.options(j).length);
  if(i<0||!s.players[i].bot)return {ok:true,idle:true};
  const before=JSON.stringify(s);
  try{const action=g.RiichiAI.choose(this.view(i));if(!action)return {ok:true,idle:true};this.act(i,action);s.version++;this.save(JSON.stringify(s));return {ok:true,action};}
  catch(e){this.s=JSON.parse(before);return {ok:false,error:String(e.message||e)}}
 }
 h(i){return M.Shoupai.fromString(this.s.players[i].hand)}
 put(i,h){this.s.players[i].hand=h.toString()}
 wind(i){return (i-this.s.dealer+this.s.n)%this.s.n}
 dir(i,from){return '_+=-'[(4+this.wind(from)-this.wind(i))%4]}
 remain(){return this.s.wall.length}
 totalKans(){return this.s.players.reduce((a,p)=>a+p.kans,0)}
 waits(h){if(h._zimo||M.Util.xiangting(h)!==0)return [];return M.Util.tingpai(h).filter(p=>!(this.s.n===3&&p[0]==='m'&&p!=='m1'&&p!=='m9'))}
 furiten(i){const p=this.s.players[i],w=this.waits(this.h(i));return p.temp||p.permanent||p.river.some(r=>w.includes(norm(r.tile)))}
 // Four copies minus this viewer's hand and public faces. Never inspect other hands or the wall.
 unseen(i,tile){
  const s=this.s,kind=norm(tile);if(s.n===3&&kind[0]==='m'&&!['m1','m9'].includes(kind))return 0;
  let seen=tiles(this.h(i)).filter(t=>norm(t)===kind).length;
  for(const p of s.players){
   seen+=p.river.filter(t=>!t.called&&norm(t.tile)===kind).length;
   if(kind==='z4')seen+=p.nuki;
  }
  // _fulou is public, including the declared identity of a concealed kan.
  for(let seat=0;seat<s.n;seat++)for(const meld of this.h(seat)._fulou)
   seen+=Array.from(meld.slice(1)).filter(d=>/[0-9]/.test(d)&&norm(meld[0]+d)===kind).length;
  // Robbing-kong/North reactions publicly declare faces before committing the meld.
  const pending=s.pending;if(pending&&pending.from!==i&&norm(pending.tile)===kind)
   seen+=pending.kind==='ankan'?4:['kakan','nuki'].includes(pending.kind)?1:0;
  seen+=s.indicators.slice(0,1+this.totalKans()).filter(t=>norm(t)===kind).length;
  return Math.max(0,4-seen);
 }
 waitInfo(i,h,discard=null,riichi=false){
  const s=this.s,p=s.players[i],waits=this.waits(h);
  const furiten=!!(p.temp||p.permanent||p.river.some(r=>waits.includes(norm(r.tile)))||(discard&&waits.includes(norm(discard))));
  const param=M.Util.hule_param({rule:RULE,menfeng:this.wind(i),zhuangfeng:Math.floor(s.round/s.n),lizhi:p.riichi||(riichi?1:0),haidi:this.remain()===0?2:0});
  const details=waits.map(tile=>{
   const ron=M.Util.hule(h,tile+this.dir(i,(i+1)%s.n),param);
   const tsumoParam={...param,hupai:{...param.hupai,haidi:this.remain()===0?1:0}};
   const tsumo=M.Util.hule(h.clone().zimo(tile),null,tsumoParam);
   return {tile,ron:!!(ron&&ron.hupai),tsumo:!!(tsumo&&tsumo.hupai),remaining:this.unseen(i,tile)};
  });
  return {furiten,waits:details};
 }
 hints(i,h,options){
  if(!h)return {waitDetails:[],discardHints:[],furiten:false};
  const base=h._zimo&&h._zimo.length===2?h.clone().dapai(h._zimo+'_'):h;
  const info=this.waitInfo(i,base),discardHints=[];
  if(!this.s.players[i].bot)for(const a of options.filter(a=>a.kind==='discard'||a.kind==='riichi')){
   const after=h.clone().dapai(a.id.slice(a.id.indexOf(':')+1));
   const candidate=this.waitInfo(i,after,a.tile,a.kind==='riichi');
   if(candidate.waits.length)discardHints.push({id:a.id,...candidate});
  }
  return {waitDetails:info.waits,discardHints,furiten:info.furiten};
 }
 score(i,from=null,tile=null,kind='discard',sticks=0){
  const s=this.s,p=s.players[i],h=this.h(i),ron=from!==null;
  if(ron&&this.furiten(i))return null;
  const indicators=s.indicators.slice(0,1+this.totalKans()),ura=p.riichi?s.ura.slice(0,indicators.length):[];
  const mapInd=a=>a.map(t=>s.n===3&&t==='m1'?'m8':t);
  const param=M.Util.hule_param({rule:RULE,menfeng:this.wind(i),zhuangfeng:Math.floor(s.round/s.n),lizhi:p.riichi,yifa:p.ippatsu,
   qianggang:kind==='kakan',lingshang:!ron&&s.drawKind==='kan',haidi:this.remain()===0&&kind==='discard'&&s.drawKind!=='kan'?(ron?2:1):0,
   tianhu:!ron&&!s.interrupted&&p.river.length===0?(i===s.dealer?1:2):0,
   baopai:mapInd(indicators),fubaopai:mapInd(ura),changbang:s.honba,lizhibang:sticks});
  param.nuki=p.nuki*(1+indicators.concat(ura).filter(t=>M.Shan.zhenbaopai(t)==='z4').length);
  const result=M.Util.hule(h,ron?tile.slice(0,2)+this.dir(i,from):null,param);
  if(!result||!result.hupai)return null;
  if((kind==='ankan'||kind==='nuki')&&!result.hupai.some(x=>x.name.includes('国士')))return null;
  if(s.n===3){result.fenpei[this.wind(i)]+=result.fenpei[3];result.defen+=result.fenpei[3];result.fenpei[3]=0;}
  result.delta=s.players.map((_,j)=>result.fenpei[this.wind(j)]);
  return result;
 }
 options(i){
  const s=this.s;if(!s||!['play','reaction'].includes(s.phase))return [];
  if(s.phase==='reaction')return s.pending.options[i]&&!s.pending.replies[i]?s.pending.options[i]:[];
  if(i!==s.turn)return [];
  const h=this.h(i),p=s.players[i],a=(M.Game.get_dapai(RULE,h)||[]).map(v=>({id:'discard:'+v,kind:'discard',tile:v.slice(0,2)}));
  if(h._zimo&&h._zimo.length===2){
   if(this.score(i))a.push({id:'tsumo',kind:'tsumo'});
   if(!p.riichi&&h.menqian&&p.score>=1000&&this.remain()>=s.n){
    for(const x of a.filter(x=>x.kind==='discard'))if(this.waits(h.clone().dapai(x.id.slice(8))).length)a.push({id:'riichi:'+x.id.slice(8),kind:'riichi',tile:x.tile});
   }
   for(const m of M.Game.get_gang_mianzi(RULE,h,null,this.remain(),this.totalKans())||[]){
    // Compare the actual wait set as well as upstream's strict decomposition check.
    if(p.riichi&&this.waits(h.clone().dapai(h._zimo)).sort().join()!==this.waits(h.clone().gang(m)).sort().join())continue;
    a.push({id:'kan:'+m,kind:'kan',meld:m});
   }
   if(s.n===3&&h._bingpai.z[4]>0&&this.remain()>0&&(!p.riichi||h._zimo==='z4'))a.push({id:'nuki',kind:'nuki'});
   if(!s.interrupted&&p.river.length===0&&M.Game.allow_pingju(RULE,h,true))a.push({id:'abort9',kind:'abort9'});
  }
  return a;
 }
 newHand(){
  const s=this.s,wall=[];for(const suit of ['m','p','s','z'])for(let n=1;n<=(suit==='z'?7:9);n++){
   if(s.n===3&&suit==='m'&&n>1&&n<9)continue;
   for(let k=0;k<4;k++)wall.push(suit+(s.red&&suit!=='z'&&n===5&&k===0?0:n));
  }
  shuffle(wall);s.rinshan=wall.splice(0,4);s.indicators=wall.splice(0,5);s.ura=wall.splice(0,5);s.deadFill=[];s.wall=wall;
  s.handSeq=(s.handSeq||0)+1;s.dealtAt=Date.now();
  s.interrupted=false;s.phase='play';s.pending=null;s.result=null;s.log=[];s.last=null;s.turn=s.dealer;s.drawKind='normal';
  for(let i=0;i<s.n;i++){let p=s.players[i],dealt=wall.splice(0,13);Object.assign(p,{dealt,hand:new M.Shoupai(dealt).toString(),river:[],riichi:0,ippatsu:false,nuki:0,temp:false,permanent:false,kans:0,ready:false});}
  this.draw(s.dealer);s.players[s.dealer].dealt.push(this.h(s.dealer)._zimo);this.log('开局');
 }
 log(text){this.s.log.push(text);if(this.s.log.length>12)this.s.log.shift()}
 interrupt(){this.s.interrupted=true;this.s.players.forEach(p=>p.ippatsu=false)}
 draw(i,kind='normal'){
  const s=this.s;if(!s.wall.length)return this.exhaust();
  s.turn=i;s.phase='play';s.pending=null;s.drawKind=kind;let tile;
  if(kind==='kan'){check(s.rinshan.length,'岭上牌不足');tile=s.rinshan.shift();s.deadFill.push(s.wall.pop());}else tile=s.wall.pop();
  this.put(i,this.h(i).zimo(tile));s.players[i].temp=false;
 }
 react(from,tile,kind='discard',meld=null,riichi=false){
  const s=this.s,options={},replies={};s.phase='reaction';s.pending={from,tile,kind,meld,riichi,options,replies};
  for(let d=1;d<s.n;d++){
   const i=(from+d)%s.n,h=this.h(i),a=[];
   if(this.score(i,from,tile,kind))a.push({id:'ron',kind:'ron'});
   if(kind==='discard'&&this.remain()>0){
    const t=tile+this.dir(i,from);
    if(s.n===4&&d===1)for(const m of M.Game.get_chi_mianzi(RULE,h,t,this.remain())||[])a.push({id:'chi:'+m,kind:'chi',meld:m});
    for(const m of M.Game.get_peng_mianzi(RULE,h,t,this.remain())||[])a.push({id:'pon:'+m,kind:'pon',meld:m});
    for(const m of M.Game.get_gang_mianzi(RULE,h,t,this.remain(),this.totalKans())||[])a.push({id:'minkan:'+m,kind:'minkan',meld:m});
   }
   if(a.length)options[i]=a.concat({id:'pass',kind:'pass'});
   else this.missed(i,tile,kind);
  }
  if(!Object.keys(options).length)this.resolve();
 }
 missed(i,tile,kind){
  const p=this.s.players[i];
  if(this.waits(this.h(i)).includes(norm(tile))){
   // Closed-kan and North can only be robbed by kokushi; other shapes are not passed wins.
   if(kind==='ankan'||kind==='nuki'){if(!M.Util.hule_mianzi(this.h(i),tile+this.dir(i,this.s.pending.from)).some(m=>m.length===13))return;}
   p.temp=true;if(p.riichi)p.permanent=true;
  }
 }
 resolve(){
  const s=this.s,q=s.pending,{from,tile,kind}=q,order=Array.from({length:s.n-1},(_,d)=>(from+d+1)%s.n),wins=order.filter(i=>q.replies[i]==='ron');
  if(wins.length===3)return this.abort('三家和（同时三家荣和）');
  if(wins.length)return this.win(wins,from,tile,kind);
  if(kind!=='discard'){
   this.interrupt();const h=this.h(from);
   if(kind==='nuki'){h.decrease('z',4);h._zimo=null;this.put(from,h);s.players[from].nuki++;this.sound('nuki',from,'z4',false,s.players[from].nuki);this.log(s.players[from].name+' 拔北');this.draw(from,'nuki');}
   else {h.gang(q.meld);this.put(from,h);s.players[from].kans++;this.sound('kan',from,q.meld.slice(0,2),false,0,null,{value:q.meld,kind,index:h._fulou.indexOf(q.meld),from});this.log(s.players[from].name+' 开杠');this.draw(from,'kan');}
   return;
  }
  if(q.riichi){s.players[from].score-=1000;s.sticks++;this.sound(s.players[from].riichi===2?'double-riichi':'riichi',from);this.sound('riichi-stick',from);if(s.n===4&&s.players.every(p=>p.riichi))return this.abort('四家立直');}
  if(s.n===4&&!s.interrupted&&s.players.every(p=>p.river.length===1)&&s.players.every(p=>/^z[1-4]$/.test(p.river[0].tile)&&p.river[0].tile===tile))return this.abort('四风连打');
  if(this.totalKans()===4&&Math.max(...s.players.map(p=>p.kans))<4)return this.abort('四杠散了');
  if(!this.remain())return this.exhaust();
  const caller=order.find(i=>/^(pon|minkan):/.test(q.replies[i]||''))??order.find(i=>/^chi:/.test(q.replies[i]||''));
  if(caller!==undefined){
   const action=q.replies[caller],m=action.slice(action.indexOf(':')+1);this.interrupt();this.put(caller,this.h(caller).fulou(m));this.sound(action.startsWith('minkan:')?'kan':action.split(':')[0],caller,tile,false,0,null,{value:m,kind:action.split(':')[0],index:this.h(caller)._fulou.length-1,from});
   s.players[from].river[s.players[from].river.length-1].called=true;
   s.turn=caller;s.pending=null;s.phase='play';s.drawKind='call';this.log(s.players[caller].name+' '+({pon:'碰',chi:'吃',minkan:'明杠'}[action.split(':')[0]]));
   if(action.startsWith('minkan:')){s.players[caller].kans++;this.draw(caller,'kan');}
  }else this.draw((from+1)%s.n);
 }
 win(winners,from=null,tile=null,kind='discard'){
  const s=this.s,delta=s.players.map(()=>0),details=[];
  for(let k=0;k<winners.length;k++){
   const i=winners[k],r=this.score(i,from,tile,kind,k===0?s.sticks:0);check(r,'和牌条件已变化');
   const winTile=from===null?this.h(i)._zimo:tile,winFrom=from===null?i:from,winSource=from===null?'tsumo':kind;
   const handTiles=tiles(this.h(i));if(from===null){const at=handTiles.lastIndexOf(winTile);if(at>=0)handTiles.splice(at,1);}
    const yakuNames={'門前清自摸和':'门前清自摸和','ドラ':'宝牌','裏ドラ':'里宝牌','赤ドラ':'赤宝牌','七対子':'七对子','槍槓':'抢杠','海底摸月':'海底摸月','河底撈魚':'河底捞鱼','嶺上開花':'岭上开花'};
    const yaku=r.hupai.map(y=>({name:yakuNames[y.name]||y.name,han:typeof y.fanshu==='number'?y.fanshu:0,yakuman:typeof y.fanshu==='string'?(y.fanshu.match(/\*/g)||[]).length:0}));
    const limit=r.damanguan?(r.damanguan===1?'役满':r.damanguan+'倍役满'):r.fanshu>=13?'累计役满':r.fanshu>=11?'三倍满':r.fanshu>=8?'倍满':r.fanshu>=6?'跳满':r.fanshu>=5||(r.fanshu===4&&r.fu>=40)||(r.fanshu===3&&r.fu>=70)?'满贯':'';
    this.sound(from===null?'tsumo':'ron',i,winTile,false,0,{from:winFrom,kind:winSource});r.delta.forEach((v,j)=>delta[j]+=v);details.push({seat:i,from:winFrom,winSource,tiles:handTiles,melds:this.h(i)._fulou.slice(),yaku,limit,name:s.players[i].name,hand:s.players[i].hand,tile:from===null?this.h(i)._zimo:tile,ron:from!==null,score:r,nuki:s.players[i].nuki,ura:s.players[i].riichi?s.ura.slice(0,1+this.totalKans()):[]});
  }
  s.sticks=0;this.finish({title:from===null?'自摸':winners.length===2?'双响 · 荣和':'荣和',delta,details,repeat:winners.includes(s.dealer),draw:false});
 }
 abort(title,seat=null){this.finish({title,delta:this.s.players.map(()=>0),details:[],repeat:true,draw:true,abort:true,...(seat===null?{}:{abortHand:{seat,tiles:tiles(this.h(seat))}})})}
 exhaust(){
  const s=this.s,tenpai=s.players.map((_,i)=>this.waits(this.h(i)).length>0),nagashi=[];
  for(let i=0;i<s.n;i++)if(s.players[i].river.length&&s.players[i].river.every(r=>!r.called&&(r.tile[0]==='z'||/[19]/.test(r.tile[1]))))nagashi.push(i);
  const delta=s.players.map(()=>0);let title='荒牌流局';
  if(nagashi.length){title='流局满贯';for(const i of nagashi)for(let j=0;j<s.n;j++)if(i!==j){const amt=(i===s.dealer||j===s.dealer?4000:2000);delta[i]+=amt;delta[j]-=amt;}}
  else{const n=tenpai.filter(Boolean).length,pool=s.n===3?2000:3000;if(n&&n<s.n)tenpai.forEach((yes,i)=>delta[i]=yes?pool/n:-pool/(s.n-n));}
  const drawDetails=s.players.map((p,i)=>({seat:i,tenpai:tenpai[i],tiles:tenpai[i]?tiles(this.h(i)):[],waits:tenpai[i]?this.waits(this.h(i)):[]}));
  this.finish({title,delta,details:[],drawDetails,tenpai,revealed:s.players.map((p,i)=>tenpai[i]?p.hand:null),nagashi,repeat:tenpai[s.dealer],draw:true});
 }
 finish(result){
  const s=this.s;result.delta.forEach((v,i)=>s.players[i].score+=v);s.phase='result';s.result=result;s.pending=null;s.players.forEach(p=>p.ready=!!p.bot);this.log(result.title);
  const last=s.round>=s.rounds*s.n-1,target=s.n===3?40000:30000,top=Math.max(...s.players.map(p=>p.score));
  result.matchOver=s.players.some(p=>p.score<0)||(!result.abort&&last&&((!result.repeat&&top>=target)||(result.repeat&&s.players[s.dealer].score>=target&&s.players[s.dealer].score===top)));
  // Fixed maximum extension of one extra wind. No endless west/north extensions.
  if(s.round>=(s.rounds+1)*s.n-1&&!result.repeat)result.matchOver=true;
  if(result.matchOver&&s.sticks){const leader=s.players.findIndex(p=>p.score===top);s.players[leader].score+=s.sticks*1000;result.delta[leader]+=s.sticks*1000;s.sticks=0;}
 }
 act(i,id){
  const s=this.s;check(this.options(i).some(a=>a.id===id),'此操作当前不可用');
  if(s.phase==='reaction'){
   s.pending.replies[i]=id;if(id!=='ron')this.missed(i,s.pending.tile,s.pending.kind);
   if(Object.keys(s.pending.options).every(k=>s.pending.replies[k]))this.resolve();return;
  }
  const h=this.h(i),p=s.players[i];
  if(id==='tsumo')return this.win([i]);
  if(id==='abort9')return this.abort('九种九牌',i);
  if(id==='nuki')return this.react(i,'z4','nuki');
  if(id.startsWith('kan:')){const m=id.slice(4),kind=/[+=-]/.test(m)?'kakan':'ankan';return this.react(i,m[0]+m.slice(-1),kind,m);}
  const riichi=id.startsWith('riichi:'),t=id.slice(id.indexOf(':')+1),tile=t.slice(0,2);
  p.ippatsu=false;
  if(riichi){p.riichi=!s.interrupted&&p.river.length===0?2:1;p.ippatsu=true;}
  this.put(i,h.dapai(t+(riichi?'*':'')));p.river.push({tile,riichi,tsumogiri:t.includes('_'),called:false});s.last={from:i,tile};
  this.sound('discard',i,tile,riichi);this.log(p.name+(riichi?' 立直':'')+' 打 '+tile);this.react(i,tile,'discard',null,riichi);
 }
 view(i){
  const s=this.s;if(!s)return {phase:'empty'};
  if(i<0)return {phase:'join',n:s.n,count:s.players.length,started:s.phase!=='lobby'};
  const h=s.players[i].hand?this.h(i):null,options=this.options(i);
  const hints=['play','reaction'].includes(s.phase)?this.hints(i,h,options):{waitDetails:[],discardHints:[],furiten:false};
  return {phase:s.phase,version:s.version,you:i,host:i===0,n:s.n,pin:s.pin,round:s.round,rounds:s.rounds,dealer:s.dealer,honba:s.honba,sticks:s.sticks,turn:s.turn,
   audioRoom:s.audioRoom||s.pin,audioSeq:s.audioSeq||0,audioEvents:s.audioEvents||[],remaining:s.wall?s.wall.length:0,indicators:s.indicators?s.indicators.slice(0,1+this.totalKans()):[],last:s.last,
   players:s.players.map((p,j)=>({name:p.name,avatar:p.avatar||"",bot:!!p.bot,score:p.score,wind:this.wind(j),riichi:p.riichi,nuki:p.nuki,river:p.river,melds:p.hand?this.h(j)._fulou:[],count:p.hand?tiles(this.h(j)).length:0,ready:p.ready,online:p.bot||Date.now()-(this.seen[p.token]||0)<12000})),
   handSeq:s.handSeq||0,dealtAt:s.dealtAt||0,matchSeq:s.matchSeq||0,matchStartedAt:s.matchStartedAt||0,openingHand:(s.players[i].dealt||[]).slice(),
   hand:h?tiles(h):[],drawn:h&&h._zimo&&h._zimo.length===2?h._zimo:null,options,furiten:hints.furiten,waits:h?this.waits(h):[],waitDetails:hints.waitDetails,discardHints:hints.discardHints,
   recovery:s.players[i].recovery,log:s.log||[],result:s.result,pending:s.pending?{from:s.pending.from,tile:s.pending.tile,kind:s.pending.kind,riichi:!!s.pending.riichi,waiting:Object.keys(s.pending.options).filter(k=>!s.pending.replies[k]).map(Number)}:null};
 }
 dispatch(req){
  const before=this.s?JSON.stringify(this.s):null;
  try{
   check(req&&typeof req.op==='string','请求格式错误');let s=this.s;
   let i=s?s.players.findIndex(p=>!p.bot&&p.token===req.token):-1;
   if(req.op==='recoverHost'){check(req.host===this.host,'仅本机房主可恢复');check(s,'尚无房间');return {ok:true,token:s.players[0].token,state:this.view(0)}}
   if(req.op==='state'){if(i>=0)this.seen[req.token]=Date.now();return {ok:true,state:this.view(i)}}
   if(req.op==='create'){
    check(req.host===this.host,'请在房主手机 App 内创建房间');check(!s,'已有房间，请先在房间内结束牌桌');check(req.n===3||req.n===4,'仅支持三人或四人');
    const p=player(String(req.name||'房主').slice(0,12));p.avatar=avatar(req.avatar===undefined?'':req.avatar);
    this.s={schema:1,audioRoom:randomHex(8),audioSeq:0,audioEvents:[],n:req.n,rounds:req.rounds===1?1:2,red:req.red!==false,pin:String(parseInt(randomHex(4),16)%900000+100000),players:[p],phase:'lobby',version:1,dealer:0,round:0,honba:0,sticks:0};
    if(req.bots===true)while(this.s.players.length<this.s.n)this.addBot();
    this.save(JSON.stringify(this.s));this.seen[p.token]=Date.now();return {ok:true,token:p.token,state:this.view(0)};
   }
   check(s,'房主尚未创建房间');
   if(req.op==='join'){
    check(String(req.pin)===s.pin,'房间码不正确');
    if(req.recovery){i=s.players.findIndex(p=>!p.bot&&p.recovery===String(req.recovery).trim().toLowerCase());check(i>=0,'恢复码不正确');}
    else {check(s.phase==='lobby'&&s.players.length<s.n,'房间已满或已经开始');const name=String(req.name||'牌友').trim().slice(0,12);check(name,'请填写昵称');check(!s.players.some(p=>p.name===name),'昵称已有人使用');s.players.push(player(name));i=s.players.length-1;s.version++;}
    if(req.avatar!==undefined)s.players[i].avatar=avatar(req.avatar);this.save(JSON.stringify(s));this.seen[s.players[i].token]=Date.now();return {ok:true,token:s.players[i].token,state:this.view(i)};
   }
   check(i>=0,'座位凭据失效，请使用恢复码重新加入');
   this.seen[req.token]=Date.now();
   if(req.id&&s.players[i].requests.includes(req.id))return {ok:true,state:this.view(i)};
   if(req.op!=='profile')check(Number.isInteger(req.version)&&req.version===s.version,'牌桌已更新，请重新选择');
   if(req.op==='profile'){s.players[i].avatar=avatar(req.avatar);}
   else if(req.op==='addBot'||req.op==='fillBots'){check(i===0&&s.phase==='lobby','仅房主可在开局前添加 AI');this.addBot();if(req.op==='fillBots')while(s.players.length<s.n)this.addBot();}
   else if(req.op==='removeBot'){check(i===0&&s.phase==='lobby','仅房主可在开局前移除 AI');check(Number.isInteger(req.seat)&&s.players[req.seat]?.bot,'该座位不是 AI');s.players.splice(req.seat,1);}
   else if(req.op==='start'){check(i===0&&s.phase==='lobby','仅房主可开局');if(req.fillBots===true)while(s.players.length<s.n)this.addBot();check(s.players.length===s.n,'还有空位，可添加 AI 后开局');s.players=shuffle(s.players); // Keep host identity separate from seat ordering.
    // Host remains player 0; randomize dealer instead, so management rights never move.
    const hostIndex=s.players.findIndex(p=>p.token===req.token);[s.players[0],s.players[hostIndex]]=[s.players[hostIndex],s.players[0]];
    s.matchSeq=(s.matchSeq||0)+1;s.matchStartedAt=Date.now();s.matchOpeningUntil=s.matchStartedAt+6000;
    s.dealer=parseInt(randomHex(2),16)%s.n;s.initialDealer=s.dealer;s.players.forEach(p=>p.score=s.n===3?35000:25000);this.newHand();
   }else if(req.op==='action')this.act(i,req.action);
   else if(req.op==='ready'){check(s.phase==='result','当前不在结算阶段');s.players[i].ready=true;}
   else if(req.op==='next'){
    check(i===0&&s.phase==='result'&&s.players.every(p=>p.ready),'请等待所有玩家确认结算');
    if(s.result.matchOver){s.matchSeq=(s.matchSeq||0)+1;s.matchStartedAt=Date.now();s.matchOpeningUntil=s.matchStartedAt+6000;s.round=0;s.dealer=s.initialDealer;s.honba=0;s.sticks=0;s.players.forEach(p=>p.score=s.n===3?35000:25000);}
    else {s.honba=s.result.draw||s.result.repeat?s.honba+1:0;if(!s.result.repeat){s.dealer=(s.dealer+1)%s.n;s.round++;}}
    this.newHand();
   }else if(req.op==='close'){check(i===0,'仅房主可结束牌桌');this.s=null;this.save('');return {ok:true,state:{phase:'empty'}};}
   else throw Error('未知操作');
   i=s.players.findIndex(p=>p.token===req.token);
   if(req.id){s.players[i].requests.push(String(req.id).slice(0,80));s.players[i].requests=s.players[i].requests.slice(-32);}
   s.version++;this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
  }catch(e){this.s=before?JSON.parse(before):null;return {ok:false,error:String(e.message||e)}}
 }
}
g.RiichiRoom=Room;if(typeof module!=='undefined')module.exports={Room,tiles,RULE,norm};
})(globalThis);
