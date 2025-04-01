using Microsoft.JSInterop;

namespace KustoDashboardLib;

public class KustoDashboardJsInterop : IAsyncDisposable
{
    Lazy<Task<IJSObjectReference>> kustoDashboardModuleTask;
    string kustoDashboardId;

    string StaticPath => $"./_content/{nameof(KustoDashboardLib)}/";

    public KustoDashboardJsInterop(IJSRuntime jsRuntime)
    {
        kustoDashboardModuleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            $"{StaticPath}/js/kustodashboard.js"
        ).AsTask());
    }

    public async Task SetupAsync(string kustoDashboardId, object objRef, string canvasId, string staticPath = null)
    {
        var module = await kustoDashboardModuleTask.Value;
        this.kustoDashboardId = kustoDashboardId;
        await module.InvokeVoidAsync("setup", kustoDashboardId, objRef, canvasId, staticPath ?? StaticPath);
    }
    public async ValueTask DisposeAsync()
    {
        if (kustoDashboardModuleTask.IsValueCreated)
        {
            var module = await kustoDashboardModuleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async Task StartAsync()
    {
        var module = await kustoDashboardModuleTask.Value;
        await module.InvokeVoidAsync("start", kustoDashboardId);
    }

    public async Task SetAsync<TValue>(string name, TValue value)
    {
        var module = await kustoDashboardModuleTask.Value;
        await module.InvokeVoidAsync("set", kustoDashboardId, name, value);
    }

}
