namespace DeliveryOrderAPI.Models
{
    public class MDO
    {
        public string Part { get; set; }
        public string Date { get; set; }
        public int Plan { get; set; }
        public int DOPlan {  get; set; }
        public int DOAct { get; set; }
        public int Stock { get; set; }
        public int StockSim { get; set; }
        public int PO {  get; set; }
        public string SupplierCode {  get; set; }
    }
}
