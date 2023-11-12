using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class DoPlanDev
{
    public int RunningId { get; set; }

    public string? RunningCode { get; set; }

    public string? Ymd { get; set; }

    public string? PartNo { get; set; }

    public double? PlanAct { get; set; }
}
