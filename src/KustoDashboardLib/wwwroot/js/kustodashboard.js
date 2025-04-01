export function setup(kustoDashboadId, pageProxy, canvasId, staticPath) {
    window.kustoDashboardLib = window.kustoDashboardLib || {};
    var that = window.kustoDashboardLib[kustoDashboadId] || {};

    that.pageProxy = pageProxy;
    that.canvas = window.document.getElementById(canvasId);
    that.staticPath = staticPath;

    that.kustoDashboad = new KustoDashboard(window.document, that.canvas);

    window.addEventListener('message', async (event) => {
        if (event.data.signature === "queryExplorer" && event.data.type === "getToken") {
            var scope = "https://trd-y1hvbqtd1mbcr0bvcm.z1.kusto.fabric.microsoft.com/.default";
            // https://learn.microsoft.com/en-us/kusto/api/monaco/host-web-ux-in-iframe?view=azure-data-explorer&preserve-view=true
            var accessToken = await pageProxy.invokeMethodAsync('GetTokenAsync', scope);
            debugger;
            event.source.postMessage({
                "type": "postToken",
                "message": accessToken,
                "scope": scope
            }, '*');
        }
    });

    window.kustoDashboardLib[kustoDashboadId] = that;
}

export function start(kustoDashboadId) {
    window.kustoDashboardLib = window.kustoDashboardLib || {};
    var that = window.kustoDashboardLib[kustoDashboadId] || {};

    //that.kustoDashboad.goToPanel01();
    
    window.kustoDashboardLib[kustoDashboadId] = that;
}

export function set(kustoDashboadId, name, value) {
    window.kustoDashboardLib = window.kustoDashboardLib || {};
    var that = window.kustoDashboardLib[kustoDashboadId] || {};

    //that.kustoDashboad.scene.set(name, value);

    window.kustoDashboardLib[kustoDashboadId] = that;
}

class KustoDashboard {

    constructor(doc, canv) {

        this.doc = doc;
        this.canv = canv;

        this.canv.width = this.width;
        this.canv.height = this.height;

        var that = this;

        //this.doc.body.addEventListener("keydown", (e) => {
        //    that.scene.handle_keys(e.keyCode, true);
        //});

        //this.doc.body.addEventListener("keyup", (e) => {
        //    that.scene.handle_keys(e.keyCode, false);
        //});

    }

    invalidate() {

        var that = this;

        //window.requestAnimationFrame(async (timestamp) => {
        //    await that.scene.loop(that, timestamp);
        //});
    }
}