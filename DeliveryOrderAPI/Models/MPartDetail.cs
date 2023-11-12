namespace DeliveryOrderAPI.Models
{
    public class MPartDetail
    {
        public string Part { get; set; }
        public int Pdlt { get; set; }
        public int BoxCap { get; set; }
        public string Date { get; set; }
        public int PartShort { get; set; }
        public double Stock { get; set; }
        public double StockSim { get; set; }
    }

}
