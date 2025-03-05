namespace DeliveryOrderAPI.Models
{
    public class MODEL_GET_DO
    {   

        public List<string> fixDateYMD { get; set; }
        public List<string> list_no_delivery_in_fixed { get; set; } = new List<string>();
        public List<string> list_delivery_in_fixed { get; set; } = new List<string>();
        public List<string> list_holiday { get; set; } = new List<string>();
        public string startFixed { get; set; } = "";
        public string endFixed { get; set; } = "";
        public List<MRESULTDO> data { get; set; }
        public string nbr { get; set; }
        public List<DoPartMaster> PartMaster { get; set; }
        public List<string> Holiday { get; set; }   
        public Dictionary<string,Dictionary<string,bool>> VenderDelivery { get; set; }  

        public List<string> DciHoliday { get; set; }
        public List<DoVenderMaster> VenderMaster { get; set; }  
        public List<DoVenderMaster> VenderSelected {  get; set; } 
        public List<MPOAlpha01> ListPO { get; set; }

        public List<DoCalPallet> ListCalPallet { get; set; }
    }
}
