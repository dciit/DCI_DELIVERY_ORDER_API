using Newtonsoft.Json;

namespace DeliveryOrderAPI.Models
{
    public class M_INSERT_LIST_DRAWING_DELIVERY_OF_DAY
    {
        public long Id { get; set; }

        public string Part { get; set; }

        public long Do { get; set; }

        public long Input { get; set; }

        public bool Checked { get; set; }

        public long Date { get; set; }

        public string Wh { get; set; }
    }
}
