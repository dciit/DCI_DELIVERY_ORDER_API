namespace DeliveryOrderAPI.Models
{
    public class MEditDO
    {
        public string runningCode { get; set; }
        public string ymd { get; set; }
        public string partno { get; set; }
        public double doVal { get; set; }
        public double doPrev { get; set; }
        public string? empCode { get; set; }
    }
}
