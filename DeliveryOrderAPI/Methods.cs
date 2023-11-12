using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DeliveryOrderAPI
{
    public class Methods
    {
        private SqlConnectDB dbSCM = new("dbSCM");
        private OraConnectDB dbAlpha = new("ALPHA01");
        private OraConnectDB dbAlpha2 = new("ALPHA02");

        public DataTable GetPlan(M_PO_DATA param)
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT A.PARTNO,A.CM,A.PARTNAME,B.PART_QTY_BOX AS BOX_QTY,
		    COALESCE((SELECT SUM(CONSUMPTION) AS PLAN_QTY 
		    FROM [dbSCM].[dbo].[vi_DO_Plan] 
		    WHERE PARTNO = A.PARTNO AND (PRDYMD >= @SDATE AND PRDYMD <= @EDATE)),0) AS PLAN_QTY ,
            A.WHUNIT AS UNIT,('" + param.startDate + "') AS PERIOD_START,('" + param.endDate + "') AS PERIOD_END";
            sql.CommandText += @" FROM [dbSCM].[dbo].[vi_DO_Plan] A 
	        LEFT JOIN [dbSCM].[dbo].[DO_MASTER] B
	        ON A.PARTNO = B.PART_NO
	        WHERE VENDER = @VENDER
	        GROUP BY A.PARTNO,A.CM,A.PARTNAME,A.WHUNIT,B.PART_QTY_BOX";
            sql.Parameters.Add(new SqlParameter("@VENDER", param.vender));
            sql.Parameters.Add(new SqlParameter("@SDATE", param.startDate));
            sql.Parameters.Add(new SqlParameter("@EDATE", param.endDate));
            DataTable dt = dbSCM.Query(sql);
            return dt;
        }
        public List<string> GetPartOfVender(string vender)
        {
            List<string> res = new List<string>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = "SELECT DISTINCT PARTNO FROM [dbSCM].[dbo].[vi_DO_Plan] WHERE VENDER = '" + vender + "'";
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                res.Add(dr["PARTNO"].ToString());
            }
            return res;
        }
        public DataTable GetPo(string part, string startDate = "00000000", string endDate = "00000000", string sort = "asc")
        {
            OracleCommand cmd = new();
            // DELYMD
            string _WhereDELYMD = (startDate != "00000000" && endDate != "00000000") ? (" AND DELYMD >= '" + startDate + "' AND DELYMD <= '" + endDate + "'") : "";
            cmd.CommandText = @"SELECT PARTNO,SUM(WHBLBQTY) AS WHBLBQTY FROM GST_DATOSD WHERE  apbit in ('U','P') AND TRIM(PARTNO) LIKE '" + part + "' " + _WhereDELYMD + "  GROUP BY PARTNO ORDER BY WHBLBQTY " + sort;
            DataTable dt = dbAlpha.Query(cmd);
            return dt;
        }

        public DataTable GetListPartBySupplier(string? _VdCode,string? _DateNow)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = @"SELECT DISTINCT PT.PARTNO FROM ND_ZUB_TBL PT LEFT JOIN ND_EPN_TBL_V1 PN ON PT.PARTNO = PN.PARTNO LEFT JOIN ND_HTC_TBL PV ON PT.PARTNO = PV.PARTNO WHERE PV.HTCODE = '" + _VdCode + "' AND(PT.STRYMN <= '" + _DateNow + "' AND PT.ENDYMN >= '" + _DateNow + "') AND(PV.STRYMN <= '" + _DateNow + "' AND PV.ENDYMN >= '" + _DateNow + "') AND PT.HATKU = '1' ORDER BY PT.PARTNO ASC";
            DataTable dt = dbAlpha.Query(cmd);
            dt.Columns.Add("PLAN_QTY", typeof(double));
            dt.Columns.Add("PARTNAME");
            dt.Columns.Add("CM");
            dt.Columns.Add("BOX_QTY");
            dt.Columns.Add("UNIT");
            dt.Columns.Add("PERIOD_START");
            dt.Columns.Add("PERIOD_END");
            dt.Columns.Add("STOCK");
            dt.Columns.Add("STOCK_PERCENT", typeof(double));
            dt.Columns.Add("PO");
            dt.Columns.Add("PO_PERCENT", typeof(double));
            return dt;
        }
    }
}
