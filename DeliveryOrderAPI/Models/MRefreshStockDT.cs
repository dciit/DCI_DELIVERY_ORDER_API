using System.Data;

namespace DeliveryOrderAPI.Models
{
    public class MRefreshStockDT
    {
        public double stock {  get; set; }
        public DataTable dt { get; set; }
    }
}
