namespace DeliveryOrderAPI.Models
{
    public class MRESULTDO
    {
        public string PartNo { get; set; }  

        public string Cm { get; set; }
        public double Plan {  get; set; }   
        public double PlanPrev {  get; set; }
        public double Do { get; set; }
        public double Stock { get; set; }
        public double PickList { get; set; }
        public double DoAct { get; set; }
        public string Vender { get; set; }
        public double PO {  get; set; } 
        public double Wip { get; set; } 
        public DateTime Date { get; set; }  
        public double POFIFO { get; set; }
        public bool holiday { get; set; } = false;
        public List<DoLogDev>? Log { get; set; } = new List<DoLogDev>();
        public string? vdCode { get; set; } = "";
        public string? vdName { get; set; } = "";

        public bool? changePlan { get; set; } = false;

        public decimal? HistoryDevPlanQTY { get; set; } = 0;

        public int? Box { get; set; } = 0;

        public int? Pallet { get; set; } = 0;

    }
}
