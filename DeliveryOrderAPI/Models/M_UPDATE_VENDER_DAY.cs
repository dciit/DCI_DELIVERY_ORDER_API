namespace DeliveryOrderAPI.Models
{
    public class M_UPDATE_VENDER_DAY
    {
        public string vender { get; set; }  
        public string? day { get; set; }
        public int? date { get; set; }   

        public bool check { get; set; } = false;
    }
}
