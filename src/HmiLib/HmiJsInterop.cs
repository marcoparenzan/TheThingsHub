using Microsoft.JSInterop;

namespace HmiLib;

public class HmiJsInterop : IAsyncDisposable
{
    Lazy<Task<IJSObjectReference>> hmiModuleTask;
    string hmiId;

    string StaticPath => $"./_content/{nameof(HmiLib)}/";

    public HmiJsInterop(IJSRuntime jsRuntime)
    {
        hmiModuleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            $"{StaticPath}/js/hmi.js"
        ).AsTask());
    }

    public async Task SetupAsync(string hmiId, object objRef, string canvasId, string staticPath = null)
    {
        var module = await hmiModuleTask.Value;
        this.hmiId = hmiId;
        await module.InvokeVoidAsync("setup", hmiId, objRef, canvasId, staticPath ?? StaticPath);
    }
    public async ValueTask DisposeAsync()
    {
        if (hmiModuleTask.IsValueCreated)
        {
            var module = await hmiModuleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async Task StartAsync()
    {
        var module = await hmiModuleTask.Value;
        await module.InvokeVoidAsync("start", hmiId);
    }

    public async Task SetAsync<TValue>(string name, TValue value)
    {
        var module = await hmiModuleTask.Value;
        await module.InvokeVoidAsync("set", hmiId, name, value);
    }

}
