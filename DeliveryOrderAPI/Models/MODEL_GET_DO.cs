namespace DeliveryOrderAPI.Models
{
    public class MODEL_GET_DO
    {   

        public List<string> fixDateYMD { get; set; }
        public List<MRESULTDO> data { get; set; }
        public string nbr { get; set; }
        public List<DoPartMaster> PartMaster { get; set; }
        public List<string> Holiday { get; set; }   
        public Dictionary<string,Dictionary<string,bool>> VenderDelivery { get; set; }  
        public List<DoVenderMaster> VenderMaster { get; set; }  
        public List<DoVenderMaster> VenderSelected {  get; set; } 
        public List<MPOAlpha01> ListPO { get; set; }
    }
}
