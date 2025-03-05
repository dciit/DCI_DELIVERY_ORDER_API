namespace DeliveryOrderAPI.Props
{
    public class PropBuyers
    {
        public List<PropBuyer> buyers { get; set; } = new List<PropBuyer>();
        public List<PropSupplier> suppliers { get; set; } = new List<PropSupplier>();
    }

    public class PropSupplier
    {
        public string buyer { get; set; } = "";
        public string buyer_name { get; set; } = "";

        public string supplier { get; set; } = "";
        public string supplier_name { get; set; } = "";
    }

    public class PropBuyer
    {
        public string code { get; set; } = "";
        public string name { get; set; } = "";
    }
}
