const fs=require('fs'),vm=require('vm'),path=require('path');
const root=path.resolve(__dirname,'..'),out=path.join(root,'build/character-review-r2');
const input=process.argv[2]||path.join(root,'build/reference/public/v0.11.252.w/code.js');
const s=fs.readFileSync(input,'utf8');
let cut=0,ctx={};
while(/^\s*(\$\w+=)+\[/.test(s.slice(cut,cut+80))){let previous=cut;
 for(let pos=s.indexOf('];',cut);pos>=0;pos=s.indexOf('];',pos+2)){
  try{let program=new vm.Script(s.slice(cut,pos+2));program.runInNewContext(ctx,{timeout:1000});cut=pos+2;break;}catch(e){if(e.name!=='SyntaxError')throw e;}
 }if(cut===previous)throw Error('Incomplete string array');
}
if(!cut)throw Error('Array statement missing');
console.log('tables',Object.keys(ctx),'tableEnd',cut);
ctx.$h=ctx.$U;ctx.$T=ctx.$6;ctx.$c=ctx.$G;
const decoded=s.slice(cut).replace(/(\$\w+)\[(0x[\da-f]+|\d+)\]/gi,(m,k,n)=>{
 if(!Array.isArray(ctx[k]))return m;let v=ctx[k][Number(n)];return Object.prototype.toString.call(v)==='[object RegExp]'?String(v):JSON.stringify(v);
});
fs.writeFileSync(path.join(out,'code-readable.js'),decoded);
for(const word of []){
 let i=-1,count=0;while((i=decoded.indexOf(word,i+1))>=0){count++;if(count<5)console.log(word,decoded.slice(Math.max(0,i-160),i+2100));}console.log('count',count);
}
