using System.ComponentModel.DataAnnotations;

namespace FabricLib.Models;

public class PowerBiTenant
{
    [Key]
    public string Name { get; set; }
    public string ProfileId { get; set; }
    public string WorkspaceId { get; set; }
    public string WorkspaceUrl { get; set; }
    public string DatabaseServer { get; set; }
    public string DatabaseName { get; set; }
    public string DatabaseUserName { get; set; }
    public string DatabaseUserPassword { get; set; }
    public DateTime Created { get; set; }
}
