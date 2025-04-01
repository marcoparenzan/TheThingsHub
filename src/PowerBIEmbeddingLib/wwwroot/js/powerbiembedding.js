export function setup(powerBiEmbeddingId, proxy, canv, staticPath) {
    window.powerBiEmbeddingLib = window.powerBiEmbeddingLib || {};
    var that = window.powerBiEmbeddingLib[powerBiEmbeddingId] || {};

    that.proxy = proxy;
    that.canvas = canv;
    that.staticPath = staticPath;

    that.powerBiEmbedding = new PowerBIEmbedding(window.document, that.proxy, that.canvas);

    window.powerBiEmbeddingLib[powerBiEmbeddingId] = that;
}

export function showReport(powerBiEmbeddingId, accessToken, embedUrl, embedReportId) {
    window.powerBiEmbeddingLib = window.powerBiEmbeddingLib || {};
    var that = window.powerBiEmbeddingLib[powerBiEmbeddingId] || {};

    that.powerBiEmbedding.showReport(accessToken, embedUrl, embedReportId);
    
    window.powerBiEmbeddingLib[powerBiEmbeddingId] = that;
}

class PowerBIEmbedding {

    constructor(doc, proxy, canv) {
        var that = this;

        that.doc = doc;
        that.proxy = proxy;
        that.canv = canv;
    }

    showReport(accessToken, embedUrl, embedReportId) {
        var that = this;

        // Get models. models contains enums that can be used.
        var models = window['powerbi-client'].models;
        var config = {
            type: 'report',
            tokenType: models.TokenType.Embed,
            accessToken: accessToken,
            embedUrl: embedUrl,
            id: embedReportId,
            permissions: models.Permissions.All,
            settings: {
                filterPaneEnabled: true,
                navContentPaneEnabled: true
            }
        };
        // Embed the report and display it within the div container.
        window.powerbi.embed(that.canv, config);
    }
}