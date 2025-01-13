using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class ViDoPlan
{
    public int? Wcno { get; set; }

    public string? Prdym { get; set; }

    public string? Prdymd { get; set; }

    public int? PdLeadTime { get; set; }

    public string? Prdltymd { get; set; }

    public string? Model { get; set; }

    public decimal? Qty { get; set; }

    public string? Partno { get; set; }

    public string? Cm { get; set; }

    public string? Partname { get; set; }

    public string? Route { get; set; }

    public string? Catmat { get; set; }

    public string? Exp { get; set; }

    public decimal? Reqqty { get; set; }

    public string? Whunit { get; set; }

    public string? Vender { get; set; }

    public decimal? Ratio { get; set; }

    public double Consumption { get; set; }
}

public class DoPlanP91
{
    public string? wcno { get; set; }

    public string? prdymd { get; set; }

    public string? model { get; set; }


    public decimal? Qty { get; set;}
}
