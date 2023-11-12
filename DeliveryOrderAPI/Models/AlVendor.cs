using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class AlVendor
{
    public string Vender { get; set; } = null!;

    public string? VenderName { get; set; }

    public string? AbbreName { get; set; }

    public string? Route { get; set; }

    public string? Currency { get; set; }

    public string? EmailPo { get; set; }

    public string? Boitype { get; set; }

    public string? PersonIncharge { get; set; }

    public bool? IsMilkRun { get; set; }

    public string? VenderCard { get; set; }
}
