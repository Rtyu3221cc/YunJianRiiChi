// Add whitespace using parsed statement/block boundaries. Never execute the Lua.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const R=path.resolve(__dirname,'..'),parser=require(path.join(R,'tools/lua-parser-local/luaparse.js'));
const base=process.argv[2]?path.resolve(process.argv[2]):path.join(R,'build/lua-reverse-20261028/decrypted'),out=path.join(base,'readable');fs.mkdirSync(out,{recursive:true});
const options={luaVersion:'LuaJIT',ranges:true,locations:true,comments:true};
const reports=[];
function normalize(value){if(Array.isArray(value))return value.map(normalize);if(value&&typeof value==='object'){const result={};for(const [k,v] of Object.entries(value))if(!['range','loc','comments'].includes(k))result[k]=normalize(v);return result;}return value;}
for(const name of fs.readdirSync(base).filter(n=>n.endsWith('.lua'))){
 const source=fs.readFileSync(path.join(base,name),'utf8'),ast=parser.parse(source,options),breaks=new Map(),methods=[];
 function br(index,depth){if(index<0)return;breaks.set(index,Math.min(depth,breaks.get(index)??depth));}
 function block(body,depth){for(const statement of body){br(statement.range[0],depth);walk(statement,depth);let end=statement.range[1];if(source[end]===';')end++;br(end,depth);}}
 function walk(node,depth){
  if(!node||typeof node!=='object')return;
  if(node.type==='FunctionDeclaration'){
   if(node.identifier&&node.identifier.type==='MemberExpression'){const m=node.identifier;methods.push({name:m.identifier.name,sourceRange:node.range});}
   block(node.body,depth+1);br(node.range[1]-3,depth);
   for(const key of ['identifier','parameters'])walk(node[key],depth);return;
  }
  if(node.type==='IfStatement'){for(const clause of node.clauses){br(clause.range[0],depth);walk(clause.condition,depth);block(clause.body,depth+1);}br(node.range[1]-3,depth);return;}
  if(['WhileStatement','ForNumericStatement','ForGenericStatement','DoStatement'].includes(node.type)){for(const [key,v] of Object.entries(node))if(key!=='body')walk(v,depth);block(node.body,depth+1);br(node.range[1]-3,depth);return;}
  if(node.type==='RepeatStatement'){block(node.body,depth+1);br(source.lastIndexOf('until',node.condition.range[0]),depth);walk(node.condition,depth);return;}
  if(Array.isArray(node)){for(const v of node)walk(v,depth);return;}
  for(const [k,v] of Object.entries(node))if(!['range','loc','comments'].includes(k))walk(v,depth);
 }
 block(ast.body,0);let formatted='',last=0;
 for(const [offset,depth] of [...breaks.entries()].sort((a,b)=>a[0]-b[0])){formatted+=source.slice(last,offset);if(formatted&&!formatted.endsWith('\n'))formatted+='\n';formatted+='    '.repeat(depth);last=offset;}
 formatted+=source.slice(last);formatted=formatted.replace(/[ \t]+\n/g,'\n').trimEnd()+'\n';
 const again=parser.parse(formatted,options);if(JSON.stringify(normalize(ast))!==JSON.stringify(normalize(again)))throw Error('AST changed while formatting '+name);
 fs.writeFileSync(path.join(out,name),formatted);reports.push({file:name,originalBytes:Buffer.byteLength(source),readableLines:formatted.split('\n').length,syntaxValidated:true,astUnchanged:true,sha256:crypto.createHash('sha256').update(formatted).digest('hex'),methods});
}
fs.writeFileSync(path.join(base,'syntax-checks.json'),JSON.stringify(reports,null,2));
for(const r of reports)process.stdout.write(`PASS ${r.file}: LuaJIT syntax, unchanged AST, ${r.readableLines} readable lines\n`);
