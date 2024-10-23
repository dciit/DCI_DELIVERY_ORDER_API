namespace DeliveryOrderAPI.Params
{
    public class ParamAddPartMaster
    {
        public string drawing { get; set; } = "";
        public string cm { get; set; } = "";
        public string vender { get; set; } = "";
        public string description { get; set; } = "";
        public string unit { get; set; } = "";
        public int boxMin { get; set; } = 0;
        public int? boxMax { get; set; } = 99999;
        public int boxQty { get; set; } = 0;
        public string updateBy { get; set; } = "";
    }
}
