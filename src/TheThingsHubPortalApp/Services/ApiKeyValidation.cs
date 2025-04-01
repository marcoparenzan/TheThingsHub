namespace DocumentsApp.Services;

public static class Constants
{
    public const string ApiKeyHeaderName = "X-API-Key";
    public const string ApiKeyName = "ApiKey";
    public const string ApiKeyQueryParamName = "code";
}

public interface IApiKeyValidation
{
    bool IsValidApiKey(string userApiKey);
}

public class ApiKeyValidation(IConfiguration configuration) : IApiKeyValidation
{
    public bool IsValidApiKey(string userApiKey)
    {
        if (string.IsNullOrWhiteSpace(userApiKey))
            return false;
        string? apiKey = configuration.GetValue<string>(Constants.ApiKeyName);
        if (apiKey == null || apiKey != userApiKey)
            return false;
        return true;
    }
}
