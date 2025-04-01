using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace CatalogLib;

public class AzureAuthInterceptor(Func<Task<string>> getAccessTokenAsync) : DbConnectionInterceptor
{
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (!(connection is SqlConnection conn))
        {
            return result;
        }

        conn.AccessToken = await getAccessTokenAsync();
        return result;
    }

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        throw new InvalidOperationException("Open connections asynchronously when using AAD authentication.");
    }
}