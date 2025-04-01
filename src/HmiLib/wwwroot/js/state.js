export default class State {

    constructor(opt) { 
        opt = opt || {};
        this.score = opt.score || 0;
        this.lives = opt.lives || 3;
    }

    addScore(score) {
        this.score += score;
        return this;
    }
}