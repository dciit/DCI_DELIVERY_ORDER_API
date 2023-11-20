using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class DoStockAlpha
{
    /// <summary>
    /// วันที่ มี STOCK ALPHA
    /// </summary>
    public DateOnly DatePd { get; set; }

    public string Partno { get; set; } = null!;

    public string Cm { get; set; } = null!;

    public string Vdcode { get; set; } = null!;

    public double Stock { get; set; }

    /// <summary>
    /// 999 คือใช้งาน นอกนั้นให้เก็บเป็น REV = 1, 2 , 3 , ....
    /// </summary>
    public int Rev { get; set; }

    public DateTime? InsertDt { get; set; }
}
