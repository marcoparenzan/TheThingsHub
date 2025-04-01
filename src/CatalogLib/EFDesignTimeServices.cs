using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using FabricLib;
using System.Data.Common;

namespace CatalogLib;

class EFDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
        => services.AddSingleton<IDatabaseModelFactory, EFDatabaseModelFactory>();
}

class EFDatabaseModelFactory : SqlServerDatabaseModelFactory
{
    public EFDatabaseModelFactory(IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger, IRelationalTypeMappingSource typeMappingSource) : base(logger, typeMappingSource)
    {
    }

    public override DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
    {
        // TODO: Acquire an access token
        ((SqlConnection)connection).AccessToken = ""; //  AccessTokenHelper.GetAccessTokenAsync().Result;

        return base.Create(connection, options);
    }
}