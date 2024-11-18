namespace DeliveryOrderAPI.Models
{
    public class MDORESULT
    {
        public int? id { get; set; }
        public int? index { get; set; }
        public string? vdCode { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public string? currentDate { get; set; }
        public string part { get; set; }
        public bool CalDoWhenStockMinus { get; set; }
        public bool calMinMax { get; set; } // เช็คยอด  min max delivery vender
        public DateTime date { get; set; }
        public double plan { get; set; } 
        public double picklist { get; set; } 
        public double doPlan { get; set; } 
        public double doAct { get; set; } 
        public double stock { get; set; } 
        public double stockSim { get; set; } 
        public double planNow { get; set; } 
        public double doBalance { get; set; } 
        public double po { get; set; } 
        public double? doAd
        
        { get; set; }

        public string? runCode { get; set; }
        public string? runRev { get; set; }

        public string? buyer { get; set; } = "";

        public string? timeScheduleDelivery { get; set; }

        public string? model { get; set; }
    }
}
