using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class DoHistoryDev
{
    public int Id { get; set; }

    public string? RunningCode { get; set; }

    public int? Rev { get; set; }

    public string? Model { get; set; }

    public string? Partno { get; set; }

    public string? Cm { get; set; }

    public string? DateVal { get; set; }

    public double? PlanVal { get; set; }

    public double? DoVal { get; set; }

    public double? StockVal { get; set; }

    public double? Stock { get; set; }

    public string? VdCode { get; set; }

    public DateTime? InsertDt { get; set; }

    public string? InsertBy { get; set; }

    public string? Status { get; set; }

    public int? Revision { get; set; }

    public string? TimeScheduleDelivery { get; set; }
}
