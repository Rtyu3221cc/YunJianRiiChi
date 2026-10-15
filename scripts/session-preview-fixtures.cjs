// Disposable public views for SessionFlowPreview. No user save or room service.
const fs=require('node:fs'),path=require('node:path');
globalThis.Majiang=require('../app/src/main/assets/core.js');require('../app/src/main/assets/ai.js');
const {Room}=require('../app/src/main/assets/engine.js');const output=path.join(__dirname,'../build/session-flow-review');fs.mkdirSync(output,{recursive:true});
for(const n of [3,4]){
 const room=new Room('render-fixture-only');let reply=room.dispatch({op:'create',host:'render-fixture-only',name:'紫砂型焰梓二号',n,rounds:2,red:true,bots:true,lobbyFlow:true,category:'friend'});
 reply=room.dispatch({op:'start',token:reply.token,version:reply.state.version,fillBots:true});if(!reply.ok)throw Error(reply.error);
 if(n===3)fs.writeFileSync(path.join(output,'play.json'),JSON.stringify(room.view(0)));
 const scores=n===4?[27600,45200,40400,-13200]:[43000,31000,31000],names=n===4?['紫砂型焰梓二号','McDonaldS','菠萝赛马','Ailory']:['紫砂型焰梓二号','电脑一','电脑二'];
 room.s.players.forEach((p,i)=>{p.name=names[i];p.score=scores[i]});room.s.round=n*2-1;
 room.finish({title:'荒牌流局',delta:Array(n).fill(0),details:[],drawDetails:[],repeat:false,draw:true});room.s.version++;
 fs.writeFileSync(path.join(output,n===4?'final-four.json':'final.json'),JSON.stringify(room.view(0)));
}
console.log('Created disposable public views for three/four-player UI preview.');
