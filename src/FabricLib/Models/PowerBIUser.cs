using System.ComponentModel.DataAnnotations;

namespace FabricLib.Models;

public class PowerBIUser
{
    [Key]
    public string LoginId { get; set; }
    public string UserName { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCreate { get; set; }
    public bool TenantAdmin { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastLogin { get; set; }
    public string TenantName { get; set; }
}
