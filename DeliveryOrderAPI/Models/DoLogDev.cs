namespace DeliveryOrderAPI.Models
{
    public class DoLogDev
    {
        public int? logId { get; set; }
        public string? doRunning { get; set; }
        public int? doRev { get; set; } 
        public string? logPartNo { get; set; }
        public string? logVdCode { get; set; }
        public int? logProdLead { get; set; } = 0;
        public string? logType { get; set; }
        public string? logFromDate { get; set; }
        public double? logFromStock { get; set; } = 0;
        public double? logFromPlan { get; set; } = 0;
        public string? logToDate { get; set; }
        public string? logNextDate { get; set; }
        public double? logNextStock { get; set; } = 0;
        public double? logToStock { get; set; } = 0;
        public double? logDo { get; set; } = 0;
        public double? logBox { get; set; } = 0;
        public string? logState { get; set; }
        public DateTime? logCreateDate { get; set; }
        public DateTime? logUpdateDate { get; set; }
        public string? logUpdateBy { get; set; }
        public string? logRemark { get; set; }
    }
}
