using Microsoft.PowerBI.Api.Models;

namespace FabricLib.Models;

public class PowerBiTenantDetails : PowerBiTenant
{
    public IList<Report> Reports { get; set; }
    public IList<Dataset> Datasets { get; set; }
    public IList<GroupUser> Members { get; set; }
}