const {test}=require('node:test'),assert=require('node:assert/strict');
const {selectTile,riverRows,riichiPlaced}=require('../app/src/main/assets/table-layout.js');
test('selected tile submits on a later second click with no double-click timer',()=>{
 let choice=selectTile(null,3,'discard:p2');assert.equal(choice.submit,false);
 choice=selectTile(choice,3,'discard:p2');assert.equal(choice.submit,true);
 assert.equal(selectTile(choice,4,'discard:p2').submit,false);
 assert.equal(selectTile(choice,3,'riichi:p2').submit,false);
});
test('river keeps six physical tiles per row including horizontal declaration',()=>{
 const tiles=Array.from({length:19},(_,i)=>({tile:'p1',riichi:i===4}));
 const rows=riverRows(tiles);assert.deepEqual(rows.map(r=>r.length),[6,6,6,1]);
 assert.equal(rows[0][4],tiles[4]);assert.equal(rows[1][0],tiles[6]);
});
test('riichi stick appears after declaration survives, never during its ron window',()=>{
 const s={players:[{riichi:1},{riichi:1}],pending:{from:0,riichi:true}};
 assert.equal(riichiPlaced(s,0),false);assert.equal(riichiPlaced(s,1),true);
 s.pending=null;assert.equal(riichiPlaced(s,0),true);
 s.players[0].riichi=0;assert.equal(riichiPlaced(s,0),false);
});
