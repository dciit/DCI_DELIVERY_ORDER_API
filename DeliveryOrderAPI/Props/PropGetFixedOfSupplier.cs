namespace DeliveryOrderAPI.Props
{
    public class PropGetFixedOfSupplier
    {
        public DateTime end_date { get; set; } // วันสุดท้ายของช่วง Fixed
        public DateTime str_date { get; set; } // วันแรกของช่วง Fixed
        public int leadtime { get; set; } = 3;
        public List<string> list_no_delivery_in_fixed { get; set; } = new List<string>(); // รายการวันที่ไม่ ส่งของ อยู่ในช่วง Fixed
        public List<string> list_delivery_in_fixed { get; set; } = new List<string>();
        public List<string> list_holiday { get; set; } = new List<string>();
    }
}
