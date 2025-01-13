namespace DeliveryOrderAPI.Models
{
    public class DOPartNumberForPU
    {

        public string vdcode { get; set; }
        public string partno { get; set; }

        public string cm { get; set; }


        public string desc { get; set; }


        public int boxQty { get; set; }

        public int minQty { get; set; }

        public string updateBy { get; set; } = "";
    }
}
