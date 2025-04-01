export default class Resources {

    constructor(opt) {
        opt = opt || {};
        this.staticPath = opt.staticPath;
    }

    load() {

        this.sounds = {
            // "start": this.newSound(this.staticPath + "sounds/hohoho-36506.mp3")
        };

        this.images = {
            "panel01": this.newImage(this.staticPath + "images/panel01.png"),
            "items": this.newImage(this.staticPath + "images/items.png")
        }; 
        
        var proms = [];
        for (var key in this.images) {
            proms.push(new Promise(res =>
                this.images[key].onload = () => {

                    // do nothing

                }
            ));
        }
        // list all image widths and heights _after_ the images have loaded:
        Promise.all(proms).then(data => {
            console.log("The images have loaded at last!\nHere are their dimensions (width,height):");
            console.log(data);
        })

    }

    newImage(src) {
        var image = new Image();
        image.src = src;
        return image;
    }

    newSound(src) {
        var sound = new Audio(src);
        sound.load();
        return sound;
    }
}
