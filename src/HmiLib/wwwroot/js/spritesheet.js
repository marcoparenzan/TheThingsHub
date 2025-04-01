export default class SpriteSheet {
    constructor(opt) {
        opt = opt || {};
        this.resources = opt.resources;

        this.frameSets = {
        };

        // items
        this._setFrameset({
            name: "tank",
            width: 25,
            height: 30,
            resourceName: "items",
            defs: [
                [  2, 159]
            ]
        });

        // panel01
        this._setFrameset({
            name: "panel01",
            width: 1000,
            height: 400,
            resourceName: "panel01",
            defs: [
                [  0,  0]
            ]
        });

    }

    _setFrameset(opt) {
        this.frameSets[opt.name] = opt;
    }

    getAnimation(name) {
        var fs = this.frameSets[name];
        var frameset = new SpriteSheetAnimation({
            name: name,
            index: 0,
            image: this.resources.images[fs.resourceName],
            frameSet: fs
        });

        return frameset;
    }
    
    getComplex(frameSetName) {
        var fs = this.frameSets[frameSetName];
        var frameset = new SpriteSheetComplex({
            index: 0,
            image: this.resources.images[fs.resourceName],
            frameSet: fs
        });

        return frameset;
    }
}

export class SpriteSheetAnimation {

    constructor(opt) {
        this.name = opt.name;
        this.index = opt.index;
        this.image = opt.image;
        this.frameSet = opt.frameSet;
        this.counter = 30;
    }

    render(ctx, x, y) {
        var frameSetDef = this.frameSet.defs[this.index];
        ctx.drawImage(
            this.image,
            frameSetDef[0],
            frameSetDef[1],
            this.frameSet.width,
            this.frameSet.height,
            x,
            y,
            this.frameSet.width,
            this.frameSet.height
        );
    }

    update() {
        this.counter--;
        if (this.counter>0) return;
        this.index++;
        this.counter = 30;
        if (this.index >= this.frameSet.defs.length) this.index = 0;
    }
}

export class SpriteSheetComplex {

    constructor(opt) {
        this.name = opt.name;
        this.image = opt.image;
        this.frameSet = opt.frameSet;
    }

    render(ctx, x, y, index) {
        var frameSetDef = this.frameSet.defs[index];
        ctx.drawImage(
            this.image,
            frameSetDef[0],
            frameSetDef[1],
            frameSetDef[2],
            frameSetDef[3],
            x,
            y,
            frameSetDef[2],
            frameSetDef[3]
        );
    }
}