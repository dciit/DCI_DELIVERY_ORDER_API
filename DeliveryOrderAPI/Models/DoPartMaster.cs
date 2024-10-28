using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class DoPartMaster
{
    public int PartId { get; set; }

    public string Partno { get; set; } = null!;

    public string Cm { get; set; } = null!;

    public string? Diameter { get; set; }

    public string? VdCode { get; set; }

    public string? Description { get; set; }

    public int? Pdlt { get; set; }

    public string? Unit { get; set; }

    public int? BoxMin { get; set; }

    public int? BoxMax { get; set; }

    public int? BoxQty { get; set; }

    public int? BoxPerPallet { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? Active { get; set; }
}
