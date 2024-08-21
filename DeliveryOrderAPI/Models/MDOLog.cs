namespace DeliveryOrderAPI.Models
{
    public class MDOLog
    {
        public string runningCode {  get; set; } = "";
        public string partNo { get; set; } = "";
        public string dateVal { get; set; } = "";
        public double prevDO { get; set; } = 0.0;
        public double doVal {  get; set; } = 0.0;
        public string status {  get; set; }
        public DateTime dtInsert { get; set; }
        public DateTime dtUpdate { get; set; }
        public string updateBy { get; set; } = "";
    }
}
