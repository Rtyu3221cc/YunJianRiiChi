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
const CHARACTER_SKINS={"yiji":[400101,400102,400103,400104,400105,400106,400107],"erjietang":[400201,400202,400203,400206,400207,400208],"jianai":[400301,400302,400303,400304,400305,400306],"qianzhi":[400401,400402,400403,400404,400405,400406,400407],"xiangyuanwu":[400501,400502,400503,400504,400505,400506,400507,400508],"fuzi":[400601,400602,400603,400604,400605,400606],"bamuwei":[400701,400702,400703,400704,400705,400706,400707,400708,400709],"jiutiao":[400801,400802,400803,400804,400805,400806,400807],"zeniya":[400901,400902,400903,400904,400905,400906,400907,400908],"kawei":[401001,401002,401003,401004],"sigongxiasheng":[401101,401102,401103,401104,401105],"wangcilang":[401201,401202,401203,401204,401205],"yizhilaikong":[401301,401302,401303,401304,401305,401306],"mingzhiyingshu":[401401,401402,401403,401404,401405,401406],"qingkuniang":[401501,401502,401503],"shala":[401601,401602,401603,401604,401605,401606,401607],"erzhigonghua":[401701,401702,401706,401707,401708,401709],"baishinainai":[401801,401802,401803,401804,401805],"xiaoniaoyouchutian":[401901,401902,401903,401904,401905,401906,401907],"wushilanyangcai":[402001,402002,402003,402004,402005,402006],"lianggongxingshu":[402101,402102,402103,402104,402105,402106],"yuesefu":[402201,402202,402203],"zhaitengzhi":[402301,402302,402303,402304],"beijianshahezi":[402401,402402,402403,402404,402405,402406,402407],"aiyin":[402501,402502,402503,402504,402505],"chutao":[402601,402602,402603,402604,402605,402606],"yuejianshan":[402701,402702,402703,402704,402705],"tengbenqiluo":[402801,402802,402803,402804,402805,402806],"huiyeji":[402901,402902,402903,402904,402905,402906,402907],"ruyuelian":[403001,403002,403003,403004],"shiyuanduihai":[403101,403102,403103,403104],"ailisha":[403201,403202,403203,403204,403205,403206,403207],"siqiqiansuili":[403301,403302,403303,403304,403305],"gongyongxiao":[403401,403402,403403,403404],"yuancunhe":[403501,403502,403503,403504],"tianjiangyi":[403601,403602,403603,403604],"gongyongzhao":[403701,403702,403703,403704],"fuji":[403801,403802,403803,403804,403805,403806,403807],"qixi":[403901,403902,403903,403904,403905],"shecan":[404001,404002,404003],"zaoyinv":[404101,404102,404103],"shengzhimo":[404201,404202,404203],"taocan":[404301,404302,404303],"qihailinai":[404401,404402,404403,404404,404405],"a37":[404501,404502,404503,404504,404505],"jichuanxiang":[404601,404602,404603,404604],"laien":[404701,404702,404703,404704],"senchuanlingzi":[404801,404802,404803,404804,404805,404806],"longchuanxiayan":[404901,404902,404903,404904,404905],"akagi":[405001,405002,405003],"washizu":[405101,405102,405103],"xiyuansiyiyu":[405201,405202],"xiaoyesiqiyu":[405301,405302,405303,405304],"xiamier":[405401,405402,405403,405404,405405],"sigonghuiye":[405501,405502,405503],"baiyinyuxing":[405601,405602,405603],"zaobanai":[405701,405702,405703],"baiyingui":[405801,405802,405803],"you":[405901,405902,405903,405904,405905,405906],"zekesi":[406001,406002,406003,406004],"beiyuanlili":[406101,406102],"zhujingjiu":[406201,406202,406203],"fulumeisuizi":[406301,406302,406303],"xinzichong":[406401,406402,406403],"yuanchengsilian":[406501,406502,406503],"sigongdongshi":[406601,406602,406603,406604,406605],"qingluan":[406701,406702,406703,406704],"ruyuecaiyin":[406801,406802,406803,406804],"weilai":[406901,406902,406903,406904],"luluxiu":[407001,407002,407003],"cc":[407101,407102,407103],"zhuque":[407201,407202,407203],"kalian":[407301,407302,407303],"lanxing":[407401,407402,407403,407404],"ling":[407501,407502,407503,407504],"dongchengxuanyin":[407601,407602],"hanna":[407701,407702,407703],"musa":[407801,407802],"yiliya":[407901,407902,407903],"meiyou":[408001,408002,408003],"xiaohei":[408101,408102,408103],"jier":[408201,408202,408203],"yifu":[408301,408302,408303,408304],"jiushi":[409001,409002,409003],"linlang":[408401,408402,408403,408404],"yuanfeng":[408501,408502,408503],"shalangbaizi":[408601,408602,408603],"xiaoniaoyouxingye":[408701,408702,408703],"lubamoailu":[408801,408802,408803],"qianhuangmuyue":[408901,408902,408903],"laiya":[409301,409302,409303],"xinxiya":[409401,409402,409403],"nanfenghua":[409501,409502],"xili":[409101,409102,409103],"ju":[409201,409202,409203],"qiancangtou":[40010001,40010002,40010003],"tongkouyuanxiang":[40010101,40010102,40010103],"fuwanxiaosi":[40010201,40010202,40010203],"shichuanchucai":[40010301,40010302,40010303],"feiming":[409601,409602],"yuanxiao":[40010701,40010702,40010703],"huayubai":[409901,409902,409903],"huayuqing":[409801,409802,409803],"jiantongying":[40010801,40010802,40010803],"yuanbanlin":[40010901,40010902,40010903],"saber":[40011001,40011002,40011003],"archer":[40011101,40011102,40011103],"jian":[40010601,40010602],"sataen":[409701,409702,409703],"liyang":[40011201,40011202,40011203],"bantianyinshi":[40011301,40011302,40011303],"guixiaotailang":[40011401,40011402,40011403],"gaoshanjinzhu":[40011501,40011502,40011503],"banbenchenma":[40011601,40011602,40011603],"mila":[40011701,40011702],"beiluo":[40011801,40011802],"miya":[40011901,40011902],"jinwu":[40012001,40012002],"tongren":[40012201,40012202,40012203],"yasina":[40012301,40012302,40012303],"lifa":[40012401,40012402,40012403],"shinai":[40012501,40012502,40012503]};
const CHARACTER_EMOJIS={"yiji":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,966,983,984,996,997],"erjietang":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,995,998],"jianai":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,959,979,999],"qianzhi":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,957,986,994,999],"xiangyuanwu":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,968,989,997],"fuzi":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,961,977,997],"bamuwei":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,961,970,976,993,999],"jiutiao":[0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,17,18,888,972,991,997],"zeniya":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,966,994,998],"kawei":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,965,982],"sigongxiasheng":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,970,977,998],"wangcilang":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,959,998],"yizhilaikong":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,966,988,999],"mingzhiyingshu":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,969,982,993],"qingkuniang":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,994],"shala":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,961,970,988,996],"erzhigonghua":[0,1,2,3,4,5,6,7,8,10,11,12,14,15,17,18,888,962,982,995],"baishinainai":[0,1,2,3,4,5,6,7,8,9,10,11,12,14,15,16,17,18,888,962,994],"xiaoniaoyouchutian":[0,1,2,3,4,5,6,7,8,9,10,11,12,14,15,16,17,18,888,972,981,986],"wushilanyangcai":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,967,988,994],"lianggongxingshu":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,981,985],"yuesefu":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,991],"zhaitengzhi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,981],"beijianshahezi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,960,976,992],"aiyin":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,957,982],"chutao":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,973,989],"yuejianshan":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,968,988],"tengbenqiluo":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,971,991],"huiyeji":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,964,976,992],"ruyuelian":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,979],"shiyuanduihai":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,959,978],"ailisha":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,957,976,985],"siqiqiansuili":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,969],"gongyongxiao":[0,1,2,3,4,5,6,7,8,10,11,12,14,969,990],"yuancunhe":[0,1,2,3,4,5,6,7,8,10,11,12,14,969,990],"tianjiangyi":[0,1,2,3,4,5,6,7,8,10,11,12,14,969,990],"gongyongzhao":[0,1,2,3,4,5,6,7,8,10,11,12,14,969,990],"fuji":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,959,967,974],"qixi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,961,970],"shecan":[0,1,2,3,4,5,6,7,8,10,11,12,14,987],"zaoyinv":[0,1,2,3,4,5,6,7,8,10,11,12,14,987],"shengzhimo":[0,1,2,3,4,5,6,7,8,10,11,12,14,987],"taocan":[0,1,2,3,4,5,6,7,8,10,11,12,14,987],"qihailinai":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,960,978],"a37":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,973,981],"jichuanxiang":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,967],"laien":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,957,967],"senchuanlingzi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,965,971],"longchuanxiayan":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,964],"akagi":[0,1,2,3,4,5,6,7,8,10,11,12,14,980],"washizu":[0,1,2,3,4,5,6,7,8,10,11,12,14,980],"xiyuansiyiyu":[0,1,2,3,4,5,6,7,8,10,11,12,14,888],"xiaoyesiqiyu":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,958],"xiamier":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,974],"sigonghuiye":[0,1,2,3,4,5,6,7,8,10,11,12,14,975],"baiyinyuxing":[0,1,2,3,4,5,6,7,8,10,11,12,14,975],"zaobanai":[0,1,2,3,4,5,6,7,8,10,11,12,14,975],"baiyingui":[0,1,2,3,4,5,6,7,8,10,11,12,14,975],"you":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,960],"zekesi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,958],"beiyuanlili":[0,1,2,3,4,5,6,7,8,10,11,12,14,888],"zhujingjiu":[0,1,2,3,4,5,6,7,8,10,11,12,14,969],"fulumeisuizi":[0,1,2,3,4,5,6,7,8,10,11,12,14,969],"xinzichong":[0,1,2,3,4,5,6,7,8,10,11,12,14,969],"yuanchengsilian":[0,1,2,3,4,5,6,7,8,10,11,12,14,969],"sigongdongshi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,962],"qingluan":[0,1,2,3,4,5,6,7,8,10,11,12,14,888],"ruyuecaiyin":[0,1,2,3,4,5,6,7,8,10,11,12,14],"weilai":[0,1,2,3,4,5,6,7,8,10,11,12,14],"luluxiu":[0,1,2,3,4,5,6,7,8,10,11,12,14,963],"cc":[0,1,2,3,4,5,6,7,8,10,11,12,14,963],"zhuque":[0,1,2,3,4,5,6,7,8,10,11,12,14,963],"kalian":[0,1,2,3,4,5,6,7,8,10,11,12,14,963],"lanxing":[0,1,2,3,4,5,6,7,8,10,11,12,14],"ling":[0,1,2,3,4,5,6,7,8,10,11,12,14],"dongchengxuanyin":[0,1,2,3,4,5,6,7,8,10,11,12,14],"hanna":[0,1,2,3,4,5,6,7,8,10,11,12,14],"musa":[0,1,2,3,4,5,6,7,8,10,11,12,14],"yiliya":[0,1,2,3,4,5,6,7,8,10,11,12,956],"meiyou":[0,1,2,3,4,5,6,7,8,10,11,12,956],"xiaohei":[0,1,2,3,4,5,6,7,8,10,11,12,956],"jier":[0,1,2,3,4,5,6,7,8,10,11,12,956],"yifu":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,930,938],"jiushi":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,936],"linlang":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,932,951],"yuanfeng":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,935],"shalangbaizi":[0,1,2,3,4,5,6,7,8,10,12,14,952],"xiaoniaoyouxingye":[0,1,2,3,4,5,6,7,8,10,11,12,14,952],"lubamoailu":[0,1,2,3,4,5,6,7,8,10,11,12,14,952],"qianhuangmuyue":[0,1,2,3,4,5,6,7,8,10,11,12,14,952],"laiya":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,942],"xinxiya":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,941],"nanfenghua":[0,1,2,3,4,5,6,7,8,10,11,12,14,888],"xili":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,933],"ju":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,930],"qiancangtou":[0,1,2,3,4,5,6,7,8,10,11,12,14,945],"tongkouyuanxiang":[0,1,2,3,4,5,6,7,8,10,11,12,14,945],"fuwanxiaosi":[0,1,2,3,4,5,6,7,8,10,11,12,14,945],"shichuanchucai":[0,1,2,3,4,5,6,7,8,10,11,12,14,945],"feiming":[0,1,2,3,4,5,6,7,8,10,11,12,14,888],"yuanxiao":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,935],"huayubai":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,931],"huayuqing":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,931],"jiantongying":[0,1,2,3,4,5,6,7,8,10,11,12,14,940],"yuanbanlin":[0,1,2,3,4,5,6,7,8,10,11,12,14,940],"saber":[0,1,2,3,4,5,6,7,8,10,11,12,14,940],"archer":[0,1,2,3,4,5,6,7,8,10,11,12,14,940],"jian":[0,1,2,3,4,5,6,7,8,10,11,12,14,888],"sataen":[0,1,2,3,5,6,7,8,10,11,12,14,888,931],"liyang":[0,1,2,3,4,5,6,7,8,10,11,12,14,888,936],"bantianyinshi":[0,1,2,3,4,5,6,7,8,10,11,12,14,934],"guixiaotailang":[0,1,2,3,4,5,6,7,8,10,11,12,14,934],"gaoshanjinzhu":[0,1,2,3,4,5,6,7,8,10,11,12,14,934],"banbenchenma":[0,1,2,3,4,5,6,7,8,10,11,12,14,934],"mila":[0,1,2,3,4,5,6,7,8,10,11,12,888],"beiluo":[0,1,2,3,4,5,6,7,8,10,11,12,888],"miya":[0,1,2,3,4,5,6,7,8,10,11,12,888],"jinwu":[0,1,2,3,4,5,6,7,8,10,11,12,888],"tongren":[0,1,2,3,4,5,6,7,8,10,11,12,928],"yasina":[0,1,2,3,4,5,6,7,8,10,11,12,928],"lifa":[0,1,2,3,4,5,6,7,8,10,11,12,928],"shinai":[0,1,2,3,4,5,6,7,8,10,11,12,928]};
function characterChoice(c){check(c&&typeof c.character==='string'&&Object.prototype.hasOwnProperty.call(CHARACTER_SKINS,c.character)&&Number.isInteger(c.skin)&&CHARACTER_SKINS[c.character].includes(c.skin),'雀士或服饰无效');return {character:c.character,skin:c.skin}}
const WARDROBE_CATALOG={"stick":["stick-default","liqi_fish","liqi_scallion","liqi_bone"],"cloth":["cloth-chigua","cloth-default"],"tileBack":["mjp_default","mjp_2308event"],"tileFace":["mjpface_default","mjpface_2308event"],"headFrame":["headframe_default","headframe_catear3"],"lobbyBackground":["background-yard","background-autumn"],"callout":["mpzs_shandian","mpzs_22event","effect_zhishi","mpzs_23winter","callout-sakura","callout-bat","callout-frost","callout-vortex"],"riichiEffect":["riichi-fire","riichi-ice","riichi-blue","riichi-bat","riichi-afterimage","riichi-feather","effect-liqi-longteng","effect-liqi-ding","effect-liqi-23ex-maozhua","effect-liqi-23ex-liandao","effect-liqi-23ex-xueqiu"],"winEffect":["ron-tornado","win-default","ron-flame","ron-wind","ron-sakura","ron-blackfire","ron-reactor","ron-meiguihua","ron-ko","ron-nilin","ron-youlingaoao","ron-23ex-heidong","ron-23ex-liandao","ron-23ex-shuizhu"],"hand":["hand-human","hand-cat","hand-cat-orange","hand-dog","hand-man"],"music":["music-riichi","music-8bit"]};
function wardrobeSelection(value){const out={};if(value==null)return out;check(typeof value==='object'&&!Array.isArray(value),'装扮配置无效');for(const [key,ids] of Object.entries(WARDROBE_CATALOG)){const id=value[key];if(id==null||id==='')continue;check(typeof id==='string'&&ids.includes(id),'装扮资源无效');out[key]=id;}return out;}
function characterSelection(req){
 const active=req.character?characterChoice(req):{character:'yiji',skin:400101},pool=req.characterPool||[];
 check(Array.isArray(pool)&&pool.length<=1024,'随机雀士列表无效');const choices=pool.map(characterChoice),unique=[...new Map(choices.map(c=>[c.character+':'+c.skin,c])).values()];
 check(req.randomCharacters!==true||unique.length>0,'请先选择随机雀士');return {...active,characterPool:unique,randomCharacters:req.randomCharacters===true,wardrobe:wardrobeSelection(req.wardrobe)};
}
function freezeCharacter(p){
 let c={character:p.character||'yiji',skin:p.skin||400101};
 if(p.randomCharacters&&p.characterPool?.length){const key=shuffle([...new Set(p.characterPool.map(c=>c.character))])[0];c=shuffle(p.characterPool.filter(c=>c.character===key).slice())[0];}
 p.matchCharacter=c.character;p.matchSkin=c.skin;p.matchWardrobe={...(p.wardrobe||{})};
}
function publicCharacter(p,phase,prepared=false){return {wardrobe:{...(phase==='lobby'&&!prepared?p.wardrobe:p.matchWardrobe||p.wardrobe||{})},character:phase==='lobby'&&!prepared?(p.character||'yiji'):(p.matchCharacter||p.character||'yiji'),skin:phase==='lobby'&&!prepared?(p.skin||400101):(p.matchSkin||p.skin||400101)}}
function player(name,token){return {name,avatar:"",token:token||randomHex(18),recovery:randomHex(6),score:0,hand:'',river:[],riichi:0,ippatsu:false,nuki:0,temp:false,permanent:false,kans:0,ready:false,requests:[]}}
class Room{
 constructor(host,save,snapshot){this.host=host;this.save=save||(()=>{});this.s=snapshot?JSON.parse(snapshot):null;this.seen={};this._bootAt=Date.now();this._waiting={};this._aiNow=()=>Date.now();this._aiPlan=null;if(this.s?.resourceSetup){this.s.resourceSetup.revision=++this.s.resourceRevision;this.s.resourceSetup.confirmations={};}}
 controlled(p){return !!(p.bot||p.auto)}
 resourcesReady(){const s=this.s;if(!s?.resourceSetup)return false;return s.players.every(p=>p.bot||p.resourceProtocol!==1||['ready','fallback'].includes(s.resourceSetup.confirmations[p.token]?.state));}
 resourceView(){const s=this.s;if(!s?.resourceSetup)return null;const x=s.resourceSetup;return {revision:x.revision,createdAt:x.createdAt,ready:this.resourcesReady(),choices:x.choices,players:s.players.map((p,seat)=>({seat,state:p.bot||p.resourceProtocol!==1?'ready':x.confirmations[p.token]?.state||'pending'}))};}

 presence(now){
  const s=this.s;let changed=false;if(s.phase==='lobby'&&s.resourceSetup&&now-s.resourceSetup.createdAt>180000){delete s.resourceSetup;changed=true;}
  for(let i=0;i<s.players.length;i++){
   const p=s.players[i];if(p.bot)continue;
   const lastSeen=this.seen[p.token]||this._bootAt,unavailable=p.away||now-lastSeen>=12000;
   if(unavailable){
    // Presence detection is immediate; AI control has its own full grace period.
    // Polls and repeated background notifications must not restart that clock.
    if(p.away&&!Number.isFinite(p.awaySince)){p.awaySince=now;changed=true;}
    const absentSince=p.away?p.awaySince:lastSeen;
    if(now-absentSince>=120000&&(!p.auto||p.autoReason!=='offline')){p.auto=true;p.autoReason='offline';changed=true;}
    continue;
   }
   const activeSince=Math.max(p.lastInputAt||s.matchStartedAt||this._bootAt,s.matchOpeningUntil||0);
   if(!p.auto&&s.idleSeconds!==0&&now-activeSince>=120000){p.auto=true;p.autoReason='timeout';changed=true;}
  }
  return changed;
 }
 advanceResult(){
  const s=this.s;
  if(s.result.matchOver){s.matchSeq=(s.matchSeq||0)+1;s.matchStartedAt=Date.now();s.matchOpeningUntil=s.matchStartedAt+6000;s.round=0;s.dealer=s.initialDealer;s.honba=0;s.sticks=0;s.players.forEach(p=>p.score=s.n===3?35000:25000);}
  else {s.honba=s.result.draw||s.result.repeat?s.honba+1:0;if(!s.result.repeat){s.dealer=(s.dealer+1)%s.n;s.round++;}}
  this.newHand();
 }
 advanceIfReady(){
  const s=this.s;if(s?.phase!=='result')return false;
  if(!Number.isFinite(s.resultFinishedAt))s.resultFinishedAt=Date.now();
  // Let an unattended AI match finish, then retain its terminal result.
  if(s.result.matchOver&&(s.result.autoTerminal||s.players.every(p=>this.controlled(p)))){s.result.autoTerminal=true;return false;}
  const now=Date.now(),online=p=>!p.bot&&!p.away&&now-(this.seen[p.token]||0)<12000;
  // Online viewers, including auto-play seats, acknowledge only after their
  // local win/draw and score presentation. Offline seats cannot block them.
  if(!s.players.some(online)){
   if(!s.players.every(p=>this.controlled(p))||now-(s.resultFinishedAt||now)<12000)return false;
  }else if(!s.players.every(p=>p.bot||p.ready||this.controlled(p)&&!online(p)))return false;
  this.advanceResult();return true;
 }
 sound(kind,seat,tile,riichi,nuki,win,meld){const s=this.s;s.audioSeq=(s.audioSeq||0)+1;s.audioEvents=(s.audioEvents||[]).concat({id:s.audioSeq,kind,seat,tile:tile||null,riichi:!!riichi,...(kind==='discard'?{riverIndex:s.players[seat].river.length}:{}),...(nuki?{nuki}:{}),...(win?{from:win.from,winSource:win.kind}:{}),...(meld?{meld:meld.value,meldKind:meld.kind,meldIndex:meld.index,from:meld.from}:{}),at:Date.now()}).slice(-24);}
 configureBots(req){
  const enabled=req.randomBotAppearance===true,pool=req.botCharacters||[];check(Array.isArray(pool)&&pool.length<=Object.keys(CHARACTER_SKINS).length,'电脑形象列表无效');
  const choices=pool.map(characterChoice);check(!enabled||choices.length>0,'没有可用的电脑形象');
  this.s.randomBotAppearance=enabled;this.s.botCharacters=[...new Map(choices.map(c=>[c.character,c])).values()];
 }
 addBot(){const s=this.s;check(s.players.length<s.n,'座位已满');let n=1;while(s.players.some(p=>p.name==='AI · '+n))n++;const p=player('AI · '+n);p.bot=true;
  let choice={character:'yiji',skin:400101};if(s.randomBotAppearance&&s.botCharacters?.length){const used=new Set(s.players.filter(p=>p.bot).map(p=>p.character)),fresh=s.botCharacters.filter(c=>!used.has(c.character));choice=shuffle((fresh.length?fresh:s.botCharacters).slice())[0];}Object.assign(p,choice);s.players.push(p);}
 emote(i,req){
  const s=this.s,p=s.players[i],now=Date.now();check(['play','reaction'].includes(s.phase),'当前不能发送表情');
  const identity=publicCharacter(p,s.phase),allowed=CHARACTER_EMOJIS[identity.character]||[];
  check(Number.isInteger(req.emoji)&&allowed.includes(req.emoji),'该角色没有这个表情');check(!Number.isFinite(p.lastEmoteAt)||now-p.lastEmoteAt>=5000,'表情冷却中');
  p.lastEmoteAt=now;s.emoteSeq=(s.emoteSeq||0)+1;s.emotes=(s.emotes||[]).filter(e=>now-e.at<5000).concat({id:s.emoteSeq,seat:i,emoji:req.emoji,...identity,at:now}).slice(-16);
  if(!p.bot){
   // Reference Desktop.showAIEmo: each AI responds with probability 0.3.
   // Keep the requested 1200-1700ms delay and schedule once on the room host.
   const pending=s.botEmoteReplies||(s.botEmoteReplies=[]);
   s.players.forEach((bot,seat)=>{
    if(!bot.bot||pending.some(e=>e.seat===seat)||Math.random()>=.3)return;
    const dueAt=now+1200+Math.floor(Math.random()*501),appearance=publicCharacter(bot,s.phase);
    if(Number.isFinite(bot.lastEmoteAt)&&dueAt-bot.lastEmoteAt<5000)return;
    const allowed=(CHARACTER_EMOJIS[appearance.character]||[]).filter(id=>id>=0&&id<9);if(!allowed.length)return;
    pending.push({seat,token:bot.token,...appearance,emoji:allowed[Math.floor(Math.random()*allowed.length)],dueAt,handSeq:s.handSeq,matchSeq:s.matchSeq});
   });
  }
 }
 tickEmotes(now){
  const s=this.s,pending=s?.botEmoteReplies;if(!pending?.length)return false;
  if(!['play','reaction'].includes(s.phase)){s.botEmoteReplies=[];return true;}
  let changed=false;s.botEmoteReplies=pending.filter(reply=>{
   const bot=s.players[reply.seat],appearance=bot&&publicCharacter(bot,s.phase);
   if(!bot?.bot||bot.token!==reply.token||reply.handSeq!==s.handSeq||reply.matchSeq!==s.matchSeq||appearance.character!==reply.character||appearance.skin!==reply.skin||now-reply.dueAt>2000){changed=true;return false;}
   if(now<reply.dueAt)return true;
   changed=true;if((!Number.isFinite(bot.lastEmoteAt)||now-bot.lastEmoteAt>=5000)&&(CHARACTER_EMOJIS[appearance.character]||[]).includes(reply.emoji))this.emote(reply.seat,{emoji:reply.emoji});
   return false;
  });return changed;
 }
 tick(){
  if(this.s?.botEmoteReplies?.length){const before=JSON.stringify(this.s);try{if(this.tickEmotes(Date.now()))this.save(JSON.stringify(this.s));}catch(e){this.s=JSON.parse(before);return {ok:false,error:String(e.message||e)};}}
  const s=this.s;if(s?.phase==='lobby'){if(s.resourceSetup&&Date.now()-s.resourceSetup.createdAt>180000){delete s.resourceSetup;s.version++;this.save(JSON.stringify(s));}return {ok:true,idle:true};}if(!s||!g.RiichiAI||!['play','reaction','result'].includes(s.phase))return {ok:true,idle:true};
  const changed=this.presence(Date.now());
  if(s.phase==='result'){
   if(this.advanceIfReady()||changed){s.version++;this.save(JSON.stringify(s));}
   return {ok:true,idle:true};
  }
  if(changed){s.version++;this.save(JSON.stringify(s));}
  if(Date.now()<Math.max(s.matchOpeningUntil||0,s.roundOpeningUntil||0))return {ok:true,idle:true};
  const i=s.phase==='play'?s.turn:s.players.findIndex((p,j)=>this.controlled(p)&&this.options(j).length);
  if(i<0||!this.controlled(s.players[i]))return {ok:true,idle:true};
  const key=[s.audioRoom,s.version,s.phase,i,s.audioSeq].join(':'),now=this._aiNow();
  if(this._aiPlan?.key!==key)this._aiPlan={key,until:now+700+Math.floor(Math.random()*301)};
  if(now<this._aiPlan.until)return {ok:true,idle:true,waitMs:this._aiPlan.until-now};
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
   return {tile,ron:!!(ron&&ron.hupai),tsumo:!!(tsumo&&tsumo.hupai),yakumanRon:ron&&ron.damanguan||0,yakumanTsumo:tsumo&&tsumo.damanguan||0,remaining:this.unseen(i,tile)};
  });
  return {furiten,waits:details};
 }
 hints(i,h,options){
  if(!h)return {waitDetails:[],discardHints:[],furiten:false,yakumanIndicator:0};
  const base=h._zimo&&h._zimo.length===2?h.clone().dapai(h._zimo+'_'):h;
  const info=this.waitInfo(i,base),discardHints=[];
  if(!this.s.players[i].bot)for(const a of options.filter(a=>a.kind==='discard'||a.kind==='riichi')){
   const after=h.clone().dapai(a.id.slice(a.id.indexOf(':')+1));
   const candidate=this.waitInfo(i,after,a.tile,a.kind==='riichi');
   if(candidate.waits.length)discardHints.push({id:a.id,...candidate});
  }
  // Only this viewer's hand is evaluated. A possible discard is a chance;
  // a committed non-furiten wait is certain only when every ron/tsumo is yakuman.
  const level=hint=>hint.waits.some(w=>w.yakumanRon||w.yakumanTsumo)?(!hint.furiten&&hint.waits.every(w=>w.yakumanRon&&w.yakumanTsumo)?2:1):0;
  const yakumanIndicator=discardHints.some(candidate=>level(candidate)>0)?1:level(info);
  return {waitDetails:info.waits,discardHints,furiten:info.furiten,yakumanIndicator};
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
  s.handSeq=(s.handSeq||0)+1;s.dealtAt=Date.now();s.roundOpeningUntil=s.dealtAt+1800;
  s.interrupted=false;s.phase='play';s.pending=null;s.result=null;s.log=[];s.last=null;s.turn=s.dealer;s.drawKind='normal';
  // Keep the event cursor monotonic, but never replay the previous hand's calls.
  s.audioEvents=[];this._aiPlan=null;
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
  const s=this.s;result.delta.forEach((v,i)=>s.players[i].score+=v);s.phase='result';s.result=result;s.resultFinishedAt=Date.now();s.pending=null;s.players.forEach(p=>p.ready=!!p.bot);this.log(result.title);
  const last=s.round>=s.rounds*s.n-1,target=s.n===3?40000:30000,top=Math.max(...s.players.map(p=>p.score));
  result.matchOver=s.players.some(p=>p.score<0)||(!result.abort&&last&&((!result.repeat&&top>=target)||(result.repeat&&s.players[s.dealer].score>=target&&s.players[s.dealer].score===top)));
  // At most one extra wind, with North as the final wind for a full match.
  if(s.round>=Math.min(s.rounds+1,4)*s.n-1&&!result.repeat)result.matchOver=true;
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
  return {resourceProtocol:s.resourceProtocol||0,resourceSetup:this.resourceView(),resourcePeers:s.players.filter(p=>!p.bot&&p.resourceShare).map(p=>({...p.resourceShare,name:p.name,seat:s.players.indexOf(p)})),phase:s.phase,version:s.version,you:i,host:i===0,n:s.n,pin:s.pin,round:s.round,rounds:s.rounds,idleSeconds:s.idleSeconds===0?0:120,dealer:s.dealer,honba:s.honba,sticks:s.sticks,turn:s.turn,lobby:{category:s.lobbyCategory||'friend',red:s.red!==false,managed:!!s.lobbyFlow},
   audioRoom:s.audioRoom||s.pin,audioSeq:s.audioSeq||0,audioEvents:s.audioEvents||[],emoteSeq:s.emoteSeq||0,emotes:(s.emotes||[]).filter(e=>Date.now()-e.at<5000).map(e=>({...e,ageMs:Math.max(0,Date.now()-e.at)})),emoteCooldownMs:Math.max(0,5000-(Date.now()-(s.players[i].lastEmoteAt||0))),remaining:s.wall?s.wall.length:0,indicators:s.indicators?s.indicators.slice(0,1+this.totalKans()):[],last:s.last,
   players:s.players.map((p,j)=>({...publicCharacter(p,s.phase,!!s.resourceSetup),name:p.name,avatar:p.avatar||"",bot:!!p.bot,auto:!!p.auto,autoReason:p.autoReason||'',takeoverMask:!!p.departed||!!p.auto&&['offline','timeout'].includes(p.autoReason),score:p.score,wind:this.wind(j),riichi:p.riichi,nuki:p.nuki,river:p.river,melds:p.hand?this.h(j)._fulou:[],count:p.hand?tiles(this.h(j)).length:0,ready:p.ready,online:p.bot||!p.away&&Date.now()-(this.seen[p.token]||0)<12000})),
   handSeq:s.handSeq||0,dealtAt:s.dealtAt||0,matchSeq:s.matchSeq||0,matchStartedAt:s.matchStartedAt||0,openingHand:(s.players[i].dealt||[]).slice(),
   hand:h?tiles(h):[],drawn:h&&h._zimo&&h._zimo.length===2?h._zimo:null,options,furiten:hints.furiten,waits:h?this.waits(h):[],waitDetails:hints.waitDetails,discardHints:hints.discardHints,yakumanIndicator:hints.yakumanIndicator||0,
   recovery:s.players[i].recovery,log:s.log||[],result:s.result,pending:s.pending?{from:s.pending.from,tile:s.pending.tile,kind:s.pending.kind,riichi:!!s.pending.riichi,waiting:Object.keys(s.pending.options).filter(k=>!s.pending.replies[k]).map(Number)}:null,
   ...(s.phase==='lobby'?{lobby:{category:s.lobbyCategory||'friend',red:s.red!==false,managed:!!s.lobbyFlow},players:s.players.map((p,j)=>({...publicCharacter(p,s.phase,!!s.resourceSetup),name:p.name,avatar:p.avatar||'',bot:!!p.bot,score:p.score,wind:this.wind(j),riichi:p.riichi,nuki:p.nuki,river:p.river,melds:[],count:0,ready:p.bot||j===0||!!p.lobbyReady,online:p.bot||Date.now()-(this.seen[p.token]||0)<12000}))}:{})};
 }
 dispatch(req){
  const before=this.s?JSON.stringify(this.s):null,waitingBefore={...this._waiting};
  try{
   check(req&&typeof req.op==='string','请求格式错误');let s=this.s;
   let i=s?s.players.findIndex(p=>!p.bot&&p.token===req.token):-1;
   if(i>=0&&req.userActive===true){const p=s.players[i];p.lastInputAt=Date.now();if(p.autoReason==='timeout'){p.auto=false;p.autoReason='';s.version++;}this.save(JSON.stringify(s));}
   if(req.op==='discover')return {ok:!!s&&String(req.pin)===s.pin};
   if(req.op==='hostSession'){check(req.host===this.host,'仅本机可查询');return s?{ok:true,token:s.players[0].token,state:this.view(0)}:{ok:true,state:{phase:'empty'}};}
   if(req.op==='peek')return {ok:true,state:this.view(i)};
   if(req.op==='recoverHost'){check(req.host===this.host,'仅本机房主可恢复');check(s,'尚无房间');return {ok:true,token:s.players[0].token,state:this.view(0)}}
   if(req.op==='state'){if(i>=0)this.seen[req.token]=Date.now();return {ok:true,state:this.view(i)}}
   if(req.op==='create'){
    check(req.host===this.host,'请在房主手机 App 内创建房间');if(s)return {ok:false,code:'ROOM_ACTIVE',error:'当前有未结束的房间',token:s.players[0].token,state:this.view(0)};check(req.n===3||req.n===4,'仅支持三人或四人');
    const p=player(String(req.name||'房主').slice(0,12));p.resourceProtocol=req.resourceProtocol===1?1:0;p.avatar=avatar(req.avatar===undefined?'':req.avatar);Object.assign(p,characterSelection(req));
    this.s={schema:1,audioRoom:randomHex(8),audioSeq:0,audioEvents:[],n:req.n,rounds:req.rounds===1?1:req.rounds===4?4:2,idleSeconds:req.idleSeconds===0?0:120,red:req.red!==false,pin:String(parseInt(randomHex(4),16)%900000+100000),players:[p],phase:'lobby',version:1,dealer:0,round:0,honba:0,sticks:0,
     resourceProtocol:req.resourceProtocol===1?1:0,resourceRevision:0,lobbyFlow:req.lobbyFlow===true,lobbyCategory:['friend','ranked','competition'].includes(req.category)?req.category:'friend'};
    this.configureBots(req);if(req.bots===true)while(this.s.players.length<this.s.n)this.addBot();
    this.save(JSON.stringify(this.s));this.seen[p.token]=Date.now();return {ok:true,token:p.token,state:this.view(0)};
   }
   check(s,'房主尚未创建房间');
   if(req.op==='join'){
    let characterNewSeat=false;
    check(String(req.pin)===s.pin,'房间码不正确');
    if(i>=0){s.players[i].auto=false;s.players[i].autoReason='';s.players[i].away=false;s.players[i].departed=false;delete this._waiting[req.token];s.version++;}
    else if(req.recovery){i=s.players.findIndex(p=>!p.bot&&p.recovery===String(req.recovery).trim().toLowerCase());check(i>=0,'座位凭据失效');}
    else {
     characterNewSeat=true;
     const name=String(req.name||'牌友').trim().slice(0,12);check(name,'请填写昵称');check(!s.players.some(p=>!p.bot&&p.name===name),'昵称已有人使用');
     if(s.phase==='lobby'){check(s.players.length<s.n,'房间已满');s.players.push(player(name));i=s.players.length-1;}
     else {i=s.players.findIndex((p,j)=>j>0&&p.bot);check(i>=0,'当前没有可接替的电脑座位');Object.assign(s.players[i],{name,token:randomHex(18),recovery:randomHex(6),requests:[],avatar:'',bot:false,auto:false,autoReason:'',away:false,departed:false,ready:false});}
     s.version++;
    }
    Object.assign(s.players[i],{auto:false,autoReason:'',away:false,departed:false,lastInputAt:Date.now()});if(req.avatar!==undefined)s.players[i].avatar=avatar(req.avatar);
    if(req.character&&(s.phase==='lobby'||characterNewSeat)){Object.assign(s.players[i],characterSelection(req));if(s.phase!=='lobby')freezeCharacter(s.players[i]);}
    s.players[i].resourceProtocol=req.resourceProtocol===1?1:0;if(characterNewSeat)delete s.resourceSetup;this.save(JSON.stringify(s));this.seen[s.players[i].token]=Date.now();return {ok:true,token:s.players[i].token,state:this.view(i)};
   }
   check(i>=0,'座位凭据失效');
   this.seen[req.token]=Date.now();
   if(req.id&&s.players[i].requests.includes(req.id))return {ok:true,state:this.view(i)};
   // Session controls are version-independent: a moving bot must not make
   // returning or leaving the table fail due to an unrelated version change.
   if(['resume','presence','autoPlay','leaveMatch','activity'].includes(req.op)){
    const p=s.players[i];
    if(req.op==='leaveMatch'){
     if(i===0){this.s=null;this.save('');return {ok:true,state:{phase:'empty'}};}
     if(s.phase==='lobby'){s.players.splice(i,1);delete s.resourceSetup;}
     else Object.assign(p,{bot:true,auto:false,autoReason:'',away:false,departed:true,token:randomHex(18),recovery:'',ready:true});
     s.version++;this.save(JSON.stringify(s));return {ok:true,state:{phase:'empty'}};
    }
    if(req.op==='resume'){check(!req.room||req.room===s.audioRoom,'牌局已取消');p.auto=false;p.autoReason='';p.away=false;delete p.awaySince;p.departed=false;p.lastInputAt=Date.now();if(s.phase==='result')p.ready=false;delete this._waiting[req.token];}
    else if(req.op==='presence'){const away=req.present===false;if(away){if(!p.away||!Number.isFinite(p.awaySince))p.awaySince=Date.now();}else{delete p.awaySince;if(!p.auto)p.lastInputAt=Date.now();}p.away=away;}
    else if(req.op==='activity'){p.lastInputAt=Date.now();if(p.autoReason==='timeout'){p.auto=false;p.autoReason='';}}
    else {check(['play','reaction'].includes(s.phase),'当前不能切换接管');p.auto=req.enabled===true;p.autoReason=p.auto?'manual':'';p.away=false;p.lastInputAt=Date.now();delete this._waiting[req.token];}
    s.version++;this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   if(!['emote','resourceAck','resourceOffer','cancelResourceSetup','profile','ready','character'].includes(req.op))check(Number.isInteger(req.version)&&req.version===s.version,'牌桌已更新，请重新选择');
   if(req.op==='emote'){
    this.emote(i,req);if(req.id){s.players[i].requests.push(String(req.id).slice(0,80));s.players[i].requests=s.players[i].requests.slice(-32);}this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   // Resource reports are public cosmetics only, independent of turn versions.
   if(req.op==='resourceOffer'){
    const offer=req.resourceShare||{};check(Number.isInteger(offer.port)&&offer.port>=1024&&offer.port<=65535&&/^[a-f0-9]{32}$/.test(offer.ticket),'资源分享信息无效');check(Array.isArray(offer.hashes)&&offer.hashes.length<=200&&offer.hashes.every(h=>/^[a-f0-9]{64}$/.test(h)),'资源清单无效');
    const address=String(req.peerAddress||'127.0.0.1').replace(/^::ffff:/,'');check(/^(127\.|10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[01])\.)/.test(address),'资源分享地址无效');s.players[i].resourceShare={address,port:offer.port,ticket:offer.ticket,hashes:[...new Set(offer.hashes)]};this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   if(req.op==='resourceAck'){
    check(s.phase==='lobby'&&s.resourceSetup&&req.setupRevision===s.resourceSetup.revision,'资源准备已更新');check(['pending','downloading','ready','fallback'].includes(req.resourceStatus),'资源确认状态无效');const keys=req.fallbackCharacters||[];check(Array.isArray(keys)&&keys.every(k=>s.resourceSetup.choices.some(c=>c.character===k)),'替代角色无效');s.resourceSetup.confirmations[req.token]={state:req.resourceStatus,fallback:keys};this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   if(req.op==='cancelResourceSetup'){check(i===0&&s.phase==='lobby','仅房主可取消开局准备');delete s.resourceSetup;this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};}
   if(req.op==='prepareResources'){
    check(i===0&&s.phase==='lobby','仅房主可准备开局');if(req.fillBots===true){this.configureBots(req);while(s.players.length<s.n)this.addBot();}check(s.players.length===s.n,'还有空座位');check(!s.lobbyFlow||s.players.every((p,j)=>p.bot||j===0||p.lobbyReady),'请等待所有玩家准备');
    if(!s.resourceSetup){s.players.forEach(freezeCharacter);s.resourceRevision=(s.resourceRevision||0)+1;s.resourceSetup={revision:s.resourceRevision,createdAt:Date.now(),choices:s.players.map(p=>({character:p.matchCharacter,skin:p.matchSkin})),confirmations:{}};}
    this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   // Waiting-room operations terminate here. No action, scoring, deal or table
   // transition implementation below is changed by the new lobby workflow.
   if(['lobbyReady','kick','leaveLobby','closeLobby'].includes(req.op)){
    check(s.phase==='lobby','仅可在开局前管理房间');
    if(req.op==='lobbyReady'){if(req.ready!==true)delete s.resourceSetup;check(i!==0,'房主无需准备');s.players[i].lobbyReady=req.ready===true;}
    else if(req.op==='kick'){delete s.resourceSetup;
     check(i===0,'仅房主可踢出玩家');check(Number.isInteger(req.seat)&&req.seat>0&&req.seat<s.players.length,'不能踢出房主或空座位');s.players.splice(req.seat,1);
    }else if(req.op==='leaveLobby'){delete s.resourceSetup;
     check(i!==0,'房主请解散房间');s.players.splice(i,1);s.version++;this.save(JSON.stringify(s));return {ok:true,state:{phase:'empty'}};
    }else{check(i===0,'仅房主可解散房间');this.s=null;this.save('');return {ok:true,state:{phase:'empty'}};}
    if(req.id){s.players[i].requests.push(String(req.id).slice(0,80));s.players[i].requests=s.players[i].requests.slice(-32);}
    s.version++;this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
   }
   if(req.op==='start'&&s.lobbyFlow)check(s.players.every((p,j)=>p.bot||j===0||p.lobbyReady),'请等待所有玩家准备');
   if(req.op==='character'){check(!s.resourceSetup,'请先取消开局准备再切换雀士');check(s.phase==='lobby','出战雀士在本局结束前固定');Object.assign(s.players[i],characterSelection(req));}
   else if(req.op==='profile'){s.players[i].avatar=avatar(req.avatar);}
   else if(req.op==='addBot'||req.op==='fillBots'){delete s.resourceSetup;check(i===0&&s.phase==='lobby','仅房主可在开局前添加 AI');this.configureBots(req);this.addBot();if(req.op==='fillBots')while(s.players.length<s.n)this.addBot();}
   else if(req.op==='removeBot'){delete s.resourceSetup;check(i===0&&s.phase==='lobby','仅房主可在开局前移除 AI');check(Number.isInteger(req.seat)&&s.players[req.seat]?.bot,'该座位不是 AI');s.players.splice(req.seat,1);}
   else if(req.op==='start'){check(i===0&&s.phase==='lobby','仅房主可开局');const prepared=s.resourceSetup;if(s.resourceProtocol===1){check(prepared&&req.setupRevision===prepared.revision,'请先完成资源准备');check(this.resourcesReady(),'请等待资源下载或选择一姬替代');}if(req.fillBots===true){this.configureBots(req);while(s.players.length<s.n)this.addBot();}check(s.players.length===s.n,'还有空位，可添加 AI 后开局');s.players=shuffle(s.players); // Keep host identity separate from seat ordering.
    // Host remains player 0; randomize dealer instead, so management rights never move.
    const hostIndex=s.players.findIndex(p=>p.token===req.token);[s.players[0],s.players[hostIndex]]=[s.players[hostIndex],s.players[0]];
    s.matchSeq=(s.matchSeq||0)+1;s.matchStartedAt=Date.now();s.matchOpeningUntil=s.matchStartedAt+6000;s.players.forEach(p=>{p.lastInputAt=s.matchOpeningUntil;if(!prepared)freezeCharacter(p);});
    delete s.resourceSetup;s.dealer=parseInt(randomHex(2),16)%s.n;s.initialDealer=s.dealer;s.players.forEach(p=>p.score=s.n===3?35000:25000);this.newHand();
   }else if(req.op==='action'){this.act(i,req.action);s.players[i].auto=false;s.players[i].autoReason='';s.players[i].lastInputAt=Date.now();s.players[i].away=false;delete this._waiting[req.token];}
   else if(req.op==='ready'){check(s.phase==='result','当前不在结算阶段');check(!req.handSeq||req.handSeq===s.handSeq,'结算已更新');s.players[i].ready=true;this.advanceIfReady();}
   else if(req.op==='next'){
    check(i===0&&s.phase==='result'&&s.players.every(p=>p.ready),'请等待所有玩家确认结算');
    this.advanceResult();
   }else if(req.op==='close'){check(i===0,'仅房主可结束牌桌');this.s=null;this.save('');return {ok:true,state:{phase:'empty'}};}
   else throw Error('未知操作');
   i=s.players.findIndex(p=>p.token===req.token);
   if(req.id){s.players[i].requests.push(String(req.id).slice(0,80));s.players[i].requests=s.players[i].requests.slice(-32);}
   s.version++;this.save(JSON.stringify(s));return {ok:true,state:this.view(i)};
  }catch(e){this.s=before?JSON.parse(before):null;this._waiting=waitingBefore;return {ok:false,error:String(e.message||e)}}
 }
}
g.RiichiRoom=Room;if(typeof module!=='undefined')module.exports={Room,tiles,RULE,norm};
})(globalThis);
