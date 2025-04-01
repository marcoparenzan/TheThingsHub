# COMMAND
Scaffold-DbContext "Data Source=(localdb)\mssqllocaldb;Initial Catalog=UnifiedNamespace2025;Trusted_Connection=True;User Id=UnifiedNamespace;Password=UnifiedNamespace" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models\Database -Force -Schemas UnifiedNamespace -Context DtContext

