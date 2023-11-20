namespace DeliveryOrderAPI.Models
{
    public class ModelAPI
    {
        public string key { get; set; } 
        public string vender {  get; set; }
        public string partno { get; set; }
        public List<MRESULTDO> data { get; set; }
        public DoPartMaster master { get; set; }
    }
}
