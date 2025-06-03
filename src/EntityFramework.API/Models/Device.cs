using System;
using System.Collections.Generic;

namespace EntityFramework;

public partial class Device
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public string AdditionalProperties { get; set; } = null!;

    public int? DeviceTypeId { get; set; }

    public ICollection<DeviceEmployee> DeviceEmployees { get; set; } = new List<DeviceEmployee>();

    public DeviceType? DeviceType { get; set; }
}
