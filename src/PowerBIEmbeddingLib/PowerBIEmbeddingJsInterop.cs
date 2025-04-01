using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PowerBIEmbeddingLib;

public class PowerBIEmbeddingJsInterop : IAsyncDisposable
{
    Lazy<Task<IJSObjectReference>> powerBiEmbeddingModuleTask;
    string powerBiEmbeddingId;

    string StaticPath => $"./_content/{nameof(PowerBIEmbeddingLib)}/";

    public PowerBIEmbeddingJsInterop(IJSRuntime jsRuntime)
    {
        powerBiEmbeddingModuleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            $"{StaticPath}js/powerbiembedding.js"
        ).AsTask());
    }

    public async Task SetupAsync(string powerBiEmbeddingId, object objRef, ElementReference canv, string staticPath = null)
    {
        var module = await powerBiEmbeddingModuleTask.Value;
        this.powerBiEmbeddingId = powerBiEmbeddingId;
        await module.InvokeVoidAsync("setup", powerBiEmbeddingId, objRef, canv, staticPath ?? StaticPath);
    }

    public async ValueTask DisposeAsync()
    {
        if (powerBiEmbeddingModuleTask.IsValueCreated)
        {
            var module = await powerBiEmbeddingModuleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async Task ShowReportAsync(string powerBiEmbeddingId, string accessToken, string embedUrl, string embedReportId)
    {
        var module = await powerBiEmbeddingModuleTask.Value;
        await module.InvokeVoidAsync("showReport", powerBiEmbeddingId, accessToken, embedUrl, embedReportId);
    }

    public async Task StartAsync()
    {
        var module = await powerBiEmbeddingModuleTask.Value;
        await module.InvokeVoidAsync("start", powerBiEmbeddingId);
    }

    public async Task SetAsync<TValue>(string name, TValue value)
    {
        var module = await powerBiEmbeddingModuleTask.Value;
        await module.InvokeVoidAsync("set", powerBiEmbeddingId, name, value);
    }

}
