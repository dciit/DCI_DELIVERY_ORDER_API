using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class AlGstDatpid
{
    public string Wcno { get; set; } = null!;

    public string Idate { get; set; } = null!;

    public string Itime { get; set; } = null!;

    public string Partno { get; set; } = null!;

    public string Kotei { get; set; } = null!;

    public string Brusn { get; set; } = null!;

    public decimal? Rqty { get; set; }

    public decimal? Sqty { get; set; }

    public decimal? Iqty { get; set; }

    public decimal? Fqty { get; set; }

    public string Slipno { get; set; } = null!;

    public string? Whum { get; set; }

    public string? Prgbit { get; set; }
}
