import Enemy from '../enemy.js';

export default class Panel01 {

    constructor(opt) { 
        this.hmi = opt.hmi;
        this.width = opt.width;
        this.height = opt.height;
        this.resources = opt.resources;
        this.spritesheet = opt.spritesheet;

        this.items = opt.items ||[];
        this.state = opt.state;

        this.keys = [];

        this.values = {
        };

        var that = this;

        var color = "green";
        if (that.values["tankColor"] == "Bad") color = "red";

        this.items.push(new Enemy({
            x: 405,
            y: 368,
            width: 150,
            height: 1,
            speed: 1,
            velX: 1,
            spritesheet: this.spritesheet,
            animationName: "tank",
            color: color,
            updateCallback: function (item) {
                item.height = that.values["tank"] || 1;
                item.y = 368-item.height;
            }
        }));

        this.reset();
    }

    reset() {
        for(var k = 0; k<this.items.length; k++) {
            this.items[k].reset();
        }
    }

    handle_keys(key, value) {
        this.keys[key] = value;
    }

    set(name, value) {
        this.values[name] = value;
    }

    async update() {

        if (this.updating == true) return;
        this.updating = true;

        // check keys
        if (this.keys[38] || this.keys[32] || this.keys[87]) {
            // up arrow or space
        }
        else if (this.keys[39] || this.keys[68]) {
            // right arrow
        }
        else if (this.keys[37] || this.keys[65]) {
            // left arrow
        }

        var k = 0;
        while (true) {
            if (k >= this.items.length) break;

            this.items[k].update();

            k++;
        }

        this.updating = false;
    }

    render(ctx) {

        var fs = this.spritesheet.getAnimation("panel01");
        fs.render(ctx, 0,0);

        for (var k = 0; k < this.items.length; k++) {
            this.items[k].render(ctx);
        }

        //
        //  Score
        //
        ctx.font = "48px Arial";
        ctx.strokeText(this.state.score || 0, 110, 50);

        //
        //  Lives
        //
        ctx.font = "48px Arial";
        ctx.strokeText(this.state.lives || 3, 210, 50);

    }

    async loop(that, timestamp) {

        this.render(this.hmi.ctx);

        await this.update();

        this.hmi.invalidate();

    }
}