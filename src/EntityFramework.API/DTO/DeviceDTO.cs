using System.ComponentModel.DataAnnotations;

namespace EntityFramework.DTO;

public class DeviceDTO
{
    [Required(ErrorMessage = "Device name is required.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Device type name is required.")]
    [MaxLength(100)]
    public string DeviceTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "IsEnabled must be provided.")]
    public bool IsEnabled { get; set; }

    [Required(ErrorMessage = "AdditionalProperties is required.")]
    public object AdditionalProperties { get; set; } = null!;
}