using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
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
        private OraConnectDB dbAlphaPlan = new("ALPHAPLAN");
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
        public List<MStockAlpha> GetStockPS8AM(string _Vender = "")
        {
            List<MStockAlpha> res = new List<MStockAlpha>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT PARTNO,STOCK,CM FROM [dbSCM].[dbo].[DO_STOCK_ALPHA] WHERE DATE_PD = FORMAT(DATEADD(hour,-8,GETDATE()),'yyyy-MM-dd') AND VDCODE = @VDCODE AND REV = 999";
            sql.Parameters.Add(new SqlParameter("VDCODE", _Vender));
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
                catch (Exception e)
                {
                    Console.WriteLine(e);
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

            //********* UPDATE QUERY GET PLAN [NEW] ***********

            //List<DoPlanP91> doPlanP91s = new List<DoPlanP91>();
            //OracleCommand cmd = new();
            //cmd.CommandText = @"SELECT  WCNO, PRDYMD, MODEL, SUM(QTY) QTY FROM(    
            //                        SELECT        WCNO, MODEL, PRDYMD, SUM(QTY) AS QTY
            //                            FROM            GSD_ACTPLN
            //                            WHERE        (PRDYMD BETWEEN '20250101' AND '99999999') AND (DATA_TYPE IN ('1', '2', '4', '5'))
            //                            GROUP BY WCNO, MODEL, PRDYMD
            //                            UNION ALL
            //                            SELECT        WCNO, MODEL, PRDYMD, SUM(QTY) * - 1 AS QTY
            //                            FROM            GSD_ACTPLN   AL_GSD_ACTPLN_1
            //                            WHERE        (PRDYMD BETWEEN '20250101' AND '99999999') AND (DATA_TYPE IN ('3'))
            //                            GROUP BY WCNO, MODEL, PRDYMD
            //                        )
            //                        WHERE LENGTH(WCNO) = 3 AND WCNO <> '900' 
            //                        GROUP BY WCNO, PRDYMD, MODEL 
            //                       ";
            //DataTable dtP91 = dbAlphaPlan.Query(cmd);
            //if (dtP91.Rows.Count > 0)
            //{
            //    foreach (DataRow dr in dtP91.Rows)
            //    {
            //        DoPlanP91 item = new DoPlanP91();
            //        item.wcno = dr["WCNO"].ToString().Trim();
            //        item.Qty = Convert.ToDecimal(dr["QTY"].ToString());
            //        item.model = dr["MODEL"].ToString().Trim();
            //        item.prdymd = dr["PRDYMD"].ToString().Trim();
            //        doPlanP91s.Add(item);

            //        SqlCommand sqlInsert = new SqlCommand();
            //        sqlInsert.CommandText = $@"INSERT INTO [dbSCM].[dbo].[DO_DailyPlan_P91] ([WCNO],[PRDYMD],[MODEL],[Plan]) 
            //                          VALUES ('{item.wcno}','{item.prdymd}','{item.model}','{item.Qty}')";
            //        int insert = dbSCM.ExecuteNonCommand(sqlInsert);
            //    }
            //}

            //**********************



            //SqlCommand sql = new SqlCommand();
            //sql.CommandText = @"SELECT PRDYMD,PARTNO,CM,VENDER,ROUND(SUM(CONSUMPTION),0)  AS CONSUMPTION
            //                             FROM [dbSCM].[dbo].[vi_DO_Plan]
            //                             WHERE PRDYMD >= @SDATE 
            //                             AND PRDYMD <= @FDATE AND VENDER = @SUPPLIER  GROUP BY PRDYMD,PARTNO,CM,VENDER ORDER BY PRDYMD ASC";
            //sql.Parameters.Add(new SqlParameter("@SDATE", sDate.ToString("yyyyMMdd")));
            //sql.Parameters.Add(new SqlParameter("@FDATE", fDate.ToString("yyyyMMdd")));
            //sql.Parameters.Add(new SqlParameter("@SUPPLIER", supplier));
            //DataTable dt = dbSCM.Query(sql);
            //if (dt.Rows.Count > 0)
            //{
            //    foreach (DataRow dr in dt.Rows)
            //    {
            //        ViDoPlan item = new ViDoPlan();
            //        item.Qty = Convert.ToDecimal(dr["CONSUMPTION"].ToString());
            //        item.Prdymd = dr["PRDYMD"].ToString();
            //        item.Partno = dr["PARTNO"].ToString();
            //        item.Cm = dr["CM"].ToString();
            //        res.Add(item);
            //    }
            //}










            //sql.CommandText = @"SELECT [DATE_VAL] PRDYMD
            //               ,[PARTNO] PARTNO
            //               ,VD_CODE VENDER
            //               ,ROUND(SUM(PLAN_VAL),0) AS CONSUMPTION

            //                  FROM [dbSCM].[dbo].[DO_HISTORY_DEV]
            //                  WHERE  RUNNING_CODE  IN (SELECT TOP(1) RUNNING_CODE FROM [dbSCM].[dbo].[DO_HISTORY_DEV] ORDER BY RUNNING_CODE desc)
            //                        and DATE_VAL >= @SDATE and DATE_VAL <= @FDATE
            //                        and VD_CODE = @SUPPLIER
            //                GROUP BY DATE_VAL,PARTNO,VD_CODE 
            //                ORDER BY PRDYMD ASC";
            //sql.Parameters.Add(new SqlParameter("@SDATE", sDate.ToString("yyyyMMdd")));
            //sql.Parameters.Add(new SqlParameter("@FDATE", fDate.ToString("yyyyMMdd")));
            //sql.Parameters.Add(new SqlParameter("@SUPPLIER", supplier));
            //DataTable dt = dbSCM.Query(sql);
            //if (dt.Rows.Count > 0)
            //{
            //    foreach (DataRow dr in dt.Rows)
            //    {
            //        ViDoPlan item = new ViDoPlan();
            //        item.Qty = Convert.ToDecimal(dr["CONSUMPTION"].ToString());
            //        item.Prdymd = dr["PRDYMD"].ToString();
            //        item.Partno = dr["PARTNO"].ToString();
            //        //item.Cm = dr["CM"].ToString();
            //        res.Add(item);
            //    }
            //}
            return res;
        }



        public List<ViDoPlan> GetPlansHistoryDev(string supplier, DateTime sDate, DateTime fDate)
        {
            List<ViDoPlan> res = new List<ViDoPlan>();

            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT [DATE_VAL] PRDYMD
                           ,[PARTNO] PARTNO
                           ,VD_CODE VENDER
                           ,ROUND(SUM(PLAN_VAL),0) AS CONSUMPTION

                              FROM [dbSCM].[dbo].[DO_HISTORY_DEV]
                              WHERE  RUNNING_CODE  IN (SELECT TOP(1) RUNNING_CODE FROM [dbSCM].[dbo].[DO_HISTORY_DEV] ORDER BY RUNNING_CODE desc)
                                    and DATE_VAL >= @SDATE and DATE_VAL <= @FDATE
                                    and VD_CODE = @SUPPLIER
                            GROUP BY DATE_VAL,PARTNO,VD_CODE 
                            ORDER BY PRDYMD ASC";
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
                    //item.Cm = dr["CM"].ToString();
                    res.Add(item);
                }
            }
            return res;
        }

        public List<ViDoPlan> GetSVPlans(string supplier, DateTime sDate, DateTime fDate)
        {
            List<ViDoPlan> res = new List<ViDoPlan>();


            string _jibu = "64";
            string _pid = DateTime.Now.ToString("ddHHmmss");
            string _partno = "3PD06362-1";

            string strOra = $"PL_DCL_N16";
            OracleCommand cmdOra = new OracleCommand();
            cmdOra.CommandText = strOra;
            cmdOra.CommandType = CommandType.StoredProcedure;
            cmdOra.Parameters.Add("inpJIBU", OracleDbType.Varchar2, _jibu, ParameterDirection.Input);
            cmdOra.Parameters.Add("inpPID", OracleDbType.Varchar2, _pid, ParameterDirection.Input);
            cmdOra.Parameters.Add("inpPARTNO", OracleDbType.Varchar2, _partno, ParameterDirection.Input);
            cmdOra.Parameters.Add("inpKOTEI", OracleDbType.Varchar2, "", ParameterDirection.Output);
            cmdOra.Parameters.Add("inpENDYMN", OracleDbType.Varchar2, "", ParameterDirection.Output);
            cmdOra.Parameters.Add("inpHBIT", OracleDbType.Varchar2, "1", ParameterDirection.Output);
            dbAlpha.ExecuteCommand(cmdOra);

            string strWK = $@"
                    WITH h AS (
                        SELECT partno, brusn, htcode, ymd, ymd_date 
                        FROM 
                            (SELECT partno, brusn, htcode,  
                                tekymn01,tekymn02,tekymn03,tekymn04,tekymn05,tekymn06,tekymn07,tekymn08,tekymn09,tekymn10,
                                tekymn11,tekymn12,tekymn13,tekymn14,tekymn15,tekymn16,tekymn17,tekymn18,tekymn19,tekymn20,
                                tekymn21,tekymn22,tekymn23,tekymn24,tekymn25,tekymn26,tekymn27,tekymn28,tekymn29,tekymn30,
                                tekymn31,tekymn32,tekymn33,tekymn34,tekymn35,tekymn36,tekymn37,tekymn38,tekymn39,tekymn40,
                                tekymn41,tekymn42,tekymn43,tekymn44,tekymn45,tekymn46,tekymn47,tekymn48,tekymn49,tekymn50,
                                tekymn51,tekymn52,tekymn53,tekymn54,tekymn55,tekymn56,tekymn57,tekymn58,tekymn59,tekymn60,
                                tekymn61,tekymn62  
                             FROM MASTER.ND_DCL_WK N
                             WHERE PID='{_pid}' AND partno LIKE '{_partno}%' AND BF_BIT = '1' AND ROWNUM = 1 )
                        UNPIVOT 
                            (ymd_date FOR ymd IN (
                                tekymn01 AS '01', tekymn02 AS '02', tekymn03 AS '03', tekymn04 AS '04', tekymn05 AS '05', tekymn06 AS '06', tekymn07 AS '07', tekymn08 AS '08', tekymn09 AS '09', tekymn10 AS '10', 
                                tekymn11 AS '11', tekymn12 AS '12', tekymn13 AS '13', tekymn14 AS '14', tekymn15 AS '15', tekymn16 AS '16', tekymn17 AS '17', tekymn18 AS '18', tekymn19 AS '19', tekymn20 AS '20',
                                tekymn21 AS '21', tekymn22 AS '22', tekymn23 AS '23', tekymn24 AS '24', tekymn25 AS '25', tekymn26 AS '26', tekymn27 AS '27', tekymn28 AS '28', tekymn29 AS '29', tekymn30 AS '30',
                                tekymn31 AS '31', tekymn32 AS '32', tekymn33 AS '33', tekymn34 AS '34', tekymn35 AS '35', tekymn36 AS '36', tekymn37 AS '37', tekymn38 AS '38', tekymn39 AS '39', tekymn40 AS '40',
                                tekymn41 AS '41', tekymn42 AS '42', tekymn43 AS '43', tekymn44 AS '44', tekymn45 AS '45', tekymn46 AS '46', tekymn47 AS '47', tekymn48 AS '48', tekymn49 AS '49', tekymn50 AS '50',
                                tekymn51 AS '51', tekymn52 AS '52', tekymn53 AS '53', tekymn54 AS '54', tekymn55 AS '55', tekymn56 AS '56', tekymn57 AS '57', tekymn58 AS '58', tekymn59 AS '59', tekymn60 AS '60',
                                tekymn61 AS '61', tekymn62 AS '62' 
        
                            ))
                    ), 
                    d AS (
                        SELECT partno, brusn, ymd, ymd_do  
                        FROM 
                            (SELECT partno, kotei, brusn, 
                                zdai01,zdai02,zdai03,zdai04,zdai05,zdai06,zdai07,zdai08,zdai09,zdai10,
                                zdai11,zdai12,zdai13,zdai14,zdai15,zdai16,zdai17,zdai18,zdai19,zdai20,
                                zdai21,zdai22,zdai23,zdai24,zdai25,zdai26,zdai27,zdai28,zdai29,zdai30,
                                zdai31,zdai32,zdai33,zdai34,zdai35,zdai36,zdai37,zdai38,zdai39,zdai40,
                                zdai41,zdai42,zdai43,zdai44,zdai45,zdai46,zdai47,zdai48,zdai49,zdai50,
                                zdai51,zdai52,zdai53,zdai54,zdai55,zdai56,zdai57,zdai58,zdai59,zdai60,
                                zdai61,zdai62 
                             FROM MASTER.ND_DCL_WK N
                             WHERE PID='{_pid}' AND partno LIKE '{_partno}%' AND KISYUN IN  ('TOTAL') )
                        UNPIVOT 
                            (ymd_do FOR ymd IN (
                                zdai01 AS '01', zdai02 AS '02', zdai03 AS '03', zdai04 AS '04', zdai05 AS '05', zdai06 AS '06', zdai07 AS '07', zdai08 AS '08', zdai09 AS '09', zdai10 AS '10', 
                                zdai11 AS '11', zdai12 AS '12', zdai13 AS '13', zdai14 AS '14', zdai15 AS '15', zdai16 AS '16', zdai17 AS '17', zdai18 AS '18', zdai19 AS '19', zdai20 AS '20',
                                zdai21 AS '21', zdai22 AS '22', zdai23 AS '23', zdai24 AS '24', zdai25 AS '25', zdai26 AS '26', zdai27 AS '27', zdai28 AS '28', zdai29 AS '29', zdai30 AS '30',
                                zdai31 AS '31', zdai32 AS '32', zdai33 AS '33', zdai34 AS '34', zdai35 AS '35', zdai36 AS '36', zdai37 AS '37', zdai38 AS '38', zdai39 AS '39', zdai40 AS '40',
                                zdai41 AS '41', zdai42 AS '42', zdai43 AS '43', zdai44 AS '44', zdai45 AS '45', zdai46 AS '46', zdai47 AS '47', zdai48 AS '48', zdai49 AS '49', zdai50 AS '50',
                                zdai51 AS '51', zdai52 AS '52', zdai53 AS '53', zdai54 AS '54', zdai55 AS '55', zdai56 AS '56', zdai57 AS '57', zdai58 AS '58', zdai59 AS '59', zdai60 AS '60',
                                zdai61 AS '61', zdai62 AS '62' ))
                    )
                    SELECT h.partno, h.brusn, h.htcode, h.ymd_date, d.ymd_do 
                    FROM h
                    LEFT JOIN d ON h.partno = d.partno AND h.ymd = d.ymd 
            ";
            OracleCommand cmdWK = new OracleCommand();
            cmdWK.CommandText = strWK;
            DataTable dtWK = dbAlpha.Query(cmdWK);


            if (dtWK.Rows.Count > 0)
            {
                foreach (DataRow dr in dtWK.Rows)
                {
                    ViDoPlan item = new ViDoPlan();
                    item.Qty = Convert.ToDecimal(dr["ymd_do"].ToString());
                    item.Prdymd = dr["ymd_date"].ToString();
                    item.Partno = dr["partno"].ToString();
                    item.Cm = dr["brusn"].ToString();
                    res.Add(item);
                }
            }
            return res;
        }





        public List<DoPartMaster> GetPartByVenderCode(string VenderCode)
        {
            List<DoPartMaster> res = new List<DoPartMaster>();
            //List<DoPartMaster> res = _DBSCM.DoPartMasters.Where(x => x.VdCode.Trim() == VenderCode.Trim() && x.Active == "ACTIVE").ToList();
            SqlCommand Sql = new SqlCommand();
            Sql.CommandText = $@"  SELECT A.* FROM (SELECT  TRIM([PARTNO]) [PARTNO]  ,[CM] ,BOX_QTY,CASE WHEN BOX_MIN IS NULL THEN BOX_QTY ELSE BOX_MIN END BOX_MIN ,BOX_MAX, DIAMETER,VD_CODE,DESCRIPTION,PDLT,UNIT,BOX_PER_PALLET
  ,ROW_NUMBER() OVER (PARTITION BY TRIM(PARTNO) ORDER BY CM DESC) rn
  FROM [dbSCM].[dbo].[DO_PART_MASTER]
  WHERE ACTIVE = 'ACTIVE' AND BOX_QTY IS NOT NULL AND VD_CODE = '{VenderCode}'
  ORDER BY VD_CODE ASC,PARTNO  ASC OFFSET 0 ROWS) A
  WHERE A.rn = 1";
            DataTable dt = dbSCM.Query(Sql);
            foreach (DataRow dr in dt.Rows)
            {
                string partno = dr["PARTNO"].ToString()!;
                string cm = dr["CM"].ToString()!;
                int boxQty = oHelper.ConvStr2Int(dr["BOX_QTY"].ToString()!);
                int boxMin = oHelper.ConvStr2Int(dr["BOX_MIN"].ToString()!);
                int boxMax = oHelper.ConvStr2Int(dr["BOX_MAX"].ToString()!);
                string diameter = dr["DIAMETER"].ToString()!;
                string vdCode = dr["VD_CODE"].ToString()!;
                string desc = dr["DESCRIPTION"].ToString()!;
                int boxPerPallet = oHelper.ConvStr2Int(dr["BOX_PER_PALLET"].ToString()!);
                int pdlt = oHelper.ConvStr2Int(dr["PDLT"].ToString()!);
                string unit = dr["UNIT"].ToString()!;
                res.Add(new DoPartMaster() { Active = "ACTIVE", PartId = 1, Partno = partno, Cm = cm, BoxQty = boxQty, BoxMin = boxMin, BoxMax = boxMax, Diameter = diameter, VdCode = vdCode, Description = desc, Pdlt = pdlt, BoxPerPallet = boxPerPallet, Unit = unit });
            }
            return res;
        }
        internal ModelRefreshStock REFRESH_STOCK(List<MRESULTDO> response, string vender, string part, string cm, List<MStockAlpha> stockAlpha)
        {
            List<MRESULTDO> res = response;
            DateTime dtNow = DateTime.Now;
            double StockLoop = 0;
            MStockAlpha ItemStock = stockAlpha.FirstOrDefault(x => x.Part == part.Trim() && x.Cm == cm.Trim())!;
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

        internal List<MRESULTDO> ADJ_DO(List<MRESULTDO> response, string vender, string part, DateTime dtLoop, double DO, Dictionary<string, bool> VdMaster,  DoVenderMaster vdMaster,double PORemain)
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
            bool Increment = false; // สำหรับการ เพิ่ม หรือ ลด วันที่ เพื่อหาวันที่สามารถจัดส่งได้ false -1 
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
                    response[index].POFIFO = PORemain;
                }
            }
            return response;
        }

        public MODEL_GET_DO CalDO(bool run, string vdcode = "", MGetPlan param = null, string doRunningCode = "", int doRev = 0, bool? hiddenPartNoPlan = true , 
                                 bool? statusWarining = false)
        {

            List<string> fixDateHiglight = new List<string>();
            DateTime dtNow = DateTime.Now;
            DateTime dtDistribute = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 08, 0, 0);
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
            DateTime dtStart = statusWarining == true ? DateTime.Now : dtNow.AddDays(-7);
            DateTime dtRun = dtNow.AddDays(dFixed);
            DateTime dtEnd = dtNow.AddDays(dFixed + dRun);
            /* A003 */
            int countFixed = 13;
            //List<DoDictMstr> rDictHoliday = _DBSCM.DoDictMstrs.Where(x => x.DictType == "holiday" && Convert.ToInt32(x.Code) >= Convert.ToInt32(ymd) && Convert.ToInt32(x.Code) <= Convert.ToInt32(dtNow.AddDays(countFixed).ToString("yyyyMMdd"))).ToList();



            // ********** ADD NEW 08/01/25 get caledar from alpha 011 ************** //
            List<CalendarAlpha> rDictHoliday = GetHolidayAlpha().Where(x => Convert.ToInt32(x.ymd) >= Convert.ToInt32(ymd) && Convert.ToInt32(x.ymd) <= Convert.ToInt32(dtNow.AddDays(countFixed).ToString("yyyyMMdd"))).ToList();

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
                    dtEnd = statusWarining == true ? dtNow.AddDays(VdProdLead) : dtNow.AddDays(VdProdLead + dRun);
                }
                Dictionary<string, bool> VenderDelivery = getDaysDelivery(oVdStd);
                if (!VenderDeliveryMaster.ContainsKey(vender)) { VenderDeliveryMaster.Add(vender, VenderDelivery); }

                // *** AFTER FIX DATE ***
                List<ViDoPlan> Plans = GetPlans(vender, dtNow, dtEnd);

                // *** BETWEEN FIX DATE ***
                List<ViDoPlan> PlansHistoryDev = GetPlansHistoryDev(vender, dtNow, dtEnd);

                List<DoPartMaster> Parts = GetPartByVenderCode(vender);


                //**** Addition Part S/V ****
                List<ViDoPlan> PlansSV = GetSVPlans(vender, dtNow, dtEnd);
                if (vender == "194013")
                {
                    Plans.AddRange(PlansSV);
                }
                PARTMASTER.AddRange(Parts);
                VENDERMASTER.Add(oVdStd);
                string PartJoin = string.Join("','", Parts.GroupBy(x => x.Partno).Select(y => y.Key).ToList());
                List<MStockAlpha> StockAlpha = GetStockPS8AM(vender);
                List<MPickList> PickLists = GetPickListBySupplier(PartJoin, dtStart, dtEnd);
                List<MDOAct> DOActs = GetDoActs(dtStart, dtEnd, PartJoin, vender);
                List<MPO> POs = GetPoDIT(dtStart, dtEnd, PartJoin);

                // ********** EDIT 15/11/24 Support holiday ************** //

                DateTime dtEndFixed = dtNow.AddDays(((oVdStd.VdProdLead != null ? (int)oVdStd.VdProdLead : 0) - 1)+1);

                // ********** EDIT 15/11/24 Update ************** //

                DateTime dtnewFixed = DateTime.Now;
                int count = 0;
              
                    while (count <= (int)oVdStd.VdProdLead)
                    {


                    //if (dtnewFixed.DayOfWeek == DayOfWeek.Saturday || dtnewFixed.DayOfWeek == DayOfWeek.Sunday)
                    //{

                    //}
                
                    // ********** EDIT 08/01/25 Update ************** //
                    if (rDictHoliday.Where(x=>x.ymd == dtnewFixed.ToString("yyyyMMdd")).ToList().Count > 0)
                    {

                    }
                    else
                    {
                        fixDateHiglight.Add(dtnewFixed.ToString("yyyy-MM-dd"));
                        count++;
                        if (count == (int)oVdStd.VdProdLead) break;

                    }


                    dtnewFixed = dtnewFixed.AddDays(1);
                    }

                dtEndFixed = dtnewFixed;




                // ************************************ //

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
                    iPOFIFO.whblbqty = oHelper.ConvStrToDB(dr["WHBLBQTY"].ToString()!);
                    iPOFIFO.whblqty = oHelper.ConvStrToDB(dr["WHBLQTY"].ToString()!);
                    iPOFIFO.partno = dr["PARTNO"].ToString().Trim();
                    iPOFIFO.brusn = dr["BRUSN"].ToString();
                    iPOFIFO.delymd = dr["DELYMD"].ToString();
                    rPOFIFO.Add(iPOFIFO);
                }

                //Parts = Parts.Where(x => x.Partno == "3P482067-1").ToList();
                foreach (DoPartMaster itemPart in Parts)
                {
                    string Part = itemPart.Partno.Trim();
                    string Cm = itemPart.Cm;
                    //MAlpart oAlPart = rAlPart.FirstOrDefault(x => x.DrawingNo == Part);
                    //if (oAlPart != null)
                    //{
                    //    Cm = oAlPart.Cm;
                    //}

                    int Pdlt = oHelper.ConvIntEmptyToInt(itemPart.Pdlt);
                    double Stock = 0;
                    DateTime dtLoop = dtStart;
                    double nPoFiFo = rPOFIFO.Where(x => x.partno == Part).Sum(x => x.whblbqty);
                    double defPOFIFO = nPoFiFo;
                    DoVenderMaster objVender = ListVenderMaster.FirstOrDefault(x => x.VdCode == vender)!;
                    double safetyStock = 0;
                    try
                    {
                        decimal? oPlanOfPart = Plans.Where(x => x.Partno != null && x.Partno.Trim() == Part.Trim() && DateTime.ParseExact(x.Prdymd,"yyyyMMdd", CultureInfo.InvariantCulture).Date >= dtLoop.Date).Sum(x => x.Qty);
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
                        MRESULTDO itemDO = new MRESULTDO();
                        itemDO.PartNo = Part;
                        itemDO.Vender = vender;
                        itemDO.vdCode = vender;
                        itemDO.vdName = objVender != null ? objVender.VdDesc : vender;
                        itemDO.Date = dtLoop;
                        if (dtLoop > dtEndFixed)
                        {
                            //DoDictMstr isHoliday = rDictHoliday.FirstOrDefault(x => x.Code == dtLoop.ToString("yyyyMMdd"));

                            // ********** ADD NEW 08/01/25 get caledar from alpha 011 ************** /
                            CalendarAlpha isHoliday = rDictHoliday.FirstOrDefault(x => x.ymd == dtLoop.ToString("yyyyMMdd"));

                            if (isHoliday != null) // Date [N] = Holiday
                            {
                                itemDO.holiday = true;
                            }
                        }
                        MPickList itemPickList = PickLists.FirstOrDefault(x => x.Partno == Part && x.Idate == dtLoop.Date.ToString(YMDFormat))!;
                        if (itemPickList != null)
                        {
                            itemDO.PickList = double.Parse(itemPickList.Fqty);
                        }
                        MDOAct itemDoAct = DOActs.FirstOrDefault(x => x.PartNo == Part && x.AcDate == dtLoop.Date.ToString(YMDFormat) && x.Vender == vender)!;
                        if (itemDoAct != null)
                        {
                            itemDO.DoAct = itemDoAct.Wqty;
                        }

                        if (dtLoop.Date == dtNow.Date)
                        {
                            List<MStockAlpha> rStock = StockAlpha.Where(x => x.Part.Trim() == Part.Trim()).ToList();
                            if (rStock.Count > 0)
                            {
                                Stock = rStock.Sum(x => x.Stock);
                                itemDO.Stock = Stock;
                            }
                        }
                       DoHistoryDev oHis = Historys.FirstOrDefault(x => x.VdCode == vender && x.DateVal == dtLoop.ToString(YMDFormat) && x.Partno == Part && x.Revision == 999)!;
                        //if ((dtNow > dtDistribute && dtLoop.Date <= dtEndFixed.Date) || (dtNow <= dtDistribute && dtLoop.Date <= dtEndFixed.Date) && oHis != null)
                        if (dtLoop.Date <= dtEndFixed.Date && oHis != null) // อดีด -> [E] Fixed
                        {
                            // 13 > 8 && 13 <= 18 || 13 <= 8 && 13 <= 18
                            //DoHistoryDev oHis = Historys.FirstOrDefault(x => x.VdCode == vender && x.DateVal == dtLoop.ToString(YMDFormat) && x.Partno == Part)!;
                            //if (oHis != null)
                            //{

                            itemDO.Plan = (double)oHis.PlanVal!;
                            itemDO.PlanPrev = itemDO.Plan;
                            //itemDO.Do = nPoFiFo < (double)oHis.DoVal! ? nPoFiFo : (double)oHis.DoVal!;
                            itemDO.Do = (double)oHis.DoVal!;
                            itemDO.Stock = itemDO.Stock;
                            // หากวันที่เป็นวันปัจจุบันขึ้นไป จะนำ STOCK - PLAN
                            if (dtLoop.Date >= dtNow.Date)
                            {
                                decimal? planP91_QTY = 0;
                                decimal? planHistoryDev = 0;

                                // ================================================================================ //
                                // [S] ========= เช็คว่าประวัติแผนการผลิต ตรงหรือไม่ตรง กับแผนผลิต (ปัจจุบัน) [16/07/24 19:40] ===== //
                                // ================================================================================ //

                                //ViDoPlan itemPlan = Plans.FirstOrDefault(x => x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;

                                ViDoPlan itemPlan = Plans.FirstOrDefault(x => x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;
                                ViDoPlan itemPlanHistoryDev = PlansHistoryDev.FirstOrDefault(x => x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;
                                if (itemPlan == null || itemPlan.Qty != oHelper.ConvDBToDec(itemDO.Plan))
                                {
                                    itemDO.Plan = oHelper.ConvDecToDB(itemPlan?.Qty);
                                    itemDO.PlanPrev = itemDO.Plan;

                                }


                                planP91_QTY = itemPlan == null ? 0 : itemPlan.Qty;
                                planHistoryDev = itemPlanHistoryDev == null ? 0 : itemPlanHistoryDev.Qty;

                                if (planP91_QTY != planHistoryDev)
                                {
                                    itemDO.changePlan = true;
                                    itemDO.HistoryDevPlanQTY = planHistoryDev;
                                }
                                // ================================================================================ //
                                // [E] ========= เช็คว่าประวัติแผนการผลิต ตรงหรือไม่ตรง กับแผนผลิต (ปัจจุบัน) [16/07/24 19:40] ===== //
                                // ================================================================================ //
                                Stock -= itemDO.Plan;
                                Stock += itemDO.Do;
                                itemDO.Stock = Stock;
                            }
                            //}

                            //================ SET PO ==================//
                            MPO itemPO = POs.FirstOrDefault(x => x.partNo.Trim() == Part && x.date == dtLoop.Date.ToString(YMDFormat) && x.vdCode == vender)!;
                            if (itemPO != null)
                            {
                                itemDO.PO = itemPO.qty;
                            }
                            //================ SET PO ==================//
                            //if (dtLoop.Date == dtNow.Date)
                            //{
                                nPoFiFo = nPoFiFo - itemDO.Do;
                                itemDO.POFIFO = nPoFiFo;
                            //}
                            DOITEM.Add(itemDO);
                        }
                        else
                        {
                            itemDO.Plan = 0;
                 
                            itemDO.PlanPrev = 0;
                            itemDO.PickList = 0;
                            ViDoPlan itemPlan = Plans.FirstOrDefault(x => x.Partno != null && x.Partno.Trim() == Part.Trim() && x.Prdymd == dtLoop.ToString(YMDFormat))!;

                            if (itemPlan != null)
                            {
                                itemDO.Plan = (double)itemPlan.Qty!;
                                itemDO.PlanPrev = itemDO.Plan;
                            }
                            ViDoPlan chkSV = PlansSV.FirstOrDefault(x => x.Partno.Trim() == Part.Trim());
                            if (chkSV != null)
                            {
                                double StockCurrent = Stock;
                                Stock -= itemDO.Plan;
                                itemDO.Stock = Stock;
                                double RequestStock = Math.Abs(Stock) < safetyStock ? (safetyStock - Math.Abs(Stock)) : Math.Abs(Stock);
                                double DO = Convert.ToDouble(chkSV.Qty);
                                DOITEM = ADJ_DO(DOITEM, vender, Part, dtLoop, DO, VenderDelivery, oVdStd, nPoFiFo); // นำตัวเลข DO ไปลงวันที่ตาม PD Leadtime (D-2)
                                ModelRefreshStock rData = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha);
                                DOITEM = rData.data;
                                Stock = rData.Stock;
                            }
                            else
                            {
                                double StockCurrent = Stock;
                                Stock -= itemDO.Plan;
                                itemDO.Stock = Stock;
                                if (Stock <= 0 || Stock < safetyStock)
                                {
                                    double RequireStock = Math.Abs(Stock) < safetyStock ? (safetyStock - Math.Abs(Stock)) : Math.Abs(Stock);
                                    double DO = GetDoVal(RequireStock, itemPart, oVdStd); // คำนวน DO จาก mstr (Min, Max, Safety stock)
                                    if (nPoFiFo == 0 || nPoFiFo < DO)
                                    { 
                                        // DO <= REMAIN PO FIFO
                                        DO = nPoFiFo;
                                    }
                                    else
                                    {
                                        nPoFiFo -= DO;
                                    }
                                    DOITEM = ADJ_DO(DOITEM, vender, Part, dtLoop, DO, VenderDelivery, oVdStd, nPoFiFo); // นำตัวเลข DO ไปลงวันที่ตาม PD Leadtime (D-2)
                                }
                                ModelRefreshStock rData = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha);
                                DOITEM = rData.data;
                                Stock = rData.Stock;
                            }
                            DOITEM.Add(itemDO);
                        }
                        dtLoop = dtLoop.AddDays(1);
                    }
                    DOITEM = REFRESH_STOCK(DOITEM, vender, Part, Cm, StockAlpha).data;
                    DOITEM = RefreshPOFIFO(DOITEM, defPOFIFO, dtNow, Part);
                }
            }
            return new MODEL_GET_DO()
            {
                fixDateYMD = fixDateHiglight.Distinct().ToList(),
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
            //double POFIFO = dOITEM.Where(x => x.Date.Date == dtNow.Date && x.PartNo == part).Sum(x => x.POFIFO);
            double POFIFO = defPOFIFO;
            foreach (MRESULTDO item in dOITEM.Where(x => x.PartNo == part).ToList())
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


        public List<CalendarAlpha> GetHolidayAlpha()
        {
            List<CalendarAlpha> calendarAlphas = new List<CalendarAlpha>();

            OracleCommand strGetPo = new OracleCommand();
            strGetPo.CommandText = @"select * FROM ND_CAL_TBL_V1
                                     where NENDO = :YEAR and FUKAKU = 0
                                     order by TUKI,NDAY";
            strGetPo.Parameters.Add(new OracleParameter(":YEAR", DateTime.Now.ToString("yyyy")));
            DataTable dt = dbAlpha.Query(strGetPo);
            string[] isHoliday = new string[dt.Rows.Count];
            foreach (DataRow dr in dt.Rows)
            {

                CalendarAlpha calendarAlpha = new CalendarAlpha();
                calendarAlpha.ymd = dr["NENDO"].ToString() + dr["TUKI"].ToString() + dr["NDAY"].ToString();

                calendarAlphas.Add(calendarAlpha);


               
            }

            return calendarAlphas;
        }
       
    }
}
