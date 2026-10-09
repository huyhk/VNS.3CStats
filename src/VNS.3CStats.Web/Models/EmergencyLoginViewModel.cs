using System.ComponentModel.DataAnnotations;

namespace VNS.ThreeCStats.Web.Models;

public sealed class EmergencyLoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
