namespace DeliveryOrderAPI.Models
{
    public class CalendarAlpha
    {

        public string ymd { get; set; }
      
    }

    public class StockWarning
    {   
        public DateTime dateRound { get; set; }
        public DateTime date { get; set; }
        public string partNo { get; set; }

        public string cm { get; set; }


        public string description { get; set; }

        public string vdCode { get; set; }
        public string vdName { get; set; }

        public int stock { get; set; }

    }
}
