namespace DeliveryOrderAPI.Models
{
    public class MSetDoPlan
    {
        public DateTime DateRunDo { get; set; }
        public List<MGetPlan> plan { get; set; }
        public Dictionary<string, DoMaster> master { get; set; }
        //public List<string> parts { get; set; }
    }
}
