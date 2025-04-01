namespace DocumentsApp.Services;

// https://code-maze.com/aspnetcore-api-key-authentication/
public class ApiKeyEndpointFilter(IApiKeyValidation apiKeyValidation) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var apiKey = context.HttpContext.Request.Headers[Constants.ApiKeyHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = context.HttpContext.Request.Query[Constants.ApiKeyQueryParamName];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Results.BadRequest();
        if (!apiKeyValidation.IsValidApiKey(apiKey!))
        {
            return Results.Unauthorized();
        }
        return await next(context);
    }
}
