namespace DeliveryOrderAPI.Models
{
    public class MGetPlan
    {
        public int? id { get; set; }
        public int? index { get; set; }
        public string? vdCode { get; set; }  
        
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public string? currentDate { get; set; }
        public string? partNo { get; set; }
        public bool CalDoWhenStockMinus { get; set; }
        public bool calMinMax { get; set; } // เช็คยอด  min max delivery vender
        public string date { get; set; } = "0";
        public string plan { get; set; } = "0";
        public string picklist {get; set; } = "0";
        public string doPlan { get; set; } = "0";
        public string doAct { get; set; } = "0";
        public string stock { get; set; } = "0";
        public string stockSim { get; set; } = "0";
        public string part { get; set; } = "0";
        public string planNow { get; set; } = "0";
        public string doBalance { get; set; } = "0";
        public string po {  get; set; } = "0";  
        public string? doAddFixed { get; set; }

        public string? runCode { get; set; }
        public string? runRev { get; set; }

        public string? buyer { get; set; } = "";

        public string? timeScheduleDelivery { get; set; }

        public string? model { get; set; }
        public bool? poshort {  get; set; }
    }
}
