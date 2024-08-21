using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class DoDictMstr
{
    public int DictId { get; set; }

    public string? DictType { get; set; }

    public string? Code { get; set; }

    public string? Description { get; set; }

    public string? RefCode { get; set; }

    public string? Note { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? DictStatus { get; set; }
}
