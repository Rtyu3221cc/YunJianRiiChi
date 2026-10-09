/* Offline heuristic opponent. Input is ONLY one player's public view. */
(function(g){
'use strict';
const M=g.Majiang,norm=p=>p.slice(0,2).replace('0','5');
function hand(v){const h=new M.Shoupai(v.hand);h._fulou=v.players[v.you].melds.slice();h._lizhi=!!v.players[v.you].riichi;h._zimo=v.drawn||(v.phase==='play'&&v.turn===v.you?h._fulou[h._fulou.length-1]:null);return h;}
function known(v){const c={};const add=p=>{p=norm(p);c[p]=(c[p]||0)+1};v.hand.forEach(add);v.indicators.forEach(add);for(const p of v.players){p.river.filter(r=>!r.called).forEach(r=>add(r.tile));p.melds.forEach(m=>(m.match(/\d/g)||[]).forEach(n=>add(m[0]+n)));for(let i=0;i<p.nuki;i++)add('z4');}return c;}
function danger(tile,v,c){const p=norm(tile),n=+p[1];let value=0;for(let i=0;i<v.players.length;i++){const enemy=v.players[i];if(i===v.you||!enemy.riichi)continue;const river=enemy.river.map(r=>norm(r.tile));if(river.includes(p))continue;let risk=p[0]==='z'?3:(n===1||n===9?5:9);risk*=Math.max(0,4-(c[p]||0))/4;if(p[0]!=='z'&&(river.includes(p[0]+(n-3))||river.includes(p[0]+(n+3))))risk*=.65;value+=risk;}return value;}
function rank(h,option,v,c){const next=h.clone().dapai(option.id.slice(option.id.indexOf(':')+1));const sh=M.Util.xiangting(next);let outs=0;for(const p of M.Util.tingpai(next)||[]){if(v.n===3&&p[0]==='m'&&!['m1','m9'].includes(p))continue;outs+=Math.max(0,4-(c[p]||0));}const p=norm(option.tile),n=+p[1];const defensive=v.players.some((p,i)=>i!==v.you&&p.riichi)&&sh>=2;return -sh*1000+outs*12-danger(p,v,c)*(defensive?650:sh===1?35:8)+(option.kind==='riichi'?130:0)-(option.tile[1]==='0'?28:0)+(p[0]==='z'?5:Math.abs(5-n));}
function choose(v){const a=v.options||[];if(!a.length)return null;const win=a.find(x=>x.kind==='tsumo'||x.kind==='ron');if(win)return win.id;
 const h=hand(v),c=known(v),kokushi=M.Util.xiangting_guoshi(h);
 const north=a.find(x=>x.kind==='nuki');if(north&&(kokushi>1||h._bingpai.z[4]>1))return north.id;
 const kan=a.find(x=>x.kind==='kan');if(kan&&!v.players.some((p,i)=>i!==v.you&&p.riichi)&&kokushi>1)return kan.id;
 const discards=a.filter(x=>x.kind==='discard'||x.kind==='riichi');if(discards.length)return discards.map(x=>({x,value:rank(h,x,v,c)})).sort((a,b)=>b.value-a.value||a.x.id.localeCompare(b.x.id))[0].x.id;
 const calls=a.filter(x=>['pon','chi','minkan'].includes(x.kind));let best=null,bestSh=M.Util.xiangting(h);
 for(const x of calls){const next=h.clone().fulou(x.meld);const all=v.hand.concat(next._fulou.flatMap(m=>(m.match(/\d/g)||[]).map(n=>m[0]+n)));
  const valueHonor=next._fulou.some(m=>m[0]==='z'&&([5,6,7,v.players[v.you].wind+1,Math.floor(v.round/v.n)+1].includes(+m[1])));
  const tanyao=all.every(p=>p[0]!=='z'&&![1,9].includes(+(p[1]||5)));
  if(!valueHonor&&!tanyao)continue;if(v.players.some((p,i)=>i!==v.you&&p.riichi))continue;
  let sh=x.kind==='minkan'?M.Util.xiangting(next):Math.min(...(next.get_dapai()||[]).map(p=>M.Util.xiangting(next.clone().dapai(p))));
  if(sh<bestSh){bestSh=sh;best=x.id;}
 }
 return best||a.find(x=>x.kind==='pass')?.id||a[0].id;
}
g.RiichiAI={choose};if(typeof module!=='undefined')module.exports=g.RiichiAI;
})(globalThis);
