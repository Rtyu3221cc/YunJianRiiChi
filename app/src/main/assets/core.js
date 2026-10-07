/* Bundled @kobalab/majiang-core 1.4.1, MIT, Satoshi Kobayashi. See LICENSE-core.txt. */
(function(g){const modules={"./board":function(module,exports,require){
/*
 *  Majiang.Board
 */
"use strict";

const Majiang = {
    Shoupai: require('./shoupai'),
    He:      require('./he')
};

class Shan {
    constructor(baopai) {
        this.paishu = 136 - 13 * 4 - 14;
        this.baopai = [].concat(baopai||[]);
        this.fubaopai;
    }
    zimo(p)         { this.paishu--; return p || '_' }
    kaigang(baopai) { this.baopai.push(baopai);      }
}

module.exports = class Board {

    constructor(kaiju) {
        if (kaiju) this.kaiju(kaiju);
    }

    kaiju(kaiju) {

        this.title  = kaiju.title;
        this.player = kaiju.player;
        this.qijia  = kaiju.qijia;

        this.zhuangfeng = 0;
        this.jushu      = 0;
        this.changbang  = 0;
        this.lizhibang  = 0;
        this.defen      = [];
        this.shan       = null;
        this.shoupai    = [];
        this.he         = [];
        this.player_id  = [0,1,2,3];
        this.lunban     = -1;

        this._lizhi;
        this._fenpei;
        this._lianzhuang;
        this._changbang;
        this._lizhibang;
    }

    menfeng(id) {
        return (id + 4 - this.qijia + 4 - this.jushu) % 4;
    }

    qipai(qipai) {
        this.zhuangfeng = qipai.zhuangfeng;
        this.jushu      = qipai.jushu;
        this.changbang  = qipai.changbang;
        this.lizhibang  = qipai.lizhibang;
        this.shan       = new Shan(qipai.baopai);
        for (let l = 0; l < 4; l++) {
            let paistr = qipai.shoupai[l] || '_'.repeat(13);
            this.shoupai[l] = Majiang.Shoupai.fromString(paistr);
            this.he[l]      = new Majiang.He();
            this.player_id[l] = (this.qijia + this.jushu + l) % 4;
            this.defen[this.player_id[l]] = qipai.defen[l];
        }
        this.lunban     = -1;

        this._lizhi     = false;
        this._fenpei    = null;
        this._changbang = qipai.changbang;
        this._lizhibang = qipai.lizhibang;
    }

    lizhi() {
        if (this._lizhi) {
            this.defen[this.player_id[this.lunban]] -= 1000;
            this.lizhibang++;
            this._lizhi = false;
        }
    }

    zimo(zimo) {
        this.lizhi();
        this.lunban = zimo.l;
        this.shoupai[zimo.l].zimo(this.shan.zimo(zimo.p), false);
    }

    dapai(dapai) {
        this.lunban = dapai.l;
        this.shoupai[dapai.l].dapai(dapai.p, false);
        this.he[dapai.l].dapai(dapai.p);
        this._lizhi = dapai.p.slice(-1) == '*';
    }

    fulou(fulou) {
        this.lizhi();
        this.he[this.lunban].fulou(fulou.m);
        this.lunban = fulou.l;
        this.shoupai[fulou.l].fulou(fulou.m, false);
    }

    gang(gang) {
        this.lunban = gang.l;
        this.shoupai[gang.l].gang(gang.m, false);
    }

    kaigang(kaigang) {
        this.shan.kaigang(kaigang.baopai);
    }

    hule(hule) {
        let shoupai = this.shoupai[hule.l];
        shoupai.fromString(hule.shoupai);
        if (hule.baojia != null) shoupai.dapai(shoupai.get_dapai().pop());
        if (this._fenpei) {
            this.changbang = 0;
            this.lizhibang = 0;
            for (let l = 0; l < 4; l++) {
                this.defen[this.player_id[l]] += this._fenpei[l];
            }
        }
        this.shan.fubaopai = hule.fubaopai;
        this._fenpei = hule.fenpei;
        this._lizhibang = 0;
        if (hule.l == 0) this._lianzhuang = true;
    }

    pingju(pingju) {
        if (! pingju.name.match(/^三家和/)) this.lizhi();
        for (let l = 0; l < 4; l++) {
            if (pingju.shoupai[l])
                this.shoupai[l].fromString(pingju.shoupai[l]);
        }
        this._fenpei = pingju.fenpei;
        this._lizhibang = this.lizhibang;
        this._lianzhuang = true;
    }

    last() {
        if (! this._fenpei) return;
        this.changbang = this._lianzhuang ? this._changbang + 1 : 0;
        this.lizhibang = this._lizhibang;
        for (let l = 0; l < 4; l++) {
            this.defen[this.player_id[l]] += this._fenpei[l];
        }
    }

    jieju(paipu) {
        for (let id = 0; id < 4; id++) {
            this.defen[id] = paipu.defen[id];
        }
        this.lunban = -1;
    }
}

},
"./game":function(module,exports,require){
/*
 *  Majiang.Game
 */
"use strict";

const Majiang = {
    rule:    require('./rule'),
    Shoupai: require('./shoupai'),
    Shan:    require('./shan'),
    He:      require('./he'),
    Util:    Object.assign(require('./xiangting'),
                           require('./hule'))
};

module.exports = class Game {

    constructor(players, callback, rule, title) {

        this._players  = players;
        this._callback = callback || (()=>{});
        this._rule     = rule || Majiang.rule();

        this._model = {
            title:      title || '電脳麻将\n' + new Date().toLocaleString(),
            player:     ['私','下家','対面','上家'],
            qijia:      0,
            zhuangfeng: 0,
            jushu:      0,
            changbang:  0,
            lizhibang:  0,
            defen:      [0,0,0,0].map(x=>this._rule['配給原点']),
            shan:       null,
            shoupai:    [],
            he:         [],
            player_id:  [ 0, 1, 2, 3 ]
        };

        this._view;

        this._status;
        this._reply = [];

        this._sync  = false;
        this._stop  = null;
        this._dwell = 0;
        this._wait  = 0;
        this._timeout_id;

        this._handler;
        this._speed = 3;
    }

    get model()      { return this._model  }
    set view(view)   { this._view = view   }
    get view()       { return this._view   }
    set dwell(ms)    { this._dwell = ms    }
    set wait(wait)   { this._wait = wait   }

    set handler(callback) { this._handler = callback }

    get speed()      { return this._speed  }
    set speed(speed) {
        this._speed = speed;
        this.dwell = speed * 200;
    }

    add_paipu(paipu) {
        this._paipu.log[this._paipu.log.length - 1].push(paipu);
    }

    delay(callback, timeout) {

        if (this._sync) return callback();

        timeout = this._dwell == 0 ? 0
                : timeout == null  ? Math.max(500, this._dwell)
                :                    timeout;
        setTimeout(callback, timeout);
    }

    say(name, l) {
        if (this._view) this._view.say(name, l);
    }

    stop(callback = ()=>{}) {
        this._stop = callback;
    }

    start() {
        if (this._timeout_id) return;
        this._stop = null;
        this._timeout_id = setTimeout(()=>this.next(), 0);
    }

    notify_players(type, msg) {

        for (let l = 0; l < 4; l++) {
            let id = this._model.player_id[l];
            if (this._sync)
                    this._players[id].action(msg[l]);
            else    setTimeout(()=>{
                        this._players[id].action(msg[l]);
                    }, 0);
        }
    }

    call_players(type, msg, timeout) {

        timeout = this._dwell == 0 ? 0
                : timeout == null  ? this._dwell
                :                    timeout;
        this._status = type;
        this._reply  = [];
        for (let l = 0; l < 4; l++) {
            let id = this._model.player_id[l];
            if (this._sync)
                    this._players[id].action(
                            msg[l], reply => this.reply(id, reply));
            else    setTimeout(()=>{
                        this._players[id].action(
                            msg[l], reply => this.reply(id, reply));
                    }, 0);
        }
        if (! this._sync)
                this._timeout_id = setTimeout(()=>this.next(), timeout);
    }

    reply(id, reply) {
        this._reply[id] = reply || {};
        if (this._sync) return;
        if (this._reply.filter(x=>x).length < 4) return;
        if (! this._timeout_id)
                this._timeout_id = setTimeout(()=>this.next(), 0);
    }

    next() {
        this._timeout_id = clearTimeout(this._timeout_id);
        if (this._reply.filter(x=>x).length < 4) return;
        if (this._stop) return this._stop();

        if      (this._status == 'kaiju')    this.reply_kaiju();
        else if (this._status == 'qipai')    this.reply_qipai();
        else if (this._status == 'zimo')     this.reply_zimo();
        else if (this._status == 'dapai')    this.reply_dapai();
        else if (this._status == 'fulou')    this.reply_fulou();
        else if (this._status == 'gang')     this.reply_gang();
        else if (this._status == 'gangzimo') this.reply_zimo();
        else if (this._status == 'hule')     this.reply_hule();
        else if (this._status == 'pingju')   this.reply_pingju();
        else                                 this._callback(this._paipu);
    }

    do_sync() {

        this._sync  = true;

        this.kaiju();

        for (;;) {
            if      (this._status == 'kaiju')    this.reply_kaiju();
            else if (this._status == 'qipai')    this.reply_qipai();
            else if (this._status == 'zimo')     this.reply_zimo();
            else if (this._status == 'dapai')    this.reply_dapai();
            else if (this._status == 'fulou')    this.reply_fulou();
            else if (this._status == 'gang')     this.reply_gang();
            else if (this._status == 'gangzimo') this.reply_zimo();
            else if (this._status == 'hule')     this.reply_hule();
            else if (this._status == 'pingju')   this.reply_pingju();
            else                                 break;
        }

        this._callback(this._paipu);

        return this;
    }

    kaiju(qijia) {

        this._model.qijia = qijia ?? Math.floor(Math.random() * 4);

        this._max_jushu = this._rule['場数'] == 0 ? 0
                        : this._rule['場数'] * 4 - 1;

        this._paipu = {
            title:  this._model.title,
            player: this._model.player,
            qijia:  this._model.qijia,
            log:    [],
            defen:  this._model.defen.concat(),
            point:  [],
            rank:   []
        };

        let msg = [];
        for (let id = 0; id < 4; id++) {
            msg[id] = JSON.parse(JSON.stringify({
                kaiju: {
                    id:     id,
                    rule:   this._rule,
                    title:  this._paipu.title,
                    player: this._paipu.player,
                    qijia:  this._paipu.qijia
                }
            }));
        }
        this.call_players('kaiju', msg, 0);

        if (this._view) this._view.kaiju();
    }

    qipai(shan) {

        let model = this._model;

        model.shan = shan || new Majiang.Shan(this._rule);
        for (let l = 0; l < 4; l++) {
            let qipai = [];
            for (let i = 0; i < 13; i++) {
                qipai.push(model.shan.zimo());
            }
            model.shoupai[l]   = new Majiang.Shoupai(qipai);
            model.he[l]        = new Majiang.He();
            model.player_id[l] = (model.qijia + model.jushu + l) % 4;
        }
        model.lunban = -1;

        this._diyizimo = true;
        this._fengpai  = this._rule['途中流局あり'];

        this._dapai = null;
        this._gang  = null;

        this._lizhi     = [ 0, 0, 0, 0 ];
        this._yifa      = [ 0, 0, 0, 0 ];
        this._n_gang    = [ 0, 0, 0, 0 ];
        this._neng_rong = [ 1, 1, 1, 1 ];

        this._hule        = [];
        this._hule_option = null;
        this._no_game     = false;
        this._lianzhuang  = false;
        this._changbang   = model.changbang;
        this._fenpei      = null;

        this._paipu.defen = model.defen.concat();
        this._paipu.log.push([]);
        let paipu = {
            qipai: {
                zhuangfeng: model.zhuangfeng,
                jushu:      model.jushu,
                changbang:  model.changbang,
                lizhibang:  model.lizhibang,
                defen:      model.player_id.map(id => model.defen[id]),
                baopai:     model.shan.baopai[0],
                shoupai:    model.shoupai.map(shoupai => shoupai.toString())
            }
        };
        this.add_paipu(paipu);

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
            for (let i = 0; i < 4; i++) {
                if (i != l) msg[l].qipai.shoupai[i] = '';
            }
        }
        this.call_players('qipai', msg);

        if (this._view) this._view.redraw();
    }

    zimo() {

        let model = this._model;

        model.lunban = (model.lunban + 1) % 4;

        let zimo = model.shan.zimo();
        model.shoupai[model.lunban].zimo(zimo);

        let paipu = { zimo: { l: model.lunban, p: zimo } };
        this.add_paipu(paipu);

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
            if (l != model.lunban) msg[l].zimo.p = '';
        }
        this.call_players('zimo', msg);

        if (this._view) this._view.update(paipu);
    }

    dapai(dapai) {

        let model = this._model;

        this._yifa[model.lunban] = 0;

        if (! model.shoupai[model.lunban].lizhi)
                                    this._neng_rong[model.lunban] = true;

        model.shoupai[model.lunban].dapai(dapai);
        model.he[model.lunban].dapai(dapai);

        if (this._diyizimo) {
            if (! dapai.match(/^z[1234]/))  this._fengpai = false;
            if (this._dapai && this._dapai.slice(0,2) != dapai.slice(0,2))
                                            this._fengpai = false;
        }
        else                                this._fengpai = false;

        if (dapai.slice(-1) == '*') {
            this._lizhi[model.lunban] = this._diyizimo ? 2 : 1;
            this._yifa[model.lunban]  = this._rule['一発あり'];
        }

        if (Majiang.Util.xiangting(model.shoupai[model.lunban]) == 0
            && Majiang.Util.tingpai(model.shoupai[model.lunban])
                            .find(p=>model.he[model.lunban].find(p)))
        {
            this._neng_rong[model.lunban] = false;
        }

        this._dapai = dapai;

        let paipu = { dapai: { l: model.lunban, p: dapai } };
        this.add_paipu(paipu);

        if (this._gang) this.kaigang();

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.call_players('dapai', msg);

        if (this._view) this._view.update(paipu);
    }

    fulou(fulou) {

        let model = this._model;

        this._diyizimo = false;
        this._yifa     = [0,0,0,0];

        model.he[model.lunban].fulou(fulou);

        let d = fulou.match(/[\+\=\-]/);
        model.lunban = (model.lunban + '_-=+'.indexOf(d)) % 4;

        model.shoupai[model.lunban].fulou(fulou);

        if (fulou.match(/^[mpsz]\d{4}/)) {
            this._gang = fulou;
            this._n_gang[model.lunban]++;
        }

        let paipu = { fulou: { l: model.lunban, m: fulou } };
        this.add_paipu(paipu);

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.call_players('fulou', msg);

        if (this._view) this._view.update(paipu);
    }

    gang(gang) {

        let model = this._model;

        model.shoupai[model.lunban].gang(gang);

        let paipu = { gang: { l: model.lunban, m: gang } };
        this.add_paipu(paipu);

        if (this._gang) this.kaigang();

        this._gang = gang;
        this._n_gang[model.lunban]++;

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.call_players('gang', msg);

        if (this._view) this._view.update(paipu);
    }

    gangzimo() {

        let model = this._model;

        this._diyizimo = false;
        this._yifa     = [0,0,0,0];

        let zimo = model.shan.gangzimo();
        model.shoupai[model.lunban].zimo(zimo);

        let paipu = { gangzimo: { l: model.lunban, p: zimo } };
        this.add_paipu(paipu);

        if (! this._rule['カンドラ後乗せ'] ||
            this._gang.match(/^[mpsz]\d{4}$/)) this.kaigang();

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
            if (l != model.lunban) msg[l].gangzimo.p = '';
        }
        this.call_players('gangzimo', msg);

        if (this._view) this._view.update(paipu);
    }

    kaigang() {

        this._gang = null;

        if (! this._rule['カンドラあり']) return;

        let model = this._model;

        model.shan.kaigang();
        let baopai = model.shan.baopai.pop();

        let paipu = { kaigang: { baopai: baopai } };
        this.add_paipu(paipu);

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.notify_players('kaigang', msg);

        if (this._view) this._view.update(paipu);
    }

    hule() {

        let model = this._model;

        if (this._status != 'hule') {
            model.shan.close();
            this._hule_option = this._status == 'gang'     ? 'qianggang'
                              : this._status == 'gangzimo' ? 'lingshang'
                              :                              null;
        }

        let menfeng  = this._hule.length ? this._hule.shift() : model.lunban;
        let rongpai  = menfeng == model.lunban ? null
                     : (this._hule_option == 'qianggang'
                            ? this._gang[0] + this._gang.slice(-1)
                            : this._dapai.slice(0,2)
                       ) + '_+=-'[(4 + model.lunban - menfeng) % 4];
        let shoupai  = model.shoupai[menfeng].clone();
        let fubaopai = shoupai.lizhi ? model.shan.fubaopai : null;

        let param = {
            rule:           this._rule,
            zhuangfeng:     model.zhuangfeng,
            menfeng:        menfeng,
            hupai: {
                lizhi:      this._lizhi[menfeng],
                yifa:       this._yifa[menfeng],
                qianggang:  this._hule_option == 'qianggang',
                lingshang:  this._hule_option == 'lingshang',
                haidi:      model.shan.paishu > 0
                            || this._hule_option == 'lingshang' ? 0
                                : ! rongpai                     ? 1
                                :                                 2,
                tianhu:     ! (this._diyizimo && ! rongpai)     ? 0
                                : menfeng == 0                  ? 1
                                :                                 2
            },
            baopai:         model.shan.baopai,
            fubaopai:       fubaopai,
            jicun:          { changbang: model.changbang,
                              lizhibang: model.lizhibang }
        };
        let hule = Majiang.Util.hule(shoupai, rongpai, param);

        if (this._rule['連荘方式'] > 0 && menfeng == 0) this._lianzhuang = true;
        if (this._rule['場数'] == 0) this._lianzhuang = false;
        this._fenpei = hule.fenpei;

        let paipu = {
            hule: {
                l:          menfeng,
                shoupai:    rongpai ? shoupai.zimo(rongpai).toString()
                                    : shoupai.toString(),
                baojia:     rongpai ? model.lunban : null,
                fubaopai:   fubaopai,
                fu:         hule.fu,
                fanshu:     hule.fanshu,
                damanguan:  hule.damanguan,
                defen:      hule.defen,
                hupai:      hule.hupai,
                fenpei:     hule.fenpei
            }
        };
        for (let key of ['fu','fanshu','damanguan']) {
            if (! paipu.hule[key]) delete paipu.hule[key];
        }
        this.add_paipu(paipu);

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.call_players('hule', msg, this._wait);

        if (this._view) this._view.update(paipu);
    }

    pingju(name, shoupai = ['','','','']) {

        let model = this._model;

        let fenpei  = [0,0,0,0];

        if (! name) {

            let n_tingpai = 0;
            for (let l = 0; l < 4; l++) {
                if (this._rule['ノーテン宣言あり'] && ! shoupai[l]
                    && ! model.shoupai[l].lizhi) continue;
                if (! this._rule['ノーテン罰あり']
                    && (this._rule['連荘方式'] != 2 || l != 0)
                    && ! model.shoupai[l].lizhi)
                {
                    shoupai[l] = '';
                }
                else if (Majiang.Util.xiangting(model.shoupai[l]) == 0
                        && Majiang.Util.tingpai(model.shoupai[l]).length > 0)
                {
                    n_tingpai++;
                    shoupai[l] = model.shoupai[l].toString();
                    if (this._rule['連荘方式'] == 2 && l == 0)
                                                    this._lianzhuang = true;
                }
                else {
                    shoupai[l] = '';
                }
            }
            if (this._rule['流し満貫あり']) {
                for (let l = 0; l < 4; l++) {
                    let all_yaojiu = true;
                    for (let p of model.he[l]._pai) {
                        if (p.match(/[\+\=\-]$/)) { all_yaojiu = false; break }
                        if (p.match(/^z/))          continue;
                        if (p.match(/^[mps][19]/))  continue;
                        all_yaojiu = false; break;
                    }
                    if (all_yaojiu) {
                        name = '流し満貫';
                        for (let i = 0; i < 4; i++) {
                            fenpei[i] += l == 0 && i == l ? 12000
                                       : l == 0           ? -4000
                                       : l != 0 && i == l ?  8000
                                       : l != 0 && i == 0 ? -4000
                                       :                    -2000;
                        }
                    }
                }
            }
            if (! name) {
                name = '荒牌平局';
                if (this._rule['ノーテン罰あり']
                    && 0 < n_tingpai && n_tingpai < 4)
                {
                    for (let l = 0; l < 4; l++) {
                        fenpei[l] = shoupai[l] ?  3000 / n_tingpai
                                               : -3000 / (4 - n_tingpai);
                    }
                }
            }
            if (this._rule['連荘方式'] == 3) this._lianzhuang = true;
        }
        else {
            this._no_game    = true;
            this._lianzhuang = true;
        }

        if (this._rule['場数'] == 0) this._lianzhuang = true;

        this._fenpei = fenpei;

        let paipu = {
            pingju: { name: name, shoupai: shoupai, fenpei: fenpei }
        };
        this.add_paipu(paipu);

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.call_players('pingju', msg, this._wait);

        if (this._view) this._view.update(paipu);
    }

    last() {

        let model = this._model;

        model.lunban = -1;
        if (this._view) this._view.update();

        if (! this._lianzhuang) {
            model.jushu++;
            model.zhuangfeng += (model.jushu / 4)|0;
            model.jushu = model.jushu % 4;
        }

        let jieju = false;
        let guanjun = -1;
        const defen = model.defen;
        for (let i = 0; i < 4; i++) {
            let id = (model.qijia + i) % 4;
            if (defen[id] < 0 && this._rule['トビ終了あり'])    jieju = true;
            if (defen[id] >= 30000
                && (guanjun < 0 || defen[id] > defen[guanjun])) guanjun = id;
        }

        let sum_jushu = model.zhuangfeng * 4 + model.jushu;

        if      (15 < sum_jushu)                                jieju = true;
        else if ((this._rule['場数'] + 1) * 4 - 1 < sum_jushu)  jieju = true;
        else if (this._max_jushu < sum_jushu) {
            if      (this._rule['延長戦方式'] == 0)             jieju = true;
            else if (this._rule['場数'] == 0)                   jieju = true;
            else if (guanjun >= 0)                              jieju = true;
            else {
                this._max_jushu += this._rule['延長戦方式'] == 3 ? 4
                                 : this._rule['延長戦方式'] == 2 ? 1
                                 :                                 0;
            }
        }
        else if (this._max_jushu == sum_jushu) {
            if (this._rule['オーラス止めあり'] && guanjun == model.player_id[0]
                && this._lianzhuang && ! this._no_game)         jieju = true;
        }

        if (jieju)  this.delay(()=>this.jieju(), 0);
        else        this.delay(()=>this.qipai(), 0);
    }

    jieju() {

        let model = this._model;

        let paiming = [];
        const defen = model.defen;
        for (let i = 0; i < 4; i++) {
            let id = (model.qijia + i) % 4;
            for (let j = 0; j < 4; j++) {
                if (j == paiming.length || defen[id] > defen[paiming[j]]) {
                    paiming.splice(j, 0, id);
                    break;
                }
            }
        }
        defen[paiming[0]] += model.lizhibang * 1000;
        this._paipu.defen = defen;

        let rank = [0,0,0,0];
        for (let i = 0; i < 4; i++) {
            rank[paiming[i]] = i + 1;
        }
        this._paipu.rank = rank;

        const round = ! this._rule['順位点'].find(p=>p.match(/\.\d$/));
        let point = [0,0,0,0];
        for (let i = 1; i < 4; i++) {
            let id = paiming[i];
            point[id] = (defen[id] - 30000) / 1000
                      + + this._rule['順位点'][i];
            if (round) point[id] = Math.round(point[id]);
            point[paiming[0]] -= point[id];
        }
        this._paipu.point = point.map(p=> p.toFixed(round ? 0 : 1));

        let paipu = { jieju: this._paipu };

        let msg = [];
        for (let l = 0; l < 4; l++) {
            msg[l] = JSON.parse(JSON.stringify(paipu));
        }
        this.call_players('jieju', msg, this._wait);

        if (this._view) this._view.summary(this._paipu);

        if (this._handler) this._handler();
    }

    get_reply(l) {
        let model = this._model;
        return this._reply[model.player_id[l]];
    }

    reply_kaiju() { this.delay(()=>this.qipai(), 0) }

    reply_qipai() { this.delay(()=>this.zimo(), 0) }

    reply_zimo() {

        let model = this._model;

        let reply = this.get_reply(model.lunban);
        if (reply.daopai) {
            if (this.allow_pingju()) {
                let shoupai = ['','','',''];
                shoupai[model.lunban] = model.shoupai[model.lunban].toString();
                return this.delay(()=>this.pingju('九種九牌', shoupai), 0);
            }
        }
        else if (reply.hule) {
            if (this.allow_hule()) {
                this.say('zimo', model.lunban);
                return this.delay(()=>this.hule());
            }
        }
        else if (reply.gang) {
            if (this.get_gang_mianzi().find(m => m == reply.gang)) {
                this.say('gang', model.lunban);
                return this.delay(()=>this.gang(reply.gang));
            }
        }
        else if (reply.dapai) {
            let dapai = reply.dapai.replace(/\*$/,'');
            if (this.get_dapai().find(p => p == dapai)) {
                if (reply.dapai.slice(-1) == '*' && this.allow_lizhi(dapai)) {
                    this.say('lizhi', model.lunban);
                    return this.delay(()=>this.dapai(reply.dapai));
                }
                return this.delay(()=>this.dapai(dapai), 0);
            }
        }

        let p = this.get_dapai().pop();
        this.delay(()=>this.dapai(p), 0);
    }

    reply_dapai() {

        let model = this._model;

        for (let i = 1; i < 4; i++) {
            let l = (model.lunban + i) % 4;
            let reply = this.get_reply(l);
            if (reply.hule && this.allow_hule(l)) {
                if (this._rule['最大同時和了数'] == 1  && this._hule.length)
                                                                    continue;
                this.say('rong', l);
                this._hule.push(l);
            }
            else {
                let shoupai = model.shoupai[l].clone().zimo(this._dapai);
                if (Majiang.Util.xiangting(shoupai) == -1)
                                                this._neng_rong[l] = false;
            }
        }
        if (this._hule.length == 3 && this._rule['最大同時和了数'] == 2) {
            let shoupai = ['','','',''];
            for (let l of this._hule) {
                shoupai[l] = model.shoupai[l].toString();
            }
            return this.delay(()=>this.pingju('三家和', shoupai));
        }
        else if (this._hule.length) {
            return this.delay(()=>this.hule());
        }

        if (this._dapai.slice(-1) == '*') {
            model.defen[model.player_id[model.lunban]] -= 1000;
            model.lizhibang++;

            if (this._lizhi.filter(x=>x).length == 4
                && this._rule['途中流局あり'])
            {
                let shoupai = model.shoupai.map(s=>s.toString());
                return this.delay(()=>this.pingju('四家立直', shoupai));
            }
        }

        if (this._diyizimo && model.lunban == 3) {
            this._diyizimo = false;
            if (this._fengpai) {
                return this.delay(()=>this.pingju('四風連打'), 0);
            }
        }

        if (this._n_gang.reduce((x, y)=> x + y) == 4) {
            if (Math.max(...this._n_gang) < 4 && this._rule['途中流局あり']) {
                return this.delay(()=>this.pingju('四開槓'), 0);
            }
        }

        if (! model.shan.paishu) {
            let shoupai = ['','','',''];
            for (let l = 0; l < 4; l++) {
                let reply = this.get_reply(l);
                if (reply.daopai) shoupai[l] = reply.daopai;
            }
            return this.delay(()=>this.pingju('', shoupai), 0);
        }

        for (let i = 1; i < 4; i++) {
            let l = (model.lunban + i) % 4;
            let reply = this.get_reply(l);
            if (reply.fulou) {
                let m = reply.fulou.replace(/0/g,'5');
                if (m.match(/^[mpsz](\d)\1\1\1/)) {
                    if (this.get_gang_mianzi(l).find(m => m == reply.fulou)) {
                        this.say('gang', l);
                        return this.delay(()=>this.fulou(reply.fulou));
                    }
                }
                else if (m.match(/^[mpsz](\d)\1\1/)) {
                    if (this.get_peng_mianzi(l).find(m => m == reply.fulou)) {
                        this.say('peng', l);
                        return this.delay(()=>this.fulou(reply.fulou));
                    }
                }
            }
        }
        let l = (model.lunban + 1) % 4;
        let reply = this.get_reply(l);
        if (reply.fulou) {
            if (this.get_chi_mianzi(l).find(m => m == reply.fulou)) {
                this.say('chi', l);
                return this.delay(()=>this.fulou(reply.fulou));
            }
        }

        this.delay(()=>this.zimo(), 0);
    }

    reply_fulou() {

        let model = this._model;

        if (this._gang) {
            return this.delay(()=>this.gangzimo(), 0);
        }

        let reply = this.get_reply(model.lunban);
        if (reply.dapai) {
            if (this.get_dapai().find(p => p == reply.dapai)) {
                return this.delay(()=>this.dapai(reply.dapai), 0);
            }
        }

        let p = this.get_dapai().pop();
        this.delay(()=>this.dapai(p), 0);
    }

    reply_gang() {

        let model = this._model;

        if (this._gang.match(/^[mpsz]\d{4}$/)) {
            return this.delay(()=>this.gangzimo(), 0);
        }

        for (let i = 1; i < 4; i++) {
            let l = (model.lunban + i) % 4;
            let reply = this.get_reply(l);
            if (reply.hule && this.allow_hule(l)) {
                if (this._rule['最大同時和了数'] == 1  && this._hule.length)
                                                                    continue;
                this.say('rong', l);
                this._hule.push(l);
            }
            else {
                let p = this._gang[0] + this._gang.slice(-1);
                let shoupai = model.shoupai[l].clone().zimo(p);
                if (Majiang.Util.xiangting(shoupai) == -1)
                                                this._neng_rong[l] = false;
            }
        }
        if (this._hule.length) {
            return this.delay(()=>this.hule());
        }

        this.delay(()=>this.gangzimo(), 0);
    }

    reply_hule() {

        let model = this._model;

        for (let l = 0; l < 4; l++) {
            model.defen[model.player_id[l]] += this._fenpei[l];
        }
        model.changbang = 0;
        model.lizhibang = 0;

        if (this._hule.length) {
            return this.delay(()=>this.hule());
        }
        else {
            if (this._lianzhuang) model.changbang = this._changbang + 1;
            return this.delay(()=>this.last(), 0);
        }
    }

    reply_pingju() {

        let model = this._model;

        for (let l = 0; l < 4; l++) {
            model.defen[model.player_id[l]] += this._fenpei[l];
        }
        model.changbang++;

        this.delay(()=>this.last(), 0);
    }

    get_dapai() {
        let model = this._model;
        return Game.get_dapai(this._rule, model.shoupai[model.lunban]);
    }

    get_chi_mianzi(l) {
        let model = this._model;
        let d = '_+=-'[(4 + model.lunban - l) % 4];
        return Game.get_chi_mianzi(this._rule, model.shoupai[l],
                                   this._dapai + d, model.shan.paishu);
    }

    get_peng_mianzi(l) {
        let model = this._model;
        let d = '_+=-'[(4 + model.lunban - l) % 4];
        return Game.get_peng_mianzi(this._rule, model.shoupai[l],
                                    this._dapai + d, model.shan.paishu);
    }

    get_gang_mianzi(l) {
        let model = this._model;
        if (l == null) {
            return Game.get_gang_mianzi(this._rule, model.shoupai[model.lunban],
                                        null, model.shan.paishu,
                                        this._n_gang.reduce((x, y)=> x + y));
        }
        else {
            let d = '_+=-'[(4 + model.lunban - l) % 4];
            return Game.get_gang_mianzi(this._rule, model.shoupai[l],
                                        this._dapai + d, model.shan.paishu,
                                        this._n_gang.reduce((x, y)=> x + y));
        }
    }

    allow_lizhi(p) {
        let model = this._model;
        return Game.allow_lizhi(this._rule, model.shoupai[model.lunban],
                                p, model.shan.paishu,
                                model.defen[model.player_id[model.lunban]]);
    }

    allow_hule(l) {
        let model = this._model;
        if (l == null) {
            let hupai = model.shoupai[model.lunban].lizhi
                     || this._status == 'gangzimo'
                     || model.shan.paishu == 0;
            return Game.allow_hule(this._rule,
                                   model.shoupai[model.lunban], null,
                                   model.zhuangfeng, model.lunban, hupai);
        }
        else {
            let p = (this._status == 'gang'
                        ? this._gang[0] + this._gang.slice(-1)
                        : this._dapai
                    ) + '_+=-'[(4 + model.lunban - l) % 4];
            let hupai = model.shoupai[l].lizhi
                     || this._status == 'gang'
                     || model.shan.paishu == 0;
            return Game.allow_hule(this._rule,
                                   model.shoupai[l], p,
                                   model.zhuangfeng, l, hupai,
                                   this._neng_rong[l]);
        }
    }

    allow_pingju() {
        let model = this._model;
        return Game.allow_pingju(this._rule, model.shoupai[model.lunban],
                                 this._diyizimo);
    }

    static get_dapai(rule, shoupai) {

        if (rule['喰い替え許可レベル'] == 0) return shoupai.get_dapai(true);
        if (rule['喰い替え許可レベル'] == 1
            && shoupai._zimo && shoupai._zimo.length > 2)
        {
            let deny = shoupai._zimo[0]
                     + (+shoupai._zimo.match(/\d(?=[\+\=\-])/)||5);
            return shoupai.get_dapai(false)
                                .filter(p => p.replace(/0/,'5') != deny);
        }
        return shoupai.get_dapai(false);
    }

    static get_chi_mianzi(rule, shoupai, p, paishu) {

        let mianzi = shoupai.get_chi_mianzi(p, rule['喰い替え許可レベル'] == 0);
        if (! mianzi) return mianzi;
        if (rule['喰い替え許可レベル'] == 1
            && shoupai._fulou.length == 3
            && shoupai._bingpai[p[0]][p[1]] == 2) mianzi = [];
        return paishu == 0 ? [] : mianzi;
    }

    static get_peng_mianzi(rule, shoupai, p, paishu) {

        let mianzi = shoupai.get_peng_mianzi(p);
        if (! mianzi) return mianzi;
        return paishu == 0 ? [] : mianzi;
    }

    static get_gang_mianzi(rule, shoupai, p, paishu, n_gang) {

        let mianzi = shoupai.get_gang_mianzi(p);
        if (! mianzi || mianzi.length == 0) return mianzi;

        if (shoupai.lizhi) {
            if (rule['リーチ後暗槓許可レベル'] == 0) return [];
            else if (rule['リーチ後暗槓許可レベル'] == 1) {
                let new_shoupai, n_hule1 = 0, n_hule2 = 0;
                new_shoupai = shoupai.clone().dapai(shoupai._zimo);
                for (let p of Majiang.Util.tingpai(new_shoupai)) {
                    n_hule1 += Majiang.Util.hule_mianzi(new_shoupai, p).length;
                }
                new_shoupai = shoupai.clone().gang(mianzi[0]);
                for (let p of Majiang.Util.tingpai(new_shoupai)) {
                    n_hule2 += Majiang.Util.hule_mianzi(new_shoupai, p).length;
                }
                if (n_hule1 > n_hule2) return [];
            }
            else {
                let new_shoupai;
                new_shoupai = shoupai.clone().dapai(shoupai._zimo);
                let n_tingpai1 = Majiang.Util.tingpai(new_shoupai).length;
                new_shoupai = shoupai.clone().gang(mianzi[0]);
                if (Majiang.Util.xiangting(new_shoupai) > 0) return [];
                let n_tingpai2 = Majiang.Util.tingpai(new_shoupai).length;
                if (n_tingpai1 > n_tingpai2) return [];
            }
        }
        return paishu == 0 || n_gang == 4 ? [] : mianzi;
    }

    static allow_lizhi(rule, shoupai, p, paishu, defen) {

        if (! shoupai._zimo)   return false;
        if (shoupai.lizhi)     return false;
        if (! shoupai.menqian) return false;

        if (! rule['ツモ番なしリーチあり'] && paishu < 4) return false;
        if (rule['トビ終了あり'] && defen < 1000)         return false;

        if (Majiang.Util.xiangting(shoupai) > 0) return false;

        if (p) {
            let new_shoupai = shoupai.clone().dapai(p);
            return Majiang.Util.xiangting(new_shoupai) == 0
                    && Majiang.Util.tingpai(new_shoupai).length > 0;
        }
        else {
            let dapai = [];
            for (let p of Game.get_dapai(rule, shoupai)) {
                let new_shoupai = shoupai.clone().dapai(p);
                if (Majiang.Util.xiangting(new_shoupai) == 0
                    && Majiang.Util.tingpai(new_shoupai).length > 0)
                {
                    dapai.push(p);
                }
            }
            return dapai.length ? dapai : false;
        }
    }

    static allow_hule(rule, shoupai, p, zhuangfeng, menfeng, hupai, neng_rong) {

        if (p && ! neng_rong) return false;

        let new_shoupai = shoupai.clone();
        if (p) new_shoupai.zimo(p);
        if (Majiang.Util.xiangting(new_shoupai) != -1) return false;

        if (hupai) return true;

        let param = {
            rule:       rule,
            zhuangfeng: zhuangfeng,
            menfeng:    menfeng,
            hupai:      {},
            baopai:     [],
            jicun:      { changbang: 0, lizhibang: 0 }
        };
        let hule = Majiang.Util.hule(shoupai, p, param);

        return hule.hupai != null;
    }

    static allow_pingju(rule, shoupai, diyizimo) {

        if (! (diyizimo && shoupai._zimo)) return false;
        if (! rule['途中流局あり']) return false;

        let n_yaojiu = 0;
        for (let s of ['m','p','s','z']) {
            let bingpai = shoupai._bingpai[s];
            let nn = (s == 'z') ? [1,2,3,4,5,6,7] : [1,9];
            for (let n of nn) {
                if (bingpai[n] > 0) n_yaojiu++;
            }
        }
        return n_yaojiu >= 9;
    }

    static allow_no_daopai(rule, shoupai, paishu) {

        if (paishu > 0 || shoupai._zimo) return false;
        if (! rule['ノーテン宣言あり']) return false;
        if (shoupai.lizhi) return false;

        return Majiang.Util.xiangting(shoupai) == 0
                && Majiang.Util.tingpai(shoupai).length > 0;
    }
}

},
"./he":function(module,exports,require){
/*
 *  Majiang.He
 */
"use strict";

const Majiang = { Shoupai: require('./shoupai') };

module.exports = class He {

    constructor() {
        this._pai  = [];
        this._find = {};
    }

    dapai(p) {
        if (! Majiang.Shoupai.valid_pai(p))         throw new Error(p);
        this._pai.push(p.replace(/[\+\=\-]$/,''));
        this._find[p[0]+(+p[1]||5)] = true;
        return this;
    }

    fulou(m) {
        if (! Majiang.Shoupai.valid_mianzi(m))      throw new Error(m);
        let p = m[0] + m.match(/\d(?=[\+\=\-])/), d = m.match(/[\+\=\-]/);
        if (! d)                                    throw new Error(m);
        if (this._pai[this._pai.length - 1].slice(0,2) != p)
                                                    throw new Error(m);
        this._pai[this._pai.length - 1] += d;
        return this;
    }

    find(p) {
        return this._find[p[0]+(+p[1]||5)];
    }
}

},
"./hule":function(module,exports,require){
/*
 *  Majiang.Util.hule
 */
"use strict";

const Majiang = {
    Shan: require('./shan'),
    rule: require('./rule')
};

function mianzi(s, bingpai, n = 1) {

    if (n > 9) return [[]];

    if (bingpai[n] == 0) return mianzi(s, bingpai, n+1);

    let shunzi = [];
    if (n <= 7 && bingpai[n] > 0 && bingpai[n+1] > 0 && bingpai[n+2] > 0) {
        bingpai[n]--; bingpai[n+1]--; bingpai[n+2]--;
        shunzi = mianzi(s, bingpai, n);
        bingpai[n]++; bingpai[n+1]++; bingpai[n+2]++;
        for (let s_mianzi of shunzi) {
            s_mianzi.unshift(s+(n)+(n+1)+(n+2));
        }
    }

    let kezi = [];
    if (bingpai[n] == 3) {
        bingpai[n] -= 3;
        kezi = mianzi(s, bingpai, n+1);
        bingpai[n] += 3;
        for (let k_mianzi of kezi) {
            k_mianzi.unshift(s+n+n+n);
        }
    }

    return shunzi.concat(kezi);
}

function mianzi_all(shoupai) {

    let shupai_all = [[]];
    for (let s of ['m','p','s']) {
        let new_mianzi = [];
        for (let mm of shupai_all) {
            for (let nn of mianzi(s, shoupai._bingpai[s])) {
                new_mianzi.push(mm.concat(nn));
            }
        }
        shupai_all = new_mianzi;
    }

    let zipai = [];
    for (let n = 1; n <= 7; n++) {
        if (shoupai._bingpai.z[n] == 0) continue;
        if (shoupai._bingpai.z[n] != 3) return [];
        zipai.push('z'+n+n+n);
    }

    let fulou = shoupai._fulou.map(m => m.replace(/0/g,'5'));

    return shupai_all.map(shupai => shupai.concat(zipai).concat(fulou));
}

function add_hulepai(mianzi, p) {

    let [s, n, d] = p;
    let regexp   = new RegExp(`^(${s}.*${n})`);
    let replacer = `$1${d}!`;

    let new_mianzi = [];

    for (let i = 0; i < mianzi.length; i++) {
        if (mianzi[i].match(/[\+\=\-]|\d{4}/)) continue;
        if (i > 0 && mianzi[i] == mianzi[i-1]) continue;
        let m = mianzi[i].replace(regexp, replacer);
        if (m == mianzi[i]) continue;
        let tmp_mianzi = mianzi.concat();
        tmp_mianzi[i] = m;
        new_mianzi.push(tmp_mianzi);
    }

    return new_mianzi;
}

function hule_mianzi_yiban(shoupai, hulepai) {

    let mianzi = [];

    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        for (let n = 1; n < bingpai.length; n++) {
            if (bingpai[n] < 2) continue;
            bingpai[n] -= 2;
            let jiangpai = s+n+n;
            for (let mm of mianzi_all(shoupai)) {
                mm.unshift(jiangpai);
                if (mm.length != 5) continue;
                mianzi = mianzi.concat(add_hulepai(mm, hulepai));
            }
            bingpai[n] += 2;
        }
    }

    return mianzi;
}

function hule_mianzi_qidui(shoupai, hulepai) {

    if (shoupai._fulou.length > 0) return [];

    let mianzi = [];

    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        for (let n = 1; n < bingpai.length; n++) {
            if (bingpai[n] == 0) continue;
            if (bingpai[n] == 2) {
                let m = (s+n == hulepai.slice(0,2))
                            ? s+n+n + hulepai[2] + '!'
                            : s+n+n;
                mianzi.push(m);
            }
            else return [];
        }
    }

    return (mianzi.length == 7) ? [ mianzi ] : [];
}

function hule_mianzi_guoshi(shoupai, hulepai) {

    if (shoupai._fulou.length > 0) return [];

    let mianzi = [];
    let n_duizi = 0;

    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        let nn = (s == 'z') ? [1,2,3,4,5,6,7] :[1,9];
        for (let n of nn) {
            if (bingpai[n] == 2) {
                let m = (s+n == hulepai.slice(0,2))
                            ? s+n+n + hulepai[2] + '!'
                            : s+n+n;
                mianzi.unshift(m);
                n_duizi++;
            }
            else if (bingpai[n] == 1) {
                let m = (s+n == hulepai.slice(0,2))
                            ? s+n + hulepai[2] + '!'
                            : s+n;
                mianzi.push(m);
            }
            else return [];
        }
    }

    return (n_duizi == 1) ? [ mianzi ] : [];
}

function hule_mianzi_jiulian(shoupai, hulepai) {

    if (shoupai._fulou.length > 0) return [];

    let s = hulepai[0];
    if (s == 'z') return [];

    let mianzi = s;
    let bingpai = shoupai._bingpai[s];
    for (let n = 1; n <= 9; n++) {
        if (bingpai[n] == 0) return [];
        if ((n == 1 || n == 9) && bingpai[n] < 3) return [];
        let n_pai = (n == hulepai[1]) ? bingpai[n] - 1 : bingpai[n];
        for (let i = 0; i < n_pai; i++) {
            mianzi += n;
        }
    }
    if (mianzi.length != 14) return [];
    mianzi += hulepai.slice(1) + '!';

    return [ [mianzi] ];
}

function hule_mianzi(shoupai, rongpai) {

    let new_shoupai = shoupai.clone();
    if (rongpai) new_shoupai.zimo(rongpai);

    if (! new_shoupai._zimo || new_shoupai._zimo.length > 2) return [];
    let hulepai = (rongpai || new_shoupai._zimo + '_').replace(/0/,'5');

    return [].concat(hule_mianzi_yiban(new_shoupai, hulepai))
             .concat(hule_mianzi_qidui(new_shoupai, hulepai))
             .concat(hule_mianzi_guoshi(new_shoupai, hulepai))
             .concat(hule_mianzi_jiulian(new_shoupai, hulepai));
}

function get_hudi(mianzi, zhuangfeng, menfeng, rule) {

    const zhuangfengpai = new RegExp(`^z${zhuangfeng+1}.*$`);
    const menfengpai    = new RegExp(`^z${menfeng+1}.*$`);
    const sanyuanpai    = /^z[567].*$/;

    const yaojiu        = /^.*[z19].*$/;
    const zipai         = /^z.*$/;

    const kezi          = /^[mpsz](\d)\1\1.*$/;
    const ankezi        = /^[mpsz](\d)\1\1(?:\1|_\!)?$/;
    const gangzi        = /^[mpsz](\d)\1\1.*\1.*$/;

    const danqi         = /^[mpsz](\d)\1[\+\=\-\_]\!$/;
    const kanzhang      = /^[mps]\d\d[\+\=\-\_]\!\d$/;
    const bianzhang     = /^[mps](123[\+\=\-\_]\!|7[\+\=\-\_]\!89)$/;

    let hudi = {
        fu:         20,
        menqian:    true,
        zimo:       true,
        shunzi:     { m: [0,0,0,0,0,0,0,0],
                      p: [0,0,0,0,0,0,0,0],
                      s: [0,0,0,0,0,0,0,0]  },
        kezi:       { m: [0,0,0,0,0,0,0,0,0,0],
                      p: [0,0,0,0,0,0,0,0,0,0],
                      s: [0,0,0,0,0,0,0,0,0,0],
                      z: [0,0,0,0,0,0,0,0]      },
        n_shunzi:   0,
        n_kezi:     0,
        n_ankezi:   0,
        n_gangzi:   0,
        n_yaojiu:   0,
        n_zipai:    0,
        danqi:      false,
        pinghu:     false,
        zhuangfeng: zhuangfeng,
        menfeng:    menfeng
    };

    for (let m of mianzi) {

        if (m.match(/[\+\=\-](?!\!)/))  hudi.menqian = false;
        if (m.match(/[\+\=\-]\!/))      hudi.zimo    = false;

        if (mianzi.length == 1) continue;

        if (m.match(danqi))             hudi.danqi   = true;

        if (mianzi.length == 13) continue;

        if (m.match(yaojiu))            hudi.n_yaojiu++;
        if (m.match(zipai))             hudi.n_zipai++;

        if (mianzi.length != 5) continue;

        if (m == mianzi[0]) {
            let fu = 0;
            if (m.match(zhuangfengpai)) fu += 2;
            if (m.match(menfengpai))    fu += 2;
            if (m.match(sanyuanpai))    fu += 2;
            fu = rule['連風牌は2符'] && fu > 2 ? 2 : fu;
            hudi.fu += fu;
            if (hudi.danqi)             hudi.fu += 2;
        }
        else if (m.match(kezi)) {
            hudi.n_kezi++;
            let fu = 2;
            if (m.match(yaojiu)) { fu *= 2;                  }
            if (m.match(ankezi)) { fu *= 2; hudi.n_ankezi++; }
            if (m.match(gangzi)) { fu *= 4; hudi.n_gangzi++; }
            hudi.fu += fu;
            hudi.kezi[m[0]][m[1]]++;
        }
        else {
            hudi.n_shunzi++;
            if (m.match(kanzhang))  hudi.fu += 2;
            if (m.match(bianzhang)) hudi.fu += 2;
            hudi.shunzi[m[0]][m[1]]++;
        }
    }

    if (mianzi.length == 7) {
        hudi.fu = 25;
    }
    else if (mianzi.length == 5) {
        hudi.pinghu = (hudi.menqian && hudi.fu == 20);
        if (hudi.zimo) {
            if (! hudi.pinghu)      hudi.fu +=  2;
        }
        else {
            if (hudi.menqian)       hudi.fu += 10;
            else if (hudi.fu == 20) hudi.fu  = 30;
        }
        hudi.fu = Math.ceil(hudi.fu / 10) * 10;
    }

    return hudi;
}

function get_pre_hupai(hupai) {

    let pre_hupai = [];

    if (hupai.lizhi == 1)   pre_hupai.push({ name: '立直', fanshu: 1 });
    if (hupai.lizhi == 2)   pre_hupai.push({ name: 'ダブル立直', fanshu: 2 });
    if (hupai.yifa)         pre_hupai.push({ name: '一発', fanshu: 1 });
    if (hupai.haidi == 1)   pre_hupai.push({ name: '海底摸月', fanshu: 1 });
    if (hupai.haidi == 2)   pre_hupai.push({ name: '河底撈魚', fanshu: 1 });
    if (hupai.lingshang)    pre_hupai.push({ name: '嶺上開花', fanshu: 1 });
    if (hupai.qianggang)    pre_hupai.push({ name: '槍槓', fanshu: 1 });

    if (hupai.tianhu == 1)  pre_hupai = [{ name: '天和', fanshu: '*' }];
    if (hupai.tianhu == 2)  pre_hupai = [{ name: '地和', fanshu: '*' }];

    return pre_hupai;
}

function get_hupai(mianzi, hudi, pre_hupai, post_hupai, rule) {

    function menqianqing() {
        if (hudi.menqian && hudi.zimo)
                return [{ name: '門前清自摸和', fanshu: 1 }];
        return [];
    }
    function fanpai() {
        let feng_hanzi = ['東','南','西','北'];
        let fanpai_all = [];
        if (hudi.kezi.z[hudi.zhuangfeng+1])
                fanpai_all.push({ name: '場風 '+feng_hanzi[hudi.zhuangfeng],
                                  fanshu: 1 });
        if (hudi.kezi.z[hudi.menfeng+1])
                fanpai_all.push({ name: '自風 '+feng_hanzi[hudi.menfeng],
                                  fanshu: 1 });
        if (hudi.kezi.z[5]) fanpai_all.push({ name: '翻牌 白', fanshu: 1 });
        if (hudi.kezi.z[6]) fanpai_all.push({ name: '翻牌 發', fanshu: 1 });
        if (hudi.kezi.z[7]) fanpai_all.push({ name: '翻牌 中', fanshu: 1 });
        return fanpai_all;
    }
    function pinghu() {
        if (hudi.pinghu)        return [{ name: '平和', fanshu: 1 }];
        return [];
    }
    function duanyaojiu() {
        if (hudi.n_yaojiu > 0)  return [];
        if (rule['クイタンあり'] || hudi.menqian)
                                return [{ name: '断幺九', fanshu: 1 }];
        return [];
    }
    function yibeikou() {
        if (! hudi.menqian)     return [];
        const shunzi = hudi.shunzi;
        let beikou = shunzi.m.concat(shunzi.p).concat(shunzi.s)
                        .map(x=>x>>1).reduce((a,b)=>a+b);
        if (beikou == 1)        return [{ name: '一盃口', fanshu: 1 }];
        return [];
    }
    function sansetongshun() {
        const shunzi = hudi.shunzi;
        for (let n = 1; n <= 7; n++) {
            if (shunzi.m[n] && shunzi.p[n] && shunzi.s[n])
                return [{ name: '三色同順', fanshu: (hudi.menqian ? 2 : 1) }];
        }
        return [];
    }
    function yiqitongguan() {
        const shunzi = hudi.shunzi;
        for (let s of ['m','p','s']) {
            if (shunzi[s][1] && shunzi[s][4] && shunzi[s][7])
                return [{ name: '一気通貫', fanshu: (hudi.menqian ? 2 : 1) }];
        }
        return [];
    }
    function hunquandaiyaojiu() {
        if (hudi.n_yaojiu == 5 && hudi.n_shunzi > 0 && hudi.n_zipai > 0)
                return [{ name: '混全帯幺九', fanshu: (hudi.menqian ? 2 : 1) }];
        return [];
    }
    function qiduizi() {
        if (mianzi.length == 7) return [{ name: '七対子', fanshu: 2 }];
        return [];
    }
    function duiduihu() {
        if (hudi.n_kezi == 4)   return [{ name: '対々和', fanshu: 2 }];
        return [];
    }
    function sananke() {
        if (hudi.n_ankezi == 3) return [{ name: '三暗刻', fanshu: 2 }];
        return [];
    }
    function sangangzi() {
        if (hudi.n_gangzi == 3) return [{ name: '三槓子', fanshu: 2 }];
        return [];
    }
    function sansetongke() {
        const kezi = hudi.kezi;
        for (let n = 1; n <= 9; n++) {
            if (kezi.m[n] && kezi.p[n] && kezi.s[n])
                                return [{ name: '三色同刻', fanshu: 2 }];
        }
        return [];
    }
    function hunlaotou() {
        if (hudi.n_yaojiu == mianzi.length
                && hudi.n_shunzi == 0 && hudi.n_zipai > 0)
                                return [{ name: '混老頭', fanshu: 2 }];
        return [];
    }
    function xiaosanyuan() {
        const kezi = hudi.kezi;
        if (kezi.z[5] + kezi.z[6] + kezi.z[7] == 2
                && mianzi[0].match(/^z[567]/))
                                return [{ name: '小三元', fanshu: 2 }];
        return [];
    }
    function hunyise() {
        for (let s of ['m','p','s']) {
            const yise = new RegExp(`^[z${s}]`);
            if (mianzi.filter(m=>m.match(yise)).length == mianzi.length
                    && hudi.n_zipai > 0)
                    return [{ name: '混一色', fanshu: (hudi.menqian ? 3 : 2) }];
        }
        return [];
    }
    function chunquandaiyaojiu() {
        if (hudi.n_yaojiu == 5 && hudi.n_shunzi > 0 && hudi.n_zipai == 0)
                return [{ name: '純全帯幺九', fanshu: (hudi.menqian ? 3 : 2) }];
        return [];
    }
    function erbeikou() {
        if (! hudi.menqian)     return [];
        const shunzi = hudi.shunzi;
        let beikou = shunzi.m.concat(shunzi.p).concat(shunzi.s)
                        .map(x=>x>>1).reduce((a,b)=>a+b);
        if (beikou == 2)        return [{ name: '二盃口', fanshu: 3 }];
        return [];
    }
    function qingyise() {
        for (let s of ['m','p','s']) {
            const yise = new RegExp(`^[${s}]`);
            if (mianzi.filter(m=>m.match(yise)).length == mianzi.length)
                    return [{ name: '清一色', fanshu: (hudi.menqian ? 6 : 5) }];
        }
        return [];
    }

    function guoshiwushuang() {
        if (mianzi.length != 13)    return [];
        if (hudi.danqi)         return [{ name: '国士無双十三面', fanshu: '**' }];
        else                    return [{ name: '国士無双', fanshu: '*' }];
    }
    function sianke() {
        if (hudi.n_ankezi != 4)     return [];
        if (hudi.danqi)         return [{ name: '四暗刻単騎', fanshu: '**' }];
        else                    return [{ name: '四暗刻', fanshu: '*' }];
    }
    function dasanyuan() {
        const kezi = hudi.kezi;
        if (kezi.z[5] + kezi.z[6] + kezi.z[7] == 3) {
            let bao_mianzi = mianzi.filter(m =>
                                m.match(/^z([567])\1\1(?:[\+\=\-]|\1)(?!\!)/));
            let baojia = (bao_mianzi[2] && bao_mianzi[2].match(/[\+\=\-]/));
            if (baojia)
                    return [{ name: '大三元', fanshu: '*', baojia: baojia[0]}];
            else    return [{ name: '大三元', fanshu: '*'}];
        }
        return [];
    }
    function sixihu() {
        const kezi = hudi.kezi;
        if (kezi.z[1] + kezi.z[2] + kezi.z[3] + kezi.z[4] == 4) {
            let bao_mianzi = mianzi.filter(m =>
                                m.match(/^z([1234])\1\1(?:[\+\=\-]|\1)(?!\!)/));
            let baojia = (bao_mianzi[3] && bao_mianzi[3].match(/[\+\=\-]/));
            if (baojia)
                    return [{name: '大四喜', fanshu: '**', baojia: baojia[0]}];
            else    return [{name: '大四喜', fanshu: '**'}];
        }
        if (kezi.z[1] + kezi.z[2] + kezi.z[3] + kezi.z[4] == 3
            && mianzi[0].match(/^z[1234]/))
                                return [{ name: '小四喜', fanshu: '*' }];
        return [];
    }
    function ziyise() {
        if (hudi.n_zipai == mianzi.length)
                                return [{ name: '字一色', fanshu: '*' }];
        return [];
    }
    function lvyise() {
        if (mianzi.filter(m => m.match(/^[mp]/)).length > 0)      return [];
        if (mianzi.filter(m => m.match(/^z[^6]/)).length > 0)     return [];
        if (mianzi.filter(m => m.match(/^s.*[1579]/)).length > 0) return [];
        return [{ name: '緑一色', fanshu: '*' }];
    }
    function qinglaotou() {
        if (hudi.n_yaojiu == 5 && hudi.n_kezi == 4 && hudi.n_zipai == 0)
                                return [{ name: '清老頭', fanshu: '*' }];
        return [];
    }
    function sigangzi() {
        if (hudi.n_gangzi == 4) return [{ name: '四槓子', fanshu: '*' }];
        return [];
    }
    function jiulianbaodeng() {
        if (mianzi.length != 1)     return [];
        if (mianzi[0].match(/^[mpsz]1112345678999/))
                                return [{ name: '純正九蓮宝燈', fanshu: '**' }];
        else                    return [{ name: '九蓮宝燈', fanshu: '*' }];
    }

    let damanguan = (pre_hupai.length > 0 && pre_hupai[0].fanshu[0] == '*')
                        ? pre_hupai : [];
    damanguan = damanguan
                .concat(guoshiwushuang())
                .concat(sianke())
                .concat(dasanyuan())
                .concat(sixihu())
                .concat(ziyise())
                .concat(lvyise())
                .concat(qinglaotou())
                .concat(sigangzi())
                .concat(jiulianbaodeng());

    for (let hupai of damanguan) {
        if (! rule['ダブル役満あり']) hupai.fanshu = '*';
        if (! rule['役満パオあり']) delete hupai.baojia;
    }
    if (damanguan.length > 0) return damanguan;

    let hupai = pre_hupai
                .concat(menqianqing())
                .concat(fanpai())
                .concat(pinghu())
                .concat(duanyaojiu())
                .concat(yibeikou())
                .concat(sansetongshun())
                .concat(yiqitongguan())
                .concat(hunquandaiyaojiu())
                .concat(qiduizi())
                .concat(duiduihu())
                .concat(sananke())
                .concat(sangangzi())
                .concat(sansetongke())
                .concat(hunlaotou())
                .concat(xiaosanyuan())
                .concat(hunyise())
                .concat(chunquandaiyaojiu())
                .concat(erbeikou())
                .concat(qingyise());

    if (hupai.length > 0) hupai = hupai.concat(post_hupai);

    return hupai;
}

function get_post_hupai(shoupai, rongpai, baopai, fubaopai) {

    let new_shoupai = shoupai.clone();
    if (rongpai) new_shoupai.zimo(rongpai);
    let paistr = new_shoupai.toString();

    let post_hupai = [];

    let suitstr = paistr.match(/[mpsz][^mpsz,]*/g);

    let n_baopai = 0;
    for (let p of baopai) {
        p = Majiang.Shan.zhenbaopai(p);
        const regexp = new RegExp(p[1],'g');
        for (let m of suitstr) {
            if (m[0] != p[0]) continue;
            m = m.replace(/0/,'5');
            let nn = m.match(regexp);
            if (nn) n_baopai += nn.length;
        }
    }
    if (n_baopai) post_hupai.push({ name: 'ドラ', fanshu: n_baopai });

    let n_hongpai = 0;
    let nn = paistr.match(/0/g);
    if (nn) n_hongpai = nn.length;
    if (n_hongpai) post_hupai.push({ name: '赤ドラ', fanshu: n_hongpai });

    let n_fubaopai = 0;
    for (let p of fubaopai || []) {
        p = Majiang.Shan.zhenbaopai(p);
        const regexp = new RegExp(p[1],'g');
        for (let m of suitstr) {
            if (m[0] != p[0]) continue;
            m = m.replace(/0/,'5');
            let nn = m.match(regexp);
            if (nn) n_fubaopai += nn.length;
        }
    }
    if (n_fubaopai) post_hupai.push({ name: '裏ドラ', fanshu: n_fubaopai });

    return post_hupai;
}

function get_defen(fu, hupai, rongpai, param) {

    if (hupai.length == 0) return { defen: 0 };

    let menfeng = param.menfeng;
    let fanshu, damanguan, defen, base, baojia, defen2, base2, baojia2;

    if (hupai[0].fanshu[0] == '*') {
        fu = undefined;
        damanguan = ! param.rule['役満の複合あり'] ? 1
                  : hupai.map(h => h.fanshu.length).reduce((x, y) => x + y);
        base      = 8000 * damanguan;

        let h = hupai.find(h => h.baojia);
        if (h) {
            baojia2 = (menfeng + { '+': 1, '=': 2, '-': 3}[h.baojia]) % 4;
            base2   = 8000 * Math.min(h.fanshu.length, damanguan);
        }
    }
    else {
        fanshu = hupai.map(h => h.fanshu).reduce((x, y) => x + y);
        base   = (fanshu >= 13 && param.rule['数え役満あり'])
                                ? 8000
               : (fanshu >= 11) ? 6000
               : (fanshu >=  8) ? 4000
               : (fanshu >=  6) ? 3000
               : param.rule['切り上げ満貫あり'] && fu << (2 + fanshu) == 1920
                    ? 2000
                    : Math.min(fu << (2 + fanshu), 2000);
    }

    let fenpei  = [ 0, 0, 0, 0 ];
    let chang = param.jicun.changbang;
    let lizhi = param.jicun.lizhibang;

    if (baojia2 != null) {
        if (rongpai) base2 = base2 / 2;
        base   = base - base2;
        defen2 = base2 * (menfeng == 0 ? 6 : 4);
        fenpei[menfeng] += defen2;
        fenpei[baojia2] -= defen2;
    }
    else defen2 = 0;

    if (rongpai || base == 0) {
        baojia = (base == 0)
                    ? baojia2
                    : (menfeng + { '+': 1, '=': 2, '-': 3}[rongpai[2]]) % 4;
        defen  = Math.ceil(base * (menfeng == 0 ? 6 : 4) / 100) * 100;
        fenpei[menfeng] += defen + chang * 300 + lizhi * 1000;
        fenpei[baojia]  -= defen + chang * 300;
    }
    else {
        let zhuangjia = Math.ceil(base * 2 / 100) * 100;
        let sanjia    = Math.ceil(base     / 100) * 100;
        if (menfeng == 0) {
            defen = zhuangjia * 3;
            for (let l = 0; l < 4; l++) {
                if (l == menfeng)
                        fenpei[l] += defen     + chang * 300 + lizhi * 1000;
                else    fenpei[l] -= zhuangjia + chang * 100;
            }
        }
        else {
            defen = zhuangjia + sanjia * 2;
            for (let l = 0; l < 4; l++) {
                if (l == menfeng)
                        fenpei[l] += defen     + chang * 300 + lizhi * 1000;
                else if (l == 0)
                        fenpei[l] -= zhuangjia + chang * 100;
                else    fenpei[l] -= sanjia    + chang * 100;
            }
        }
    }

    return {
        hupai:      hupai,
        fu:         fu,
        fanshu:     fanshu,
        damanguan:  damanguan,
        defen:      defen + defen2,
        fenpei:     fenpei
    };
}

function hule(shoupai, rongpai, param) {

    if (rongpai) {
        if (! rongpai.match(/[\+\=\-]$/)) throw new Error(rongpai);
        rongpai = rongpai.slice(0,2) + rongpai.slice(-1);
    }

    let max;

    let pre_hupai  = get_pre_hupai(param.hupai);
    let post_hupai = get_post_hupai(shoupai, rongpai,
                                    param.baopai, param.fubaopai);

    if (param.nuki) post_hupai.push({name:'拔北宝牌', fanshu:param.nuki});
    for (let mianzi of hule_mianzi(shoupai, rongpai)) {

        let hudi  = get_hudi(mianzi, param.zhuangfeng, param.menfeng,
                             param.rule);
        let hupai = get_hupai(mianzi, hudi, pre_hupai, post_hupai, param.rule);
        let rv    = get_defen(hudi.fu, hupai, rongpai, param);

        if (! max || rv.defen > max.defen
            || rv.defen == max.defen
                && (! rv.fanshu || rv.fanshu > max.fanshu
                    || rv.fanshu == max.fanshu && rv.fu > max.fu)) max = rv;
    }

    return max;
}

function hule_param(param = {}) {

    let rv = {
        rule:           param.rule       ?? Majiang.rule(),
        zhuangfeng:     param.zhuangfeng ?? 0,
        menfeng:        param.menfeng    ?? 1,
        hupai: {
            lizhi:      param.lizhi      ?? 0,
            yifa:       param.yifa       ?? false,
            qianggang:  param.qianggang  ?? false,
            lingshang:  param.lingshang  ?? false,
            haidi:      param.haidi      ?? 0,
            tianhu:     param.tianhu     ?? 0
        },
        baopai:         param.baopai   ? [].concat(param.baopai)   : [],
        fubaopai:       param.fubaopai ? [].concat(param.fubaopai) : null,
        jicun: {
            changbang:  param.changbang  ?? 0,
            lizhibang:  param.lizhibang  ?? 0
        }
    };

    return rv;
}

module.exports = {
    hule:        hule,
    hule_param:  hule_param,
    hule_mianzi: hule_mianzi,
};

},
"./index":function(module,exports,require){
/*!
 *  @kobalab/majiang-core v1.4.1
 *
 *  Copyright(C) 2021 Satoshi Kobayashi
 *  Released under the MIT license
 *  https://github.com/kobalab/majiang-core/blob/master/LICENSE
 */

"use strict";

module.exports = {
    rule:    require('./rule'),
    Shoupai: require('./shoupai'),
    Shan:    require('./shan'),
    He:      require('./he'),
    Board:   require('./board'),
    Game:    require('./game'),
    Player:  require('./player'),
    Util:    Object.assign(require('./xiangting'),
                           require('./hule'))
}

},
"./player":function(module,exports,require){
/*
 *  Majiang.Player
 */
"use strict";

const Majiang = {
    Shoupai: require('./shoupai'),
    He:      require('./he'),
    Game:    require('./game'),
    Board:   require('./board'),
    Util:    Object.assign(require('./xiangting'),
                           require('./hule'))
};

module.exports = class Player {

    constructor() {
        this._model = new Majiang.Board();
    }

    action(msg, callback) {

        this._callback = callback;

        if      (msg.kaiju)    this.kaiju  (msg.kaiju);
        else if (msg.qipai)    this.qipai  (msg.qipai);
        else if (msg.zimo)     this.zimo   (msg.zimo);
        else if (msg.dapai)    this.dapai  (msg.dapai);
        else if (msg.fulou)    this.fulou  (msg.fulou);
        else if (msg.gang)     this.gang   (msg.gang);
        else if (msg.gangzimo) this.zimo   (msg.gangzimo, true)
        else if (msg.kaigang)  this.kaigang(msg.kaigang);
        else if (msg.hule)     this.hule   (msg.hule);
        else if (msg.pingju)   this.pingju (msg.pingju);
        else if (msg.jieju)    this.jieju  (msg.jieju);
    }

    get shoupai() { return this._model.shoupai[this._menfeng] }
    get he()      { return this._model.he[this._menfeng]      }
    get shan()    { return this._model.shan                   }
    get hulepai() {
        return Majiang.Util.xiangting(this.shoupai) == 0
                && Majiang.Util.tingpai(this.shoupai)
            || [];
    }
    get model()    { return this._model  }
    set view(view) { this._view = view   }
    get view()     { return this._view   }

    kaiju(kaiju) {
        this._id   = kaiju.id;
        this._rule = kaiju.rule;
        this._model.kaiju(kaiju);
        if (this._view) this._view.kaiju(kaiju.id);

        if (this._callback) this.action_kaiju(kaiju);
    }

    qipai(qipai) {
        this._model.qipai(qipai);
        this._menfeng   = this._model.menfeng(this._id);
        this._diyizimo  = true;
        this._n_gang    = 0;
        this._neng_rong = true;
        if (this._view) this._view.redraw();

        if (this._callback) this.action_qipai(qipai);
    }

    zimo(zimo, gangzimo) {
        this._model.zimo(zimo);
        if (gangzimo) this._n_gang++;
        if (this._view) {
            if (gangzimo) this._view.update({ gangzimo: zimo });
            else          this._view.update({ zimo: zimo });
        }

        if (this._callback) this.action_zimo(zimo, gangzimo);
    }

    dapai(dapai) {

        if (dapai.l == this._menfeng) {
            if (! this.shoupai.lizhi) this._neng_rong = true;
        }

        this._model.dapai(dapai);
        if (this._view) this._view.update({ dapai: dapai });

        if (this._callback) this.action_dapai(dapai);

        if (dapai.l == this._menfeng) {
            this._diyizimo = false;
            if (this.hulepai.find(p=> this.he.find(p))) this._neng_rong = false;
        }
        else {
            let s = dapai.p[0], n = +dapai.p[1]||5;
            if (this.hulepai.find(p=> p == s+n)) this._neng_rong = false;
        }
    }

    fulou(fulou) {
        this._model.fulou(fulou);
        if (this._view) this._view.update({ fulou: fulou });

        if (this._callback) this.action_fulou(fulou);

        this._diyizimo = false;
    }

    gang(gang) {
        this._model.gang(gang);
        if (this._view) this._view.update({ gang: gang });

        if (this._callback) this.action_gang(gang);

        this._diyizimo = false;
        if (gang.l != this._menfeng && ! gang.m.match(/^[mpsz]\d{4}$/)) {
            let s = gang.m[0], n = +gang.m.slice(-1)||5;
            if (this.hulepai.find(p=> p == s+n)) this._neng_rong = false;
        }
    }

    kaigang(kaigang) {
        this._model.kaigang(kaigang);
        if (this._view) this._view.update({ kaigang: kaigang });
    }

    hule(hule) {
        this._model.hule(hule);
        if (this._view) this._view.update({ hule: hule });
        if (this._callback) this.action_hule(hule);
    }

    pingju(pingju) {
        this._model.pingju(pingju);
        if (this._view) this._view.update({ pingju: pingju });
        if (this._callback) this.action_pingju(pingju);
    }

    jieju(paipu) {
        this._model.jieju(paipu);
        this._paipu = paipu;
        if (this._view) this._view.summary(paipu);
        if (this._callback) this.action_jieju(paipu);
    }

    get_dapai(shoupai) {
        return Majiang.Game.get_dapai(this._rule, shoupai);
    }
    get_chi_mianzi(shoupai, p) {
        return Majiang.Game.get_chi_mianzi(this._rule, shoupai, p,
                                           this.shan.paishu);
    }
    get_peng_mianzi(shoupai, p) {
        return Majiang.Game.get_peng_mianzi(this._rule, shoupai, p,
                                            this.shan.paishu);
    }
    get_gang_mianzi(shoupai, p) {
        return Majiang.Game.get_gang_mianzi(this._rule, shoupai, p,
                                            this.shan.paishu, this._n_gang);
    }
    allow_lizhi(shoupai, p) {
        return Majiang.Game.allow_lizhi(this._rule, shoupai, p,
                                        this.shan.paishu,
                                        this._model.defen[this._id]);
    }
    allow_hule(shoupai, p, hupai) {
        hupai = hupai || shoupai.lizhi || this.shan.paishu == 0;
        return Majiang.Game.allow_hule(this._rule, shoupai, p,
                                       this._model.zhuangfeng, this._menfeng,
                                       hupai, this._neng_rong);
    }
    allow_pingju(shoupai) {
        return Majiang.Game.allow_pingju(this._rule, shoupai,
                                         this._diyizimo);
    }
    allow_no_daopai(shoupai) {
        return Majiang.Game.allow_no_daopai(this._rule, shoupai,
                                            this.shan.paishu);
    }
}

},
"./rule":function(module,exports,require){
/*
 *  Majinng.rule
 */
"use strict";

module.exports = function(param = {}) {

    let rule = {
        /* 点数関連 */
        '配給原点': 25000,
        '順位点':   ['20.0','10.0','-10.0','-20.0'],
        '連風牌は2符': false,

        /* 赤牌有無/クイタンなど */
        '赤牌':         { m: 1, p: 1, s: 1 },
        'クイタンあり': true,
        '喰い替え許可レベル': 0,
            // 0: 喰い替えなし, 1: スジ喰い替えあり,  2: 現物喰い替えもあり

        /* 局数関連 */
        '場数':             2,
            // 0: 一局戦, 1: 東風戦, 2： 東南戦, 4: 一荘戦
        '途中流局あり':     true,
        '流し満貫あり':     true,
        'ノーテン宣言あり': false,
        'ノーテン罰あり':   true,
        '最大同時和了数': 2,
            // 1: 頭ハネ, 2: ダブロンあり, 3: トリロンあり
        '連荘方式':         2,
            // 0: 連荘なし, 1: 和了連荘, 2: テンパイ連荘, 3: ノーテン連荘
        'トビ終了あり':     true,
        'オーラス止めあり': true,
        '延長戦方式':       1,
            // 0: 延長戦なし, 1: サドンデス, 2: 連荘優先サドンデス, 3: 4局固定

        /* リーチ/ドラ関連 */
        '一発あり':         true,
        '裏ドラあり':       true,
        'カンドラあり':     true,
        'カン裏あり':       true,
        'カンドラ後乗せ':   true,
        'ツモ番なしリーチあり':   false,
        'リーチ後暗槓許可レベル': 2,
            // 0: 暗槓不可, 1: 牌姿の変わる暗槓不可, 2： 待ちの変わる暗槓不可

        /* 役満関連 */
        '役満の複合あり':   true,
        'ダブル役満あり':   true,
        '数え役満あり':     true,
        '役満パオあり':     true,
        '切り上げ満貫あり': false,
    };

    for (let key of Object.keys(param)) {
        rule[key] = param[key];
    }

    return rule;
}

},
"./shan":function(module,exports,require){
/*
 *  Majiang.Shan
 */
"use strict";

const Majiang = { Shoupai: require('./shoupai') };

module.exports = class Shan {

    static zhenbaopai(p) {
        if (! Majiang.Shoupai.valid_pai(p)) throw new Error(p);
        let s = p[0], n = + p[1] || 5;
        return s == 'z' ? (n < 5  ? s + (n % 4 + 1) : s + ((n - 4) % 3 + 5))
                        : s + (n % 9 + 1);
    }

    constructor(rule) {

        this._rule = rule;
        let hongpai = rule['赤牌'];

        let pai = [];
        for (let s of ['m','p','s','z']) {
            for (let n = 1; n <= (s == 'z' ? 7 : 9); n++) {
                for (let i = 0; i < 4; i++) {
                    if (n == 5 && i < hongpai[s]) pai.push(s+0);
                    else                          pai.push(s+n);
                }
            }
        }

        this._pai = [];
        while (pai.length) {
            this._pai.push(pai.splice(Math.random()*pai.length, 1)[0]);
        }

        this._baopai     = [this._pai[4]];
        this._fubaopai   = rule['裏ドラあり'] ? [this._pai[9]] : null;
        this._weikaigang = false;
        this._closed     = false;
    }

    zimo() {
        if (this._closed)     throw new Error(this);
        if (this.paishu == 0) throw new Error(this);
        if (this._weikaigang) throw new Error(this);
        return this._pai.pop();
    }

    gangzimo() {
        if (this._closed)             throw new Error(this);
        if (this.paishu == 0)         throw new Error(this);
        if (this._weikaigang)         throw new Error(this);
        if (this._baopai.length == 5) throw new Error(this);
        this._weikaigang = this._rule['カンドラあり'];
        if (! this._weikaigang) this._baopai.push('');
        return this._pai.shift();
    }

    kaigang() {
        if (this._closed)                 throw new Error(this);
        if (! this._weikaigang)           throw new Error(this);
        this._baopai.push(this._pai[4]);
        if (this._fubaopai && this._rule['カン裏あり'])
            this._fubaopai.push(this._pai[9]);
        this._weikaigang = false;
        return this;
    }

    close() { this._closed = true; return this }

    get paishu() { return this._pai.length - 14 }

    get baopai() { return this._baopai.filter(x=>x) }

    get fubaopai() {
        return ! this._closed ? null
             : this._fubaopai ? this._fubaopai.concat()
             :                  null;
    }
}

},
"./shoupai":function(module,exports,require){
/*
 *  Majiang.Shoupai
 */
"use strict";

module.exports = class Shoupai {

    static valid_pai(p) {
        if (p.match(/^(?:[mps]\d|z[1-7])_?\*?[\+\=\-]?$/)) return p;
    }

    static valid_mianzi(m) {

        if (m.match(/^z.*[089]/)) return;
        let h = m.replace(/0/g,'5');
        if (h.match(/^[mpsz](\d)\1\1[\+\=\-]\1?$/)) {
            return m.replace(/([mps])05/,'$1'+'50');
        }
        else if (h.match(/^[mpsz](\d)\1\1\1[\+\=\-]?$/)) {
            return m[0]+m.match(/\d(?![\+\=\-])/g).sort().reverse().join('')
                       +(m.match(/\d[\+\=\-]$/)||[''])[0];
        }
        else if (h.match(/^[mps]\d+\-\d*$/)) {
            let hongpai = m.match(/0/);
            let nn = h.match(/\d/g).sort();
            if (nn.length != 3)                               return;
            if (+nn[0] + 1 != +nn[1] || +nn[1] + 1 != +nn[2]) return;
            h = h[0]+h.match(/\d[\+\=\-]?/g).sort().join('');
            return hongpai ? h.replace(/5/,'0') : h;
        }
    }

    constructor(qipai = []) {

        this._bingpai = {
            _:  0,
            m: [0,0,0,0,0,0,0,0,0,0],
            p: [0,0,0,0,0,0,0,0,0,0],
            s: [0,0,0,0,0,0,0,0,0,0],
            z: [0,0,0,0,0,0,0,0],
        };
        this._fulou = [];
        this._zimo  = null;
        this._lizhi = false;

        for (let p of qipai) {
            if (p == '_') {
                this._bingpai._++;
                continue;
            }
            if (! (p = Shoupai.valid_pai(p)))       throw new Error(p);
            let s = p[0], n = +p[1];
            if (this._bingpai[s][n] == 4)           throw new Error([this, p]);
            this._bingpai[s][n]++;
            if (s != 'z' && n == 0) this._bingpai[s][5]++;
        }
    }

    static fromString(paistr = '') {

        let fulou   = paistr.split(',');
        let bingpai = fulou.shift();

        let qipai = bingpai.match(/^_*/)[0].match(/_/g) || [];
        for (let suitstr of bingpai.match(/[mpsz]\d+_*/g) || []) {
            let s = suitstr[0];
            for (let n of suitstr.match(/\d/g)) {
                if (s == 'z' && (n < 1 || 7 < n)) continue;
                qipai.push(s+n);
            }
            qipai = qipai.concat(suitstr.match(/_/g)||[]);
        }
        qipai = qipai.slice(0, 14 - fulou.filter(x=>x).length * 3);
        let zimo = qipai.length + fulou.length * 3 == 14 && qipai.slice(-1)[0];
        const shoupai = new Shoupai(qipai);

        let last;
        for (let m of fulou) {
            if (! m) { shoupai._zimo = last; break }
            m = Shoupai.valid_mianzi(m);
            if (m) {
                shoupai._fulou.push(m);
                last = m;
            }
        }

        shoupai._zimo  = shoupai._zimo || zimo || null;
        shoupai._lizhi = bingpai.slice(-1) == '*';

        return shoupai;
    }

    toString() {

        let paistr = '';

        for (let s of ['m','p','s','z']) {
            let suitstr = s;
            let bingpai = this._bingpai[s];
            let n_hongpai = s == 'z' ? 0 : bingpai[0];
            for (let n = 1; n < bingpai.length; n++) {
                let n_pai = bingpai[n];
                if (this._zimo) {
                    if (s+n == this._zimo)           { n_pai--;             }
                    if (n == 5 && s+0 == this._zimo) { n_pai--; n_hongpai-- }
                }
                for (let i = 0; i < n_pai; i++) {
                    if (n ==5 && n_hongpai > 0) { suitstr += 0; n_hongpai-- }
                    else                        { suitstr += n;             }
                }
            }
            if (suitstr.length > 1) paistr += suitstr;
        }
        paistr += '_'.repeat(this._bingpai._ + (this._zimo == '_' ? -1 : 0));
        if (this._zimo && this._zimo.length <= 2) paistr += this._zimo;
        if (this._lizhi)                          paistr += '*';

        for (let m of this._fulou) {
            paistr += ',' + m;
        }
        if (this._zimo && this._zimo.length > 2) paistr += ',';

        return paistr;
    }

    clone() {

        const shoupai = new Shoupai();

        shoupai._bingpai = {
            _: this._bingpai._,
            m: this._bingpai.m.concat(),
            p: this._bingpai.p.concat(),
            s: this._bingpai.s.concat(),
            z: this._bingpai.z.concat(),
        };
        shoupai._fulou = this._fulou.concat();
        shoupai._zimo  = this._zimo;
        shoupai._lizhi = this._lizhi;

        return shoupai;
    }

    fromString(paistr) {
        const shoupai = Shoupai.fromString(paistr);
        this._bingpai = {
            _: shoupai._bingpai._,
            m: shoupai._bingpai.m.concat(),
            p: shoupai._bingpai.p.concat(),
            s: shoupai._bingpai.s.concat(),
            z: shoupai._bingpai.z.concat(),
        };
        this._fulou = shoupai._fulou.concat();
        this._zimo  = shoupai._zimo;
        this._lizhi = shoupai._lizhi;

        return this;
    }

    decrease(s, n) {
        let bingpai = this._bingpai[s]; n = + n;
        if (bingpai[n] == 0 || n == 5 && bingpai[0] == bingpai[5]) {
            if (this._bingpai._ == 0)               throw new Error([this,s+n]);
            this._bingpai._--;
        }
        else {
            bingpai[n]--;
            if (n == 0) bingpai[5]--;
        }
    }

    zimo(p, check = true) {
        if (check && this._zimo)                    throw new Error([this, p]);
        if (p == '_') {
            this._bingpai._++;
            this._zimo = p;
        }
        else {
            if (! Shoupai.valid_pai(p))             throw new Error(p);
            let s = p[0], n = +p[1];
            let bingpai = this._bingpai[s];
            if (bingpai[n] == 4)                    throw new Error([this, p]);
            bingpai[n]++;
            if (n == 0) {
                if (bingpai[5] == 4)                throw new Error([this, p]);
                bingpai[5]++;
            }
            this._zimo = s+n;
        }
        return this;
    }

    dapai(p, check = true) {
        if (check && ! this._zimo)                  throw new Error([this, p]);
        if (! Shoupai.valid_pai(p))                 throw new Error(p);
        let s = p[0], n = +p[1];
        this.decrease(s, n);
        this._zimo = null;
        if (p.slice(-1) == '*') this._lizhi = true;
        return this;
    }

    fulou(m, check = true) {
        if (check && this._zimo)                    throw new Error([this, m]);
        if (m != Shoupai.valid_mianzi(m))           throw new Error(m);
        if (m.match(/\d{4}$/))                      throw new Error([this, m]);
        if (m.match(/\d{3}[\+\=\-]\d$/))            throw new Error([this, m]);
        let s = m[0];
        for (let n of m.match(/\d(?![\+\=\-])/g)) {
            this.decrease(s, n);
        }
        this._fulou.push(m);
        if (! m.match(/\d{4}/)) this._zimo = m;
        return this;
    }

    gang(m, check = true) {
        if (check && ! this._zimo)                  throw new Error([this, m]);
        if (check && this._zimo.length > 2)         throw new Error([this, m]);
        if (m != Shoupai.valid_mianzi(m))           throw new Error(m);
        let s = m[0];
        if (m.match(/\d{4}$/)) {
            for (let n of m.match(/\d/g)) {
                this.decrease(s, n);
            }
            this._fulou.push(m);
        }
        else if (m.match(/\d{3}[\+\=\-]\d$/)) {
            let m1 = m.slice(0,5);
            let i = this._fulou.findIndex(m2 => m1 == m2);
            if (i < 0)                              throw new Error([this, m]);
            this._fulou[i] = m;
            this.decrease(s, m.slice(-1));
        }
        else                                        throw new Error([this, m]);
        this._zimo = null;
        return this;
    }

    get menqian() {
        return this._fulou.filter(m=>m.match(/[\+\=\-]/)).length == 0;
    }

    get lizhi() { return this._lizhi }

    get_dapai(check = true) {

        if (! this._zimo) return null;

        let deny = {};
        if (check && this._zimo.length > 2) {
            let m = this._zimo;
            let s = m[0];
            let n = + m.match(/\d(?=[\+\=\-])/) || 5;
            deny[s+n] = true;
            if (! m.replace(/0/,'5').match(/^[mpsz](\d)\1\1/)) {
                if (n < 7 && m.match(/^[mps]\d\-\d\d$/)) deny[s+(n+3)] = true;
                if (3 < n && m.match(/^[mps]\d\d\d\-$/)) deny[s+(n-3)] = true;
            }
        }

        let dapai = [];
        if (! this._lizhi) {
            for (let s of ['m','p','s','z']) {
                let bingpai = this._bingpai[s];
                for (let n = 1; n < bingpai.length; n++) {
                    if (bingpai[n] == 0)  continue;
                    if (deny[s+n])        continue;
                    if (s+n == this._zimo && bingpai[n] == 1) continue;
                    if (s == 'z' || n != 5)          dapai.push(s+n);
                    else {
                        if (bingpai[0] > 0
                            && s+0 != this._zimo || bingpai[0] > 1)
                                                     dapai.push(s+0);
                        if (bingpai[0] < bingpai[5]) dapai.push(s+n);
                    }
                }
            }
        }
        if (this._zimo.length == 2) dapai.push(this._zimo + '_');
        return dapai;
    }

    get_chi_mianzi(p, check = true) {

        if (this._zimo) return null;
        if (! Shoupai.valid_pai(p))                     throw new Error(p);

        let mianzi = [];
        let s = p[0], n = + p[1] || 5, d = p.match(/[\+\=\-]$/);
        if (! d)                                        throw new Error(p);
        if (s == 'z' || d != '-') return mianzi;
        if (this._lizhi) return mianzi;

        let bingpai = this._bingpai[s];
        if (3 <= n && bingpai[n-2] > 0 && bingpai[n-1] > 0) {
            if (! check
                || (3 < n ? bingpai[n-3] : 0) + bingpai[n]
                        < 14 - (this._fulou.length + 1) * 3)
            {
                if (n-2 == 5 && bingpai[0] > 0) mianzi.push(s+'067-');
                if (n-1 == 5 && bingpai[0] > 0) mianzi.push(s+'406-');
                if (n-2 != 5 && n-1 != 5 || bingpai[0] < bingpai[5])
                                            mianzi.push(s+(n-2)+(n-1)+(p[1]+d));
            }
        }
        if (2 <= n && n <= 8 && bingpai[n-1] > 0 && bingpai[n+1] > 0) {
            if (! check || bingpai[n] < 14 - (this._fulou.length + 1) * 3) {
                if (n-1 == 5 && bingpai[0] > 0) mianzi.push(s+'06-7');
                if (n+1 == 5 && bingpai[0] > 0) mianzi.push(s+'34-0');
                if (n-1 != 5 && n+1 != 5 || bingpai[0] < bingpai[5])
                                            mianzi.push(s+(n-1)+(p[1]+d)+(n+1));
            }
        }
        if (n <= 7 && bingpai[n+1] > 0 && bingpai[n+2] > 0) {
            if (! check
                ||  bingpai[n] + (n < 7 ? bingpai[n+3] : 0)
                        < 14 - (this._fulou.length + 1) * 3)
            {
                if (n+1 == 5 && bingpai[0] > 0) mianzi.push(s+'4-06');
                if (n+2 == 5 && bingpai[0] > 0) mianzi.push(s+'3-40');
                if (n+1 != 5 && n+2 != 5 || bingpai[0] < bingpai[5])
                                            mianzi.push(s+(p[1]+d)+(n+1)+(n+2));
            }
        }
        return mianzi;
    }

    get_peng_mianzi(p) {

        if (this._zimo) return null;
        if (! Shoupai.valid_pai(p))                     throw new Error(p);

        let mianzi = [];
        let s = p[0], n = + p[1] || 5, d = p.match(/[\+\=\-]$/);
        if (! d)                                        throw new Error(p);
        if (this._lizhi) return mianzi;

        let bingpai = this._bingpai[s];
        if (bingpai[n] >= 2) {
            if (n == 5 && bingpai[0] >= 2)  mianzi.push(s+'00'+p[1]+d);
            if (n == 5 && bingpai[0] >= 1 && bingpai[5] - bingpai[0] >=1)
                                            mianzi.push(s+'50'+p[1]+d);
            if (n != 5 || bingpai[5] - bingpai[0] >=2)
                                            mianzi.push(s+n+n+p[1]+d);
        }
        return mianzi;
    }

    get_gang_mianzi(p) {

        let mianzi = [];
        if (p) {
            if (this._zimo) return null;
            if (! Shoupai.valid_pai(p))                 throw new Error(p);

            let s = p[0], n = + p[1] || 5, d = p.match(/[\+\=\-]$/);
            if (! d)                                    throw new Error(p);
            if (this._lizhi) return mianzi;

            let bingpai = this._bingpai[s];
            if (bingpai[n] == 3) {
                if (n == 5) mianzi = [ s + '5'.repeat(3 - bingpai[0])
                                         + '0'.repeat(bingpai[0]) + p[1]+d ];
                else        mianzi = [ s+n+n+n+n+d ];
            }
        }
        else {
            if (! this._zimo) return null;
            if (this._zimo.length > 2) return null;
            let p = this._zimo.replace(/0/,'5');

            for (let s of ['m','p','s','z']) {
                let bingpai = this._bingpai[s];
                for (let n = 1; n < bingpai.length; n++) {
                    if (bingpai[n] == 0) continue;
                    if (bingpai[n] == 4) {
                        if (this._lizhi && s+n != p) continue;
                        if (n == 5) mianzi.push(s + '5'.repeat(4 - bingpai[0])
                                                  + '0'.repeat(bingpai[0]));
                        else        mianzi.push(s+n+n+n+n);
                    }
                    else {
                        if (this._lizhi) continue;
                        for (let m of this._fulou) {
                            if (m.replace(/0/g,'5').slice(0,4) == s+n+n+n) {
                                if (n == 5 && bingpai[0] > 0) mianzi.push(m+0);
                                else                          mianzi.push(m+n);
                            }
                        }
                    }
                }
            }
        }
        return mianzi;
    }
}

},
"./xiangting":function(module,exports,require){
/*
 *  Majiang.Util.xiangting
 */
"use strict";

function _xiangting(m, d, g, j) {

    let n = j ? 4 : 5;
    if (m         > 4) { d += m     - 4; m = 4         }
    if (m + d     > 4) { g += m + d - 4; d = 4 - m     }
    if (m + d + g > n) {                 g = n - m - d }
    if (j) d++;
    return 13 - m * 3 - d * 2 - g;
}

function dazi(bingpai) {

    let n_pai = 0, n_dazi = 0, n_guli = 0;

    for (let n = 1; n <= 9; n++) {
        n_pai += bingpai[n];
        if (n <= 7 && bingpai[n+1] == 0 && bingpai[n+2] == 0) {
            n_dazi += n_pai >> 1;
            n_guli += n_pai  % 2;
            n_pai = 0;
        }
    }
    n_dazi += n_pai >> 1;
    n_guli += n_pai  % 2;

    return { a: [ 0, n_dazi, n_guli ],
             b: [ 0, n_dazi, n_guli ] };
}

function mianzi(bingpai, n = 1) {

    if (n > 9) return dazi(bingpai);

    let max = mianzi(bingpai, n+1);

    if (n <= 7 && bingpai[n] > 0 && bingpai[n+1] > 0 && bingpai[n+2] > 0) {
        bingpai[n]--; bingpai[n+1]--; bingpai[n+2]--;
        let r = mianzi(bingpai, n);
        bingpai[n]++; bingpai[n+1]++; bingpai[n+2]++;
        r.a[0]++; r.b[0]++;
        if (r.a[2] < max.a[2]
            || r.a[2] == max.a[2] && r.a[1] < max.a[1]) max.a = r.a;
        if (r.b[0] > max.b[0]
            || r.b[0] == max.b[0] && r.b[1] > max.b[1]) max.b = r.b;
    }

    if (bingpai[n] >= 3) {
        bingpai[n] -= 3;
        let r = mianzi(bingpai, n+1);
        bingpai[n] += 3;
        r.a[0]++; r.b[0]++;
        if (r.a[2] < max.a[2]
            || r.a[2] == max.a[2] && r.a[1] < max.a[1]) max.a = r.a;
        if (r.b[0] > max.b[0]
            || r.b[0] == max.b[0] && r.b[1] > max.b[1]) max.b = r.b;
    }

    return max;
}

function mianzi_all(shoupai, jiangpai) {

    let r = {
        m: mianzi(shoupai._bingpai.m),
        p: mianzi(shoupai._bingpai.p),
        s: mianzi(shoupai._bingpai.s),
    };

    let z = [0, 0, 0];
    for (let n = 1; n <= 7; n++) {
        if      (shoupai._bingpai.z[n] >= 3) z[0]++;
        else if (shoupai._bingpai.z[n] == 2) z[1]++;
        else if (shoupai._bingpai.z[n] == 1) z[2]++;
    }

    let n_fulou = shoupai._fulou.length;

    let min = 13;

    for (let m of [r.m.a, r.m.b]) {
        for (let p of [r.p.a, r.p.b]) {
            for (let s of [r.s.a, r.s.b]) {
                let x = [n_fulou, 0, 0];
                for (let i = 0; i < 3; i++) {
                    x[i] += m[i] + p[i] + s[i] + z[i];
                }
                let n_xiangting = _xiangting(x[0], x[1], x[2], jiangpai);
                if (n_xiangting < min) min = n_xiangting;
            }
        }
    }

    return min;
}

function xiangting_yiban(shoupai) {

    let min = mianzi_all(shoupai);

    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        for (let n = 1; n < bingpai.length; n++) {
            if (bingpai[n] >= 2) {
                bingpai[n] -= 2;
                let n_xiangting = mianzi_all(shoupai, true);
                bingpai[n] += 2;
                if (n_xiangting < min) min = n_xiangting;
            }
        }
    }
    if (min == -1 && shoupai._zimo && shoupai._zimo.length > 2) return 0;

    return min;
}

function xiangting_guoshi(shoupai) {

    if (shoupai._fulou.length) return Infinity;

    let n_yaojiu = 0;
    let n_duizi  = 0;

    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        let nn = (s == 'z') ? [1,2,3,4,5,6,7] : [1,9];
        for (let n of nn) {
            if (bingpai[n] >= 1) n_yaojiu++;
            if (bingpai[n] >= 2) n_duizi++;
        }
    }

    return n_duizi ? 12 - n_yaojiu : 13 - n_yaojiu;
}

function xiangting_qidui(shoupai) {

    if (shoupai._fulou.length) return Infinity;

    let n_duizi = 0;
    let n_guli  = 0;

    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        for (let n = 1; n < bingpai.length; n++) {
            if      (bingpai[n] >= 2) n_duizi++;
            else if (bingpai[n] == 1) n_guli++;
        }
    }

    if (n_duizi          > 7) n_duizi = 7;
    if (n_duizi + n_guli > 7) n_guli  = 7 - n_duizi;

    return 13 - n_duizi * 2 - n_guli;
}

function xiangting(shoupai) {
    return Math.min(
        xiangting_yiban(shoupai),
        xiangting_guoshi(shoupai),
        xiangting_qidui(shoupai)
    );
}

function tingpai(shoupai, f_xiangting = xiangting) {

    if (shoupai._zimo) return null;

    let pai = [];
    let n_xiangting = f_xiangting(shoupai);
    for (let s of ['m','p','s','z']) {
        let bingpai = shoupai._bingpai[s];
        for (let n = 1; n < bingpai.length; n++) {
            if (bingpai[n] >= 4) continue;
            bingpai[n]++;
            if (f_xiangting(shoupai) < n_xiangting) pai.push(s+n);
            bingpai[n]--;
        }
    }
    return pai;
}

module.exports = {
    xiangting_guoshi: xiangting_guoshi,
    xiangting_qidui:  xiangting_qidui,
    xiangting_yiban:  xiangting_yiban,
    xiangting:        xiangting,
    tingpai:          tingpai
}

},
},cache={};function req(id){if(cache[id])return cache[id].exports;const m={exports:{}};cache[id]=m;modules[id](m,m.exports,req);return m.exports;}g.Majiang=req('./index');if(typeof module!=='undefined')module.exports=g.Majiang;})(globalThis);
