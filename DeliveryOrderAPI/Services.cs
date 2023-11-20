using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DeliveryOrderAPI
{
    public class Services
    {
        private readonly DBSCM _DBSCM;
        private SqlConnectDB dbSCM = new("dbSCM");
        private OraConnectDB dbAlpha = new("ALPHA01");
        private OraConnectDB dbAlpha2 = new("ALPHA02");

        public Services(DBSCM dBSCM)
        {
            _DBSCM = dBSCM;
        }

        public double ConvDay(string valDay = "")
        {
            return (valDay == null || valDay == "null" || valDay == "") ? 0 : double.Parse(valDay);
        }

        public DataTable GetPartOfVender(string SupplierCode)
        {
            SqlCommand sql = new SqlCommand();
            //sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.PARTNO IN ('" + _PartJoin + "')";
            sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.VD_CODE = '" + SupplierCode + "' ORDER BY PARTNO ASC";
            DataTable dt = dbSCM.Query(sql);
            return dt;
        }



        public List<MStockAlpha> GetStockAlpha(DateTime _DateRunProcess, string PARTS = "")
        {
            List<MStockAlpha> res = new List<MStockAlpha>();
            string _PART_JOIN_STRING = "";
            string _Year = _DateRunProcess.Year.ToString();
            string _Month = _DateRunProcess.Month.ToString("00");
            string date = _Year + "" + _Month;
            OracleCommand cmd = new();
            PARTS = PARTS != "" ? PARTS : _PART_JOIN_STRING;
            cmd.CommandText = @"SELECT '" + _DateRunProcess.ToString("yyyyMMdd") + "',MC1.PARTNO, MC1.CM, DECODE(SB1.DSBIT,'1','OBSOLETE','2','DEAD STOCK','3',CASE WHEN TRIM(SB1.STOPDATE) IS NOT NULL AND SB1.STOPDATE <= TO_CHAR(SYSDATE,'YYYYMMDD') THEN 'NOT USE ' || SB1.STOPDATE ELSE ' ' END, ' ') PART_STATUS, MC1.LWBAL, NVL(AC1.ACQTY,0) AS ACQTY, NVL(PID.ISQTY,0) AS ISQTY, MC1.LWBAL + NVL(AC1.ACQTY,0) - NVL(PID.ISQTY,0) AS WBAL,NVL(RT3.LREJ,0) + NVL(PID.REJIN,0) - NVL(AC1.REJOUT,0) AS REJQTY, MC2.QC, MC2.WH1, MC2.WH2, MC2.WH3, MC2.WHA, MC2.WHB, MC2.WHC, MC2.WHD, MC2.WHE,ZUB.HATANI AS UNIT, EPN.KATAKAN AS DESCR, F_GET_HTCODE_RATIO(MC1.JIBU,MC1.PARTNO, '" + _DateRunProcess.ToString("yyyyMMdd") + "') AS HTCODE, F_GET_MSTVEN_VDABBR(MC1.JIBU,F_GET_HTCODE_RATIO(MC1.JIBU,MC1.PARTNO,'" + _DateRunProcess.ToString("yyyyMMdd") + "')) SUPPLIER, SB1.LOCA1, SB1.LOCA2, SB1.LOCA3, SB1.LOCA4, SB1.LOCA5, SB1.LOCA6, SB1.LOCA7, SB1.LOCA8 FROM	(SELECT	* FROM	DST_DATMC1 WHERE	TRIM(YM) = :YM AND TRIM(PARTNO) IN ('" + PARTS + "') AND CM LIKE '%'";
            cmd.CommandText = cmd.CommandText + @") MC1, 
        		(SELECT	PARTNO, CM, SUM(WQTY) AS ACQTY, SUM(CASE WHEN WQTY < 0 THEN -1 * WQTY ELSE 0 END) AS REJOUT 
        		 FROM	DST_DATAC1 
        		 WHERE	ACDATE >= :DATE_START 
        			AND	ACDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%'  AND CM LIKE '%'
        		 GROUP BY PARTNO, CM 
        		) AC1, 
        		(SELECT	PARTNO, BRUSN AS CM, SUM(FQTY) AS ISQTY, SUM(DECODE(REJBIT,'R',-1*FQTY,0)) AS REJIN 
        		 FROM	MASTER.GST_DATPID@ALPHA01 
        		 WHERE	IDATE >= :DATE_START 
        			AND	IDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%'  AND BRUSN LIKE '%'
        		 GROUP BY PARTNO, BRUSN 
        		) PID, 
        		(SELECT    PARTNO, CM, SUM(DECODE(WHNO,'QC',BALQTY)) AS QC,SUM(DECODE(WHNO,'W1',BALQTY)) AS WH1,SUM(DECODE(WHNO,'W2',BALQTY)) AS WH2,SUM(DECODE(WHNO,'W3',BALQTY)) AS WH3, 
                           SUM(DECODE(WHNO,'WA',BALQTY)) AS WHA,SUM(DECODE(WHNO,'WB',BALQTY)) AS WHB,SUM(DECODE(WHNO,'WC',BALQTY)) AS WHC,SUM(DECODE(WHNO,'WD',BALQTY)) AS WHD,SUM(DECODE(WHNO,'WE',BALQTY)) AS WHE 
                    FROM    (SELECT    MC2.PARTNO, MC2.CM, MC2.WHNO, MC2.LWBAL, NVL(AC1.ACQTY,0) AS ACQTY, NVL(PID.ISQTY,0) AS ISQTY, MC2.LWBAL + NVL(AC1.ACQTY,0) - NVL(PID.ISQTY,0) AS BALQTY 
                            FROM    (SELECT    * 
                                    FROM    DST_DATMC2 
                                    WHERE    YM = :YM  AND TRIM(PARTNO) LIKE '%' AND CM LIKE '%'
                                   ) MC2, 
                                   (SELECT    PARTNO, CM, WHNO, SUM(WQTY) AS ACQTY 
                                    FROM    DST_DATAC1 
                                    WHERE    ACDATE >= :DATE_START 
                                       AND    ACDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%' AND CM LIKE '%'
                                    GROUP BY PARTNO, CM, WHNO 
                                   ) AC1, 
                                   (SELECT    PARTNO, BRUSN AS CM, WHNO, SUM(FQTY) AS ISQTY 
                                    FROM    (SELECT    * 
                                             FROM    MASTER.GST_DATPID@ALPHA01 
                                             WHERE    IDATE >= :DATE_START 
                                               AND    IDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%' AND BRUSN LIKE '%'
                                            UNION ALL 
                                             SELECT    * 
                                             FROM    DST_DATPID3 
                                             WHERE    IDATE >= :DATE_START 
                                               AND    IDATE <= :DATE_RUN  AND TRIM(PARTNO) LIKE '%' AND BRUSN LIKE '%'
                                           ) 
                                    GROUP BY PARTNO, BRUSN, WHNO 
                                   ) PID 
                            WHERE    MC2.PARTNO    = AC1.PARTNO(+) 
                               AND    MC2.CM        = AC1.CM(+) 
                               AND    MC2.WHNO    = AC1.WHNO(+) 
                               AND    MC2.PARTNO    = PID.PARTNO(+) 
                               AND    MC2.CM        = PID.CM(+) 
                               AND    MC2.WHNO    = PID.WHNO(+) 
                           ) 
                    GROUP BY PARTNO, CM 
                   ) MC2, 
                   MASTER.ND_EPN_TBL_V1@ALPHA01 EPN, DST_MSTSB1 SB1, MASTER.ND_ZUB_TBL@ALPHA01 ZUB, DST_DATRT3 RT3 
           WHERE    MC1.PARTNO    = AC1.PARTNO(+) 
               AND    MC1.CM        = AC1.CM(+) 
               AND    MC1.PARTNO    = PID.PARTNO(+) 
               AND    MC1.CM        = PID.CM(+) 
               AND    MC1.YM        = RT3.YM(+) 
               AND    MC1.PARTNO    = RT3.PARTNO(+) 
               AND    MC1.CM        = RT3.CM(+) 
               AND    MC1.PARTNO    = EPN.PARTNO(+) 
               AND    MC1.PARTNO    = SB1.PARTNO(+) 
               AND    MC1.CM        = SB1.CM(+) 
               AND    MC1.PARTNO    = MC2.PARTNO(+) 
               AND    MC1.CM        = MC2.CM(+) 
               AND    MC1.PARTNO    = ZUB.PARTNO(+) 
               AND    ZUB.STRYMN(+) <= :DATE_START 
               AND    ZUB.ENDYMN(+) >  :DATE_RUN 
               AND    ZUB.KSNBIT(+) <> '2'";
            cmd.Parameters.Add(new OracleParameter(":YM", date));
            cmd.Parameters.Add(new OracleParameter(":DATE_START", _DateRunProcess.ToString("yyyyMM01")));
            cmd.Parameters.Add(new OracleParameter(":DATE_RUN", _DateRunProcess.ToString("yyyyMMdd")));
            DataTable dt = dbAlpha2.Query(cmd);
            foreach (DataRow dr in dt.Rows)
            {
                res.Add(new MStockAlpha()
                {
                    Part = dr["PARTNO"].ToString().Trim(),
                    Stock = double.Parse(dr["WBAL"].ToString()),
                    Cm = dr["CM"].ToString().Trim()
                });
            }
            return res;
        }
        public List<MStockAlpha> GetStockPS8AM(DateTime _Date, string _Vender = "")
        {
            List<MStockAlpha> res = new List<MStockAlpha>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_STOCK_ALPHA] WHERE VDCODE = @VDCODE AND REV = 999";
            sql.Parameters.Add(new SqlParameter("VDCODE", _Vender));
            sql.Parameters.Add(new SqlParameter("DATE", _Date.ToString("yyyyMMdd")));
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                res.Add(new MStockAlpha()
                {
                    Part = dr["PARTNO"].ToString().Trim(),
                    Stock = double.Parse(dr["STOCK"].ToString()),
                    Cm = dr["CM"].ToString().Trim()
                });
            }
            return res;
        }

        public double GetDoVal(double planTarget, DoPartMaster itemPartMstr)
        {
            try
            {
                double _DoVal = 0;
                if (planTarget > 0)
                {
                    _DoVal = Math.Ceiling(planTarget / Convert.ToDouble(itemPartMstr.BoxQty)) * Convert.ToDouble(itemPartMstr.BoxQty);
                    _DoVal = _CheckMinDelivery(_DoVal, Convert.ToDouble(itemPartMstr.BoxMin));
                    _DoVal = _CheckMaxDelivery(_DoVal, Convert.ToDouble(itemPartMstr.BoxMax));
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

        internal MRefreshStock refreshStockSim(List<MDORESULT> res, DateTime dtDelivery, string PartCode, string SupplierCode)
        {
            var resEnd = res.LastOrDefault(x => x.part == PartCode && x.vdCode == SupplierCode);
            int indexEnd = res.FindIndex(x => x.part == PartCode && x.date == resEnd.date);
            int indexStart = res.FindIndex(x => x.date == dtDelivery && x.part == PartCode && x.vdCode == SupplierCode);
            double stockSim = res[indexStart].stockSim;
            bool firstRound = true;
            while (indexStart <= indexEnd)
            {
                if (firstRound)
                {
                    firstRound = false;
                }
                else
                {
                    stockSim = (stockSim - res[indexStart].plan) + res[indexStart].doPlan;
                }
                res[indexStart].stockSim = stockSim;
                indexStart++;
            }
            return new MRefreshStock()
            {
                Results = res,
                StockSim = stockSim
            };
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

        internal DateTime checkDelivery(DateTime dtDelivery, DataTable supplierMstr)
        {
            string shortDay = dtDelivery.ToString("ddd");
            bool CanDelivery = Convert.ToBoolean(supplierMstr.Rows[0]["VD_" + shortDay].ToString());
            if (!CanDelivery)
            {
                while (!CanDelivery)
                {
                    shortDay = dtDelivery.ToString("ddd");
                    CanDelivery = Convert.ToBoolean(supplierMstr.Rows[0]["VD_" + shortDay].ToString());
                    dtDelivery = dtDelivery.AddDays(-1);
                }
            }
            return dtDelivery;
        }

        internal DataTable GetSupplierMaster(string? supplierCode)
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT  *  FROM [dbSCM].[dbo].[DO_VENDER_MASTER] WHERE VD_CODE = '" + supplierCode + "'";
            DataTable dt = dbSCM.Query(sql);
            return dt;
        }


        public List<DoHistory> refreshStock(List<DoHistory> historys, string vdCode, DateTime date, string? part, double? plan)
        {
            int indexEnd = historys.FindLastIndex(x => x.VdCode == vdCode && x.Partno == part);
            int indexStart = historys.FindIndex(x => x.DateVal == date.AddDays(-1).ToString("yyyyMMdd") && x.Partno == part && x.VdCode == vdCode);
            double stockSim = (double)historys[indexStart].StockVal;
            bool firstRound = true;
            while (indexStart <= indexEnd)
            {
                if (firstRound)
                {
                    firstRound = false;
                }
                else
                {
                    stockSim = (stockSim - (double)historys[indexStart].PlanVal) + (double)historys[indexStart].DoVal;
                }
                historys[indexStart].StockVal = stockSim;
                indexStart++;
            }
            return historys;
        }

        public MRefreshStockDT RefreshStockDt(DataTable dtResponse, string vdCode, DateTime dtEnd, DateTime dt, string? part, double doAdj, DoPartMaster PartMstr, Dictionary<string, bool> daysDelivery)
        {

            dt = dt.AddDays((int)PartMstr.Pdlt * -1);
            if (dt < dtEnd)
            {
                dt = dtEnd;
            }
            bool CanDelivery = false;
            string DayText = dt.ToString("ddd").ToUpper();
            CanDelivery = daysDelivery[DayText.ToUpper()];
            while (!CanDelivery)
            {
                DayText = dt.ToString("ddd").ToUpper();
                CanDelivery = daysDelivery[DayText.ToUpper()];
                if (dt <= dtEnd)
                {
                    CanDelivery = true;
                }
                else
                {
                    dt = dt.AddDays(-1);
                }
            }

            var contentStart = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == dt.ToString("yyyyMMdd") && x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == part).FirstOrDefault();
            int indexStart = dtResponse.Rows.IndexOf(contentStart);

            var contentLast = dtResponse.AsEnumerable().LastOrDefault(x => x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == part); // data วันแรกของช่วง RUN D/O
            int indexEnd = dtResponse.Rows.IndexOf(contentLast);

            double stockLoop = double.Parse(contentStart.Field<string>("STOCKSIM"));
            bool ft = true;
            while (indexStart <= indexEnd)
            {
                double doLoop = double.Parse(dtResponse.Rows[indexStart]["DO"].ToString());
                double planLoop = double.Parse(dtResponse.Rows[indexStart]["PLAN"].ToString());
                doLoop += doAdj;
                doAdj = 0; // เพิ่ม D/O แค่ครั้งแรก
                if (ft)
                {
                    stockLoop = stockLoop + doLoop;
                    ft = false;
                }
                else
                {
                    stockLoop = (stockLoop - planLoop) + doLoop;
                }
                dtResponse.Rows[indexStart]["DO"] = double.Parse(dtResponse.Rows[indexStart]["DO"].ToString()) + doLoop;
                dtResponse.Rows[indexStart]["STOCKSIM"] = stockLoop;
                indexStart++;
            }

            return new MRefreshStockDT()
            {
                dt = dtResponse,
                stock = stockLoop
            };
        }

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

        internal DataTable sortDt(DataTable dtResponse)
        {
            DataView dv = dtResponse.DefaultView;
            dv.Sort = "PART ASC, DATE ASC";
            DataTable sortedDT = dv.ToTable();
            return sortedDT;
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
            //sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.PARTNO IN ('" + _PartJoin + "')";
            List<DoPartMaster> res = _DBSCM.DoPartMasters.Where(x => x.VdCode.Trim() == VenderCode.Trim()).ToList();
            return res;
        }

        internal ModelRefreshStock REFRESH_STOCK(List<MRESULTDO> response, string vender, string part, string cm, List<MStockAlpha> stockAlpha)
        {
            string YMDFormat = "yyyyMMdd";
            List<MRESULTDO> res = response;
            DateTime dtNow = DateTime.Now;
            //dtLoop = dtLoop.AddDays(Pdlt * -1);
            //if (dtLoop < DateTime.Now)
            //{
            //    dtLoop = DateTime.Now;
            //}
            //dtLoop = dtLoop.AddDays(-1);
            //int FirstIndex = res.FindIndex(x => x.Vender == vender && x.PartNo == part && x.Date.ToString(YMDFormat) == dtLoop.ToString(YMDFormat));
            double StockLoop = 0;
            MStockAlpha ItemStock = stockAlpha.FirstOrDefault(x => x.Part == part && x.Cm == cm)!;
            if (ItemStock != null)
            {
                StockLoop = ItemStock.Stock;
            }
            var FirstItem = res.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.Date == dtNow.Date &&  x.Vender == vender && x.PartNo.Trim() == part);
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

        internal List<MRESULTDO> ADJ_DO(List<MRESULTDO> response, string vender, string part, DateTime dtLoop, int pdlt, double DO, Dictionary<string, bool> venderDelivery, DateTime dtRun, bool haveHistory)
        {
            string YMDFormat = "yyyyMMdd";
            MRESULTDO LastItemOfPart = response.OrderBy(x => x.Date).LastOrDefault(x => x.Vender == vender && x.PartNo == part)!;
            dtLoop = dtLoop.AddDays(pdlt * (-1));
            DateTime dtNow = DateTime.Now;
            if (dtLoop < dtNow)
            {
                dtLoop = dtNow;
            }
            if (haveHistory && dtLoop < dtRun)
            {
                dtLoop = dtRun;
            }
            string ShortDay = dtLoop.ToString("ddd").ToUpper();
            bool CanDelivery = venderDelivery.ContainsKey(ShortDay) ? venderDelivery[ShortDay.ToUpper()] : false;
            bool Oparator = false;
            while (!CanDelivery)
            {
                dtLoop = dtLoop.AddDays(Oparator ? 1 : -1);
                if (dtLoop.Date > LastItemOfPart.Date.Date)
                {
                    break;
                }
                if (dtLoop.Date < dtNow.Date)
                {
                    dtLoop = dtNow;
                    ShortDay = dtLoop.ToString("ddd").ToUpper();
                    CanDelivery = venderDelivery.ContainsKey(ShortDay) ? venderDelivery[ShortDay.ToUpper()] : false;
                    if (!CanDelivery)
                    {
                        Oparator = !Oparator;
                    }
                }
                else
                {
                    ShortDay = dtLoop.ToString("ddd").ToUpper();
                    CanDelivery = venderDelivery.ContainsKey(ShortDay) ? venderDelivery[ShortDay.ToUpper()] : false;
                }
            }
            int index = response.FindIndex(x => x.Vender == vender && x.PartNo == part && x.Date.ToString(YMDFormat) == dtLoop.ToString(YMDFormat));
            if (index != -1)
            {
                response[index].Do += DO;
            }
            return response;
        }


        public MODEL_GET_DO CalDO(bool run, string vdcode = "")
        {
            vdcode = vdcode != null ? vdcode : "";
            string YMDFormat = "yyyyMMdd";
            string nbr = "";
            List<MRESULTDO> DOITEM = new List<MRESULTDO>();
            List<DoPartMaster> PARTMASTER = new List<DoPartMaster>();
            List<DoVenderMaster> VENDERMASTER = new List<DoVenderMaster>();
            Dictionary<string, Dictionary<string, bool>> VenderDeliveryMaster = new Dictionary<string, Dictionary<string, bool>>();
            int dFixed = 2;
            int dRun = 7;
            DateTime dtNow = DateTime.Now;
            DateTime dtStart = dtNow.AddDays(-7);
            DateTime dtRun = dtNow.AddDays(dFixed);
            DateTime dtEnd = dtNow.AddDays(dFixed + dRun);
            List<DoHistoryDev> Historys = _DBSCM.DoHistoryDevs.Where(x => x.Revision == 999).OrderBy(x => x.Partno).ToList();
            if (Historys.Count > 0)
            {
                int rev = (int)Historys.FirstOrDefault()!.Rev!;
                nbr = $"{Historys.FirstOrDefault()!.RunningCode}{rev.ToString("D3")}";
            }
            List<DoDictMstr> ListVenderOfBuyer = _DBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == "41256").ToList();
            if (vdcode != "")
            {
                ListVenderOfBuyer = ListVenderOfBuyer.Where(x => x.RefCode == vdcode).ToList();
            }
            foreach (DoDictMstr itemVender in ListVenderOfBuyer)
            {
                string vender = itemVender.RefCode!;
                DoVenderMaster VenderMaster = _DBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == vender)!;
                Dictionary<string, bool> VenderDelivery = getDaysDelivery(VenderMaster);
                if (!VenderDeliveryMaster.ContainsKey(vender)) { VenderDeliveryMaster.Add(vender, VenderDelivery); }
                List<ViDoPlan> Plans = GetPlans(vender, dtNow, dtEnd);
                List<DoPartMaster> Parts = GetPartByVenderCode(vender);
                PARTMASTER.AddRange(Parts);
                VENDERMASTER.Add(VenderMaster);
                string PartJoin = string.Join("','", Parts.GroupBy(x => x.Partno).Select(y => y.Key).ToList());
                //List<MStockAlpha> StockAlpha = GetStockAlpha(dtNow, PartJoin);
                List<MStockAlpha> StockAlpha = GetStockPS8AM(dtNow, vender);
                List<MPickList> PickLists = GetPickListBySupplier(PartJoin, dtStart, dtEnd);
                List<MDOAct> DOActs = GetDoActs(dtStart, dtEnd, PartJoin, vender);
                List<MPO> POs = GetPoDIT(dtStart, dtEnd, PartJoin);
                foreach (DoPartMaster itemPart in Parts)
                {
                    string Part = itemPart.Partno.Trim();
                    string Cm = itemPart.Cm;
                    if (Plans.FirstOrDefault(x => x.Partno == Part) != null) { Cm = Plans.FirstOrDefault(x => x.Partno == Part)!.Cm!; }
                    int Pdlt = (int)itemPart.Pdlt!;
                    double Stock = 0;
                    DateTime dtLoop = dtStart;
                    while (dtLoop <= dtEnd)
                    {
                        MRESULTDO itemResponse = new MRESULTDO();
                        itemResponse.PartNo = Part;
                        itemResponse.Vender = vender;
                        itemResponse.Date = dtLoop;
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
                        MPO itemPO = POs.FirstOrDefault(x => x.partNo.Trim() == Part && x.date == dtLoop.Date.ToString(YMDFormat) && x.vdCode == vender)!;
                        if (itemPO != null)
                        {
                            itemResponse.PO = itemPO.qty;
                        }
                        if (dtLoop.Date == dtNow.Date)
                        {
                            MStockAlpha itemStock = StockAlpha.FirstOrDefault(x => x.Part.Trim() == Part.Trim() && x.Cm == Cm)!;
                            if (itemStock != null)
                            {
                                Stock = itemStock.Stock;
                                itemResponse.Stock = Stock;
                            }
                        }
                        if (dtLoop.Date < dtNow.Date)
                        {
                            DoHistoryDev itemHistory = Historys.FirstOrDefault(x => x.VdCode == vender && x.DateVal == dtLoop.ToString(YMDFormat) && x.Partno == Part)!;
                            if (itemHistory != null)
                            {
                                itemResponse.Plan = (double)itemHistory.PlanVal!;
                                itemResponse.Do = (double)itemHistory.DoVal!;
                                itemResponse.Stock = (double)itemHistory.StockVal!;
                            }
                            DOITEM.Add(itemResponse);
                        }
                        else
                        {
                            DoHistoryDev itemHistory = (dtLoop.Date >= dtNow.Date && dtLoop.Date <= dtRun.Date) ? Historys.FirstOrDefault(x => x.VdCode == vender && x.DateVal == dtLoop.ToString(YMDFormat) && x.Partno == Part)! : null!;
                            if (itemHistory != null) // เอาประวัติมาแสดง
                            {
                                itemResponse.Plan = (double)itemHistory.PlanVal!;
                                itemResponse.Do = (double)itemHistory.DoVal!;
                                Stock = (Stock - itemResponse.Plan) + itemResponse.Do;
                                itemResponse.Stock = Stock;
                                itemResponse.Wip = 0;
                                DOITEM.Add(itemResponse);
                            }
                            else
                            {
                                itemResponse.Plan = 0;
                                itemResponse.PickList = 0;
                                ViDoPlan itemPlan = Plans.FirstOrDefault(x => x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;
                                if (itemPlan != null)
                                {
                                    itemResponse.Plan = (double)itemPlan.Qty!;
                                }

                                Stock -= itemResponse.Plan;
                                itemResponse.Stock = Stock;
                                DOITEM.Add(itemResponse);
                                if (Stock < 0)
                                {
                                    double DO = GetDoVal(Math.Abs(Stock), itemPart);
                                    DOITEM = ADJ_DO(DOITEM, vender, Part, dtLoop, Pdlt, DO, VenderDelivery, dtRun, Historys.Count > 0 ? true : false);
                                    ModelRefreshStock rData = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha);
                                    DOITEM = rData.data;
                                    Stock = rData.Stock;
                                }
                            }
                        }
                        dtLoop = dtLoop.AddDays(1);
                    }
                }
            }
            //List<DoDictMstr> HOLIDAY = _GET_HOLIDAY();
            return new MODEL_GET_DO()
            {
                data = DOITEM,
                PartMaster = PARTMASTER,
                VenderMaster = VENDERMASTER,
                nbr = nbr,
                VenderDelivery = VenderDeliveryMaster
            };
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
