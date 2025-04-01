import State from './state.js';
import SpriteSheet from './spritesheet.js';
import Resources from './resources.js';

import Panel01 from './scenes/panel01.js';

export function setup(hmiId, pageProxy, canvasId, staticPath) {
    window.hmiLib = window.hmiLib || {};
    var that = window.hmiLib[hmiId] || {};

    that.pageProxy = pageProxy;
    that.canvas = window.document.getElementById(canvasId);
    that.context = that.canvas.getContext("2d");
    that.staticPath = staticPath;

    that.resources = new Resources({
        staticPath: staticPath
    });
    that.resources.load();

    that.hmi = new Hmi(window.document, that.canvas, that.context, that.resources);

    window.hmiLib[hmiId] = that;
}

export function start(hmiId) {
    window.hmiLib = window.hmiLib || {};
    var that = window.hmiLib[hmiId] || {};

    that.hmi.goToPanel01();
    
    window.hmiLib[hmiId] = that;
}

export function set(hmiId, name, value) {
    window.hmiLib = window.hmiLib || {};
    var that = window.hmiLib[hmiId] || {};

    that.hmi.scene.set(name, value);

    window.hmiLib[hmiId] = that;
}

class Hmi {

    constructor(doc, canv, ctx, resources) {

        this.doc = doc;
        this.canv = canv;
        this.ctx = ctx;
        this.resources = resources;
        this.framesCount = 0;
        this.width = 1000;
        this.height = 400;

        this.canv.width = this.width;
        this.canv.height = this.height;

        this.spritesheet = new SpriteSheet({
            resources: this.resources
        });

        var that = this;

        this.doc.body.addEventListener("keydown", (e) => {
            that.scene.handle_keys(e.keyCode, true);
        });

        this.doc.body.addEventListener("keyup", (e) => {
            that.scene.handle_keys(e.keyCode, false);
        });
    }

    goToPanel01() {
        this.state = new State();

        this.scene = new Panel01({
            hmi: this,
            width: this.width,
            height: this.height,
            resources: this.resources,
            spritesheet: this.spritesheet,
            state: this.state
        });  
        this.invalidate();
    }

    invalidate() {

        var that = this;

        window.requestAnimationFrame(async (timestamp) => {
            await that.scene.loop(that, timestamp);
        });
    }
}