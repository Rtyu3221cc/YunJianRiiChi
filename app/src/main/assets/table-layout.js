/* Shared, deterministic layout and selection rules; no timing threshold. */
(function(g){
'use strict';
function selectTile(previous,index,action){return {index,action,submit:previous?.index===index&&previous?.action===action};}
function riverRows(river){const rows=[];for(let i=0;i<river.length;i+=6)rows.push(river.slice(i,i+6));return rows;}
function riichiPlaced(s,i){const p=s.players[i];return !!p.riichi&&!(s.pending?.riichi&&s.pending.from===i);}
const api={selectTile,riverRows,riichiPlaced};
if(typeof module!=='undefined'&&module.exports)module.exports=api;else g.TableLayout=api;
})(typeof globalThis!=='undefined'?globalThis:this);
