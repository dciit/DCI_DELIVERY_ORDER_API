namespace DeliveryOrderAPI.Models
{
    public class M_UPDATE_VENDER_DETAIL
    {
        public string vender { get; set; }
        public int min { get; set; }
        public int max { get; set; }
        public int round { get; set; }
        //public string timeSchedule { get; set; }    
        public bool vdBoxPeriod { get; set; } = false;
        public int vdProdLead { get; set; } = 7;
    }
}
