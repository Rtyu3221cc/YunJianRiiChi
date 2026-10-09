/* Offline clips are driven only by committed, public room events. */
(function(g){
'use strict';
class VoicePlayer {
 constructor(audio,options={}){this.audio=audio;this.clips=options.clips??g.MajsoulVoice?.clips?.reduce((m,e)=>(m[e.key]=e.webPath,m),{});this.enabled=options.enabled!==false;this.volume=options.volume??.8;this.room=null;this.cursor=0;this.queue=[];this.playing=false;this.run=0;this.blocked=false;this.lastAt=0;this.suspended=false;}
 stop(){this.run++;this.audio.pause();this.audio.onended=null;this.audio.onerror=null;this.queue=[];this.playing=false;}
 reset(){this.stop();this.room=null;this.cursor=0;}
 suspend(flag){this.suspended=flag;this.reset();}
 configure(enabled,volume){this.enabled=enabled;this.volume=Math.max(0,Math.min(1,Number(volume)||0));this.audio.volume=this.volume;if(!enabled)this.stop();}
 accept(s){
  if(!s?.audioRoom){this.reset();return;}
  if(this.room!==s.audioRoom){this.reset();this.room=s.audioRoom;this.cursor=s.audioSeq||0;this.lastAt=Date.now();return;}
  const now=Date.now(),stale=now-this.lastAt>=8000;this.lastAt=now;
  let events=(s.audioEvents||[]).filter(e=>e.id>this.cursor);
  this.cursor=Math.max(this.cursor,s.audioSeq||0);
  if(!this.enabled||this.suspended||g.document?.hidden||stale)return;
  if(events.some(e=>e.kind==='ron'||e.kind==='tsumo')){this.stop();events=events.filter(e=>e.kind==='ron'||e.kind==='tsumo');}
  for(const e of events){const clip=e.kind==='discard'?e.tile:e.kind;if(this.clips?this.clips[clip]:/^(?:[mps][0-9]|z[1-7]|ron|tsumo)$/.test(clip))this.queue.push({clip,at:now});}
  this.queue=this.queue.slice(-8);this.next();
 }
 next(){
  if(this.playing)return;let e;do{e=this.queue.shift();}while(e&&Date.now()-e.at>=8000);if(!e)return;
  const path=this.clips?this.clips[e.clip]:'voice/'+e.clip+'.wav';if(!path){this.next();return;}
  this.playing=true;const run=++this.run;this.audio.src=path;this.audio.volume=e.volume??this.volume;
  const done=()=>{if(run!==this.run)return;this.playing=false;this.next();};this.audio.onended=done;this.audio.onerror=done;
  this.audio.play().then(()=>{this.blocked=false;}).catch(()=>{if(run===this.run){this.blocked=true;this.stop();}});
 }
 preview(volume){this.stop();this.queue=[{clip:'ron',at:Date.now(),volume:Math.max(0,Math.min(1,Number(volume)||0))}];this.next();}
}
if(typeof module!=='undefined'&&module.exports)module.exports={VoicePlayer};
else {const output=new Audio();output.id="voice-output";output.hidden=true;output.preload="auto";document.body.append(output);g.RiichiVoice=new VoicePlayer(output,{enabled:localStorage.getItem('riichi-voice')!=='off',volume:localStorage.getItem('riichi-volume')===null?.8:Number(localStorage.getItem('riichi-volume'))});document.addEventListener('visibilitychange',()=>{g.RiichiVoice.suspend(document.hidden);});}
})(typeof globalThis!=='undefined'?globalThis:this);
