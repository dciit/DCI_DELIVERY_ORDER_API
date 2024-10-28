using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace DeliveryOrderAPI
{
    public class Services
    {
        private readonly DBSCM _DBSCM;
        private readonly DBHRM _DBHRM;
        private SqlConnectDB dbSCM = new("dbSCM");
        private OraConnectDB dbAlpha = new("ALPHA01");
        private OraConnectDB dbAlpha2 = new("ALPHA02");
        private Helper oHelper = new Helper();
        public Services(DBSCM dBSCM, DBHRM dBHRM)
        {
            _DBSCM = dBSCM;
            _DBHRM = dBHRM;
        }
        //public double ConvDay(string valDay = "")
        //{
        //    return (valDay == null || valDay == "null" || valDay == "") ? 0 : double.Parse(valDay);
        //}

        //public DataTable GetPartOfVender(string SupplierCode)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    //sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.PARTNO IN ('" + _PartJoin + "')";
        //    sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.VD_CODE = '" + SupplierCode + "' ORDER BY PARTNO ASC";
        //    DataTable dt = dbSCM.Query(sql);
        //    return dt;
        //}
        public List<MGetListBuyer> GetListBuyer()
        {
            var ListBuyer = _DBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.RefCode != "").ToList();
            List<MGetListBuyer> res = (from item in (from dict in ListBuyer
                                                     select new
                                                     {
                                                         code = dict.Code
                                                     }).GroupBy(x => x.code).ToList()
                                       join emp in _DBHRM.Employees
                                  on item.Key equals emp.Code
                                       select new MGetListBuyer
                                       {
                                           empcode = emp.Code,
                                           fullname = $"{emp.Pren!.ToUpper()}{emp.Name} {emp.Surn}"
                                       }).ToList();
            return res;
        }
        public List<MStockAlpha> GetStockPS8AM(DateTime _Date, string _Vender = "")
        {
            List<MStockAlpha> res = new List<MStockAlpha>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT PARTNO,STOCK,CM FROM [dbSCM].[dbo].[DO_STOCK_ALPHA] WHERE VDCODE = @VDCODE AND REV = 999";
            sql.Parameters.Add(new SqlParameter("VDCODE", _Vender));
            sql.Parameters.Add(new SqlParameter("DATE", _Date.ToString("yyyyMMdd")));
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    res.Add(new MStockAlpha()
                    {
                        Part = dr["PARTNO"].ToString().Trim(),
                        Stock = oHelper.ConvStrToDB(dr["STOCK"].ToString()),
                        Cm = dr["CM"].ToString().Trim()
                    });
                }
                catch
                {

                }
            }
            return res;
        }
        public double GetDoVal(double stockSim, DoPartMaster oPartStd, DoVenderMaster oVdStd)
        {
            try
            {
                double _DoVal = 0;
                if (stockSim > 0)
                {
                    _DoVal = Math.Ceiling(stockSim / oHelper.ConvUnInt2Db(oPartStd.BoxQty)) * oHelper.ConvUnInt2Db(oPartStd.BoxQty);
                    _DoVal = _CheckMinDelivery(_DoVal, oHelper.ConvUnInt2Db(oPartStd.BoxMin));
                    _DoVal = _CheckMaxDelivery(_DoVal, oHelper.ConvUnInt2Db(oPartStd.BoxMax));
                    if (oVdStd.VdBoxPeriod == true)
                    {
                        decimal _box = oHelper.ConvIntToDec(oPartStd.BoxQty != null ? (int)oPartStd.BoxQty : 0);
                        decimal _minQty = oHelper.ConvIntToDec(oPartStd.BoxMin != null ? (int)oPartStd.BoxMin : 0);
                        decimal _do = oHelper.ConvDBToDec(_DoVal);
                        decimal _used = Math.Ceiling(_do / _minQty);
                        _DoVal = oHelper.ConvDecToDB(_used * _minQty);
                    }
                }
                //double safetyStock = oHelper.ConvInt2Db(oVdStd.VdSafetyStock);
                //if ((stockSim + _DoVal) < safetyStock)
                //{
                //    _DoVal += (safetyStock - (stockSim + _DoVal));
                //}
                return _DoVal;
            }
            catch
            {
                return 0;
            }
        }
        private double _CheckMaxDelivery(double doVal, double maxQty)
        {
            try
            {
                double _DoValByMax = 0;
                if (doVal > maxQty)
                {
                    _DoValByMax = maxQty;
                }
                else
                {
                    _DoValByMax = doVal;
                }
                return _DoValByMax;
            }
            catch
            {
                return doVal;
            }
        }
        private double _CheckMinDelivery(double doVal, double minQty)
        {
            double _DoValByMin = doVal;
            try
            {
                if (doVal <= minQty)
                {
                    _DoValByMin = minQty;
                }
                return _DoValByMin;
            }
            catch
            {
                return _DoValByMin;
            }
        }
        public List<MDOAct> GetDoActs(DateTime sDate, DateTime eDate, string Parts, string SupplierCode)
        {
            List<MDOAct> res = new List<MDOAct>();
            OracleCommand cmd = new();
            cmd.CommandText = "SELECT PARTNO,SUM(WQTY) AS WQTY,ACDATE,HTCODE FROM DST_DATAC1 WHERE TRIM(PARTNO) IN ('" + Parts + "') AND TRIM(ACDATE) BETWEEN '" + sDate.ToString("yyyyMMdd") + "' AND '" + eDate.ToString("yyyyMMdd") + "' AND HTCODE = '" + SupplierCode + "' GROUP BY PARTNO,ACDATE,HTCODE";
            DataTable dt = dbAlpha2.Query(cmd);
            foreach (DataRow dr in dt.Rows)
            {
                MDOAct item = new MDOAct();
                item.PartNo = dr["PARTNO"]!.ToString()!.Trim();
                item.Wqty = double.Parse(dr["WQTY"]!.ToString()!);
                item.AcDate = dr["ACDATE"]!.ToString()!;
                item.Vender = dr["HTCODE"]!.ToString()!;
                res.Add(item);
            }
            return res;
        }
        internal Dictionary<string, bool> getDaysDelivery(DoVenderMaster? venderMaster)
        {
            Dictionary<string, bool> res = new Dictionary<string, bool>
            {
                { "MON", bool.Parse(venderMaster.VdMon.ToString()) },
                { "TUE", bool.Parse(venderMaster.VdTue.ToString()) },
                { "WED", bool.Parse(venderMaster.VdWed.ToString()) },
                { "THU", bool.Parse(venderMaster.VdThu.ToString()) },
                { "FRI", bool.Parse(venderMaster.VdFri.ToString()) },
                { "SAT", bool.Parse(venderMaster.VdSat.ToString()) },
                { "SUN", bool.Parse(venderMaster.VdSun.ToString()) }
            };
            return res;
        }
        internal List<MPO> GetPoDIT(DateTime sDate, DateTime eDate, string PartJoin)
        {
            List<MPO> res = new List<MPO>();
            OracleCommand cmd = new();
            cmd.CommandText = @"SELECT A.PARTNO,A.DELYMD,SUM(A.WHBLBQTY)  AS WHBLBQTY,B.HTCODE FROM GST_DATOSD A LEFT JOIN GST_DATOSC B ON A.PONO = B.PONO WHERE  A.apbit in ('U','P') AND TRIM(A.PARTNO) IN ('" + PartJoin + "')  AND A.DELYMD >= '" + sDate.ToString("yyyyMMdd") + "' AND A.DELYMD <= '" + eDate.ToString("yyyyMMdd") + "' GROUP BY A.PARTNO,A.DELYMD,B.HTCODE";
            DataTable dt = dbAlpha.Query(cmd);
            foreach (DataRow dr in dt.Rows)
            {
                MPO item = new MPO();
                item.date = dr["DELYMD"].ToString();
                item.qty = double.Parse(dr["WHBLBQTY"].ToString());
                item.partNo = dr["PARTNO"].ToString().Trim();
                item.vdCode = dr["HTCODE"].ToString().Trim();
                res.Add(item);
            }
            return res;
        }
        public List<ViDoPlan> GetPlans(string supplier, DateTime sDate, DateTime fDate)
        {
            List<ViDoPlan> res = new List<ViDoPlan>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT PRDYMD,PARTNO,CM,VENDER,ROUND(SUM(CONSUMPTION),0)  AS CONSUMPTION
                                         FROM [dbSCM].[dbo].[vi_DO_Plan]
                                         WHERE PRDYMD >= @SDATE 
                                         AND PRDYMD <= @FDATE AND VENDER = @SUPPLIER  GROUP BY PRDYMD,PARTNO,CM,VENDER ORDER BY PRDYMD ASC";
            sql.Parameters.Add(new SqlParameter("@SDATE", sDate.ToString("yyyyMMdd")));
            sql.Parameters.Add(new SqlParameter("@FDATE", fDate.ToString("yyyyMMdd")));
            sql.Parameters.Add(new SqlParameter("@SUPPLIER", supplier));
            DataTable dt = dbSCM.Query(sql);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    ViDoPlan item = new ViDoPlan();
                    item.Qty = Convert.ToDecimal(dr["CONSUMPTION"].ToString());
                    item.Prdymd = dr["PRDYMD"].ToString();
                    item.Partno = dr["PARTNO"].ToString();
                    item.Cm = dr["CM"].ToString();
                    res.Add(item);
                }
            }
            return res;
        }
        public List<DoPartMaster> GetPartByVenderCode(string VenderCode)
        {
            SqlCommand sql = new SqlCommand();
            List<DoPartMaster> res = _DBSCM.DoPartMasters.Where(x => x.VdCode.Trim() == VenderCode.Trim()).ToList();
            return res;
        }
        internal ModelRefreshStock REFRESH_STOCK(List<MRESULTDO> response, string vender, string part, string cm, List<MStockAlpha> stockAlpha)
        {
            List<MRESULTDO> res = response;
            DateTime dtNow = DateTime.Now;
            double StockLoop = 0;
            MStockAlpha ItemStock = stockAlpha.FirstOrDefault(x => x.Part == part && x.Cm == cm)!;
            if (ItemStock != null)
            {
                StockLoop = ItemStock.Stock;
            }
            var FirstItem = res.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.Date == dtNow.Date && x.Vender == vender && x.PartNo.Trim() == part);
            var LastItem = res.OrderBy(x => x.Date).LastOrDefault(x => x.Vender == vender && x.PartNo.Trim() == part);
            if (FirstItem != null && LastItem != null)
            {
                DateTime dtEnd = LastItem.Date;
                DateTime dtLoop = FirstItem.Date;
                while (dtLoop.Date <= dtEnd.Date)
                {
                    var ItemLoop = res.FirstOrDefault(x => x.Vender.Trim() == vender.Trim() && x.PartNo.Trim() == part.Trim() && x.Date.Date == dtLoop.Date);
                    int index = res.IndexOf(ItemLoop);
                    double PlanLoop = ItemLoop!.Plan;
                    double DOLoop = ItemLoop!.Do;
                    StockLoop = (StockLoop - PlanLoop) + DOLoop;
                    res[index].Stock = StockLoop;
                    //res[index].POFIFO -= dol
                    dtLoop = dtLoop.AddDays(1);
                }
            }
            else
            {
                Console.WriteLine("DATA NOT MATCH");
            }
            //int LastIndex = -1;
            //double Stock = 0;
            //if (LastItem != null)
            //{
            //    LastIndex = res.IndexOf(LastItem);
            //}
            //if (FirstIndex != -1 && LastIndex != -1)
            //{
            //    int StartIndex = FirstIndex;
            //    Stock = res[FirstIndex].Stock;
            //    double Plan = res[FirstIndex].Plan;
            //    double DO = 0;
            //    while (FirstIndex <= LastIndex)
            //    {
            //        DO = res[FirstIndex].Do;
            //        if (FirstIndex > StartIndex)
            //        {
            //            Plan = res[FirstIndex].Plan;
            //            Stock = (Stock - Plan) + DO;
            //        }
            //        else
            //        {
            //            Stock += DO;
            //        }
            //        res[FirstIndex].Stock = Stock;
            //        FirstIndex++;
            //    }
            //    dtLoop = dtLoop.AddDays(1);
            //}
            //else
            //{
            //    Console.WriteLine("ERROR");
            //}
            return new ModelRefreshStock() { data = res, Stock = StockLoop };
        }
        //public List<DoDictMstr> _GET_HOLIDAY()
        //{
        //    List<DoDictMstr> res = new List<DoDictMstr>();
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_DictMstr] WHERE DICT_TYPE = 'holiday'";
        //    DataTable dt = dbSCM.Query(sql);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        res.Add(new DoDictMstr()
        //        {
        //            Code = dr["CODE"].ToString()
        //        });
        //    }
        //    return res;
        //}
        public string CreateToken(string username)
        {

            List<Claim> claims = new()
            {
                new Claim("username", Convert.ToString(username)),
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("scm.daikin.co.jp"));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(15),
                signingCredentials: cred
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
        //internal int DayAvailable(DateTime dtLoop, List<DoDictMstr> rDictHoliday, Dictionary<string, bool> venderDelivery)
        //{
        //    int index = -1;
        //    List<string> rRestDay = venderDelivery.Where(x => x.Value == true).Select(x => x.Key).ToList();
        //    string ShortDay = dtLoop.ToString("ddd").ToUpper();
        //    index = rRestDay.FindIndex(x => x == ShortDay); // Short Day ตรงกับวัน
        //    bool isHoliday = rDictHoliday.FirstOrDefault(x => x.Code == dtLoop.ToString("yyyyMMdd")) != null ? true : false;
        //    index = isHoliday == true ? -1 : index; // -1 คือไม่สามารถส่งได้
        //    return index;
        //}

        internal List<MRESULTDO> ADJ_DO(List<MRESULTDO> response, string vender, string part, DateTime dtLoop, int pdlt, double DO, Dictionary<string, bool> VdMaster, DateTime dtRun, bool haveHistory, List<DoDictMstr> rDictHoliday, DoVenderMaster vdMaster, double stockFrom, double planForm, double StockCalOfDate, DoPartMaster PartMaster,double totalPOFIFO)
        {
            string YMDFormat = "yyyyMMdd";
            DateTime dtNow = DateTime.Now;
            DateTime dtEnd = dtNow.AddDays(14);
            DateTime dtEndFixed = dtNow.AddDays((vdMaster.VdProdLead != null ? (int)vdMaster.VdProdLead : 0) - 1);
            DateTime dt3PM = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 22, 0, 0);
            if (dtNow > dt3PM)
            {
                //over3PM = true;
                dtEndFixed = dtEndFixed.AddDays(1);
            }
            dtLoop = dtLoop.AddDays(-2); // หาวันที่สามารถลงยอด D/O ได้ใกล้และเร็วที่สุดจากวันที่ Stock ติดลบ - PrdLeadtime
            if (dtLoop.Date < dtEndFixed.Date)
            {
                dtLoop = dtEndFixed;
            }
            bool CanDelivery = VdMaster.ContainsKey(dtLoop.ToString("ddd").ToUpper()) ? VdMaster[dtLoop.ToString("ddd").ToUpper().ToUpper()] : false;
            bool Increment = false; // สำหรับการ เพิ่ม หรือ ลด วันที่ เพื่อหาวันที่สามารถจัดส่งได้
            while (CanDelivery == false)
            {
                if (Increment == false)
                {
                    dtLoop = dtLoop.AddDays(-1);
                    if (dtLoop.Date <= dtEndFixed.Date) // ==== ถ้าตำกว่าช่วง Fixed จะเปลี่ยน Increment เป็นขาบวก [+]
                    {
                        dtLoop = dtLoop.AddDays(1);
                        Increment = true;
                    }
                }
                else
                {
                    dtLoop = dtLoop.AddDays(1);
                    if (dtLoop.Date >= dtEnd.Date)
                    {
                        break;
                    }
                }
                CanDelivery = VdMaster.ContainsKey(dtLoop.ToString("ddd").ToUpper()) ? VdMaster[dtLoop.ToString("ddd").ToUpper().ToUpper()] : false;
            }
            if (CanDelivery == true)
            {
                int index = response.FindIndex(x => x.Vender == vender && x.PartNo == part && x.Date.ToString(YMDFormat) == dtLoop.ToString(YMDFormat));
                if (index != -1)
                {
                    response[index].Do += DO;
                    response[index].POFIFO = totalPOFIFO - DO;
                }
            }
            return response;
        }
        public MODEL_GET_DO CalDO(bool run, string vdcode = "", MGetPlan param = null, string doRunningCode = "", int doRev = 0, bool? hiddenPartNoPlan = true)
        {
            DateTime dtNow = DateTime.Now;
            DateTime dtDistribute = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 22, 0, 0);
            string ymd = DateTime.Now.ToString("yyyyMMdd");
            List<MPOAlpha01> rPOFIFO = new List<MPOAlpha01>();
            vdcode = vdcode != null ? vdcode : "";
            string buyer = param != null ? param.buyer : "";
            List<DoVenderMaster> oVenderSelect = new List<DoVenderMaster>();
            if (vdcode == "" && !run)
            {
                List<DoDictMstr> listVender = _DBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == buyer).ToList();
                if (listVender.Count > 0)
                {
                    oVenderSelect = _DBSCM.DoVenderMasters.Where(x => x.VdCode == listVender[0].RefCode).ToList();
                    if (oVenderSelect.Count > 0)
                    {
                        vdcode = oVenderSelect[0].VdCode;
                    }
                }
            }
            else
            {
                oVenderSelect = _DBSCM.DoVenderMasters.Where(x => x.VdCode == vdcode).ToList();
            }
            List<MAlpart> rAlPart = new List<MAlpart>();
            SqlCommand sqlAlPart = new SqlCommand();
            sqlAlPart.CommandText = @"SELECT DrawingNo,CM  FROM [dbSCM].[dbo].[AL_Part]  where Route = 'D'";
            DataTable dtAlPart = dbSCM.Query(sqlAlPart);
            foreach (DataRow dr in dtAlPart.Rows)
            {
                MAlpart itemAlPart = new MAlpart();
                itemAlPart.DrawingNo = dr["DrawingNo"].ToString();
                itemAlPart.Cm = dr["CM"].ToString();
                rAlPart.Add(itemAlPart);
            }
            string YMDFormat = "yyyyMMdd";
            string nbr = "";
            List<MRESULTDO> DOITEM = new List<MRESULTDO>();
            List<DoPartMaster> PARTMASTER = new List<DoPartMaster>();
            List<DoVenderMaster> VENDERMASTER = new List<DoVenderMaster>();
            Dictionary<string, Dictionary<string, bool>> VenderDeliveryMaster = new Dictionary<string, Dictionary<string, bool>>();
            int dFixed = 2;
            int dRun = 7;
            DateTime dtStart = dtNow.AddDays(-7);
            DateTime dtRun = dtNow.AddDays(dFixed);
            DateTime dtEnd = dtNow.AddDays(dFixed + dRun);
            /* A003 */
            int countFixed = 13;
            List<DoDictMstr> rDictHoliday = _DBSCM.DoDictMstrs.Where(x => x.DictType == "holiday" && Convert.ToInt32(x.Code) >= Convert.ToInt32(ymd) && Convert.ToInt32(x.Code) <= Convert.ToInt32(dtNow.AddDays(countFixed).ToString("yyyyMMdd"))).ToList();
            /* [E] A003 */

            List<DoHistoryDev> Historys = new List<DoHistoryDev>();
            try
            {
                Historys = _DBSCM.DoHistoryDevs.Where(x => x.Revision == 999).OrderBy(x => x.Partno).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (Historys.Count > 0)
            {
                int rev = (int)Historys.FirstOrDefault()!.Rev!;
                nbr = $"{Historys.FirstOrDefault()!.RunningCode}{rev.ToString("D3")}";
            }
            List<DoVenderMaster> ListVenderMaster = _DBSCM.DoVenderMasters.ToList();
            List<DoDictMstr> ListVenderOfBuyer = _DBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == "41256" && x.DictStatus == "999").ToList();
            if (vdcode != "")
            {
                ListVenderOfBuyer = ListVenderOfBuyer.Where(x => x.RefCode == vdcode).ToList();
            }
            List<DoVenderMaster> VdStds = _DBSCM.DoVenderMasters.ToList();
            foreach (DoDictMstr itemVender in ListVenderOfBuyer)
            {
                string vender = itemVender.RefCode!;
                DoVenderMaster oVdStd = VdStds.FirstOrDefault(x => x.VdCode == vender)!;
                if (oVdStd != null)
                {
                    int VdProdLead = Convert.ToInt32(oVdStd.VdProdLead) - 1;
                    dtRun = dtNow.AddDays(VdProdLead);
                    dtEnd = dtNow.AddDays(VdProdLead + dRun);
                }
                Dictionary<string, bool> VenderDelivery = getDaysDelivery(oVdStd);
                if (!VenderDeliveryMaster.ContainsKey(vender)) { VenderDeliveryMaster.Add(vender, VenderDelivery); }
                List<ViDoPlan> Plans = GetPlans(vender, dtNow, dtEnd);
                List<DoPartMaster> Parts = GetPartByVenderCode(vender);
                PARTMASTER.AddRange(Parts);
                VENDERMASTER.Add(oVdStd);
                string PartJoin = string.Join("','", Parts.GroupBy(x => x.Partno).Select(y => y.Key).ToList());
                List<MStockAlpha> StockAlpha = GetStockPS8AM(dtNow, vender);
                List<MPickList> PickLists = GetPickListBySupplier(PartJoin, dtStart, dtEnd);
                List<MDOAct> DOActs = GetDoActs(dtStart, dtEnd, PartJoin, vender);
                List<MPO> POs = GetPoDIT(dtStart, dtEnd, PartJoin);

                DateTime dtEndFixed = dtNow.AddDays((oVdStd.VdProdLead != null ? (int)oVdStd.VdProdLead : 0) - 1);
                OracleCommand strGetPo = new OracleCommand();
                strGetPo.CommandText = @"SELECT C.HTCODE, G.PONO, G.ITEMNO, 
                                           G.PARTNO, G.BRUSN, 
                                           G.DELYMD,  
                                           C.ODRYMD, G.CALYMD,G.RCSYMD, 
                                           G.RCCYMD, 
                                           G.WHUM, G.WHQTY,  G.WHBLQTY, G.WHBLBQTY, 
                                           G.IVUM, G.IVQTY,  G.IVBLQTY, G.IVBLBQTY,    
                                           G.APBIT 
                                        FROM MASTER.GST_DATOSD G
                                        LEFT JOIN MASTER.GST_DATOSC C ON C.PONO = G.PONO 
                                        WHERE G.jibu = '64'   
                                          and itemno like '%' 
                                          and APBIT IN ('U','P')
                                          and C.htcode = :htcode";
                strGetPo.Parameters.Add(new OracleParameter(":htcode", vender));
                DataTable dt = dbAlpha.Query(strGetPo);
                foreach (DataRow dr in dt.Rows)
                {
                    MPOAlpha01 iPOFIFO = new MPOAlpha01();
                    iPOFIFO.pono = dr["PONO"].ToString();
                    iPOFIFO.itemno = dr["ITEMNO"].ToString();
                    iPOFIFO.htcode = dr["HTCODE"].ToString();
                    iPOFIFO.whqty = dr["WHQTY"].ToString();
                    iPOFIFO.whblbqty = Convert.ToDouble(dr["WHBLBQTY"].ToString());
                    iPOFIFO.whblqty = Convert.ToDouble(dr["WHBLQTY"].ToString());
                    iPOFIFO.partno = dr["PARTNO"].ToString().Trim();
                    iPOFIFO.brusn = dr["BRUSN"].ToString();
                    iPOFIFO.delymd = dr["DELYMD"].ToString();
                    rPOFIFO.Add(iPOFIFO);
                }

                //Parts = Parts.Where(x => x.Partno == "3P568191-1").ToList();
                foreach (DoPartMaster itemPart in Parts)
                {
                    string Part = itemPart.Partno.Trim();
                    string Cm = itemPart.Cm;
                    MAlpart oAlPart = rAlPart.FirstOrDefault(x => x.DrawingNo == Part);
                    if (oAlPart != null)
                    {
                        Cm = oAlPart.Cm;
                    }
                    int Pdlt = oHelper.ConvIntEmptyToInt(itemPart.Pdlt);
                    double Stock = 0;
                    DateTime dtLoop = dtStart;
                    double nPoFiFo = rPOFIFO.Where(x => x.partno == Part).Sum(x => x.whblbqty);
                    double defPOFIFO = nPoFiFo;
                    DoVenderMaster objVender = ListVenderMaster.FirstOrDefault(x => x.VdCode == vender)!;
                    double safetyStock = 0;
                    try
                    {
                        decimal? oPlanOfPart = Plans.Where(x => x.Partno != null && x.Partno.Trim() == Part.Trim() && Convert.ToDateTime(x.Prdymd).Date >= dtLoop.Date).Sum(x => x.Qty);
                        if (oPlanOfPart == 0)
                        {
                            safetyStock = 0;
                        }
                    }
                    catch
                    {
                        safetyStock = oHelper.ConvInt2Db(oVdStd.VdSafetyStock);
                    }
                    while (dtLoop <= dtEnd)
                    {
                        MRESULTDO itemResponse = new MRESULTDO();
                        itemResponse.PartNo = Part;
                        itemResponse.Vender = vender;
                        itemResponse.vdCode = vender;
                        itemResponse.vdName = objVender != null ? objVender.VdDesc : vender;
                        itemResponse.Date = dtLoop;
                        if (dtLoop > dtEndFixed)
                        {
                            DoDictMstr isHoliday = rDictHoliday.FirstOrDefault(x => x.Code == dtLoop.ToString("yyyyMMdd"));
                            if (isHoliday != null) // Date [N] = Holiday
                            {
                                itemResponse.holiday = true;
                            }
                        }
                        MPickList itemPickList = PickLists.FirstOrDefault(x => x.Partno == Part && x.Idate == dtLoop.Date.ToString(YMDFormat))!;
                        if (itemPickList != null)
                        {
                            itemResponse.PickList = double.Parse(itemPickList.Fqty);
                        }
                        MDOAct itemDoAct = DOActs.FirstOrDefault(x => x.PartNo == Part && x.AcDate == dtLoop.Date.ToString(YMDFormat) && x.Vender == vender)!;
                        if (itemDoAct != null)
                        {
                            itemResponse.DoAct = itemDoAct.Wqty;
                        }

                        if (dtLoop.Date == dtNow.Date)
                        {
                            List<MStockAlpha> rStock = StockAlpha.Where(x => x.Part.Trim() == Part.Trim()).ToList();
                            if (rStock.Count > 0)
                            {
                                Stock = rStock.Sum(x => x.Stock);
                                itemResponse.Stock = Stock;
                            }
                        }
                        if ((dtNow > dtDistribute && dtLoop.Date <= dtEndFixed.Date) || (dtNow <= dtDistribute && dtLoop.Date <= dtEndFixed.Date))
                        {
                            DoHistoryDev oHis = Historys.FirstOrDefault(x => x.VdCode == vender && x.DateVal == dtLoop.ToString(YMDFormat) && x.Partno == Part)!;
                            if (oHis != null)
                            {
                                itemResponse.Plan = (double)oHis.PlanVal!;
                                itemResponse.PlanPrev = itemResponse.Plan;
                                itemResponse.Do = (double)oHis.DoVal!;
                                itemResponse.Stock = (double)oHis.StockVal!;
                                // หากวันที่เป็นวันปัจจุบันขึ้นไป จะนำ STOCK - PLAN
                                if (dtLoop.Date >= dtNow.Date)
                                {
                                    // ================================================================================ //
                                    // [S] ========= เช็คว่าประวัติแผนการผลิต ตรงหรือไม่ตรง กับแผนผลิต (ปัจจุบัน) [16/07/24 19:40] ===== //
                                    // ================================================================================ //
                                    ViDoPlan itemPlan = Plans.FirstOrDefault(x => x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;
                                    if (itemPlan == null || itemPlan.Qty != oHelper.ConvDBToDec(itemResponse.Plan))
                                    {
                                        itemResponse.Plan = oHelper.ConvDecToDB(itemPlan?.Qty);
                                        itemResponse.PlanPrev = itemResponse.Plan;
                                    }
                                    // ================================================================================ //
                                    // [E] ========= เช็คว่าประวัติแผนการผลิต ตรงหรือไม่ตรง กับแผนผลิต (ปัจจุบัน) [16/07/24 19:40] ===== //
                                    // ================================================================================ //
                                    Stock -= itemResponse.Plan;
                                    Stock += itemResponse.Do;
                                    itemResponse.Stock = Stock;
                                }
                            }

                            //================ SET PO ==================//
                            MPO itemPO = POs.FirstOrDefault(x => x.partNo.Trim() == Part && x.date == dtLoop.Date.ToString(YMDFormat) && x.vdCode == vender)!;
                            if (itemPO != null)
                            {
                                itemResponse.PO = itemPO.qty;
                            }
                            //================ SET PO ==================//
                            if (dtLoop.Date == dtNow.Date)
                            {
                                nPoFiFo = nPoFiFo - itemResponse.Do;
                                itemResponse.POFIFO = nPoFiFo;
                            }
                            DOITEM.Add(itemResponse);
                        }
                        else
                        {
                            itemResponse.Plan = 0;
                            itemResponse.PlanPrev = 0;
                            itemResponse.PickList = 0;
                            ViDoPlan itemPlan = Plans.FirstOrDefault(x => x.Partno != null && x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;
                            if (itemPlan != null)
                            {
                                itemResponse.Plan = (double)itemPlan.Qty!;
                                itemResponse.PlanPrev = itemResponse.Plan;
                            }
                            double StockCurrent = Stock;
                            Stock -= itemResponse.Plan;
                            itemResponse.Stock = Stock;
                            if (Stock <= 0 || Stock < safetyStock)
                            {
                                double RequestStock = Math.Abs(Stock) < safetyStock ? (safetyStock - Math.Abs(Stock)) : Math.Abs(Stock);
                                double DO = GetDoVal(RequestStock, itemPart, oVdStd); // คำนวน DO จาก mstr (Min, Max, Safety stock)
                                if (nPoFiFo == 0 || nPoFiFo < DO)
                                {
                                    DO = 0;
                                }
                                DOITEM = ADJ_DO(DOITEM, vender, Part, dtLoop, Pdlt, DO, VenderDelivery, dtRun, Historys.Count > 0 ? true : false, rDictHoliday, oVdStd, StockCurrent, itemResponse.Plan, Stock, itemPart ,nPoFiFo); // นำตัวเลข DO ไปลงวันที่ตาม PD Leadtime (D-2)
                            }
                            ModelRefreshStock rData = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha);
                            DOITEM = rData.data;
                            Stock = rData.Stock;
                            DOITEM.Add(itemResponse);
                        }
                        dtLoop = dtLoop.AddDays(1);
                    }
                    DOITEM = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha).data;
                    DOITEM = RefreshPOFIFO(DOITEM,defPOFIFO,dtNow,Part);
                }
            }
            //bool dev = false;
            //if (run == true || dev == true)
            //{
            //    List<MRESULTDO>? rITEM = DOITEM.Where(x => x.Log.Count > 0).ToList();
            //    foreach (MRESULTDO oItem in rITEM)
            //    {
            //        foreach (DoLogDev oLog in oItem.Log)
            //        {
            //            SqlCommand sqlInsertLog = new SqlCommand();
            //            sqlInsertLog.CommandText = @"INSERT INTO [dbo].[DO_LOG_DEV] ([DO_RUNNING],[DO_REV],[LOG_PART_NO],[LOG_VD_CODE] ,[LOG_PROD_LEAD],[LOG_TYPE],[LOG_FROM_DATE],[LOG_FROM_STOCK],[LOG_FROM_PLAN],[LOG_NEXT_DATE],[LOG_NEXT_STOCK],[LOG_TO_DATE],[LOG_DO],[LOG_BOX],[LOG_STATE],[LOG_REMARK],[LOG_CREATE_DATE],[LOG_UPDATE_DATE],[LOG_UPDATE_BY])  VALUES  (@DO_RUNNING,@DO_REV,@PART,@VDCODE,@PRODLEAD,@TYPE,@F_DATE,@F_STOCK,@F_PLAN,@N_DATE,@N_STOCK,@T_DATE,@DO,@BOX,@STATE,@REMARK,GETDATE(),GETDATE(),@UPDAET_BY) SELECT SCOPE_IDENTITY()";
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@DO_RUNNING", doRunningCode));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@DO_REV", doRev));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@PART", oLog.logPartNo));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@VDCODE", oLog.logVdCode));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@PRODLEAD", oLog.logProdLead));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@TYPE", oLog.logType));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@F_DATE", oLog.logFromDate));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@F_STOCK", oLog.logFromStock));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@F_PLAN", oLog.logFromPlan));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@N_DATE", oLog.logNextDate));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@N_STOCK", oLog.logNextStock));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@T_DATE", oLog.logToDate));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@DO", oLog.logDo));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@BOX", oLog.logBox));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@STATE", oLog.logState));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@REMARK", oLog.logRemark));
            //            sqlInsertLog.Parameters.Add(new SqlParameter("@UPDAET_BY", oLog.logUpdateBy));
            //            dbSCM.ExecuteNonCommand(sqlInsertLog);
            //        }
            //    }
            //}

            return new MODEL_GET_DO()
            {
                data = DOITEM,
                PartMaster = PARTMASTER,
                VenderMaster = VENDERMASTER,
                nbr = nbr,
                VenderDelivery = VenderDeliveryMaster,
                VenderSelected = oVenderSelect,
                ListPO = rPOFIFO.OrderBy(x => x.delymd).ToList()
            };
        }
        private List<MRESULTDO> RefreshPOFIFO(List<MRESULTDO> dOITEM, double defPOFIFO, DateTime dtNow, string part)
        {
            double POFIFO = dOITEM.Where(x => x.Date.Date == dtNow.Date && x.PartNo == part).Sum(x=>x.POFIFO);
            foreach (MRESULTDO item in dOITEM.Where(x=> x.PartNo == part).ToList())
            {
                if (item.Date.Date >= dtNow.Date)
                {
                    POFIFO -= item.Do;
                    item.POFIFO = POFIFO;
                }
                else
                {
                    item.POFIFO = 0;
                }
            }
            return dOITEM;
        }
        private int fnInsertLog(string part, string vdcode, string type, string fromDate, double stockCurrent, double plan, string toDate, double toStock, double doVal, double box, string state = "notused", int? prodlead = 0, string remark = "")
        {
            int insert = 0;
            //string updateBy = "SYSTEM";
            //SqlCommand sqlCheckLog = new SqlCommand();
            //sqlCheckLog.CommandText = @"SELECT LOG_ID FROM [dbSCM]. [dbo].[DO_LOG_DEV] WHERE LOG_PART_NO = @LOG_PART_NO and LOG_VD_CODE =@LOG_VD_CODE and LOG_TYPE = @LOG_TYPE and LOG_FROM_DATE = @LOG_FROM_DATE and LOG_FROM_PLAN = @LOG_FROM_PLAN and LOG_FROM_STOCK  = @LOG_FROM_STOCK  and LOG_NEXT_DATE = @LOG_NEXT_DATE and LOG_NEXT_STOCK = @LOG_NEXT_STOCK  and LOG_DO = @LOG_DO and LOG_BOX = @LOG_BOX and LOG_STATE = @LOG_STATE and LOG_REMARK = @LOG_REMARK";
            ////and LOG_TO_DATE = @LOG_TO_DATE
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_PART_NO", part));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_VD_CODE", vdcode));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_TYPE", type));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_FROM_DATE", fromDate));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_FROM_PLAN", plan));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_FROM_STOCK", stockCurrent));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_NEXT_DATE", toDate));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_NEXT_STOCK", toStock));
            ////sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_TO_DATE", toDate));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_DO", doVal));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_BOX", box));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_STATE", state));
            //sqlCheckLog.Parameters.Add(new SqlParameter("@LOG_REMARK", remark));
            //DataTable dtLog = dbSCM.Query(sqlCheckLog);
            //if (dtLog.Rows.Count == 0)
            //{
            //       SqlCommand sqlInsertLog = new SqlCommand();
            //       sqlInsertLog.CommandText = @"INSERT INTO [dbo].[DO_LOG_DEV]
            //      ([LOG_PART_NO]
            //      ,[LOG_VD_CODE]
            //       ,[LOG_PROD_LEAD]
            //      ,[LOG_TYPE]
            //      ,[LOG_FROM_DATE]
            //      ,[LOG_FROM_STOCK]
            //      ,[LOG_FROM_PLAN]
            //      ,[LOG_NEXT_DATE]
            //      ,[LOG_NEXT_STOCK]
            //      ,[LOG_TO_DATE]
            //      ,[LOG_DO]
            //      ,[LOG_BOX]
            //      ,[LOG_STATE]
            //      ,[LOG_REMARK]
            //      ,[LOG_CREATE_DATE]
            //      ,[LOG_UPDATE_DATE]
            //      ,[LOG_UPDATE_BY])
            //VALUES
            //      (@PART,@VDCODE,@PRODLEAD,@TYPE,@F_DATE,@F_STOCK,@F_PLAN,@N_DATE,@N_STOCK,@T_DATE,@DO,@BOX,@STATE,@REMARK,GETDATE(),GETDATE(),@UPDAET_BY) SELECT SCOPE_IDENTITY()";
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@PART", part));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@VDCODE", vdcode));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@PRODLEAD", prodlead.HasValue ? prodlead : 2));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@TYPE", type.ToUpper()));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@F_DATE", fromDate));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@F_STOCK", stockCurrent));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@F_PLAN", plan));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@N_DATE", toDate));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@N_STOCK", toStock));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@T_DATE", toDate));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@DO", doVal));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@BOX", box));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@STATE", state));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@REMARK", remark));
            //       sqlInsertLog.Parameters.Add(new SqlParameter("@UPDAET_BY", updateBy.ToUpper()));
            //       insert = dbSCM.ExecuteNonCommand(sqlInsertLog);
            //}



            return insert;
        }

        public List<MPickList> GetPickListBySupplier(string Parts, DateTime DateStart, DateTime DateEnd)
        {
            List<MPickList> res = new List<MPickList>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = "SELECT PARTNO,CAST(IDATE AS DATE) AS IDATE,SUM(FQTY) AS FQTY FROM [dbSCM].[dbo].[AL_GST_DATPID] WHERE PARTNO IN ('" + Parts + "') AND PRGBIT IN ('F','C') AND IDATE >= @startProcess AND IDATE <= @endProcess GROUP BY PARTNO,IDATE";
            sql.Parameters.Add(new SqlParameter("startProcess", DateStart.Date));
            sql.Parameters.Add(new SqlParameter("endProcess", DateEnd.Date));
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                res.Add(new MPickList()
                {
                    Partno = dr["PARTNO"].ToString(),
                    Fqty = dr["FQTY"].ToString(),
                    Idate = DateTime.Parse(dr["IDATE"].ToString()).ToString("yyyyMMdd")
                });
            }
            return res;
        }
        public string ChkVer(string dictSyst, string dictType)
        {
            string vers = "";
            SqlCommand sql = new SqlCommand();
            try
            {
                sql.CommandText = $@"SELECT CODE AS VERSION FROM [dbo].[DictMstr] WHERE DICT_SYSTEM = '{dictSyst}' AND DICT_TYPE = '{dictType}' AND DICT_STATUS = 'ACTIVE'";
                DataTable dt = dbSCM.Query(sql);
                if (dt.Rows.Count > 0)
                {
                    vers = dt.Rows[0]["VERSION"].ToString()!;
                }
            }
            catch { vers = "1.0.0"; }
            return vers;
        }
    }
}
