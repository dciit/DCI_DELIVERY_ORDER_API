using System;
using System.Collections.Generic;

namespace DeliveryOrderAPI.Models;

public partial class DoVenderMaster
{
    public string VdCode { get; set; } = null!;

    public string? VdDesc { get; set; }

    public bool? VdLimitBox { get; set; }

    public double? VdBox { get; set; }

    public bool? VdBoxPeriod { get; set; }

    public int? VdRound { get; set; }

    public double? VdMinDelivery { get; set; }

    public double? VdMaxDelivery { get; set; }

    public bool? VdMon { get; set; }

    public bool? VdTue { get; set; }

    public bool? VdWed { get; set; }

    public bool? VdThu { get; set; }

    public bool? VdFri { get; set; }

    public bool? VdSat { get; set; }

    public bool? VdSun { get; set; }

    public bool? VdDay1 { get; set; }

    public bool? VdDay2 { get; set; }

    public bool? VdDay3 { get; set; }

    public bool? VdDay4 { get; set; }

    public bool? VdDay5 { get; set; }

    public bool? VdDay6 { get; set; }

    public bool? VdDay7 { get; set; }

    public bool? VdDay8 { get; set; }

    public bool? VdDay9 { get; set; }

    public bool? VdDay10 { get; set; }

    public bool? VdDay11 { get; set; }

    public bool? VdDay12 { get; set; }

    public bool? VdDay13 { get; set; }

    public bool? VdDay14 { get; set; }

    public bool? VdDay15 { get; set; }

    public bool? VdDay16 { get; set; }

    public bool? VdDay17 { get; set; }

    public bool? VdDay18 { get; set; }

    public bool? VdDay19 { get; set; }

    public bool? VdDay20 { get; set; }

    public bool? VdDay21 { get; set; }

    public bool? VdDay22 { get; set; }

    public bool? VdDay23 { get; set; }

    public bool? VdDay24 { get; set; }

    public bool? VdDay25 { get; set; }

    public bool? VdDay26 { get; set; }

    public bool? VdDay27 { get; set; }

    public bool? VdDay28 { get; set; }

    public bool? VdDay29 { get; set; }

    public bool? VdDay30 { get; set; }

    public bool? VdDay31 { get; set; }

    public string? VdTimeScheduleDelivery { get; set; }

    public int? VdProdLead { get; set; }

    public string? VdStatus { get; set; }

    public int VdSafetyStock { get; set; }
}
