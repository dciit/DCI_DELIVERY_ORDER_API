namespace DeliveryOrderAPI.Models
{
    public class M_PO_DATA
    {
        public string? vender { get; set; }

        public string? partNo { get; set; }
        public double? totalPO { get; set; }

        public string? sort { get; set; } = "desc";
        public string? startDate { get; set; } = DateTime.Now.ToString("yyyyMMdd");
        public string? endDate { get; set; } = DateTime.Now.ToString("yyyyMMdd");
    }
}
