using System.Data;

namespace DeliveryOrderAPI.Models
{
    public class MGetPlanDataSet
    {
        public string runningCode { get; set; }
        public string refCode { get; set; }

        //public string? runCode { get; set; } 
        //public int? runRev { get; set; }
        public List<MPO> po { get; set; }
        public List<MGetPlan> data { get; set; }
        public List<DoVenderMaster>? vdMstr { get; set; }
        public List<DoDictMstr>? holiday { get; set; }
        public Dictionary<string, DoMaster> master { get; set; }

        public List<MPO> pos { get; set; }

    }
}
