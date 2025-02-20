using Microsoft.Identity.Client;
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




public class DoResPartList
{
    public int PdLeadTime { get; set; }
    public string MODEL { get; set; }
    public string PARTNO { get; set; }
    public string CM { get; set; }
    public string PARTNAME { get; set; }
    public string ROUTE { get; set; }
    public string CATMAT { get; set; }
    public int EXP { get; set; }
    public decimal REQQTY { get; set; }
    public string WHUNIT { get; set; }
    public string VENDER { get; set; }
    public decimal RATIO { get; set; }
}




//public class DoPlanResult
//{   
//    public string WCNO { get; set; }

//    public string PRDYM { get; set; }

//    public string PRDYMD { get; set; }
//    public int PdLeadTime { get; set; }

//    public string PRDLTYMD { get; set; }
//    public string MODEL { get; set; }


//    public decimal QTY { get; set; }
//    public string PARTNO { get; set; }
//    public string CM { get; set; }
//    public string PARTNAME { get; set; }
//    public string ROUTE { get; set; }
//    public string CATMAT { get; set; }
//    public int EXP { get; set; }
//    public decimal REQQTY { get; set; }
//    public string WHUNIT { get; set; }
//    public string VENDER { get; set; }
//    public decimal RATIO { get; set; }

//    public decimal CONSUMPTION { get; set; }
//}

