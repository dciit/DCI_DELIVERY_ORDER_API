namespace DeliveryOrderAPI.Models
{
    public class MRESRESULT
    {
        public List<MGetPlan> Plan { get; set; }
        public List<DoDictMstr> Holiday { get; set; }

        public List<DoPartMaster> PartMasters { get; set; }
    }
}
