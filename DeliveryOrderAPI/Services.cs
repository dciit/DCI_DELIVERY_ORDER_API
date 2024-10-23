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
        public Services(DBSCM dBSCM)
        {
            _DBSCM = dBSCM;
        }

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



        //public List<MStockAlpha> GetStockAlpha(DateTime _DateRunProcess, string PARTS = "")
        //{
        //    List<MStockAlpha> res = new List<MStockAlpha>();
        //    string _PART_JOIN_STRING = "";
        //    string _Year = _DateRunProcess.Year.ToString();
        //    string _Month = _DateRunProcess.Month.ToString("00");
        //    string date = _Year + "" + _Month;
        //    OracleCommand cmd = new();
        //    PARTS = PARTS != "" ? PARTS : _PART_JOIN_STRING;
        //    cmd.CommandText = @"SELECT '" + _DateRunProcess.ToString("yyyyMMdd") + "',MC1.PARTNO, MC1.CM, DECODE(SB1.DSBIT,'1','OBSOLETE','2','DEAD STOCK','3',CASE WHEN TRIM(SB1.STOPDATE) IS NOT NULL AND SB1.STOPDATE <= TO_CHAR(SYSDATE,'YYYYMMDD') THEN 'NOT USE ' || SB1.STOPDATE ELSE ' ' END, ' ') PART_STATUS, MC1.LWBAL, NVL(AC1.ACQTY,0) AS ACQTY, NVL(PID.ISQTY,0) AS ISQTY, MC1.LWBAL + NVL(AC1.ACQTY,0) - NVL(PID.ISQTY,0) AS WBAL,NVL(RT3.LREJ,0) + NVL(PID.REJIN,0) - NVL(AC1.REJOUT,0) AS REJQTY, MC2.QC, MC2.WH1, MC2.WH2, MC2.WH3, MC2.WHA, MC2.WHB, MC2.WHC, MC2.WHD, MC2.WHE,ZUB.HATANI AS UNIT, EPN.KATAKAN AS DESCR, F_GET_HTCODE_RATIO(MC1.JIBU,MC1.PARTNO, '" + _DateRunProcess.ToString("yyyyMMdd") + "') AS HTCODE, F_GET_MSTVEN_VDABBR(MC1.JIBU,F_GET_HTCODE_RATIO(MC1.JIBU,MC1.PARTNO,'" + _DateRunProcess.ToString("yyyyMMdd") + "')) SUPPLIER, SB1.LOCA1, SB1.LOCA2, SB1.LOCA3, SB1.LOCA4, SB1.LOCA5, SB1.LOCA6, SB1.LOCA7, SB1.LOCA8 FROM	(SELECT	* FROM	DST_DATMC1 WHERE	TRIM(YM) = :YM AND TRIM(PARTNO) IN ('" + PARTS + "') AND CM LIKE '%'";
        //    cmd.CommandText = cmd.CommandText + @") MC1, 
        //		(SELECT	PARTNO, CM, SUM(WQTY) AS ACQTY, SUM(CASE WHEN WQTY < 0 THEN -1 * WQTY ELSE 0 END) AS REJOUT 
        //		 FROM	DST_DATAC1 
        //		 WHERE	ACDATE >= :DATE_START 
        //			AND	ACDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%'  AND CM LIKE '%'
        //		 GROUP BY PARTNO, CM 
        //		) AC1, 
        //		(SELECT	PARTNO, BRUSN AS CM, SUM(FQTY) AS ISQTY, SUM(DECODE(REJBIT,'R',-1*FQTY,0)) AS REJIN 
        //		 FROM	MASTER.GST_DATPID@ALPHA01 
        //		 WHERE	IDATE >= :DATE_START 
        //			AND	IDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%'  AND BRUSN LIKE '%'
        //		 GROUP BY PARTNO, BRUSN 
        //		) PID, 
        //		(SELECT    PARTNO, CM, SUM(DECODE(WHNO,'QC',BALQTY)) AS QC,SUM(DECODE(WHNO,'W1',BALQTY)) AS WH1,SUM(DECODE(WHNO,'W2',BALQTY)) AS WH2,SUM(DECODE(WHNO,'W3',BALQTY)) AS WH3, 
        //                   SUM(DECODE(WHNO,'WA',BALQTY)) AS WHA,SUM(DECODE(WHNO,'WB',BALQTY)) AS WHB,SUM(DECODE(WHNO,'WC',BALQTY)) AS WHC,SUM(DECODE(WHNO,'WD',BALQTY)) AS WHD,SUM(DECODE(WHNO,'WE',BALQTY)) AS WHE 
        //            FROM    (SELECT    MC2.PARTNO, MC2.CM, MC2.WHNO, MC2.LWBAL, NVL(AC1.ACQTY,0) AS ACQTY, NVL(PID.ISQTY,0) AS ISQTY, MC2.LWBAL + NVL(AC1.ACQTY,0) - NVL(PID.ISQTY,0) AS BALQTY 
        //                    FROM    (SELECT    * 
        //                            FROM    DST_DATMC2 
        //                            WHERE    YM = :YM  AND TRIM(PARTNO) LIKE '%' AND CM LIKE '%'
        //                           ) MC2, 
        //                           (SELECT    PARTNO, CM, WHNO, SUM(WQTY) AS ACQTY 
        //                            FROM    DST_DATAC1 
        //                            WHERE    ACDATE >= :DATE_START 
        //                               AND    ACDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%' AND CM LIKE '%'
        //                            GROUP BY PARTNO, CM, WHNO 
        //                           ) AC1, 
        //                           (SELECT    PARTNO, BRUSN AS CM, WHNO, SUM(FQTY) AS ISQTY 
        //                            FROM    (SELECT    * 
        //                                     FROM    MASTER.GST_DATPID@ALPHA01 
        //                                     WHERE    IDATE >= :DATE_START 
        //                                       AND    IDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%' AND BRUSN LIKE '%'
        //                                    UNION ALL 
        //                                     SELECT    * 
        //                                     FROM    DST_DATPID3 
        //                                     WHERE    IDATE >= :DATE_START 
        //                                       AND    IDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%' AND BRUSN LIKE '%'
        //                                   ) 
        //                            GROUP BY PARTNO, BRUSN, WHNO 
        //                           ) PID 
        //                    WHERE    MC2.PARTNO    = AC1.PARTNO(+) 
        //                       AND    MC2.CM        = AC1.CM(+) 
        //                       AND    MC2.WHNO    = AC1.WHNO(+) 
        //                       AND    MC2.PARTNO    = PID.PARTNO(+) 
        //                       AND    MC2.CM        = PID.CM(+) 
        //                       AND    MC2.WHNO    = PID.WHNO(+) 
        //                   ) 
        //            GROUP BY PARTNO, CM 
        //           ) MC2, 
        //           MASTER.ND_EPN_TBL_V1@ALPHA01 EPN, DST_MSTSB1 SB1, MASTER.ND_ZUB_TBL@ALPHA01 ZUB, DST_DATRT3 RT3 
        //   WHERE    MC1.PARTNO    = AC1.PARTNO(+) 
        //       AND    MC1.CM        = AC1.CM(+) 
        //       AND    MC1.PARTNO    = PID.PARTNO(+) 
        //       AND    MC1.CM        = PID.CM(+) 
        //       AND    MC1.YM        = RT3.YM(+) 
        //       AND    MC1.PARTNO    = RT3.PARTNO(+) 
        //       AND    MC1.CM        = RT3.CM(+) 
        //       AND    MC1.PARTNO    = EPN.PARTNO(+) 
        //       AND    MC1.PARTNO    = SB1.PARTNO(+) 
        //       AND    MC1.CM        = SB1.CM(+) 
        //       AND    MC1.PARTNO    = MC2.PARTNO(+) 
        //       AND    MC1.CM        = MC2.CM(+) 
        //       AND    MC1.PARTNO    = ZUB.PARTNO(+) 
        //       AND    ZUB.STRYMN(+) <= :DATE_START 
        //       AND    ZUB.ENDYMN(+) >  :DATE_RUN 
        //       AND    ZUB.KSNBIT(+) <> '2'";
        //    cmd.Parameters.Add(new OracleParameter(":YM", date));
        //    cmd.Parameters.Add(new OracleParameter(":DATE_START", _DateRunProcess.ToString("yyyyMM01")));
        //    cmd.Parameters.Add(new OracleParameter(":DATE_RUN", _DateRunProcess.ToString("yyyyMMdd")));
        //    DataTable dt = dbAlpha2.Query(cmd);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        res.Add(new MStockAlpha()
        //        {
        //            Part = dr["PARTNO"].ToString().Trim(),
        //            Stock = double.Parse(dr["WBAL"].ToString()),
        //            Cm = dr["CM"].ToString().Trim()
        //        });
        //    }
        //    return res;
        //}
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

        public double GetDoVal(double planTarget, DoPartMaster oPartStd, DoVenderMaster oVdStd)
        {
            try
            {
                double _DoVal = 0;
                if (planTarget > 0)
                {
                    _DoVal = Math.Ceiling(planTarget / Convert.ToDouble(oPartStd.BoxQty)) * Convert.ToDouble(oPartStd.BoxQty);
                    _DoVal = _CheckMinDelivery(_DoVal, Convert.ToDouble(oPartStd.BoxMin));
                    _DoVal = _CheckMaxDelivery(_DoVal, Convert.ToDouble(oPartStd.BoxMax));
                    if (oVdStd.VdBoxPeriod == true)
                    {
                        decimal _box = oHelper.ConvIntToDec(oPartStd.BoxQty != null ? (int)oPartStd.BoxQty : 0);
                        decimal _minQty = oHelper.ConvIntToDec(oPartStd.BoxMin != null ? (int)oPartStd.BoxMin : 0);
                        decimal _do = oHelper.ConvDBToDec(_DoVal);
                        decimal _used = Math.Ceiling(_do / _minQty);
                        _DoVal = oHelper.ConvDecToDB(_used * _minQty);
                    }
                }
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

        //internal MRefreshStock refreshStockSim(List<MDORESULT> res, DateTime dtDelivery, string PartCode, string SupplierCode)
        //{
        //    var resEnd = res.LastOrDefault(x => x.part == PartCode && x.vdCode == SupplierCode);
        //    int indexEnd = res.FindIndex(x => x.part == PartCode && x.date == resEnd.date);
        //    int indexStart = res.FindIndex(x => x.date == dtDelivery && x.part == PartCode && x.vdCode == SupplierCode);
        //    double stockSim = res[indexStart].stockSim;
        //    bool firstRound = true;
        //    while (indexStart <= indexEnd)
        //    {
        //        if (firstRound)
        //        {
        //            firstRound = false;
        //        }
        //        else
        //        {
        //            stockSim = (stockSim - res[indexStart].plan) + res[indexStart].doPlan;
        //        }
        //        res[indexStart].stockSim = stockSim;
        //        indexStart++;
        //    }
        //    return new MRefreshStock()
        //    {
        //        Results = res,
        //        StockSim = stockSim
        //    };
        //}

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

        //internal DateTime checkDelivery(DateTime dtDelivery, DataTable supplierMstr)
        //{
        //    string shortDay = dtDelivery.ToString("ddd");
        //    bool CanDelivery = Convert.ToBoolean(supplierMstr.Rows[0]["VD_" + shortDay].ToString());
        //    if (!CanDelivery)
        //    {
        //        while (!CanDelivery)
        //        {
        //            shortDay = dtDelivery.ToString("ddd");
        //            CanDelivery = Convert.ToBoolean(supplierMstr.Rows[0]["VD_" + shortDay].ToString());
        //            dtDelivery = dtDelivery.AddDays(-1);
        //        }
        //    }
        //    return dtDelivery;
        //}

        //internal DataTable GetSupplierMaster(string? supplierCode)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = @"SELECT  *  FROM [dbSCM].[dbo].[DO_VENDER_MASTER] WHERE VD_CODE = '" + supplierCode + "'";
        //    DataTable dt = dbSCM.Query(sql);
        //    return dt;
        //}


        //public List<DoHistory> refreshStock(List<DoHistory> historys, string vdCode, DateTime date, string? part, double? plan)
        //{
        //    int indexEnd = historys.FindLastIndex(x => x.VdCode == vdCode && x.Partno == part);
        //    int indexStart = historys.FindIndex(x => x.DateVal == date.AddDays(-1).ToString("yyyyMMdd") && x.Partno == part && x.VdCode == vdCode);
        //    double stockSim = (double)historys[indexStart].StockVal;
        //    bool firstRound = true;
        //    while (indexStart <= indexEnd)
        //    {
        //        if (firstRound)
        //        {
        //            firstRound = false;
        //        }
        //        else
        //        {
        //            stockSim = (stockSim - (double)historys[indexStart].PlanVal) + (double)historys[indexStart].DoVal;
        //        }
        //        historys[indexStart].StockVal = stockSim;
        //        indexStart++;
        //    }
        //    return historys;
        //}

        //public MRefreshStockDT RefreshStockDt(DataTable dtResponse, string vdCode, DateTime dtEnd, DateTime dt, string? part, double doAdj, DoPartMaster PartMstr, Dictionary<string, bool> daysDelivery)
        //{

        //    dt = dt.AddDays((int)PartMstr.Pdlt * -1);
        //    if (dt < dtEnd)
        //    {
        //        dt = dtEnd;
        //    }
        //    bool CanDelivery = false;
        //    string DayText = dt.ToString("ddd").ToUpper();
        //    CanDelivery = daysDelivery[DayText.ToUpper()];
        //    while (!CanDelivery)
        //    {
        //        DayText = dt.ToString("ddd").ToUpper();
        //        CanDelivery = daysDelivery[DayText.ToUpper()];
        //        if (dt <= dtEnd)
        //        {
        //            CanDelivery = true;
        //        }
        //        else
        //        {
        //            dt = dt.AddDays(-1);
        //        }
        //    }

        //    var contentStart = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == dt.ToString("yyyyMMdd") && x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == part).FirstOrDefault();
        //    int indexStart = dtResponse.Rows.IndexOf(contentStart);

        //    var contentLast = dtResponse.AsEnumerable().LastOrDefault(x => x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == part); // data วันแรกของช่วง RUN D/O
        //    int indexEnd = dtResponse.Rows.IndexOf(contentLast);

        //    double stockLoop = double.Parse(contentStart.Field<string>("STOCKSIM"));
        //    bool ft = true;
        //    while (indexStart <= indexEnd)
        //    {
        //        double doLoop = double.Parse(dtResponse.Rows[indexStart]["DO"].ToString());
        //        double planLoop = double.Parse(dtResponse.Rows[indexStart]["PLAN"].ToString());
        //        doLoop += doAdj;
        //        doAdj = 0; // เพิ่ม D/O แค่ครั้งแรก
        //        if (ft)
        //        {
        //            stockLoop = stockLoop + doLoop;
        //            ft = false;
        //        }
        //        else
        //        {
        //            stockLoop = (stockLoop - planLoop) + doLoop;
        //        }
        //        dtResponse.Rows[indexStart]["DO"] = double.Parse(dtResponse.Rows[indexStart]["DO"].ToString()) + doLoop;
        //        dtResponse.Rows[indexStart]["STOCKSIM"] = stockLoop;
        //        indexStart++;
        //    }

        //    return new MRefreshStockDT()
        //    {
        //        dt = dtResponse,
        //        stock = stockLoop
        //    };
        //}

        internal Dictionary<string, bool> getDaysDelivery(DoVenderMaster? venderMaster)
        {
            Dictionary<string, bool> res = new Dictionary<string, bool>();
            res.Add("MON", bool.Parse(venderMaster.VdMon.ToString()));
            res.Add("TUE", bool.Parse(venderMaster.VdTue.ToString()));
            res.Add("WED", bool.Parse(venderMaster.VdWed.ToString()));
            res.Add("THU", bool.Parse(venderMaster.VdThu.ToString()));
            res.Add("FRI", bool.Parse(venderMaster.VdFri.ToString()));
            res.Add("SAT", bool.Parse(venderMaster.VdSat.ToString()));
            res.Add("SUN", bool.Parse(venderMaster.VdSun.ToString()));
            return res;
        }

        //internal DataTable sortDt(DataTable dtResponse)
        //{
        //    DataView dv = dtResponse.DefaultView;
        //    dv.Sort = "PART ASC, DATE ASC";
        //    DataTable sortedDT = dv.ToTable();
        //    return sortedDT;
        //}

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
                    int IndexLoop = res.IndexOf(ItemLoop);
                    double PlanLoop = ItemLoop!.Plan;
                    double DOLoop = ItemLoop!.Do;
                    StockLoop = (StockLoop - PlanLoop) + DOLoop;
                    res[IndexLoop].Stock = StockLoop;
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
        public List<DoDictMstr> _GET_HOLIDAY()
        {
            List<DoDictMstr> res = new List<DoDictMstr>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_DictMstr] WHERE DICT_TYPE = 'holiday'";
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                res.Add(new DoDictMstr()
                {
                    Code = dr["CODE"].ToString()
                });
            }
            return res;
        }

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

        /* A002 : เช็คว่าวันที่ N ไม่ตรงกับวันหยุด ส อ และ ไม่ตรงกับวันหยุดเทศกาล   [-1 = ไม่สามารถวันนี้ได้]*/
        internal int DayAvailable(DateTime dtLoop, List<DoDictMstr> rDictHoliday, Dictionary<string, bool> venderDelivery)
        {
            int index = -1;
            List<string> rRestDay = venderDelivery.Where(x => x.Value == true).Select(x => x.Key).ToList();
            string ShortDay = dtLoop.ToString("ddd").ToUpper();
            index = rRestDay.FindIndex(x => x == ShortDay); // Short Day ตรงกับวัน
            bool isHoliday = rDictHoliday.FirstOrDefault(x => x.Code == dtLoop.ToString("yyyyMMdd")) != null ? true : false;
            index = isHoliday == true ? -1 : index; // -1 คือไม่สามารถส่งได้
            return index;
        }

        internal List<MRESULTDO> ADJ_DO(List<MRESULTDO> response, string vender, string part, DateTime dtLoop, int pdlt, double DO, Dictionary<string, bool> VdMaster, DateTime dtRun, bool haveHistory, List<DoDictMstr> rDictHoliday, DoVenderMaster vdMaster, double stockFrom, double planForm, double StockCalOfDate, DoPartMaster PartMaster)
        {
            string YMDFormat = "yyyyMMdd";
            DateTime dtNow = DateTime.Now;
            DateTime dtEnd = dtNow.AddDays(14);
            DateTime dtEndFixed = dtNow.AddDays((vdMaster.VdProdLead != null ? (int)vdMaster.VdProdLead : 0) - 1);

            // ============================================================= //
            // [S] === เพิ่มเช็คว่าเวลาเกินเวลาที่ระบบ Distribute [16/07/2024 17:15] === //
            // ============================================================= //
            //bool over3PM = false;
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
            // ============================================================= //
            // [E] === เพิ่มเช็คว่าเวลาเกินเวลาที่ระบบ Distribute [16/07/2024 17:15] === //
            // ============================================================= //

            //DateTime dtFrom = dtLoop;

            //MRESULTDO LastItemOfPart = response.OrderBy(x => x.Date).LastOrDefault(x => x.Vender == vender && x.PartNo == part)!;
            //dtLoop = dtLoop.AddDays(pdlt * (-1));
            //TimeSpan timeNow = DateTime.Now.TimeOfDay;
            //TimeSpan timeFix = new TimeSpan(15, 10, 0);

            //if (dtLoop < dtNow)
            //{
            //    dtLoop = dtNow;
            //}
            //if (haveHistory && dtLoop < dtRun)
            //{
            //    dtLoop = dtRun;
            //    if (over3PM)
            //    {
            //        dtLoop = dtLoop.AddDays(1);
            //    }
            //}

            // ===================================================================== //
            // ==== [S] แก้ไขวิธีการหาวันที่สามารถลงยอด D/O ที่เกิดจากการติดลบ [16/07/24 17:25] === //
            // ===================================================================== //
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
                }
            }
            // ===================================================================== //
            // ==== [E] แก้ไขวิธีการหาวันที่สามารถลงยอด D/O ที่เกิดจากการติดลบ [16/07/24 17:25] === //
            // ===================================================================== //

            //bool CanDelivery = venderDelivery.ContainsKey(ShortDay) ? venderDelivery[ShortDay.ToUpper()] : false;

            //if (timeNow < timeFix) { dtEndFixed = dtEndFixed.AddDays(-1); }
            //DateTime dtStartDefailt = dtNow;
            //List<string> weekHoliday = venderDelivery.Where(x => x.Value == false).Select(y => y.Key).ToList();
            //bool direction = true; // true = D[n-1], false = D[n+1]
            //bool found = false;

            //List<DoLogDev> rHistoryIDNotUsed = new List<DoLogDev>();
            //while (dtLoop.Date > dtEndFixed.Date && dtLoop.Date < dtEnd.Date)
            //{
            //    ShortDay = dtLoop.ToString("ddd").ToUpper();
            //    if (dtLoop.Date != dtFrom.Date)
            //    {
            //        bool isWeekHoliday = weekHoliday.FirstOrDefault(x => x == ShortDay) != null ? true : false;
            //        bool isFastivalHoliday = rDictHoliday.Where(x => x.Code == dtLoop.ToString("yyyyMMdd")).ToList().Count > 0 ? true : false;

            //        /* A005 */
            //        if (isWeekHoliday != true && isFastivalHoliday != true)
            //        {
            //            found = true;
            //            DoLogDev oLog = new DoLogDev();
            //            oLog.logPartNo = part;
            //            oLog.logVdCode = vdMaster.VdCode;
            //            oLog.logProdLead = 2;
            //            oLog.logType = "ASSIGN";
            //            oLog.logFromDate = dtFrom.ToString("yyyyMMdd");
            //            oLog.logFromPlan = planForm;
            //            oLog.logFromStock = stockFrom;
            //            oLog.logNextDate = dtLoop.ToString("yyyyMMdd");
            //            oLog.logNextStock = StockCalOfDate;
            //            oLog.logToDate = dtLoop.ToString("yyyyMMdd");
            //            oLog.logDo = DO;
            //            oLog.logBox = PartMaster.BoxQty.HasValue ? (double)PartMaster.BoxQty : 0.0;
            //            oLog.logState = "used";
            //            oLog.logRemark = "";
            //            oLog.logCreateDate = DateTime.Now;
            //            oLog.logUpdateDate = DateTime.Now;
            //            oLog.logUpdateBy = "system";
            //            rHistoryIDNotUsed.Add(oLog);
            //            /* A004 */
            //            //int insertLog =     fnInsertLog(part, vender, "ASSIGN", dtCurrent.ToString("yyyyMMdd"), stockFrom, planForm, dtLoop.ToString("yyyyMMdd"), StockCalOfDate, DO, PartMaster.BoxQty.HasValue ? (double)PartMaster.BoxQty : 0.0, "used", vdMaster.VdProdLead, "");
            //            //if (insertLog > 0 && rHistoryIDNotUsed.Count > 0)
            //            //{
            //            //    foreach(DoLogDev item in rHistoryIDNotUsed)
            //            //    {
            //            //        SqlCommand sqlUpdateEndDate = new SqlCommand();
            //            //        sqlUpdateEndDate.CommandText = @"UPDATE [dbSCM].[dbo].[DO_LOG_DEV] SET LOG_TO_DATE = @LOG_TO_DATE where LOG_PART_NO = @LOG_PART_NO and LOG_VD_CODE = @LOG_VD_CODE and LOG_TYPE = @LOG_TYPE and LOG_FROM_DATE = @LOG_FROM_DATE and LOG_NEXT_DATE = @LOG_NEXT_DATE";
            //            //        sqlUpdateEndDate.Parameters.Add(new SqlParameter("@LOG_TO_DATE", dtLoop.ToString("yyyyMMdd")));
            //            //        sqlUpdateEndDate.Parameters.Add(new SqlParameter("@LOG_PART_NO", item.logPartNo));
            //            //        sqlUpdateEndDate.Parameters.Add(new SqlParameter("@LOG_VD_CODE", item.logVdCode));
            //            //        sqlUpdateEndDate.Parameters.Add(new SqlParameter("@LOG_TYPE", item.logType));
            //            //        sqlUpdateEndDate.Parameters.Add(new SqlParameter("@LOG_FROM_DATE", item.logFromDate));
            //            //        sqlUpdateEndDate.Parameters.Add(new SqlParameter("@LOG_NEXT_DATE", item.logNextDate));
            //            //        int update = dbSCM.ExecuteNonCommand(sqlUpdateEndDate);
            //            //    }
            //            //    rHistoryIDNotUsed.Clear();
            //            //}
            //            /* [E] A004 */
            //            break;
            //        }
            //        else
            //        {
            //            /* A004 */

            //            //int insertLog = fnInsertLog(part, vender, "ASSIGN", dtCurrent.ToString("yyyyMMdd"), stockFrom, planForm, dtLoop.ToString("yyyyMMdd"), StockCalOfDate, DO, PartMaster.BoxQty.HasValue ? (double)PartMaster.BoxQty : 0.0, "notused", vdMaster.VdProdLead, remark);
            //            //if (insertLog > 0)
            //            //{
            //            //    DoLogDev rowNotUsed = new DoLogDev();
            //            //    rowNotUsed.logPartNo = part;
            //            //    rowNotUsed.logVdCode = vender;
            //            //    rowNotUsed.logType = "ASSIGN";
            //            //    rowNotUsed.logFromDate = dtCurrent.ToString("yyyyMMdd");
            //            //    rowNotUsed.logNextDate = dtLoop.ToString("yyyyMMdd");
            //            //    rHistoryIDNotUsed.Add(rowNotUsed);
            //            //}

            //            /* [E] A004 */
            //            List<string> rRemark = new List<string>();
            //            rRemark.Add(isWeekHoliday == false ? "weekly" : "");
            //            rRemark.Add(isFastivalHoliday == false ? "fastival" : "");
            //            string remark = string.Join(",", rRemark.Where(x => x != "").ToList());
            //            DoLogDev oLog = new DoLogDev();
            //            oLog.logPartNo = part;
            //            oLog.logVdCode = vdMaster.VdCode;
            //            oLog.logProdLead = 2;
            //            oLog.logType = "ASSIGN";
            //            oLog.logFromDate = dtFrom.ToString("yyyyMMdd");
            //            oLog.logFromPlan = planForm;
            //            oLog.logFromStock = stockFrom;
            //            oLog.logNextDate = dtLoop.ToString("yyyyMMdd");
            //            oLog.logNextStock = StockCalOfDate;
            //            oLog.logDo = DO;
            //            oLog.logBox = PartMaster.BoxQty.HasValue ? (double)PartMaster.BoxQty : 0.0;
            //            oLog.logState = "notused";
            //            oLog.logRemark = remark;
            //            oLog.logCreateDate = DateTime.Now;
            //            oLog.logUpdateDate = DateTime.Now;
            //            oLog.logUpdateBy = "system";

            //            dtLoop = dtLoop.AddDays(direction == true ? -1 : 1);
            //            oLog.logToDate = dtLoop.ToString("yyyyMMdd");
            //            rHistoryIDNotUsed.Add(oLog);

            //            if (dtLoop.Date == dtEndFixed.Date)
            //            {
            //                direction = false;
            //                dtLoop = dtEndFixed.AddDays(1);
            //            }
            //        }
            //        if (found == true)
            //        {
            //            break;
            //        }
            //    }
            //    else
            //    {
            //        dtLoop = dtLoop.AddDays(direction == true ? -1 : 1);
            //    }
            //}
            ///* A003 */

            ///* A001 */
            ////int vdFixedDay = vdMaster.VdProdLead ?? 0;
            ////DateTime dtFixed = dtNow.AddDays(vdFixedDay - 1); // วันสุดท้ายที่ Fixed 
            ////int overAfternoon = DateTime.Compare(dtNow, new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 15, 0, 0)); // หาก < 15:00 ต้อง dtFixed - 1D
            ////if (overAfternoon == 0)
            ////{
            ////    dtFixed = dtFixed.AddDays(-1);
            ////}
            ////if (rDictHoliday.Where(x => x.Code == dtLoop.ToString("yyyyMMdd")).ToList().Count > 0) // x.Code [yyyyMMdd] 
            ////{
            ////    CanDelivery = false;
            ////}
            ///* [E] A001 */
            ///* A002 */
            ////bool Oparator = overAfternoon > 0 ? true : false;   // 1+ = Over Afternoon 15:00 [03:00 PM] จะเปลี่ยนจาก D-n เป็น D+n
            ///* [E] A002 */


            ///* A003 : CLOSE CODE */
            ////bool Oparator = false;
            ////while (!CanDelivery)
            ////{
            ////    dtLoop = dtLoop.AddDays(Oparator ? 1 : -1);
            ////    if (dtLoop.Date > LastItemOfPart.Date.Date)
            ////    {
            ////        break;
            ////    }
            ////    if (dtLoop.Date < dtNow.Date)
            ////    {
            ////        dtLoop = dtNow;
            ////        ShortDay = dtLoop.ToString("ddd").ToUpper();
            ////        CanDelivery = venderDelivery.ContainsKey(ShortDay) ? venderDelivery[ShortDay.ToUpper()] : false;
            ////        if (!CanDelivery)
            ////        {
            ////            Oparator = !Oparator;
            ////        }
            ////    }
            ////    else
            ////    {
            ////        ShortDay = dtLoop.ToString("ddd").ToUpper();
            ////        CanDelivery = venderDelivery.ContainsKey(ShortDay) ? venderDelivery[ShortDay.ToUpper()] : false;
            ////    }
            ////}

            ///* A002 */
            ////if (dtLoop < dtFixed)
            ////{
            ////    DateTime dtEnd = dtNow.AddDays(14);
            ////    dtLoop = dtFixed;
            ////    int index = DayAvailable(dtLoop, rDictHoliday, venderDelivery);
            ////    while (index == -1 && dtLoop <= dtEnd)
            ////    {
            ////        dtLoop = dtLoop.AddDays(1);
            ////        index = DayAvailable(dtLoop, rDictHoliday, venderDelivery);
            ////    }
            ////}
            ///* [E] A002 */


            //if (dtLoop.ToString("yyyyMMdd") == dtNow.ToString("yyyyMMdd"))
            //{
            //    if (timeNow < timeFix)
            //    {
            //        int index = response.FindIndex(x => x.Vender == vender && x.PartNo == part && x.Date.ToString(YMDFormat) == dtLoop.ToString(YMDFormat));
            //        if (index != -1)
            //        {
            //            response[index].Do += DO;
            //        }
            //    }
            //}
            //else
            //{
            //    if (dtLoop > dtRun) // ต้องเป็นวันที่มากกว่า วัน Fixed ถึงจะ Adj D/O value
            //    {
            //        int index = response.FindIndex(x => x.Vender == vender && x.PartNo == part && x.Date.ToString(YMDFormat) == dtLoop.ToString(YMDFormat));
            //        if (index != -1)
            //        {
            //            response[index].Do += DO;
            //            response[index].Log.AddRange(rHistoryIDNotUsed);
            //        }
            //    }
            //    else if (dtLoop.ToString("yyyyMMdd") == dtRun.ToString("yyyyMMdd"))
            //    {
            //        if (timeNow < timeFix)
            //        {
            //            int index = response.FindIndex(x => x.Vender == vender && x.PartNo == part && x.Date.ToString(YMDFormat) == dtLoop.ToString(YMDFormat));
            //            if (index != -1)
            //            {
            //                response[index].Do += DO;
            //            }
            //        }
            //    }
            //}
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
                //Parts = Parts.Where(x => x.Partno == "3PC08565-1").ToList();
                PARTMASTER.AddRange(Parts);
                VENDERMASTER.Add(oVdStd);
                string PartJoin = string.Join("','", Parts.GroupBy(x => x.Partno).Select(y => y.Key).ToList());
                //List<MStockAlpha> StockAlpha = GetStockAlpha(dtNow, PartJoin);
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

                foreach (DoPartMaster itemPart in Parts)
                {
                    string Part = itemPart.Partno.Trim();
                    string Cm = itemPart.Cm;
                    //if (hiddenPartNoPlan == true)
                    //{
                    //    if (Plans.Where(x => x.Partno != null && x.Partno.Trim() == Part.Trim()).Sum(x => x.Qty) == 0)
                    //    {
                    //        continue;
                    //    }
                    //}
                    MAlpart oAlPart = rAlPart.FirstOrDefault(x => x.DrawingNo == Part);
                    if (oAlPart != null)
                    {
                        Cm = oAlPart.Cm;
                    }
                    int Pdlt = oHelper.ConvIntEmptyToInt(itemPart.Pdlt);
                    double Stock = 0;
                    DateTime dtLoop = dtStart;
                    double nPoFiFo = rPOFIFO.Where(x => x.partno == Part).Sum(x => x.whblbqty);
                    DoVenderMaster objVender = ListVenderMaster.FirstOrDefault(x => x.VdCode == vender)!;
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
                        //if (Part == "2P659567-1")
                        //{
                        //    Console.WriteLine("asda");
                        //}
                        // หากเวลาปัจจุบันมากกว่า 22:00 จะค้นหาประวัติของวันที่สิ้นสุด Fixed ด้วย ==== [16/07/2024 16:51] ==== //
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
                            nPoFiFo = nPoFiFo - itemResponse.Do;
                            itemResponse.POFIFO = nPoFiFo;
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
                            nPoFiFo = nPoFiFo - itemResponse.Do;
                            itemResponse.POFIFO = nPoFiFo;
                            if (Stock <= 0)
                            {
                                double DO = GetDoVal(Math.Abs(Stock), itemPart, oVdStd);
                                DOITEM = ADJ_DO(DOITEM, vender, Part, dtLoop, Pdlt, DO, VenderDelivery, dtRun, Historys.Count > 0 ? true : false, rDictHoliday, oVdStd, StockCurrent, itemResponse.Plan, Stock, itemPart);
                                ModelRefreshStock rData = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha);
                                DOITEM = rData.data;
                                Stock = rData.Stock;
                            }
                            DOITEM.Add(itemResponse);
                        }
                        dtLoop = dtLoop.AddDays(1);
                    }
                }
            }
            bool dev = false;
            if (run == true || dev == true)
            {
                List<MRESULTDO>? rITEM = DOITEM.Where(x => x.Log.Count > 0).ToList();
                foreach (MRESULTDO oItem in rITEM)
                {
                    foreach (DoLogDev oLog in oItem.Log)
                    {
                        SqlCommand sqlInsertLog = new SqlCommand();
                        sqlInsertLog.CommandText = @"INSERT INTO [dbo].[DO_LOG_DEV] ([DO_RUNNING],[DO_REV],[LOG_PART_NO],[LOG_VD_CODE] ,[LOG_PROD_LEAD],[LOG_TYPE],[LOG_FROM_DATE],[LOG_FROM_STOCK],[LOG_FROM_PLAN],[LOG_NEXT_DATE],[LOG_NEXT_STOCK],[LOG_TO_DATE],[LOG_DO],[LOG_BOX],[LOG_STATE],[LOG_REMARK],[LOG_CREATE_DATE],[LOG_UPDATE_DATE],[LOG_UPDATE_BY])  VALUES  (@DO_RUNNING,@DO_REV,@PART,@VDCODE,@PRODLEAD,@TYPE,@F_DATE,@F_STOCK,@F_PLAN,@N_DATE,@N_STOCK,@T_DATE,@DO,@BOX,@STATE,@REMARK,GETDATE(),GETDATE(),@UPDAET_BY) SELECT SCOPE_IDENTITY()";
                        sqlInsertLog.Parameters.Add(new SqlParameter("@DO_RUNNING", doRunningCode));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@DO_REV", doRev));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@PART", oLog.logPartNo));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@VDCODE", oLog.logVdCode));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@PRODLEAD", oLog.logProdLead));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@TYPE", oLog.logType));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@F_DATE", oLog.logFromDate));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@F_STOCK", oLog.logFromStock));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@F_PLAN", oLog.logFromPlan));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@N_DATE", oLog.logNextDate));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@N_STOCK", oLog.logNextStock));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@T_DATE", oLog.logToDate));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@DO", oLog.logDo));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@BOX", oLog.logBox));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@STATE", oLog.logState));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@REMARK", oLog.logRemark));
                        sqlInsertLog.Parameters.Add(new SqlParameter("@UPDAET_BY", oLog.logUpdateBy));
                        dbSCM.ExecuteNonCommand(sqlInsertLog);
                    }
                }
            }

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

       
    }


}
