const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const layoutFile=path.join(root,'docs/wardrobe-resource-layout-r22.json');
const moves=fs.existsSync(layoutFile)?JSON.parse(fs.readFileSync(layoutFile,'utf8')).moves:[];
const remap=new Map(moves.map(m=>[path.resolve(root,'unity',m.from),path.resolve(root,'unity',m.to)]));
const directory=JSON.parse(fs.readFileSync(path.join(root,'unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-windows.json'),'utf8'));
for(const row of directory.addresses){
 const source=path.resolve(root,'unity',row.source),address=path.resolve(root,'unity/Assets/Resources',row.path);
 remap.set(row.path.endsWith(path.extname(source))?address:address+path.extname(source),source);
}
function sourceFile(file){const absolute=path.resolve(root,file);return fs.existsSync(absolute)?absolute:remap.get(absolute)||absolute;}
module.exports={sourceFile};
