namespace DeliveryOrderAPI.Models
{
    public class MPlan
    {
        public string? Wcno { get; set; }
        public int? DayPlan { get; set; }
        public string? DatePlan { get; set; }
        public string? Model { get; set; }
        public string? PartNo { get; set; }
        public double? Useg { get; set; } = 0;
        public float? Suryo { get; set; } = 0;
        public double Plan { get; set; } = 0;
        public double QtyBox { get; set; } = 0;
        public string Cm { get; set; }  
        public string PartDesc { get; set; }
        public double PickList { get; set; } = 0;
        public double Stock { get; set; } = 0;
        public double StockSimulate { get; set; } = 0;
        public double DoPlan { get; set; } = 0;
        public double DoAct { get; set; } = 0;
        public double Po { get; set; } = 0;


    }
}
