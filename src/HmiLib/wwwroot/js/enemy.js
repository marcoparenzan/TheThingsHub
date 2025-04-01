export default class Enemy {

    constructor(opt) { // this.width / 2, 200
        this.opt = opt || {};

        this.reset();
    }

    reset() {
        this.x = this.opt.x;
        this.y = this.opt.y;
        this.width = this.opt.width || 20;
        this.height = this.opt.height || 34;
        this.speed = this.opt.speed || 0;
        this.velX = this.opt.velX || 0;
        this.velY = this.opt.velY || 0;
        this.jumping = this.opt.jumping || false;
        this.grounded = this.opt.grounded || true;
        this.color = this.opt.color || '#E6AC27';
        this.friction = this.opt.friction || 0.0;
        this.gravity = this.opt.gravitry || 0.0;
        this.animationName = this.opt.animationName;
        this.spritesheet = this.opt.spritesheet;
        this.updateCallback = this.opt.updateCallback;

        this._updateAnimation();
    }

    setCollided(value) {
        if (value != '') {
        }
        this.collided = value;
    }

    _updateAnimation() {
        var name = this.animationName;
        if (this.animation != null) {
            if (this.animation.name == name) return;
        }
        this.animation = this.spritesheet.getAnimation(name);
    }

    update() {

        this.updateCallback(this);

        //this.velX *= this.friction;
        // this.velY += this.gravity;

        // if (this.grounded) {
        //     this.velY = 0;
        // }

        // this.x += this.velX;
        // this.y += this.velY;

        this.animation.update();

        this._updateAnimation();
    }

    render(ctx) {
        // this.animation.render(ctx, this.x, this.y);
        ctx.fillStyle = this.color;
        ctx.fillRect(this.x, this.y, this.width, this.height);
    }
}