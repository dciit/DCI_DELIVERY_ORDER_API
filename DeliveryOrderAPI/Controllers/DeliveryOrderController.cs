
using Azure;
using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Emit;
using System.Security.Claims;

namespace DeliveryOrderAPI.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class DeliveryOrderController : Controller
    {
        Methods Methods = new Methods();
        DataTable dtResult = new DataTable();
        DataTable dtResponse = new DataTable();
        DataTable _PLAN = new DataTable(); // เก็บแผนการผลิตจาก vi_DO_Plan DBSCM
        DataTable _PARTS = new DataTable(); // เก็บ Part ที่ได้จาก Supplier
        DataTable _DO_MSTR = new DataTable();
        DataTable PASTMASTER = new DataTable();
        List<DoHistory> _HISTORY = new List<DoHistory>();
        List<DoDictMstr> _HOLIDAY = new List<DoDictMstr>();
        List<string> _DIFF_PART = new List<string>();
        DataTable _STOCKS = new DataTable();
        DataTable _PLAN_HISTORY = new DataTable();
        List<MPickList> _PICKLIST = new List<MPickList>();
        DataRow[] _PartMstr = new DataRow[] { };
        List<MDOAct> _DO_ACT = new List<MDOAct>();
        DataTable _PO = new DataTable();
        //string _PART_JOIN_STRING = "";
        //double _StockBalance = 0;
        //double _StockSimBalance = 0;
        int _Box = 0;
        int _BoxCap = 0;
        DateTime _RunProcess = DateTime.Now;
        DateTime _FixedProcess = DateTime.Now;
        DateTime _StartProcess = DateTime.Now;
        DateTime _EndProcess = DateTime.Now;
        private SqlConnectDB dbSCM = new("dbSCM");
        private SqlConnectDB dbDCI = new("dbDCI");
        private OraConnectDB dbAlpha = new("ALPHA01");
        private OraConnectDB dbAlpha2 = new("ALPHA02");
        private readonly DBSCM _contextDBSCM;
        private readonly DBHRM _contextDBHRM;
        Services serv = new Services(new DBSCM());
        DataTable dtPlanToday = new DataTable();
        DataTable PartMstr = new DataTable();
        //***********************************************************************//
        //                          NEW VERSION USE MODEL ONLY                   //
        //***********************************************************************//
        List<MPO> PO_OF_VENDER = new List<MPO>();
        List<MPO> POS = new List<MPO>();
        //***********************************************************************//
        //                          EN -- NEW VERSION USE MODEL ONLY                   //
        //***********************************************************************//
        Dictionary<string, DoVenderMaster> DoVenderMasters = new Dictionary<string, DoVenderMaster>();
        public DeliveryOrderController(DBSCM contextDBSCM, DBHRM contextDBHRM)
        {
            _contextDBSCM = contextDBSCM;
            _contextDBHRM = contextDBHRM;
        }

        [HttpPost]
        //[Authorize]
        [Route("/getPlans")]
        public async Task<IActionResult> getPlans([FromBody] MGetPlan param)
        {
            MODEL_GET_DO response = serv.CalDO(false,param.vdCode!);
            return Ok(response);
        }

        [HttpPost]
        [Route("/insertDO")]
        public async Task<IActionResult> InsertDO([FromBody] MRUNDO param)
        {
            string YMDFormat = "yyyyMMdd";
            DateTime dtNow = DateTime.Now;
            string nbr = dtNow.ToString("yyyyMMdd");
            int rev = 0;
            MODEL_GET_DO GroupDO = serv.CalDO(true);
            DoHistoryDev prev = _contextDBSCM.DoHistoryDevs.FirstOrDefault(x => x.RunningCode == nbr && x.Revision == 999)!;
            if (prev != null)
            {
                rev = (int)prev.Rev!;
            }
            rev++;
            foreach (MRESULTDO itemDO in GroupDO.data)
            {
                DoHistoryDev item = new DoHistoryDev()
                {
                    RunningCode = nbr,
                    Rev = rev,
                    Model = "",
                    Partno = itemDO.PartNo,
                    DateVal = itemDO.Date.ToString(YMDFormat),
                    PlanVal = itemDO.Plan,
                    DoVal = itemDO.Do,
                    StockVal = itemDO.Stock,
                    Stock = 0,
                    VdCode = itemDO.Vender,
                    InsertDt = dtNow,
                    InsertBy = param.empcode,
                    Revision = 999
                };
                _contextDBSCM.DoHistoryDevs.Add(item);
            }
            int insert = _contextDBSCM.SaveChanges();
            if (insert > 0 && prev != null)
            {
                List<DoHistoryDev> listPrev = _contextDBSCM.DoHistoryDevs.Where(x => x.RunningCode == prev.RunningCode && x.Rev == prev.Rev).ToList();
                listPrev.ForEach(a => a.Revision = prev.Rev);
                _contextDBSCM.SaveChanges();
            }
            return Ok(new
            {
                status = insert,
                nbr = $"{nbr}{rev.ToString("D3")}"
            });
        }
        //private MGetPlanDataSet GetPlanDataSet(string venderCode = "", string? buyer = "", bool CalDoWhenStockMinus = false)
        //{
        //    List<MStockAlpha> StockAlpha = new List<MStockAlpha>();
        //    string _RunningCode = "";
        //    var QueryRunningCodeCurrent = _contextDBSCM.DoHistories.Where(x => x.Revision == 999).OrderByDescending(x => x.DateVal).ToList();
        //    string RUN_CODE = "";
        //    int? RUN_REV = 1;
        //    if (QueryRunningCodeCurrent != null && QueryRunningCodeCurrent.Count > 0)
        //    {
        //        RUN_CODE = QueryRunningCodeCurrent.Take(1).FirstOrDefault().RunningCode;
        //        RUN_REV = QueryRunningCodeCurrent.Take(1).FirstOrDefault().Rev;
        //        _RunningCode = RUN_CODE + "" + RUN_REV?.ToString("D3");
        //    }
        //    Dictionary<string, DoMaster> masters = new();
        //    DateTime _Today = DateTime.Now;
        //    DateTime _StartDate = _Today.AddDays(-7);
        //    DateTime _EndDate = _Today.AddMonths(1);
        //    _StartDate = new DateTime(_StartDate.Year, _StartDate.Month, 1).Date;
        //    _EndDate = new DateTime(_EndDate.Year, _EndDate.Month, DateTime.DaysInMonth(_EndDate.Year, _EndDate.Month)).Date;
        //    string sDate = _StartDate.ToString("yyyy-MM-dd 00:00:00");
        //    string fDate = _EndDate.ToString("yyyy-MM-dd 00:00:00");
        //    int _mstrFixed = 7;
        //    int _mstrLast = 7;
        //    int _mstrForecast = 13;
        //    string _PartNo = "";
        //    string _Cm = "";
        //    List<string> rVd = new List<string>();
        //    string DateLoop = "";
        //    int _INDEX = 0;

        //    //List<MPartDetail> rPartDetail = new List<MPartDetail>();
        //    Dictionary<string, double> rStockOfPart = new Dictionary<string, double>();
        //    _RunProcess = DateTime.Now;
        //    _FixedProcess = _RunProcess.AddDays(_mstrFixed);
        //    _StartProcess = _RunProcess.AddDays(_mstrLast * -1);
        //    _EndProcess = _RunProcess.AddDays(_mstrForecast);
        //    dtResponse.Columns.Add("INDEX");
        //    dtResponse.Columns.Add("ID");
        //    dtResponse.Columns.Add("PART");
        //    dtResponse.Columns.Add("DATE");
        //    dtResponse.Columns.Add("PLAN");
        //    dtResponse.Columns.Add("PICKLIST");
        //    dtResponse.Columns.Add("DO");
        //    dtResponse.Columns.Add("DOACT");
        //    dtResponse.Columns.Add("STOCK");
        //    dtResponse.Columns.Add("STOCKSIM");
        //    dtResponse.Columns.Add("DOBALANCE");
        //    dtResponse.Columns.Add("PO");
        //    dtResponse.Columns.Add("DO_ADD_FIXED");
        //    dtResponse.Columns.Add("VD_CODE");
        //    dtResponse.Columns.Add("BOX");
        //    dtResponse.Columns.Add("BOX_CAP");
        //    dtResponse.Columns.Add("PLAN_NOW");
        //    dtResponse.Columns.Add("TIME");
        //    dtResponse.Columns.Add("MODEL");
        //    dtResponse.Columns.Add("POSHORT");
        //    venderCode = venderCode == "-" ? "" : venderCode;
        //    var listVender = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && (buyer != "" ? x.Code == buyer : x.RefCode != "")).ToList();

        //    // ---------------------------------------------------------------------------------------//
        //    // ------------------------------------ FOR DEBUG -----------------------------------------//
        //    // ---------------------------------------------------------------------------------------//
        //    //venderCode = "021022";
        //    // ---------------------------------------------------------------------------------------//
        //    // ------------------------------------ FOR DEBUG -----------------------------------------//
        //    // ---------------------------------------------------------------------------------------//

        //    if (venderCode != "" && venderCode != "-")
        //    {
        //        listVender = listVender.Where(x => x.RefCode == venderCode).ToList();
        //    }
        //    var rVender = listVender.GroupBy(x => x.RefCode).ToList();
        //    foreach (var itemVender in rVender)
        //    {
        //        // --- RESET ENV. --- //
        //        dtResponse.Rows.Clear();
        //        _PLAN.Rows.Clear();
        //        _StockBalance = 0;
        //        _INDEX = 0;
        //        _HISTORY.Clear();
        //        //_HISTORY_OF_PART.Clear();
        //        //StockHistory = 0;
        //        dtPlanToday.Clear();
        //        _DO_ACT.Clear();
        //        rStockOfPart.Clear();
        //        _PART_JOIN_STRING = "";
        //        // --- END --- //

        //        // --- SET VARIABLE. --- //
        //        //bool SetFirstStock = true;
        //        bool NoHistory = false;
        //        bool ClickRunDo = false;
        //        int IndexAfterFixed = 0;
        //        DateTime LastDateHaveHistory = _RunProcess;
        //        Dictionary<string, string> IndexOfHistory = new Dictionary<string, string>();
        //        Dictionary<string, int> IndexAfterFixedDate = new Dictionary<string, int>();
        //        string vdCode = itemVender.FirstOrDefault().RefCode;
        //        DoVenderMaster VenderMaster = _contextDBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == vdCode);
        //        Dictionary<string, bool> DaysDelivery = serv.getDaysDelivery(VenderMaster);
        //        if (!DoVenderMasters.ContainsKey(vdCode))
        //        {
        //            DoVenderMasters.Add(vdCode, _contextDBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == vdCode));
        //        }
        //        DataTable vdCalendar = GetVenderMaster(vdCode); // ข้อมูลวันส่งของร้านค้า
        //        DoDictMstr VdDict = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "VENDER_BOX_CAPACITY" && x.Code == vdCode).FirstOrDefault(); // ข้อมูลจำนวนกล่องในการส่งของ
        //        var venderDetail = _contextDBSCM.DoVenderMasters.Where(x => x.VdCode == vdCode).FirstOrDefault();
        //        int min = 0; // จำนวนกล่องที่ร้านค้าส่งขั้นต่ำต่อวัน
        //        int max = 0; // จำนวนกล่องที่ร้านค้าส่งสูงสุดต่อวัน
        //        if (VdDict != null)
        //        {
        //            min = int.Parse(VdDict.RefCode); // จำนวนกล่องที่ร้านค้าส่งขั้นต่ำต่อวัน
        //            max = int.Parse(VdDict.Note); // จำนวนกล่องที่ร้านค้าส่งสูงสุดต่อวัน
        //        }
        //        bool CheckMinMaxPerShip = (bool)(venderDetail.VdLimitBox != null ? venderDetail.VdLimitBox : false); // ร้านค้ากำหนดจำนวน BOX ต่อวันหรือไม่ ?
        //        // --- END --- //

        //        // --- GET PLAN FROM VIEW : vi_DO_Plan (DB:DBSCM) (SERVER:226.86)
        //        _PLAN = dbSCM.Query(SqlViewGetPlan(vdCode, _StartProcess, _EndProcess));
        //        // --- END --- //


        //        // --- GROUP BY PART IN '_PLAN' --- //
        //        DataView view = new DataView(_PLAN);
        //        _PARTS = view.ToTable(true, "PARTNO", "CM", "VENDER");
        //        _PART_JOIN_STRING = string.Join("','", _PARTS.Rows.OfType<DataRow>().Select(r => r[0].ToString()));

        //        //_PICKLIST = GetPickListBySupplier(_PART_JOIN_STRING);
        //        // --- END --- //

        //        // --- SET OTHER --- //
        //        //_DO_MSTR = _GET_MSTR(_PART_JOIN_STRING);
        //        _DO_MSTR = GetPartOfVender(vdCode);
        //        _STOCKS = _GET_STOCK(_RunProcess, _PART_JOIN_STRING);
        //        //StockAlpha = serv.GetStockAlpha();
        //        _HOLIDAY = _GET_HOLIDAY();
        //        _DIFF_PART = _GET_DIFF_PART();

        //        //-------------------------------------------------------------//
        //        //------------------------ PO OF VENDER -----------------------//
        //        //-------------------------------------------------------------//
        //        PO_OF_VENDER = serv.GetPoDIT(vdCode, _StartDate, _EndDate, _PART_JOIN_STRING);

        //        List<MPO> POsum = PO_OF_VENDER.GroupBy(x => new { x.vdCode, x.partNo }).Select(g => new MPO()
        //        {
        //            vdCode = g.Key.vdCode,
        //            partNo = g.Key.partNo,
        //            qty = g.Sum(s => s.qty)
        //        }).ToList();
        //        POS.AddRange(POsum);
        //        //-------------------------------------------------------------//
        //        //------------------------ PO OF VENDER -----------------------//
        //        //-------------------------------------------------------------//

        //        // ยอดรวม PO ต่อ PART,VD

        //        _DO_ACT = _GET_DO_ACT(_StartProcess, _EndProcess);
        //        // --- END --- //
        //        _HISTORY = _GET_HISTORY(vdCode, _StartProcess, _EndProcess);
        //        _PICKLIST = GetPickListBySupplier(_PART_JOIN_STRING);

        //        if (_HISTORY.Count > 0)
        //        {
        //            DateTime LastDateFixed = _FixedProcess.AddDays(6);
        //            string PartOfHistory = "";
        //            double valSumPoOfPart = 0;
        //            bool POSHORT = false;
        //            foreach (DoHistory itemHistory in _HISTORY)
        //            {

        //                if (PartOfHistory == "" || PartOfHistory != itemHistory.Partno)
        //                {
        //                    PartOfHistory = itemHistory.Partno;
        //                    POSHORT = false;
        //                    var contentSumPoOfPart = POsum.FirstOrDefault(x => x.vdCode == vdCode && x.partNo == PartOfHistory);
        //                    valSumPoOfPart = contentSumPoOfPart != null ? contentSumPoOfPart.qty : 0;
        //                }
        //                //----- START PO (MODEL) -----//

        //                //-----  END  PO (MODEL) -----//

        //                //----- START PICKLIST (MODEL) -----//
        //                var contentPL = _PICKLIST.Where(x => x.Partno == PartOfHistory && x.Idate == itemHistory.DateVal).FirstOrDefault();
        //                double PickList = contentPL != null ? double.Parse(contentPL.Fqty) : 0;
        //                //----- END PICKLIST (MODEL) -----//




        //                //_PartMstr = _DO_MSTR.Select(" PARTNO = '" + Part + "' ");
        //                //int Index = _HISTORY.IndexOf(itemHistory);

        //                double DOAct = GetDoAct(PartOfHistory, itemHistory.DateVal, _DO_ACT);
        //                double DO = (double)itemHistory.DoVal;
        //                double Plan = (double)itemHistory.PlanVal;
        //                double Stock = (double)itemHistory.StockVal;
        //                DateTime Date = DateTime.ParseExact(itemHistory.DateVal, "yyyyMMdd", null);
        //                if (PartOfHistory == "3PD03145-1")
        //                {
        //                    Console.WriteLine(PartOfHistory);
        //                }
        //                MPlan PlanCurrent = GetPlanOfView(vdCode, PartOfHistory, Date);
        //                if (Plan != PlanCurrent.Plan && Date > DateTime.Now.AddDays(_mstrFixed - 1))
        //                {
        //                    itemHistory.PlanVal = PlanCurrent.Plan;
        //                    _HISTORY = serv.refreshStock(_HISTORY, vdCode, Date, PartOfHistory, itemHistory.PlanVal);
        //                }
        //                Date = new DateTime(Date.Year, Date.Month, Date.Day, _RunProcess.Hour, _RunProcess.Minute, _RunProcess.Second);
        //                //double PO = _FindPoVal(Part, Date);

        //                // NEW PO_VAL BY MODEL //
        //                var contentPO = PO_OF_VENDER.FirstOrDefault(x => x.partNo == PartOfHistory && x.date == itemHistory.DateVal);
        //                double PO = contentPO != null ? contentPO.qty : 0;
        //                // END NEW PO_VAL BY MODEL //

        //                if (CalDoWhenStockMinus) // มีเงื่อนไขระบุว่า Stock ห้ามติดลบ ระหว่าง Fixed - (D/O+PDLT)
        //                {
        //                    if (Date < _FixedProcess.AddDays(6)) // จะคำนวนเฉพาะ Fixed + 7 day
        //                    {
        //                        if (false && LastDateFixed.ToString("yyyyMMdd") == Date.ToString("yyyyMMdd") && Stock < 0)
        //                        {
        //                            if (CheckMinMaxPerShip) // ร้านค้ามีการระบุว่า จัดส่งขั้นต่ำ+มากสุด หรือไม่ ? (EXP - MIN : 15, MAX = 31)
        //                            {
        //                                var PartOfFixed = _HISTORY.Where(x => x.DateVal == _FixedProcess.ToString("yyyyMMdd") && x.Partno == PartOfHistory).FirstOrDefault();
        //                                int IndexStartFixed = _HISTORY.IndexOf(PartOfFixed);
        //                                DO = _FIND_DO_VAL(PartOfHistory, Math.Abs(Stock));
        //                                DateTime FixedDate = _FixedProcess; // ลูปคำนวน Stock Sim ตั้งแต่วันที่ Fixed - End Fixed
        //                                Stock = (double)_HISTORY[IndexStartFixed].StockVal;
        //                                double PlanFixed = 0;
        //                                while (FixedDate < Date)
        //                                {
        //                                    if (FixedDate.ToString("yyyyMMdd") != _FixedProcess.ToString("yyyyMMdd"))
        //                                    {
        //                                        DO = (double)_HISTORY[IndexStartFixed].DoVal;
        //                                        PlanFixed = (double)_HISTORY[IndexStartFixed].PlanVal;
        //                                    }
        //                                    Stock = (Stock + DO) - PlanFixed;
        //                                    dtResponse.Rows[IndexStartFixed]["DO"] = double.Parse(dtResponse.Rows[IndexStartFixed]["DO"].ToString()) + DO;
        //                                    dtResponse.Rows[IndexStartFixed]["STOCKSIM"] = Stock;
        //                                    FixedDate = FixedDate.AddDays(1);
        //                                    IndexStartFixed++;
        //                                }
        //                                Stock -= Plan;
        //                                DO = 0;
        //                            }
        //                            else // ร้านค้า ไม่ ระบุว่าต้องส่งขั้นต่ำ - สูง เท่าไหร่ ?
        //                            {
        //                                var PartOfFixed = _HISTORY.Where(x => x.DateVal == _FixedProcess.ToString("yyyyMMdd") && x.Partno == PartOfHistory).FirstOrDefault();
        //                                int IndexStartFixed = _HISTORY.IndexOf(PartOfFixed);
        //                                DO = _FIND_DO_VAL(PartOfHistory, Math.Abs(Stock));
        //                                _HISTORY[IndexStartFixed].DoVal += DO;
        //                                dtResponse.Rows[IndexStartFixed]["DO"] = double.Parse(dtResponse.Rows[IndexStartFixed]["DO"].ToString()) + DO; // แก้ไขจำนวน D/O ในวันที่ Fixed date
        //                                DateTime FixedDate = _FixedProcess; // ลูปคำนวน Stock Sim ตั้งแต่วันที่ Fixed - End Fixed
        //                                Stock = (double)_HISTORY[IndexStartFixed - 1].StockVal;
        //                                double PlanFixed = 0;
        //                                while (FixedDate < Date)
        //                                {
        //                                    PlanFixed = (double)_HISTORY[IndexStartFixed].PlanVal;
        //                                    DO = (double)_HISTORY[IndexStartFixed].DoVal;
        //                                    Stock = (Stock + DO) - PlanFixed;
        //                                    dtResponse.Rows[IndexStartFixed]["STOCKSIM"] = Stock;
        //                                    FixedDate = FixedDate.AddDays(1);
        //                                    IndexStartFixed++;
        //                                }
        //                                Stock -= Plan;
        //                                DO = 0;
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine("1231");
        //                    }
        //                }
        //                string model = itemHistory.Model == null ? PlanCurrent.Model : itemHistory.Model;

        //                //----- (START) IF FIRST DAY OF PART ADJUST (PO SUM) -----//
        //                //if (itemHistory.DateVal == dt)
        //                //{

        //                //}
        //                //----- (END) IF FIRST DAY OF PART ADJUST (PO SUM) -----//

        //                //------------------ ADD PO SUM FROM FIRST INDEX OF PART -----------------//
        //                if (Date.ToString("yyyyMMdd") == DateTime.Now.AddDays(-1).ToString("yyyyMMdd"))
        //                {
        //                    PO = valSumPoOfPart;
        //                }
        //                else if (Date >= _Today.AddDays(-1))
        //                {
        //                    valSumPoOfPart -= (double)itemHistory.PlanVal;
        //                    if (valSumPoOfPart <= 0 && DO > 0)
        //                    {
        //                        POSHORT = true;
        //                    }
        //                }
        //                //------------------------------- END ------------------------------------//
        //                dtResponse.Rows.Add(_INDEX, itemHistory.Id, itemHistory.Partno, itemHistory.DateVal, itemHistory.PlanVal, PickList, DO, DOAct, itemHistory.Stock, itemHistory.StockVal, 0, PO, 0, itemHistory.VdCode, _Box, _BoxCap, 0, itemHistory.TimeScheduleDelivery, PlanCurrent.Model, POSHORT);
        //                _INDEX++;
        //            }

        //            var Parts = _HISTORY.GroupBy(x => x.Partno);

        //            if (CalDoWhenStockMinus) // คำนวนยอด D/O ในช่วง RUN D/O
        //            {

        //                DateTime dtRun = DateTime.Now.AddDays(_mstrFixed - 1); // หา Stock ก่อนวัน RUN D/O จำนวน 1 วัน
        //                foreach (var ItemPart in Parts)
        //                {

        //                    string Part = ItemPart.Key;
        //                    //if (Part == "3PD04918-1")
        //                    //{
        //                    //    Console.WriteLine("asdasd");
        //                    //}
        //                    //if (Part == "3PD03145-1")
        //                    //{
        //                    //    Console.WriteLine("123123");
        //                    //}
        //                    var contentStart = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == dtRun.ToString("yyyyMMdd") && x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == Part).FirstOrDefault(); // data วันแรกของช่วง RUN D/O
        //                    var contentLast = dtResponse.AsEnumerable().LastOrDefault(x => x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == Part); // data วันแรกของช่วง RUN D/O
        //                    int indexStart = dtResponse.Rows.IndexOf(contentStart);
        //                    int indexEnd = dtResponse.Rows.IndexOf(contentLast);
        //                    double stockLoop = double.Parse(contentStart.Field<string>("STOCKSIM"));
        //                    string dtStart = contentStart.Field<string>("DATE");
        //                    string dtEnd = contentLast.Field<string>("DATE");
        //                    DateTime dtLoop = DateTime.ParseExact(dtStart, "yyyyMMdd", null);
        //                    dtLoop = dtLoop.AddDays(1);
        //                    indexStart++;
        //                    DateTime dtLast = DateTime.ParseExact(dtEnd, "yyyyMMdd", null);
        //                    DateTime dEnd = new DateTime(_EndProcess.Year, _EndProcess.Month, _EndProcess.Day, 0, 0, 0);
        //                    if (dtLast < dEnd)
        //                    {
        //                        while (dtLast < dEnd)
        //                        {
        //                            dtLast = dtLast.AddDays(1);
        //                            double doActLoop = 0;
        //                            double doLoop = 0;
        //                            MPlan PlanCurrent = GetPlanOfView(vdCode, Part, dtLast);
        //                            dtResponse.Rows.Add(0, 0, Part, dtLast.ToString("yyyyMMdd"), PlanCurrent.Plan, 0, doLoop, doActLoop, stockLoop, stockLoop, 0, 0, 0, vdCode, _Box, _BoxCap, 0, "", PlanCurrent.Model);
        //                        }
        //                    }

        //                    dtResponse = serv.sortDt(dtResponse);


        //                    DoPartMaster PartMaster = _contextDBSCM.DoPartMasters.FirstOrDefault(x => x.Partno == Part && x.VdCode == vdCode);

        //                    while (dtLoop <= dtLast)
        //                    {
        //                        double planLoop = double.Parse(dtResponse.Rows[indexStart].Field<string>("PLAN"));
        //                        double doLoop = double.Parse(dtResponse.Rows[indexStart].Field<string>("DO"));
        //                        stockLoop = (stockLoop - planLoop) + doLoop;
        //                        if (stockLoop < 0)
        //                        {
        //                            doLoop = serv.GetDoVal(Math.Abs(stockLoop), PartMaster);
        //                            MRefreshStockDT res = serv.RefreshStockDt(dtResponse, vdCode, _FixedProcess, dtLoop, Part, doLoop, PartMaster, DaysDelivery);

        //                            stockLoop = double.Parse(res.dt.AsEnumerable().Where(x => x.Field<string>("VD_CODE") == vdCode && x.Field<string>("PART") == Part && x.Field<string>("DATE") == dtLoop.ToString("yyyyMMdd")).FirstOrDefault().Field<string>("STOCKSIM"));
        //                            dtResponse = res.dt;
        //                        }
        //                        dtResponse.Rows[indexStart]["STOCKSIM"] = stockLoop;
        //                        dtLoop = dtLoop.AddDays(1);
        //                        indexStart++;
        //                    }

        //                }
        //            }
        //        }
        //        else
        //        {
        //            _PO = _GET_PO(_StartDate, _EndDate, vdCode);
        //            var contextHistory = _contextDBSCM.DoHistories.Where(x => x.Revision == 999 && x.VdCode == vdCode).ToList();
        //            foreach (DataRow dr in _PARTS.Rows)
        //            {
        //                _PartMstr = new DataRow[] { };
        //                _StockBalance = 0;
        //                _PartNo = dr["PARTNO"].ToString();
        //                _Cm = dr["CM"].ToString();
        //                _PartMstr = _DO_MSTR.Select(" PARTNO = '" + _PartNo + "' ");
        //                DateTime LoopDate = _StartProcess;
        //                Dictionary<string, double> PlanByPartNo = _GET_PLAN_BY_PARTNO(vdCode, _PartNo, LoopDate);
        //                Dictionary<string, double> POByPartNo = _GET_PO_BY_PARTNO(_PartNo, LoopDate);
        //                while (LoopDate <= _EndProcess)
        //                {
        //                    DateLoop = LoopDate.ToString("yyyyMMdd");
        //                    int _Id = 0;
        //                    double PickList = 0;
        //                    var contextPicklist = _PICKLIST.Where(x => x.Partno == _PartNo && x.Idate == DateLoop).FirstOrDefault();
        //                    if (contextPicklist != null)
        //                    {
        //                        PickList = double.Parse(contextPicklist.Fqty);
        //                    }
        //                    double _DoVal = 0;
        //                    double _DoActVal = GetDoAct(_PartNo, DateLoop, _DO_ACT);
        //                    double _StockSim = 0;
        //                    double _PoVal = POByPartNo.ContainsKey(DateLoop) ? POByPartNo[DateLoop] : 0;
        //                    double _DoBal = 0;
        //                    double _PlanLoop = 0;
        //                    double NewPlan = 0;
        //                    _PlanLoop = PlanByPartNo.ContainsKey(DateLoop) ? PlanByPartNo[DateLoop] : 0;
        //                    if (LoopDate < _RunProcess)
        //                    {
        //                        var historyInDate = contextHistory.Where(x => x.DateVal == DateLoop && x.Partno == _PartNo).FirstOrDefault();
        //                        if (historyInDate != null)
        //                        {
        //                            _DoVal = (double)historyInDate.DoVal;
        //                        }
        //                        _StockSimBalance = 0;
        //                        _StockSim = 0;
        //                    }
        //                    if (LoopDate == _RunProcess)
        //                    {
        //                        DataRow[] _FindStock = _STOCKS.Select(" PARTNO = '" + _PartNo + "' AND CM = '" + _Cm + "' ");
        //                        if (_FindStock.Count() > 0)
        //                        {
        //                            // ต้องใช้ WBAL เท่านั้น
        //                            _StockSimBalance = _FindStock[0]["WBAL"].ToString() != "" ? Convert.ToDouble(_FindStock[0]["WBAL"].ToString()) : 0;
        //                            _StockBalance = _StockSimBalance;
        //                            if (!rStockOfPart.ContainsKey(_PartNo))
        //                            {
        //                                rStockOfPart.Add(_PartNo, _StockBalance);
        //                            }
        //                        }
        //                    }
        //                    if (LoopDate >= _StartProcess && LoopDate <= _RunProcess) // START DATE => FIXED DATE
        //                    {
        //                        _DoVal = 0;
        //                        _BoxCap = int.Parse(_PartMstr[0]["BOX_QTY"].ToString());
        //                    }
        //                    if ((LoopDate >= _RunProcess && LoopDate < _FixedProcess) || LoopDate >= _FixedProcess && LoopDate <= _EndProcess)
        //                    {
        //                        _DoVal = _CAL_DO(_PartNo, LoopDate, _PlanLoop, LastDateHaveHistory, CalDoWhenStockMinus);
        //                    }
        //                    _StockSim = _StockSimBalance;
        //                    dtResponse.Rows.Add(_INDEX, _Id, _PartNo, LoopDate.ToString("yyyyMMdd"), _PlanLoop, PickList, _DoVal, _DoActVal, _StockBalance, _StockSim, _DoBal, _PoVal, 0, dr["VENDER"].ToString(), _Box, _BoxCap, NewPlan);
        //                    if (LoopDate == _FixedProcess)
        //                    {
        //                        IndexAfterFixed = _INDEX;
        //                    }
        //                    _INDEX++;
        //                    LoopDate = LoopDate.AddDays(1);
        //                }
        //            }
        //            DateTime isDate = _EndProcess;
        //            Dictionary<string, int> CountInPart = new Dictionary<string, int>();
        //            Dictionary<string, bool> DayIsMax = new Dictionary<string, bool>();
        //            Dictionary<string, int> IndexOfPart = new Dictionary<string, int>();
        //            DateTime DateHistory = _RunProcess;
        //            if (IndexOfHistory.Count > 0)
        //            {
        //                DateHistory = DateTime.Parse(DateTime.ParseExact(IndexOfHistory.FirstOrDefault().Value, "yyyyMMdd", null).ToString("yyyy/MM/dd") + " " + DateTime.Now.ToString("HH:mm:ss"));
        //                DateTime date = Convert.ToDateTime(DateTime.ParseExact(IndexOfHistory.FirstOrDefault().Value, "yyyyMMdd", null));
        //                DateHistory = new DateTime(date.Year, date.Month, date.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        //            }

        //            if (false && CheckMinMaxPerShip == false) // ร้านค้านี้ ได้ระบุไว้ว่า ไม่จำกัดการกล่องในการส่งแต่ละวัน ให้ปัดค่าไปโดยอ้างอิงจาก box qty ของ part
        //            {
        //                dtResponse = LEFT_DO_VALUE(dtResponse, _PARTS, _DO_MSTR, DateHistory, CalDoWhenStockMinus);
        //            }
        //            if (false && CheckMinMaxPerShip == true)
        //            {
        //                Dictionary<string, bool> BoxMaxInDate = new Dictionary<string, bool>();
        //                Dictionary<string, double> BoxStock = new Dictionary<string, double>();
        //                DateTime EndDate = _EndProcess;
        //                while (EndDate >= _RunProcess)
        //                {
        //                    if (!BoxMaxInDate.ContainsKey(EndDate.ToString("yyyyMMdd")))
        //                    {
        //                        var PartInDate = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == EndDate.ToString("yyyyMMdd")).ToList();
        //                        double BoxInDate = PartInDate.Sum(x => double.Parse(x.Field<string>("BOX")));
        //                        var ItemPart = dtResponse.AsEnumerable().GroupBy(x => x.Field<string>("PART"));

        //                        if (BoxInDate > max)
        //                        {
        //                            foreach (var item in ItemPart) // ลูปเอา BOX ออก จนเหลือ max
        //                            {
        //                                if (BoxInDate == max)
        //                                {
        //                                    BoxMaxInDate.Add(EndDate.ToString("yyyyMMdd"), true);
        //                                    break;
        //                                }
        //                                else
        //                                {
        //                                    string Part = item.Key;
        //                                    var PartInPart = PartInDate.Where(x => x.Field<string>("PART") == Part).FirstOrDefault();
        //                                    int IndexPart = dtResponse.Rows.IndexOf(PartInPart);
        //                                    double BoxCap = double.Parse(PartInPart.Field<string>("BOX_CAP"));
        //                                    double BoxInPart = double.Parse(PartInPart.Field<string>("BOX"));
        //                                    double BoxCanRemove = BoxInPart;
        //                                    if ((BoxInDate - BoxInPart) < max)
        //                                    {
        //                                        BoxCanRemove = BoxInDate - max;
        //                                        //BoxCanRemove -= (BoxInPart - (BoxInDate - max));
        //                                    }

        //                                    dtResponse.Rows[IndexPart]["BOX"] = double.Parse(dtResponse.Rows[IndexPart]["BOX"].ToString()) - BoxCanRemove;
        //                                    dtResponse.Rows[IndexPart]["DO"] = double.Parse(dtResponse.Rows[IndexPart]["DO"].ToString()) - (BoxCanRemove * BoxCap);
        //                                    BoxInDate -= BoxCanRemove;
        //                                    if (!BoxStock.ContainsKey(Part)) { BoxStock.Add(Part, BoxCanRemove); } else { BoxStock[Part] += BoxCanRemove; }
        //                                }
        //                            }
        //                        }
        //                        else if (BoxInDate < min && BoxInDate > 0)
        //                        {
        //                            var dtGroupBox = dtResponse.AsEnumerable().GroupBy(x => x.Field<string>("DATE")).Select(g => new
        //                            {
        //                                date = g.Key,
        //                                box = g.Sum(g => double.Parse(g.Field<string>("BOX")))
        //                            });
        //                            var DateTargetAddBox = dtGroupBox.Where(x => x.box < min && dtParse(x.date) >= _RunProcess.AddDays(-1)).FirstOrDefault();
        //                            if (DateTargetAddBox != null)
        //                            {
        //                                int DiffBoxCanAdd = max - int.Parse(DateTargetAddBox.box.ToString());
        //                                var PartData = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == DateTargetAddBox.date).FirstOrDefault();
        //                                int IndexPart = dtResponse.Rows.IndexOf(PartData);
        //                                //foreach(var ItemPart )

        //                            }
        //                        }
        //                    }
        //                    EndDate = EndDate.AddDays(-1);
        //                }

        //                DateTime startDate = _RunProcess;
        //                var Parts = BoxStock.GroupBy(x => x.Key).ToList();
        //                while (startDate <= _EndProcess)
        //                {
        //                    if (!BoxMaxInDate.ContainsKey(startDate.ToString("yyyyMMdd")))
        //                    {
        //                        var DataInDate = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == startDate.ToString("yyyyMMdd")).ToList();
        //                        double BoxInDate = DataInDate.Sum(x => double.Parse(x.Field<string>("BOX")));
        //                        double BoxCanAdd = max - BoxInDate;
        //                        foreach (KeyValuePair<string, double> itemStock in BoxStock.Where(x => x.Value > 0.0).ToDictionary(x => x.Key, x => x.Value).OrderByDescending(x => x.Value))
        //                        {
        //                            if (BoxInDate >= max)
        //                            {
        //                                BoxMaxInDate.Add(startDate.ToString("yyyyMMdd"), true);
        //                                break;
        //                            }
        //                            string Part = itemStock.Key;
        //                            //if (Part == "3P623866-1/1")
        //                            //{
        //                            //    Console.WriteLine("123");
        //                            //}
        //                            var PartInDate = DataInDate.Where(x => x.Field<string>("PART") == Part).FirstOrDefault();
        //                            int IndexStock = dtResponse.Rows.IndexOf(PartInDate);
        //                            double BoxCap = double.Parse(PartInDate.Field<string>("BOX_CAP"));
        //                            double BoxInStock = itemStock.Value;
        //                            BoxInStock = BoxCanAdd > BoxInStock ? BoxInStock : (BoxInStock - (BoxInStock - BoxCanAdd));
        //                            dtResponse.Rows[IndexStock]["DO"] = double.Parse(dtResponse.Rows[IndexStock]["DO"].ToString()) + (BoxInStock * BoxCap);
        //                            dtResponse.Rows[IndexStock]["BOX"] = double.Parse(dtResponse.Rows[IndexStock]["BOX"].ToString()) + (BoxInStock);
        //                            BoxStock[Part] -= BoxInStock;
        //                            BoxInDate += BoxInStock;
        //                        }
        //                    }
        //                    startDate = startDate.AddDays(1);
        //                }
        //            }

        //            // --- END CHECK BOX PER SHIP --- //
        //            if (false && CheckMinMaxPerShip == true)
        //            {

        //                if (NoHistory || ClickRunDo)
        //                {
        //                    dtResponse = RefreshStockWithOutHistory(dtResponse);
        //                }
        //                else
        //                {
        //                    dtResponse = RefreshStock(dtResponse, LastDateHaveHistory);
        //                }
        //            }
        //        }
        //        dtResult.Merge(dtResponse);
        //        PASTMASTER.Merge(_DO_MSTR);
        //    }

        //    //DataTable Suppliers = dtResponse.AsEnumerable().GroupBy(x => x.Field<string>("VD_CODE")).Select(g => g.First()).CopyToDataTable();
        //    //var TimeSchedulePartSupply = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "TIME_SCHEDULE_PS");
        //    //List<MTimeSchedulePS> resTimeSchedule = new List<MTimeSchedulePS>();
        //    //foreach (var Time in TimeSchedulePartSupply)
        //    //{
        //    //    int Minuted = int.Parse(Time.RefCode);
        //    //    string TimeText = Convert.ToDateTime(Time.Code).ToString("hh:mm", CultureInfo.CurrentCulture) + " - " + Convert.ToDateTime(Time.Code).AddMinutes(Minuted).ToString("hh:mm", CultureInfo.CurrentCulture);
        //    //    int CapLoad = int.Parse(Time.Note);
        //    //    int CapCount = 0;
        //    //    if (CapCount < CapLoad)
        //    //    {
        //    //        var itemSupplier = Suppliers.Rows[0];
        //    //        var PartOfDate = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == _RunProcess.ToString("yyyyMMdd") && x.Field<string>("VD_CODE") == itemSupplier.Field<string>("VD_CODE") && double.Parse(x.Field<string>("DO")) > 0);
        //    //        foreach (var itemPart in PartOfDate)
        //    //        {
        //    //            MTimeSchedulePS item = new MTimeSchedulePS();
        //    //            item.RunningCode = _RunningCode;
        //    //            item.Time = TimeText;
        //    //            item.Vender = itemSupplier.Field<string>("VD_CODE");
        //    //            item.PartNo = itemPart.Field<string>("PART");
        //    //            item.Qty = double.Parse(itemPart.Field<string>("DO"));
        //    //            resTimeSchedule.Add(item);
        //    //        }

        //    //    }
        //    //    Console.WriteLine(Time);
        //    //}


        //    dtResult = SetTimeSchedule(dtResult);


        //    List<MGetPlan> PlanList = new List<MGetPlan>();
        //    foreach (DataRow dr in dtResult.Rows)
        //    {

        //        PlanList.Add(new MGetPlan()
        //        {
        //            id = Convert.ToInt32(dr["ID"].ToString()),
        //            date = dr["DATE"].ToString(),
        //            plan = dr["PLAN"].ToString(),
        //            picklist = dr["PICKLIST"].ToString(),
        //            doPlan = dr["DO"].ToString(),
        //            stock = dr["STOCK"].ToString(),
        //            part = dr["PART"].ToString(),
        //            doBalance = dr["DOBALANCE"].ToString(),
        //            doAct = dr["DOACT"].ToString(),
        //            stockSim = dr["STOCKSIM"].ToString(),
        //            po = dr["PO"].ToString(),
        //            doAddFixed = dr["DO_ADD_FIXED"].ToString(),
        //            vdCode = dr["VD_CODE"].ToString(),
        //            planNow = dr["PLAN_NOW"].ToString(),
        //            timeScheduleDelivery = dr["TIME"].ToString(),
        //            model = dr["MODEL"].ToString(),
        //            poshort = dr["POSHORT"].ToString() != "" ? bool.Parse(dr["POSHORT"].ToString()) : false
        //        });
        //    }

        //    foreach (DataRow dr in PASTMASTER.Rows)
        //    {
        //        if (!masters.ContainsKey(dr["PARTNO"].ToString()))
        //        {
        //            masters.Add(dr["PARTNO"].ToString(), new DoMaster
        //            {
        //                PartCm = dr["CM"].ToString(),
        //                PartNo = dr["PARTNO"].ToString(),
        //                PartDesc = dr["DESCRIPTION"].ToString(),
        //                PartFixedDate = 1,
        //                VdProdLeadtime = Convert.ToInt32(dr["PDLT"].ToString()),
        //                PartQtyBox = Convert.ToInt32(dr["BOX_QTY"].ToString()),
        //                VdMinDelivery = Convert.ToInt32(dr["BOX_MIN"].ToString()),
        //                VdMaxDelivery = Convert.ToInt32(dr["BOX_MAX"].ToString()),
        //                VdSun = Convert.ToBoolean(dr["VD_SUN"].ToString()),
        //                VdMon = Convert.ToBoolean(dr["VD_MON"].ToString()),
        //                VdTue = Convert.ToBoolean(dr["VD_TUE"].ToString()),
        //                VdWed = Convert.ToBoolean(dr["VD_WED"].ToString()),
        //                VdThu = Convert.ToBoolean(dr["VD_THU"].ToString()),
        //                VdFri = Convert.ToBoolean(dr["VD_FRI"].ToString()),
        //                VdSat = Convert.ToBoolean(dr["VD_SAT"].ToString()),
        //                PartUnit = dr["UNIT"].ToString(),
        //            });
        //        }
        //    }

        //    //var test = dtResponse.Compute("SUM(BOX)",string.Empty);
        //    MGetPlanDataSet PlanResult = new MGetPlanDataSet()
        //    {
        //        data = PlanList,
        //        master = masters,
        //        runningCode = _RunningCode,
        //        holiday = _HOLIDAY,
        //        pos = POS
        //    };
        //    //var sum = boxCapacity.Where(x => x.date == "20230821").Sum(x => x.box);
        //    //Console.WriteLine(sum);

        //    return PlanResult;
        //}

        //        private string FindModelByPart(string vdCode, string Part, string Date)
        //        {
        //            string model = "";
        //            SqlCommand sql = new SqlCommand();
        //            sql.CommandText = @"PN.WCNO, LEFT(PN.PRDYMD, 6) AS PRDYM, PN.PRDYMD, PN.MODEL, PN.[Plan] AS QTY, PL.PARTNO, PL.CM, PT.Description AS PARTNAME, PL.ROUTE, PL.CATMAT, PL.EXP, PL.REQQTY, PL.WHUNIT, VD.VENDER, VD.RATIO, 
        //                         (PL.REQQTY * PN.[Plan]) * (VD.RATIO / 100) AS CONSUMPTION
        //FROM            dbSCM.dbo.vi_AL_DailyPlan AS PN LEFT OUTER JOIN
        //                         dbBCS.dbo.vi_RES_PART_LIST AS PL ON PN.MODEL = PL.MODEL LEFT OUTER JOIN
        //                          dbSCM.dbo.AL_Part AS PT ON PL.PARTNO = PT.DrawingNo LEFT OUTER JOIN
        //                         dbBCS.dbo.CST_PART_MASTER AS PTS ON PL.PARTNO = PTS.PARTNO LEFT OUTER JOIN
        //                         dbBCS.dbo.vi_RES_VENDER_RATIO AS VD ON PL.PARTNO = VD.PARTNO
        //WHERE        (PL.ROUTE IN ('D', 'T')) AND (PL.CATMAT IN ('P', 'A', 'R')) AND (PL.EXP IN ('4')) AND (PN.[Plan] > 0) AND (PL.PARTNO NOT IN
        //                             (SELECT        APARTNO
        //                               FROM             dbSCM.dbo.AL_Outside_Master)) AND (PTS.ORDER_TYPE = '1') 
        //							   AND VD.VENDER = '" + vdCode + "' AND  PL.PARTNO = '" + Part + "' AND PN.PRDYMD = '" + Date + "'";
        //            DataTable dt = dbSCM.Query(sql);
        //            foreach (DataRow dr in dt.Rows)
        //            {
        //                model = dr["MODEL"].ToString();
        //            }
        //            return model;
        //        }

        //private DataTable SetTimeSchedule(DataTable dtResult)
        //{
        //    int IndexOfItem = 0;
        //    foreach (DataRow dr in dtResult.Rows)
        //    {
        //        double Plan = double.Parse(dr["DO"].ToString());
        //        string vdCode = dr["VD_CODE"].ToString();
        //        if (Plan > 0)
        //        {
        //            DoVenderMaster VenderMaster = DoVenderMasters[vdCode];
        //            dtResult.Rows[IndexOfItem]["TIME"] = VenderMaster.VdTimeScheduleDelivery;
        //        }
        //        IndexOfItem++;
        //    }
        //    return dtResult;
        //}

        //private DataTable GetPartOfVender(string vdCode)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    //sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.PARTNO IN ('" + _PartJoin + "')";
        //    sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.VD_CODE = '" + vdCode + "' ORDER BY PARTNO ASC";
        //    DataTable dt = dbSCM.Query(sql);
        //    return dt;
        //}

        //private MPart getPartOfDtResponse(string vdCode, string part, DateTime dateByPdlt)
        //{
        //    MPart res = new MPart();
        //    var content = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == part && x.Field<string>("VD_CODE") == vdCode && x.Field<string>("DATE") == dateByPdlt.ToString("yyyyMMdd")).FirstOrDefault();
        //    if (content != null)
        //    {
        //        res.index = dtResponse.Rows.IndexOf(content);
        //        res.plan = double.Parse(content.Field<string>("PLAN"));
        //        res.stocksim = double.Parse(content.Field<string>("STOCKSIM"));
        //        res.doVal = double.Parse(content.Field<string>("DO"));
        //        res.date = dateByPdlt;
        //    }
        //    return res;
        //}

        //private MPlan GetPlanOfView(string vdCode, string? part, DateTime date)
        //{
        //    MPlan PartDetail = new MPlan()
        //    {
        //        Plan = 0,
        //        StockSimulate = 0,
        //        DoPlan = 0
        //    };
        //    var context = _PLAN.Select(" VENDER = '" + vdCode + "' AND PARTNO = '" + part + "' AND (PRDYMD LIKE '" + date.ToString("yyyyMMdd") + "')").ToList();
        //    foreach (DataRow dr in context)
        //    {
        //        PartDetail.Plan += dr["CONSUMPTION"].ToString() != "" ? Convert.ToDouble(dr["CONSUMPTION"].ToString()) : 0;
        //        PartDetail.Model = dr["MODEL"].ToString();
        //    }
        //    return PartDetail;
        //}

        //private DataTable CalWithBoxPart(DataTable dtResponse)
        //{
        //    string PartLoop = "";
        //    string VdCode = "";
        //    double DOLoop = 0;
        //    DateTime DateLoop;
        //    DateTime DateStart = new DateTime(_StartProcess.Year, _StartProcess.Month, _StartProcess.Day, 0, 0, 0);
        //    int IndexLoop = 0;
        //    double Diff = 0;
        //    DoPartMaster PartMstr = new DoPartMaster();
        //    foreach (DataRow dr in dtResponse.Rows)
        //    {
        //        if (PartLoop == "" || PartLoop != dr["PART"].ToString())
        //        {
        //            PartLoop = dr["PART"].ToString();

        //            VdCode = dr["VD_CODE"].ToString();
        //            PartMstr = _contextDBSCM.DoPartMasters.Where(x => x.Partno == PartLoop && x.VdCode == VdCode).FirstOrDefault();
        //        }
        //        IndexLoop = int.Parse(dr["INDEX"].ToString());
        //        DateLoop = DateTime.ParseExact(dr["DATE"].ToString(), "yyyyMMdd", null);
        //        DOLoop = double.Parse(dr["DO"].ToString());
        //        if (PartMstr != null && DOLoop > PartMstr.BoxMax)
        //        {

        //            if (DateLoop == DateStart) // วันแรก n run process
        //            {

        //            }
        //            else // วันที่ n มากกว่า n run process 
        //            {
        //                bool FoundDateTarget = true;
        //                Diff += DOLoop - (double)PartMstr.BoxMax;
        //                dtResponse.Rows[IndexLoop]["DO"] = double.Parse(dtResponse.Rows[IndexLoop]["DO"].ToString()) - Diff;
        //                DateLoop = DateLoop.AddDays(-1);
        //                IndexLoop--;
        //                while (DateLoop >= DateStart)
        //                {
        //                    DOLoop = double.Parse(dtResponse.Rows[IndexLoop]["DO"].ToString());
        //                    if (DOLoop < PartMstr.BoxMax)
        //                    {
        //                        DOLoop = double.Parse(dtResponse.Rows[IndexLoop]["DO"].ToString()) + Diff;
        //                        //DOLoop = Math.Ceiling(DOLoop / Convert.ToDouble(PartMstr.BoxQty.ToString())) * Convert.ToDouble(PartMstr.BoxQty.ToString());
        //                        //DOLoop = _CheckMinDelivery(DOLoop, Convert.ToDouble(PartMstr.BoxMin.ToString()));
        //                        //DOLoop = _CheckMaxDelivery(DOLoop, Convert.ToDouble(PartMstr.BoxMax.ToString()));
        //                        Diff -= DOLoop;
        //                        dtResponse.Rows[IndexLoop]["DO"] = DOLoop;
        //                        Diff = 0;
        //                    }
        //                    if (DateLoop == DateStart)
        //                    {
        //                        FoundDateTarget = false;
        //                    }
        //                    IndexLoop--;
        //                    DateLoop = DateLoop.AddDays(-1);
        //                }
        //            }
        //        }
        //    }
        //    return dtResponse;
        //}

        //private double GetDoAct(string? partNo, string date, List<MDOAct> _DO_ACT)
        //{
        //    double doAct = 0;
        //    var contextDoAct = _DO_ACT.Where(x => x.PartNo.Trim() == partNo && x.AcDate == date).FirstOrDefault();
        //    if (contextDoAct != null)
        //    {
        //        doAct = contextDoAct.Wqty;
        //    }
        //    return doAct;
        //}

        //private List<MPickList> GetPickListBySupplier(string Parts)
        //{
        //    List<MPickList> res = new List<MPickList>();
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = "SELECT PARTNO,CAST(IDATE AS DATE) AS IDATE,SUM(FQTY) AS FQTY FROM [dbSCM].[dbo].[AL_GST_DATPID] WHERE PARTNO IN ('" + Parts + "') AND PRGBIT IN ('F','C') AND IDATE >= @startProcess AND IDATE <= @endProcess GROUP BY PARTNO,IDATE";
        //    sql.Parameters.Add(new SqlParameter("startProcess", _StartProcess.Date));
        //    sql.Parameters.Add(new SqlParameter("endProcess", _EndProcess.Date));
        //    DataTable dt = dbSCM.Query(sql);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        res.Add(new MPickList()
        //        {
        //            Partno = dr["PARTNO"].ToString(),
        //            Fqty = dr["FQTY"].ToString(),
        //            Idate = DateTime.Parse(dr["IDATE"].ToString()).ToString("yyyyMMdd")
        //        });
        //    }
        //    return res;
        //}

        //private DateTime dtParse(string dateEnd)
        //{
        //    return DateTime.ParseExact(dateEnd, "yyyyMMdd", null);
        //}

        //private double GetPlanToDay(string dateReset, string vdCode, string Part)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = @"SELECT PRDYMD,PARTNO,CM,VENDER,ROUND(SUM(CONSUMPTION),0)  AS CONSUMPTION FROM [dbSCM].[dbo].[vi_DO_Plan] WHERE PRDYMD = '" + dateReset + "' AND VENDER = '" + vdCode + "'  AND PARTNO = '" + Part + "' GROUP BY PRDYMD,PARTNO,CM,VENDER ORDER BY PRDYMD ASC";
        //    //sql.Parameters.Add(new SqlParameter("PRDYMD", dateReset.ToString("yyyyMMdd")));
        //    //sql.Parameters.Add(new SqlParameter("VD_CODE", vdCode));
        //    DataTable dt = dbSCM.Query(sql);
        //    double Plan = 0;
        //    if (dt.Rows.Count > 0)
        //    {
        //        Plan = double.Parse(dt.Rows[0]["CONSUMPTION"].ToString());
        //    }
        //    return Plan;
        //}

        //private DataTable RefreshStockWithOutHistory(DataTable dtResponse)
        //{
        //    var Parts = dtResponse.AsEnumerable().GroupBy(x => x.Field<string>("PART")).ToList();
        //    foreach (var ItemPart in Parts)
        //    {

        //        string Part = ItemPart.Key;
        //        //if (Part == "3PD04371-1")
        //        //{
        //        //    Console.WriteLine("123");
        //        //}
        //        double Stock = 0;
        //        DateTime DateLoop = _RunProcess;
        //        int IndexPart = 0;
        //        while (DateLoop <= _EndProcess)
        //        {
        //            if (DateLoop == _RunProcess)
        //            {
        //                var PartInDate = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == Part && x.Field<string>("DATE") == DateLoop.ToString("yyyyMMdd")).FirstOrDefault();
        //                IndexPart = dtResponse.Rows.IndexOf(PartInDate);
        //                Stock = double.Parse(PartInDate.Field<string>("STOCK"));
        //            }
        //            Stock = (Stock + int.Parse(dtResponse.Rows[IndexPart]["DO"].ToString())) - int.Parse(dtResponse.Rows[IndexPart]["PLAN"].ToString());
        //            dtResponse.Rows[IndexPart]["STOCKSIM"] = Stock;
        //            IndexPart++;
        //            DateLoop = DateLoop.AddDays(1);
        //        }
        //    }
        //    return dtResponse;
        //}

        //private double CalDoFromHistory(string _PartNo, DateTime _Date, double _PlanDefault, double _StockSim, double _DoDefault, string vdCode)
        //{
        //    try
        //    {
        //        double _DoVal = 0;
        //        double _Target = _StockSim;
        //        _Target = Math.Abs(_Target);
        //        int PDLT = 0;
        //        int BOX = 0;
        //        if (_PartMstr.Count() > 0)
        //        {
        //            PDLT = int.Parse(_PartMstr[0]["PDLT"].ToString());
        //            BOX = int.Parse(_PartMstr[0]["BOX_QTY"].ToString());
        //            _BoxCap = BOX;
        //        }
        //        //if (PDLT == 0)
        //        //{
        //        _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //        //    _StockSimBalance = _DoVal - _Target;
        //        //}
        //        //else
        //        //{
        //        //DateTime _DateNow = _Date.AddDays(-PDLT);
        //        //var StockMinus = _HISTORY_OF_PART.Where(x => DateTime.ParseExact(x.DateVal, "yyyyMMdd", null) >= new DateTime(_RunProcess.Year, _RunProcess.Month, _RunProcess.Day, 0, 0, 0) && DateTime.ParseExact(x.DateVal, "yyyyMMdd", null) <= new DateTime(_RunProcess.AddDays(13 + PDLT).Year, _RunProcess.AddDays(13 + PDLT).Month, _RunProcess.AddDays(13 + PDLT).Day, 0, 0, 0) && x.StockVal < 0).Sum(x => x.StockVal); // รวม stock ติดลบ
        //        //if (_PartNo == "3PD04468-3")
        //        //{
        //        //    Console.WriteLine("123");
        //        //}
        //        var StockMinusStart = _HISTORY_OF_PART.Where(x => DateTime.ParseExact(x.DateVal, "yyyyMMdd", null) >= new DateTime(_RunProcess.Year, _RunProcess.Month, _RunProcess.Day, 0, 0, 0) && DateTime.ParseExact(x.DateVal, "yyyyMMdd", null) <= new DateTime(_RunProcess.AddDays(13 + PDLT).Year, _RunProcess.AddDays(13 + PDLT).Month, _RunProcess.AddDays(13 + PDLT).Day, 0, 0, 0) && x.StockVal < 0).FirstOrDefault();
        //        var StockMinusEnd = _HISTORY_OF_PART.Where(x => DateTime.ParseExact(x.DateVal, "yyyyMMdd", null) >= new DateTime(_RunProcess.Year, _RunProcess.Month, _RunProcess.Day, 0, 0, 0) && DateTime.ParseExact(x.DateVal, "yyyyMMdd", null) <= new DateTime(_RunProcess.AddDays(13 + PDLT).Year, _RunProcess.AddDays(13 + PDLT).Month, _RunProcess.AddDays(13 + PDLT).Day, 0, 0, 0) && x.StockVal < 0).LastOrDefault();
        //        var StockMinus = (StockMinusStart != null ? (double)StockMinusStart.StockVal : 0) + (StockMinusEnd != null ? (double)StockMinusEnd.StockVal : 0);
        //        _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //        DateTime sDate = _FixedProcess;
        //        double Stock = 0;
        //        int IndexOfData = 0;
        //        while (sDate <= _Date)
        //        {
        //            var PartData = FindPartInHistory(_PartNo, sDate);
        //            if (sDate == _FixedProcess)
        //            {
        //                Stock = (double)_HISTORY_OF_PART[PartData.index - 1].StockVal;
        //                var PartOfDt = FindPartInDt(_PartNo, sDate);
        //                IndexOfData = PartOfDt.index;
        //            }
        //            if (StockMinus < 0)
        //            {
        //                var PartMstr = _contextDBSCM.DoPartMasters.Where(x => x.Partno == _PartNo && x.VdCode == vdCode).FirstOrDefault();
        //                double DoCanAdd = 0;
        //                if (PartMstr != null)
        //                {
        //                    if (Math.Abs((double)StockMinus) <= PartMstr.BoxMax)
        //                    {
        //                        DoCanAdd = Math.Abs((double)StockMinus);
        //                    }
        //                    else if (Math.Abs((double)StockMinus) > PartMstr.BoxMax)
        //                    {
        //                        DoCanAdd = Math.Abs((double)PartMstr.BoxMax);
        //                    }
        //                    StockMinus += DoCanAdd;
        //                    PartData.doVal += DoCanAdd;
        //                }
        //            }
        //            Stock = (Stock + PartData.doVal) - PartData.plan;
        //            _HISTORY_OF_PART[PartData.index].DoVal = PartData.doVal;
        //            _HISTORY_OF_PART[PartData.index].StockVal = Stock;

        //            if (sDate == _Date)
        //            {
        //                Console.WriteLine(123);
        //                DateTime sFixedDate = _FixedProcess;
        //                while (sFixedDate < _Date)
        //                {
        //                    var PartHistory = FindPartInHistory(_PartNo, sFixedDate);
        //                    dtResponse.Rows[PartHistory.index]["DO"] = PartHistory.doVal;
        //                    dtResponse.Rows[PartHistory.index]["STOCKSIM"] = PartHistory.stocksim;
        //                    sFixedDate = sFixedDate.AddDays(1);
        //                }
        //            }
        //            //try
        //            //{
        //            //    dtResponse.Rows[IndexOfData]["DO"] = PartData.doVal;
        //            //    dtResponse.Rows[IndexOfData]["STOCKSIM"] = Stock;
        //            //}
        //            //catch(Exception e)
        //            //{
        //            //    Console.WriteLine(e);
        //            //}

        //            sDate = sDate.AddDays(1);
        //        }
        //        return _DoVal;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        return 0;
        //    }
        //}

        //private DoHistory FindIndexOfPartAndDate(DateTime Date, string PartNo)
        //{
        //    DoHistory PartData = _HISTORY.Where(x => x.Partno == PartNo && x.DateVal == Date.ToString("yyyyMMdd")).FirstOrDefault();
        //    return PartData;
        //}

        //private DataTable RefreshWhenChangePlan(DataTable dtResponse, DateTime lastDateHaveHistory)
        //{
        //    Dictionary<string, int> LastIndex = new Dictionary<string, int>();
        //    var Parts = dtResponse.AsEnumerable().GroupBy(x => x.Field<string>("PART")).ToList();
        //    foreach (var ItemPart in Parts)
        //    {
        //        string Part = ItemPart.Key;
        //        DateTime DateLoop = lastDateHaveHistory;
        //        if (!LastIndex.ContainsKey(Part))
        //        {
        //            var PartDetail = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == Part && x.Field<string>("DATE") == lastDateHaveHistory.ToString("yyyyMMdd")).FirstOrDefault();
        //            LastIndex.Add(Part, int.Parse(PartDetail.Field<string>("INDEX")));
        //        }

        //        int IndexPart = LastIndex[Part];
        //        double Stock = double.Parse(dtResponse.Rows[IndexPart]["STOCKSIM"].ToString());
        //        while (DateLoop <= _EndProcess)
        //        {
        //            int NewPlan = int.Parse(dtResponse.Rows[IndexPart]["PLAN_NOW"].ToString());
        //            if (NewPlan > 0)
        //            {
        //                Stock = (Stock + int.Parse(dtResponse.Rows[IndexPart]["DO"].ToString())) - int.Parse(dtResponse.Rows[IndexPart]["PLAN"].ToString());
        //                dtResponse.Rows[IndexPart]["STOCKSIM"] = Stock;
        //            }
        //            IndexPart++;
        //            DateLoop = DateLoop.AddDays(1);
        //        }
        //    }
        //    return dtResponse;
        //}

        //private DataTable LEFT_DO_VALUE(DataTable dtResponse, DataTable Parts, DataTable PartMasters, DateTime DateHistory, bool calDoWhenStockMinus)
        //{
        //    DateTime DateLoop = _RunProcess.AddDays(14);
        //    Dictionary<string, int> DOofPart = new Dictionary<string, int>();
        //    Dictionary<string, int> IndexOfPart = new Dictionary<string, int>();
        //    while (DateLoop >= _RunProcess)
        //    {
        //        foreach (DataRow itemPart in Parts.Rows)
        //        {
        //            string Part = itemPart["PARTNO"].ToString();
        //            //if (calDoWhenStockMinus ? (DateLoop >= _FixedProcess) : (_RunProcess == DateHistory ? (DateLoop >= _RunProcess) : (DateLoop > _FixedProcess)))
        //            //{
        //            if (DateLoop >= _RunProcess)
        //            {
        //                if (!DOofPart.ContainsKey(Part)) { DOofPart.Add(Part, 0); }
        //                var PartVals = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == Part && x.Field<string>("DATE") == DateLoop.ToString("yyyyMMdd")).FirstOrDefault();
        //                if (PartVals != null)
        //                {
        //                    int IndexVal = ParseInt(PartVals.Field<string>("INDEX"));
        //                    int DoVal = ParseInt(PartVals.Field<string>("DO"));
        //                    DOofPart[Part] += DoVal;
        //                    if (Part == "2P473200-1" && DoVal > 0)
        //                    {
        //                        Console.WriteLine("123");
        //                    }
        //                    dtResponse.Rows[IndexVal]["DO"] = 0;
        //                    //if (calDoWhenStockMinus ? (DateLoop == _FixedProcess) : (_RunProcess == DateHistory ? DateLoop == _RunProcess : DateLoop == DateHistory.AddDays(1)))
        //                    //{
        //                    if (DateLoop == _RunProcess)
        //                    {
        //                        if (!IndexOfPart.ContainsKey(Part)) { IndexOfPart.Add(Part, IndexVal); }
        //                    }
        //                }
        //            }
        //        }
        //        DateLoop = DateLoop.AddDays(-1);
        //    }
        //    Dictionary<string, int> IndexOfPartUsed = IndexOfPart.ToDictionary(x => x.Key, x => x.Value);
        //    foreach (DataRow itemPart in Parts.Rows)
        //    {
        //        string Part = itemPart["PARTNO"].ToString();
        //        var PartDetail = PartMasters.AsEnumerable().Where(x => x.Field<string>("PARTNO") == Part).FirstOrDefault();
        //        DateLoop = _RunProcess;

        //        while (DateLoop <= _EndProcess)
        //        {
        //            //if ((_RunProcess == DateHistory ? DateLoop >= _RunProcess : DateLoop > DateHistory) || calDoWhenStockMinus)
        //            //{
        //            //if (DateLoop >= _RunProcess)
        //            //{
        //            if (DOofPart[Part] == 0)
        //            {
        //                break;
        //            }
        //            if (PartDetail != null)
        //            {
        //                string DayText = DateLoop.ToString("ddd").ToUpper();
        //                bool CanDelivery = true;
        //                CanDelivery = Convert.ToBoolean(_PartMstr[0]["VD_" + DayText].ToString());
        //                if (CanDelivery)
        //                {
        //                    int PartIndex = IndexOfPartUsed[Part];
        //                    int DoVal = DOofPart[Part];
        //                    int BoxMax = PartDetail.Field<int>("BOX_MAX");
        //                    int BoxMin = PartDetail.Field<int>("BOX_MIN");
        //                    int DOCanAdd = DoVal <= BoxMax ? DoVal : BoxMax;
        //                    dtResponse.Rows[PartIndex]["DO"] = DOCanAdd;
        //                    DOofPart[Part] -= DOCanAdd;
        //                }
        //                IndexOfPartUsed[Part]++;
        //            }
        //            //}
        //            DateLoop = DateLoop.AddDays(1);
        //        }

        //        if (DateHistory == _RunProcess) // ปรับ stock sim แบบไม่มีประวัติ
        //        {
        //            DateLoop = _RunProcess;
        //            //bool SetStockFirst = true;
        //            double StockLoop = 0;
        //            while (DateLoop <= _EndProcess)
        //            {
        //                MPart PartData = FindPartInDt(Part, DateLoop);
        //                if (DateLoop == _RunProcess)
        //                {
        //                    StockLoop = PartData.stock;

        //                    //SetStockFirst = false;
        //                }
        //                StockLoop = (StockLoop + PartData.doVal) - PartData.plan;
        //                dtResponse.Rows[PartData.index]["STOCKSIM"] = StockLoop;
        //                DateLoop = DateLoop.AddDays(1);
        //            }
        //        }
        //        else if (calDoWhenStockMinus && _HISTORY.Count == 0)
        //        {

        //            DateLoop = _FixedProcess;
        //            bool SetStockFirst = true;
        //            double StockLoop = 0;
        //            while (DateLoop <= _EndProcess)
        //            {
        //                double Stock = double.Parse(dtResponse.Rows[IndexOfPart[Part]]["STOCKSIM"].ToString());
        //                double doVal = double.Parse(dtResponse.Rows[IndexOfPart[Part]]["DO"].ToString());
        //                double Plan = double.Parse(dtResponse.Rows[IndexOfPart[Part]]["PLAN"].ToString());
        //                if (SetStockFirst)
        //                {
        //                    StockLoop = Stock;
        //                    SetStockFirst = false;
        //                    dtResponse.Rows[IndexOfPart[Part]]["STOCKSIM"] = StockLoop;
        //                }
        //                else
        //                {
        //                    StockLoop = (StockLoop + doVal) - Plan;
        //                    dtResponse.Rows[IndexOfPart[Part]]["STOCKSIM"] = StockLoop;
        //                }
        //                IndexOfPart[Part]++;
        //                DateLoop = DateLoop.AddDays(1);
        //            }
        //        }
        //        else if (calDoWhenStockMinus && _HISTORY.Count > 0) // เมื่อกดปุ่มออก D/O และมีประวัติก่อนหน้า
        //        {
        //            DateLoop = _FixedProcess;
        //            var PartData = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == DateLoop.ToString("yyyyMMdd") && x.Field<string>("PART") == Part).FirstOrDefault();
        //            int IndexStart = dtResponse.Rows.IndexOf(PartData);
        //            double stockStart = double.Parse(dtResponse.Rows[IndexStart]["STOCKSIM"].ToString());
        //            IndexStart++;
        //            //DateLoop = DateLoop.AddDays(1);
        //            while (DateLoop < _EndProcess)
        //            {
        //                double PlanLoop = double.Parse(dtResponse.Rows[IndexStart]["PLAN"].ToString());
        //                double DoLoop = double.Parse(dtResponse.Rows[IndexStart]["DO"].ToString());
        //                stockStart = (stockStart + DoLoop) - PlanLoop;
        //                dtResponse.Rows[IndexStart]["STOCKSIM"] = stockStart;
        //                IndexStart++;
        //                DateLoop = DateLoop.AddDays(1);
        //            }
        //        }
        //    }
        //    return dtResponse;
        //}

        //public MPart FindPartInDt(string PartNo, DateTime Date)
        //{
        //    var Part = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == PartNo && x.Field<string>("DATE") == Date.ToString("yyyyMMdd")).FirstOrDefault();
        //    int IndexOfDt = dtResponse.Rows.IndexOf(Part);
        //    MPart res = new MPart()
        //    {
        //        index = IndexOfDt,
        //        doVal = double.Parse(Part.Field<string>("DO")),
        //        plan = double.Parse(Part.Field<string>("PLAN")),
        //        stock = double.Parse(Part.Field<string>("STOCK")),
        //        stocksim = double.Parse(Part.Field<string>("STOCKSIM")),
        //    };
        //    return res;
        //}


        //public MPart FindPartInHistory(string PartNo, DateTime Date)
        //{
        //    var Part = _HISTORY_OF_PART.Where(x => x.Partno == PartNo && x.DateVal == Date.ToString("yyyyMMdd")).FirstOrDefault();
        //    int IndexOfDt = _HISTORY_OF_PART.IndexOf(Part);
        //    MPart res = new MPart()
        //    {
        //        index = IndexOfDt,
        //        doVal = (double)Part.DoVal,
        //        plan = (double)Part.PlanVal,
        //        stock = (double)Part.Stock,
        //        stocksim = (double)Part.StockVal,
        //    };
        //    return res;
        //}

        //public DataTable RefreshStock(DataTable dtResponse, DateTime lastDateHaveHistory)
        //{
        //    Dictionary<string, double> LastStock = new Dictionary<string, double>();
        //    Dictionary<string, int> LastIndex = new Dictionary<string, int>();
        //    var Parts = dtResponse.AsEnumerable().GroupBy(x => x.Field<string>("PART")).ToList();
        //    DateTime LastDateHistory = lastDateHaveHistory;
        //    LastDateHistory = _FixedProcess;
        //    foreach (var ItemPart in Parts)
        //    {
        //        string Part = ItemPart.Key;
        //        //if (LastDateHistory != _RunProcess && !LastStock.ContainsKey(Part))
        //        //{
        //        //    var PartDetail = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == Part && x.Field<string>("DATE") == LastDateHistory.AddDays(-1).ToString("yyyyMMdd")).FirstOrDefault();
        //        //    LastStock.Add(Part, double.Parse(PartDetail.Field<string>("STOCKSIM")));
        //        //    LastIndex.Add(Part, int.Parse(PartDetail.Field<string>("INDEX")) + 1);
        //        //}
        //        //else
        //        //{
        //        //    if (!LastStock.ContainsKey(Part))
        //        //    {
        //        //        var PartDetail = dtResponse.AsEnumerable().Where(x => x.Field<string>("PART") == Part && x.Field<string>("DATE") == _RunProcess.ToString("yyyyMMdd")).FirstOrDefault();
        //        //        LastStock.Add(Part, double.Parse(PartDetail.Field<string>("STOCK")));
        //        //        LastIndex.Add(Part, int.Parse(PartDetail.Field<string>("INDEX")));
        //        //    }
        //        //}
        //        //DateTime DateLoop = (LastDateHistory == _RunProcess) ? _RunProcess : LastDateHistory;
        //        DateTime DateLoop = _RunProcess;
        //        var PartRunProcess = dtResponse.AsEnumerable().Where(x => x.Field<string>("DATE") == DateLoop.ToString("yyyyMMdd") && x.Field<string>("PART") == Part).FirstOrDefault();
        //        double Stock = double.Parse(PartRunProcess.Field<string>("STOCK").ToString());
        //        double Plan = 0;
        //        double DO = 0;
        //        int IndexRunProcess = dtResponse.Rows.IndexOf(PartRunProcess);
        //        //int IndexPart = LastIndex[Part];
        //        while (DateLoop <= _EndProcess)
        //        {
        //            //if (DateLoop.ToString("yyyyMMdd") != _RunProcess.ToString("yyyyMMdd"))
        //            //{
        //            Plan = double.Parse(dtResponse.Rows[IndexRunProcess]["PLAN"].ToString());
        //            //}
        //            DO = double.Parse(dtResponse.Rows[IndexRunProcess]["DO"].ToString());
        //            Stock = (Stock + DO) - Plan;
        //            dtResponse.Rows[IndexRunProcess]["STOCKSIM"] = Stock;
        //            IndexRunProcess++;
        //            DateLoop = DateLoop.AddDays(1);
        //        }
        //    }
        //    return dtResponse;
        //}

        //public int ParseInt(string? v)
        //{
        //    try
        //    {
        //        return int.Parse(v);
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}

        //        private SqlCommand SqlViewGetPlan(string vdCode, DateTime runProcess, DateTime endProcess)
        //        {
        //            SqlCommand sql = new SqlCommand();
        //            sql.CommandText = @"SELECT PRDYMD,MODEL,PARTNO,CM,VENDER,ROUND(SUM(CONSUMPTION),0)  AS CONSUMPTION
        //                                         FROM [dbSCM].[dbo].[vi_DO_Plan]
        //                                         WHERE PRDYMD >= @SDATE 
        //                                         AND PRDYMD <= @FDATE

        //AND VENDER = @VD_CODE  GROUP BY PRDYMD,MODEL,PARTNO,CM,VENDER ORDER BY PRDYMD ASC";

        //            //AND(PARTNO LIKE '2P473200-1')

        //            sql.Parameters.Add(new SqlParameter("@SDATE", runProcess.ToString("yyyyMMdd")));
        //            sql.Parameters.Add(new SqlParameter("@FDATE", endProcess.ToString("yyyyMMdd")));
        //            //if (venderCode != "")
        //            //{
        //            sql.Parameters.Add(new SqlParameter("@VD_CODE", vdCode));
        //            //}
        //            return sql;
        //        }
        //        private List<M_SQL_VIEW_DO_PLAN> SQLV_DOPLAN(string supplier, DateTime sDate, DateTime fDate)
        //        {
        //            List<M_SQL_VIEW_DO_PLAN> res = new List<M_SQL_VIEW_DO_PLAN>();
        //            SqlCommand sql = new SqlCommand();
        //            //sql.CommandText = @"SELECT PRDYMD,MODEL,PARTNO,CM,VENDER,ROUND(SUM(CONSUMPTION),0)  AS CONSUMPTION
        //            //                             FROM [dbSCM].[dbo].[vi_DO_Plan]
        //            //                             WHERE PRDYMD >= @SDATE 
        //            //                             AND PRDYMD <= @FDATE AND VENDER = @SUPPLIER  GROUP BY PRDYMD,MODEL,PARTNO,CM,VENDER ORDER BY PRDYMD ASC";
        //            sql.CommandText = @"SELECT PRDYMD,PARTNO,CM,VENDER,ROUND(SUM(CONSUMPTION),0)  AS CONSUMPTION
        //                                         FROM [dbSCM].[dbo].[vi_DO_Plan]
        //                                         WHERE PRDYMD >= @SDATE 
        //                                         AND PRDYMD <= @FDATE AND VENDER = @SUPPLIER  GROUP BY PRDYMD,PARTNO,CM,VENDER ORDER BY PRDYMD ASC";
        //            sql.Parameters.Add(new SqlParameter("@SDATE", sDate.ToString("yyyyMMdd")));
        //            sql.Parameters.Add(new SqlParameter("@FDATE", fDate.ToString("yyyyMMdd")));
        //            sql.Parameters.Add(new SqlParameter("@SUPPLIER", supplier));
        //            DataTable dt = dbSCM.Query(sql);
        //            if (dt.Rows.Count > 0)
        //            {
        //                foreach (DataRow dr in dt.Rows)
        //                {
        //                    M_SQL_VIEW_DO_PLAN item = new M_SQL_VIEW_DO_PLAN();
        //                    item.Qty = Convert.ToDouble(dr["CONSUMPTION"].ToString());
        //                    item.Date = DateTime.ParseExact(dr["PRDYMD"].ToString(), "yyyyMMdd", null);
        //                    item.PartNo = dr["PARTNO"].ToString();
        //                    item.Cm = dr["CM"].ToString();
        //                    res.Add(item);
        //                }
        //            }
        //            return res;
        //        }

        //        public DateTime ConvertExtra(object date)
        //        {
        //            return DateTime.ParseExact(date.ToString(), "yyyyMMdd", null);
        //        }

        //private void initBoxCapacity(string vender, string? part, DateTime date, double doVal)
        //{
        //    var dr = boxCapacity.FirstOrDefault(x => x.vender == vender && x.part == part && x.date == date.ToString("yyyyMMdd"));
        //    if (doVal != 0)
        //    {
        //        Console.WriteLine("213");
        //    }
        //    if (dr != null)
        //    {
        //        Console.WriteLine(dr);
        //    }
        //    else
        //    {
        //        boxCapacity.Add(new MBoxCapacity()
        //        {
        //            vender = vender,
        //            date = date.ToString("yyyyMMdd"),
        //            part = part,
        //            box = doVal / Convert.ToDouble(_PartMstr[0]["VD_BOX"])
        //        });
        //    }
        //}

        //private DataTable GetVenderMaster(string vdCode)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = "SELECT * FROM [dbSCM].[dbo].[DO_VENDER_MASTER] WHERE VD_CODE = @VDCODE";
        //    sql.Parameters.Add(new SqlParameter("@VDCODE", vdCode));
        //    DataTable dt = dbSCM.Query(sql);
        //    return dt;
        //}

        //private Dictionary<string, double> _GET_PICKLIST_BY_PARTNO(string? partNo)
        //{
        //    Dictionary<string, double> res = new Dictionary<string, double>();
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = "SELECT PARTNO,CAST(IDATE AS DATE) AS IDATE,SUM(FQTY) AS FQTY FROM [dbSCM].[dbo].[AL_GST_DATPID] WHERE PARTNO = @partCode AND PRGBIT IN ('F','C') AND IDATE >= @startProcess AND IDATE <= @endProcess GROUP BY PARTNO,IDATE";
        //    sql.Parameters.Add(new SqlParameter("partCode", partNo));
        //    sql.Parameters.Add(new SqlParameter("startProcess", _StartProcess.Date));
        //    sql.Parameters.Add(new SqlParameter("endProcess", _EndProcess.Date));
        //    DataTable dt = dbSCM.Query(sql);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        res.Add(DateTime.Parse(dr["IDATE"].ToString()).ToString("yyyyMMdd"), dr["FQTY"].ToString() != "" ? Convert.ToDouble(dr["FQTY"].ToString()) : 0);
        //    }
        //    return res;
        //}

        //private Dictionary<string, double> _GET_PO_BY_PARTNO(string? partNo, DateTime loopDate)
        //{
        //    Dictionary<string, double> res = new Dictionary<string, double>();
        //    var context = _PO.Select(" PARTNO = '" + partNo + "' AND (DELYMD LIKE '" + loopDate.ToString("yyyyMM") + "%' OR DELYMD LIKE '" + loopDate.AddMonths(1).ToString("yyyyMM") + "%')").ToList();
        //    foreach (DataRow dr in context)
        //    {
        //        res.Add(dr["DELYMD"].ToString(), dr["WHBLBQTY"].ToString() != "" ? Convert.ToDouble(dr["WHBLBQTY"].ToString()) : 0);
        //    }
        //    return res;
        //}

        //private Dictionary<string, double> _GET_PLAN_BY_PARTNO(string vender, string? partNo, DateTime loopDate)
        //{
        //    Dictionary<string, double> res = new Dictionary<string, double>();
        //    try
        //    {
        //        var context = _PLAN.Select(" VENDER = '" + vender + "' AND PARTNO = '" + partNo + "' AND (PRDYMD LIKE '" + loopDate.ToString("yyyyMM") + "%' OR PRDYMD LIKE '" + loopDate.AddMonths(1).ToString("yyyyMM") + "%')").ToList();
        //        foreach (DataRow dr in context)
        //        {
        //            if (res.ContainsKey(dr["PRDYMD"].ToString()))
        //            {
        //                res[dr["PRDYMD"].ToString()] += dr["CONSUMPTION"].ToString() != "" ? Convert.ToDouble(dr["CONSUMPTION"].ToString()) : 0;
        //            }
        //            else
        //            {
        //                res.Add(dr["PRDYMD"].ToString(), dr["CONSUMPTION"].ToString() != "" ? Convert.ToDouble(dr["CONSUMPTION"].ToString()) : 0);
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        Console.WriteLine("123");
        //    }
        //    return res;
        //}

        //private string _GET_SUPPLIER_BUYER(string? buyer)
        //{
        //    string SupplierJoin = "";
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_DictMstr] WHERE DICT_TYPE = 'BUYER' AND CODE = '" + buyer + "'";
        //    DataTable dt = dbSCM.Query(sql);
        //    if (dt.Rows.Count > 0)
        //    {
        //        DataView view = new DataView(dt);
        //        DataTable _SUPPLIER = view.ToTable(true, "REF_CODE");
        //        SupplierJoin = string.Join("','", _SUPPLIER.Rows.OfType<DataRow>().Select(r => r[0].ToString()));
        //    }
        //    return SupplierJoin;
        //}


        //private double GetDoAct(string partNo, DateTime loopDate)
        //{
        //    try
        //    {
        //        DataRow[] doAct = _DO_ACT.Select("PARTNO = '" + partNo + "' AND ACDATE = '" + loopDate.ToString("yyyyMM") + "'");
        //        return doAct.Count() > 0 ? Convert.ToDouble(doAct[0]["WQTY"]) : 0;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}

        //private List<MDOAct> _GET_DO_ACT(DateTime sDate, DateTime eDate)
        //{
        //    List<MDOAct> res = new List<MDOAct>();
        //    OracleCommand cmd = new();
        //    cmd.CommandText = "SELECT PARTNO,SUM(WQTY) AS WQTY,ACDATE FROM DST_DATAC1 WHERE TRIM(PARTNO) IN ('" + _PART_JOIN_STRING + "') AND TRIM(ACDATE) BETWEEN '" + sDate.ToString("yyyyMM") + "' AND '" + eDate.AddMonths(1).ToString("yyyyMM") + "' GROUP BY PARTNO,ACDATE";
        //    DataTable dt = dbAlpha2.Query(cmd);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        MDOAct item = new MDOAct();
        //        item.PartNo = dr["PARTNO"].ToString();
        //        item.Wqty = double.Parse(dr["WQTY"].ToString());
        //        item.AcDate = dr["ACDATE"].ToString();
        //        res.Add(item);
        //    }
        //    return res;
        //}

        //private List<string> _GET_DIFF_PART()
        //{
        //    List<string> listPartOfNow = _PARTS.AsEnumerable().Select(r => r.Field<string>("PARTNO")).ToList();
        //    List<string> listPartOfHistory = _HISTORY.Select(r => r.Partno).Distinct().ToList();
        //    foreach (string s in listPartOfNow)
        //    {
        //        var contain = listPartOfHistory.FirstOrDefault(x => x.Contains(s));
        //        if (contain != null)
        //        {
        //            int indexOf = listPartOfHistory.IndexOf(contain);
        //            listPartOfHistory.RemoveAt(indexOf);
        //        }
        //    }
        //    DataTable dt = new DataTable();
        //    dt.Columns.Add("PARTNO");
        //    dt.Columns.Add("CM");
        //    dt.Columns.Add("VENDER");
        //    foreach (string part in listPartOfHistory)
        //    {
        //        DataTable dtMstr = _GET_MSTR(part);
        //        foreach (DataRow dr in dtMstr.Rows)
        //        {
        //            dt.Rows.Add(part, dr["CM"].ToString(), dr["VD_CODE"].ToString());
        //            _DO_MSTR.Rows.Add(dr.ItemArray);
        //        }
        //    }
        //    _PARTS.Merge(dt);
        //    DataView dv = new DataView(_PARTS);
        //    dv.Sort = "PARTNO ASC";
        //    _PARTS = dv.ToTable();
        //    return listPartOfHistory;
        //}

        //private DataTable _GET_MSTR(string vdCode, string _PartJoin = "")
        //{
        //    SqlCommand sql = new SqlCommand();
        //    //sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.PARTNO IN ('" + _PartJoin + "')";
        //    sql.CommandText = @"SELECT PART.*,VD.* FROM [dbSCM].[dbo].[DO_PART_MASTER] PART LEFT JOIN [dbSCM].[dbo].[DO_VENDER_MASTER] VD ON PART.VD_CODE = VD.VD_CODE WHERE PART.VD_CODE = '" + vdCode + "'";
        //    DataTable dt = dbSCM.Query(sql);
        //    return dt;
        //}

        //private List<DoDictMstr> _GET_HOLIDAY()
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

        //private List<DoHistory> _GET_HISTORY(string? _VdCode = "", DateTime _StartDate = default, DateTime _EndDate = default)
        //{
        //    List<DoHistory> res = new List<DoHistory>();
        //    SqlCommand sql = new SqlCommand();
        //    string RunningCode = DateTime.Now.ToString("yyyyMMdd");
        //    string WhereVdCodeA = _VdCode != "" ? " AND A.VD_CODE LIKE '" + _VdCode + "'" : "";
        //    //          sql.CommandText = @"SELECT A.*
        //    //FROM [dbSCM].[dbo].[DO_HISTORY] A
        //    //where  A.RUNNING_CODE = (SELECT TOP(1) RUNNING_CODE FROM [dbSCM].[dbo].[DO_HISTORY] ORDER BY RUNNING_CODE DESC) 
        //    //" + WhereVdCodeA + " AND A.REV = (SELECT TOP(1) REV FROM [dbSCM].[dbo].[DO_HISTORY] WHERE RUNNING_CODE = A.RUNNING_CODE " + WhereVdCode + " ORDER BY REV DESC)";
        //    //sql.CommandText = @"SELECT A.* FROM [dbSCM].[dbo].[DO_HISTORY] A where A.RUNNING_CODE = '" + RunningCode + "' AND A.REVISION = 999 " + WhereVdCodeA + " AND DATE_VAL >= @SDATE AND DATE_VAL <= @FDATE ORDER BY REV DESC";
        //    sql.CommandText = @"SELECT A.* FROM [dbSCM].[dbo].[DO_HISTORY] A where  A.REVISION = 999 " + WhereVdCodeA + " AND DATE_VAL >= @SDATE AND DATE_VAL <= @FDATE ORDER BY REV DESC";

        //    sql.Parameters.Add(new SqlParameter("@RUN_CODE", _RunProcess.ToString("yyyyMMdd")));
        //    sql.Parameters.Add(new SqlParameter("@SDATE", _StartDate.ToString("yyyyMMdd")));
        //    sql.Parameters.Add(new SqlParameter("@FDATE", _EndDate.ToString("yyyyMMdd")));
        //    DataTable dt = dbSCM.Query(sql);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        res.Add(new DoHistory()
        //        {
        //            Id = Convert.ToInt32(dr["ID"].ToString()),
        //            RunningCode = dr["RUNNING_CODE"].ToString(),
        //            Rev = Convert.ToInt16(dr["REV"].ToString()),
        //            Partno = dr["PARTNO"].ToString().Trim(),
        //            DateVal = dr["DATE_VAL"].ToString(),
        //            Stock = Convert.ToDouble(dr["STOCK"].ToString()),
        //            PlanVal = Convert.ToDouble(dr["PLAN_VAL"].ToString()),
        //            DoVal = Convert.ToDouble(dr["DO_VAL"].ToString()),
        //            StockVal = Convert.ToDouble(dr["STOCK_VAL"].ToString()),
        //            VdCode = dr["VD_CODE"].ToString()
        //        });
        //    }
        //    return res;
        //}
        //private double _FindPoVal(string? partNo, DateTime loopDate)
        //{
        //    double _PoVal = 0;
        //    try
        //    {
        //        DataRow[] _DrPo = _PO.Select("PARTNO = '" + partNo + "' AND DELYMD = '" + loopDate.ToString("yyyyMMdd") + "'");
        //        if (_DrPo.Count() > 0)
        //        {
        //            _PoVal = Convert.ToDouble(_DrPo[0]["WHBLBQTY"].ToString());
        //        }
        //        return _PoVal;
        //    }
        //    catch
        //    {
        //        return _PoVal;
        //    }
        //}
        //private double _CAL_DO(string? _PartNo, DateTime _Date, double _PlanNow, DateTime lastDateHaveHistory, bool CalDoWhenStockWhenMinus = false)
        //{
        //    double _DoVal = 0;
        //    try
        //    {
        //        if (_StockSimBalance >= _PlanNow)
        //        {
        //            _StockSimBalance = _StockSimBalance - _PlanNow;
        //        }
        //        else
        //        {
        //            double _Target = Math.Abs(_StockSimBalance - _PlanNow);
        //            int PDLT = 0;
        //            int BOX = 0;
        //            if (_PartMstr.Count() > 0)
        //            {
        //                PDLT = int.Parse(_PartMstr[0]["PDLT"].ToString());
        //                BOX = int.Parse(_PartMstr[0]["BOX_QTY"].ToString());
        //                _BoxCap = BOX;
        //            }
        //            if (PDLT == 0)
        //            {
        //                _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //                _StockSimBalance = _DoVal - _Target;
        //            }
        //            else
        //            {
        //                DateTime _DateNow = _Date.AddDays(-PDLT);
        //                if (_DateNow < _RunProcess)
        //                {
        //                    _DateNow = _RunProcess;
        //                }
        //                string _CalendarDay = _DateNow.ToString("ddd").ToUpper();
        //                bool _CanDelivery = true;
        //                var dtHoliday = _HOLIDAY.FirstOrDefault(x => x.Code == _DateNow.ToString("yyyyMMdd")); // ถ้าดูจากวันที่จัดส่ง ถ้าส่งจะมาเช็ควันหยุด(TB:DO_DictMstr) อีกที
        //                if (dtHoliday != null)
        //                {
        //                    _CanDelivery = false;
        //                }
        //                else
        //                {
        //                    _CanDelivery = Convert.ToBoolean(_PartMstr[0]["VD_" + _CalendarDay].ToString());
        //                }
        //                bool _Once = true;
        //                if (lastDateHaveHistory == _RunProcess || CalDoWhenStockWhenMinus || _HISTORY.Count == 0) // ระบบจะคำนวนยอด D/O เมื่อไม่มีประวัติหรือระบุว่าให้คำนวนตลอด
        //                {
        //                    if (_DateNow > _RunProcess)
        //                    {
        //                        while (!_CanDelivery)
        //                        {
        //                            _DateNow = _DateNow.AddDays(-1);
        //                            _CalendarDay = _DateNow.ToString("ddd").ToUpper();
        //                            dtHoliday = _HOLIDAY.FirstOrDefault(x => x.Code == _DateNow.ToString("yyyyMMdd")); // ถ้าดูจากวันที่จัดส่ง ถ้าส่งจะมาเช็ควันหยุด(TB:DO_DictMstr) อีกที
        //                            if (dtHoliday != null)
        //                            {
        //                                _CanDelivery = false;
        //                            }
        //                            else
        //                            {
        //                                if (Convert.ToBoolean(_PartMstr[0]["VD_DAY" + _DateNow.Day].ToString()))
        //                                {
        //                                    _CanDelivery = true;

        //                                }
        //                                else
        //                                {
        //                                    _CanDelivery = Convert.ToBoolean(_PartMstr[0]["VD_" + _CalendarDay].ToString());
        //                                }
        //                            }
        //                        }
        //                        if (!_CanDelivery)
        //                        {
        //                            _StockSimBalance = _StockSimBalance - _PlanNow;
        //                        }

        //                        if (_DateNow > _RunProcess)
        //                        {
        //                            DataRow[] _PlanAdjPdLeadTime = dtResponse.Select(" PART='" + _PartNo + "' AND DATE = '" + _DateNow.ToString("yyyyMMdd") + "'  ");
        //                            var PartData = FindPartInDt(_PartNo, _DateNow);
        //                            //int _IndexOfStart = dtResponse.Rows.IndexOf(_PlanAdjPdLeadTime[0]);
        //                            //int _IndexOfLoop = dtResponse.Rows.IndexOf(_PlanAdjPdLeadTime[0]);
        //                            int _IndexOfLoop = PartData.index;
        //                            while (_DateNow < _Date && _CanDelivery)
        //                            {
        //                                if (_Once)
        //                                {

        //                                    _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //                                    dtResponse.Rows[_IndexOfLoop]["DO"] = Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["DO"]) + _DoVal;
        //                                    dtResponse.Rows[_IndexOfLoop]["STOCKSIM"] = Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["STOCKSIM"]) + _DoVal;
        //                                    _Box = Convert.ToInt32(dtResponse.Rows[_IndexOfLoop]["DO"]) / BOX;
        //                                    dtResponse.Rows[_IndexOfLoop]["BOX"] = _Box;
        //                                    _StockSimBalance = Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["STOCKSIM"]);
        //                                    _Once = false;
        //                                    _DoVal = 0;
        //                                }
        //                                else
        //                                {
        //                                    dtResponse.Rows[_IndexOfLoop]["DO"] = 0;
        //                                    dtResponse.Rows[_IndexOfLoop]["STOCKSIM"] = _StockSimBalance - Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["PLAN"]);
        //                                    dtResponse.Rows[_IndexOfLoop]["BOX"] = 0;
        //                                    _Box = 0;
        //                                    _StockSimBalance = Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["STOCKSIM"]);
        //                                    if (_StockSimBalance < 0)
        //                                    {
        //                                        _Target = Math.Abs(_StockSimBalance);
        //                                        _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //                                        _StockSimBalance = (Convert.ToDouble(dtResponse.Rows[_IndexOfLoop - 1]["STOCKSIM"]) + _DoVal) - Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["PLAN"]);
        //                                        dtResponse.Rows[_IndexOfLoop]["DO"] = _DoVal;
        //                                        dtResponse.Rows[_IndexOfLoop]["STOCKSIM"] = _StockSimBalance;
        //                                        dtResponse.Rows[_IndexOfLoop]["BOX"] = _DoVal / BOX;
        //                                    }
        //                                }
        //                                _DateNow = _DateNow.AddDays(1);
        //                                _IndexOfLoop = _IndexOfLoop + 1;
        //                            }
        //                            //_StockSimBalance -= _PlanNow;
        //                            _StockSimBalance = (_StockSimBalance + _DoVal) - _PlanNow;
        //                        }
        //                        else if (_DateNow == _RunProcess && _Date != _RunProcess)
        //                        {
        //                            _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //                            DataRow[] content = dtResponse.Select(" PART='" + _PartNo + "' AND DATE = '" + _DateNow.ToString("yyyyMMdd") + "'  ");
        //                            int _IndexOfLoop = dtResponse.Rows.IndexOf(content[0]);
        //                            int _Stock = int.Parse(dtResponse.Rows[_IndexOfLoop]["STOCKSIM"].ToString());
        //                            _StockSimBalance = _Stock;
        //                            while (_DateNow < _Date && _CanDelivery)
        //                            {
        //                                if (_DateNow == _RunProcess)
        //                                {
        //                                    _DoVal = Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["DO"].ToString()) + _DoVal;
        //                                    _StockSimBalance = _DoVal - Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["PLAN"]);
        //                                }
        //                                else
        //                                {
        //                                    if ((_StockSimBalance - Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["PLAN"])) == -672)
        //                                    {
        //                                        Console.WriteLine("213");
        //                                    }
        //                                    _DoVal = 0;
        //                                    _StockSimBalance = _StockSimBalance - Convert.ToDouble(dtResponse.Rows[_IndexOfLoop]["PLAN"]);
        //                                }
        //                                dtResponse.Rows[_IndexOfLoop]["DO"] = _DoVal;
        //                                dtResponse.Rows[_IndexOfLoop]["STOCKSIM"] = _StockSimBalance;
        //                                dtResponse.Rows[_IndexOfLoop]["BOX"] = _DoVal / BOX;
        //                                _IndexOfLoop++;
        //                                _DateNow = _DateNow.AddDays(1);
        //                            }
        //                            _StockSimBalance -= _PlanNow;
        //                            _DoVal = 0;
        //                        }
        //                        if (_Date == _RunProcess)
        //                        {
        //                            _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //                            _StockSimBalance = (_StockSimBalance + _DoVal) - _PlanNow;
        //                        }
        //                    }
        //                    else // กรณีวันก่อนหน้าที่มากกว่า RunProcess ไม่มีวันไหนสามารถส่งได้ จะต้องvอก D/O ในวันนั้นเลย
        //                    {
        //                        _DoVal = _FIND_DO_VAL(_PartNo, _Target);
        //                        _StockSimBalance = (_StockSimBalance + _DoVal) - _PlanNow;
        //                    }
        //                }
        //                else
        //                {
        //                    _DoVal = 0;
        //                    _StockSimBalance = (_StockSimBalance) - _PlanNow;
        //                }
        //            }
        //        }
        //        return _DoVal;
        //    }
        //    catch
        //    {
        //        return _DoVal;
        //    }
        //}

        //private double _FIND_DO_VAL(string _PartNo, double Plan)
        //{
        //    try
        //    {
        //        double _DoVal = 0;
        //        if (Plan > 0)
        //        {
        //            _DoVal = Math.Ceiling(Plan / Convert.ToDouble(_PartMstr[0]["BOX_QTY"].ToString())) * Convert.ToDouble(_PartMstr[0]["BOX_QTY"].ToString());
        //            _DoVal = _CheckMinDelivery(_DoVal, Convert.ToDouble(_PartMstr[0]["BOX_MIN"].ToString()));
        //            _DoVal = _CheckMaxDelivery(_DoVal, Convert.ToDouble(_PartMstr[0]["BOX_MAX"].ToString()));
        //            _Box = Convert.ToInt32(_DoVal) / Convert.ToInt32(_PartMstr[0]["BOX_QTY"].ToString());
        //        }
        //        return _DoVal;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}
        //private double _CheckMaxDelivery(double doVal, double maxQty)
        //{
        //    try
        //    {
        //        double _DoValByMax = 0;
        //        if (doVal > maxQty)
        //        {
        //            _DoValByMax = maxQty;
        //        }
        //        else
        //        {
        //            _DoValByMax = doVal;
        //        }
        //        return _DoValByMax;
        //    }
        //    catch
        //    {
        //        return doVal;
        //    }
        //}

        //private double _CheckMinDelivery(double doVal, double minQty)
        //{
        //    double _DoValByMin = doVal;
        //    try
        //    {
        //        if (doVal <= minQty)
        //        {
        //            _DoValByMin = minQty;
        //        }
        //        return _DoValByMin;
        //    }
        //    catch
        //    {
        //        return _DoValByMin;
        //    }
        //}

        //private double _FindPlanVal(string _PartNo, DataRow[] drPlan, DateTime _LoopDate)
        //{
        //    try
        //    {
        //        double _PlanVal = 0;
        //        DataRow[] dtPlanExist = _PLAN_HISTORY.Select(" PART_NO = '" + _PartNo + "' AND YMD = '" + _LoopDate.ToString("yyyyMMdd") + "'", "RUNNING_CODE DESC");
        //        if (dtPlanExist.Count() > 0) // ถ้ามี Plan อยู่แล้ว
        //        {
        //            _PlanVal = Convert.ToDouble(dtPlanExist[0]["PLAN_ACT"]);
        //        }
        //        else
        //        {
        //            _PlanVal = Convert.ToDouble(drPlan[0]["CONSUMPTION"]);
        //        }
        //        return _PlanVal;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}

        //private double _SetPickListVal(DataRow[] drPickList)
        //{
        //    try
        //    {
        //        return Convert.ToDouble(drPickList[0]["FQTY"].ToString());
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}

        //private DataTable GetPickLists(string? partCode, DateTime startProcess, DateTime endProcess)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = "SELECT PARTNO,CAST(IDATE AS DATE) AS IDATE,SUM(FQTY) AS FQTY FROM [dbSCM].[dbo].[AL_GST_DATPID] WHERE PARTNO = @partCode AND PRGBIT IN ('F','C') AND IDATE >= @startProcess AND IDATE <= @endProcess GROUP BY PARTNO,IDATE";
        //    sql.Parameters.Add(new SqlParameter("partCode", partCode));
        //    sql.Parameters.Add(new SqlParameter("startProcess", startProcess.Date));
        //    sql.Parameters.Add(new SqlParameter("endProcess", endProcess.Date));
        //    DataTable dt = dbSCM.Query(sql);
        //    return dt;
        //}


        //private DataTable _GET_PO(DateTime _StartDate, DateTime _EndDate, string _VdCode)
        //{
        //    OracleCommand cmd = new();
        //    cmd.CommandText = @"SELECT A.PARTNO,A.DELYMD,SUM(A.WHBLBQTY) AS WHBLBQTY FROM GST_DATOSD A LEFT JOIN GST_DATOSC B ON A.PONO = B.PONO WHERE  A.apbit in ('U','P') AND TRIM(A.PARTNO) IN ('" + _PART_JOIN_STRING + "') AND B.HTCODE = '" + _VdCode + "' AND A.DELYMD >= '" + _StartDate.ToString("yyyyMMdd") + "' AND A.DELYMD <= '" + _EndDate.ToString("yyyyMMdd") + "' GROUP BY A.PARTNO,A.DELYMD";
        //    DataTable dt = dbAlpha.Query(cmd);
        //    return dt;
        //}

        //public DataTable _GET_STOCK(DateTime _DateRunProcess, string PARTS = "")
        //{
        //    string _Year = _DateRunProcess.Year.ToString();
        //    string _Month = _DateRunProcess.Month.ToString("00");
        //    string date = _Year + "" + _Month;
        //    OracleCommand cmd = new();
        //    PARTS = PARTS != "" ? PARTS : _PART_JOIN_STRING;
        //    //WHERE TRIM(YM) = :YM AND TRIM(PARTNO) LIKE '%' || :PART_NO || '%'  AND CM LIKE '%'
        //    //cmd.CommandText = "SELECT YM, PARTNO, WBAL FROM DST_DATMC2 WHERE TRIM(YM) = :PL_DATE AND TRIM(PARTNO) = :PARTNO AND TRIM(WHNO) IN('W1')";
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
        //    return dt;
        //}

        //public double getPickList(string partno, string date)
        //{
        //    double valPickList = 0;
        //    SqlCommand sql = new();
        //    sql.CommandText = "SELECT SUM(FQTY) AS FQTY FROM [dbSCM].[dbo].[AL_GST_DATPID] WHERE  IDATE = @PICKLIST_DATE AND PARTNO = @PARTNO AND PRGBIT IN ('F','C')";
        //    sql.Parameters.Add(new SqlParameter("@PICKLIST_DATE", date));
        //    sql.Parameters.Add(new SqlParameter("@PARTNO", partno));
        //    DataTable dt = dbSCM.Query(sql);
        //    if (dt.Rows.Count > 0)
        //    {
        //        valPickList = dt.Rows[0]["FQTY"].ToString() != "" ? double.Parse(dt.Rows[0]["FQTY"].ToString()) : 0;
        //    }
        //    return valPickList;
        //}


        //[HttpGet]
        //[Route("/getSupplier/{buyer}")]
        //public IActionResult getSupplier(string buyer = "")
        //{
        //    var Suppliers = _contextDBSCM.AlVendors.Where(x => x.Route == "D").ToList();
        //    SqlCommand sql = new SqlCommand();
        //    DataTable dt = new DataTable();
        //    if (buyer != "" && buyer != "all")
        //    {
        //        buyer = (buyer != "" && buyer != "all") ? (" AND (A.CODE = '" + buyer + "')") : "";
        //        sql.CommandText = @"SELECT A.*,B.* FROM DO_DictMstr A LEFT JOIN  [dbSCM].[dbo].[DO_VENDER_MASTER] B ON A.REF_CODE = B.VD_CODE WHERE (A.DICT_TYPE = 'BUYER')" + buyer;
        //        dt = dbSCM.Query(sql);
        //    }
        //    else
        //    {
        //        sql.CommandText = "SELECT DISTINCT REF_CODE FROM DO_DictMstr WHERE (DICT_TYPE = 'BUYER')";
        //        DataTable dtDistinct = dbSCM.Query(sql);
        //        foreach (DataRow dr in dtDistinct.Rows)
        //        {
        //            string vdCode = dr["REF_CODE"].ToString();
        //            SqlCommand sqlVender = new SqlCommand();
        //            sqlVender.CommandText = @"SELECT TOP(1) A.*,B.* FROM DO_DictMstr A LEFT JOIN   [dbSCM].[dbo].[DO_VENDER_MASTER] B ON A.REF_CODE = B.VD_CODE WHERE (A.DICT_TYPE = 'BUYER' AND A.REF_CODE = '" + vdCode + "')";
        //            DataTable dtVender = dbSCM.Query(sqlVender);
        //            dt.Merge(dtVender);
        //        }
        //    }
        //    return Ok(ConvertDataTableasJSON(dt));
        //}

        //[HttpGet]
        //[Route("/RunDo/{carehistory}")]
        //public ActionResult RunDo(bool carehistory = true)
        //{
        //    MGetPlanDataSet _PlanDataSet = GetPlanDataSet("", "41256", true); // carehistory = false ออกแผนใหม่ ไม่สนใจประวัติการแก้ไข D/O ก่อนหน้า
        //    string dtRunProcess = DateTime.Now.ToString("yyyyMMdd");
        //    //string dtEndFixed = DateTime.Now.AddDays(14).ToString("yyyyMMdd");
        //    int _Rev = 1;
        //    string _RunningCode = "";
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_HISTORY] WHERE RUNNING_CODE = @RUN_CODE AND REVISION = 999 ORDER BY REV DESC";
        //    sql.Parameters.Add(new SqlParameter("@RUN_CODE", dtRunProcess));
        //    DataTable dt = dbSCM.Query(sql);
        //    if (dt.Rows.Count > 0)
        //    {
        //        _Rev = Convert.ToInt16(dt.Rows[0]["REV"].ToString()) + 1;
        //        _RunningCode = dt.Rows[0]["RUNNING_CODE"].ToString();
        //    }
        //    SqlCommand sqlUpdate = new SqlCommand();
        //    //sqlUpdate.CommandText = @"UPDATE [dbo].[DO_HISTORY] SET [REVISION] = @REVISION WHERE RUNNING_CODE = @RUNNING_CODE AND REV < @REV AND (DATE_VAL >= @DATE_S AND DATE_VAL < @DATE_F)";
        //    sqlUpdate.CommandText = @"UPDATE [dbo].[DO_HISTORY] SET [REVISION] = 1 WHERE REVISION = 999";
        //    //sqlUpdate.Parameters.Add(new SqlParameter("@REV", _Rev));
        //    //sqlUpdate.Parameters.Add(new SqlParameter("@DATE_S", new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0)));
        //    //sqlUpdate.Parameters.Add(new SqlParameter("@DATE_F", new DateTime(_EndProcess.Year, _EndProcess.Month, _EndProcess.Day, 23, 59, 59)));
        //    dbSCM.Query(sqlUpdate);
        //    foreach (MGetPlan item in _PlanDataSet.data)
        //    {
        //        //if (DateTime.ParseExact(item.date, "yyyyMMdd", null) >= DateTime.ParseExact(dtRunProcess, "yyyyMMdd", null) && DateTime.ParseExact(item.date, "yyyyMMdd", null) < DateTime.ParseExact(dtEndFixed, "yyyyMMdd", null))
        //        //{
        //        DoHistory itemHistory = new DoHistory()
        //        {
        //            RunningCode = dtRunProcess,
        //            Rev = _Rev,
        //            Partno = item.part,
        //            DateVal = item.date,
        //            PlanVal = Convert.ToDouble(item.plan),
        //            DoVal = Convert.ToDouble(item.doPlan),
        //            StockVal = Convert.ToDouble(item.stockSim),
        //            Stock = Convert.ToDouble(item.stock),
        //            InsertDt = DateTime.Now,
        //            InsertBy = "41256",
        //            Status = "pending",
        //            VdCode = item.vdCode,
        //            Revision = 999,
        //            TimeScheduleDelivery = item.timeScheduleDelivery
        //        };
        //        _contextDBSCM.Add(itemHistory);
        //        //}
        //    }
        //    int res = _contextDBSCM.SaveChanges();
        //    return Ok(new { RunningCode = dtRunProcess + "" + _Rev.ToString("D3") });
        //}

        //private string ConvertDataTableasJSON(DataTable dataTable)
        //{
        //    return JsonConvert.SerializeObject(dataTable);
        //}

        //[HttpPost]
        //[Route("/data/stock")]
        //public ActionResult DATA_STOCK_PAGE([FromBody] M_PO_DATA obj)
        //{
        //    DataTable dtParts = Methods.GetListPartBySupplier(obj.vender, obj.startDate);
        //    DataTable response = Methods.GetPlan(obj);
        //    foreach (DataRow dr in dtParts.Rows)
        //    {
        //        string? partno = dr["PARTNO"].ToString().Trim();
        //        DataRow[] parts = response.Select("PARTNO = '" + partno + "'");
        //        if (parts.Count() > 0)
        //        {
        //            dr["CM"] = parts[0]["CM"].ToString().Trim();
        //            dr["PLAN_QTY"] = Convert.ToDouble(parts[0]["PLAN_QTY"].ToString());
        //            dr["PERIOD_START"] = parts[0]["PERIOD_START"].ToString();
        //            dr["PERIOD_END"] = parts[0]["PERIOD_END"].ToString();
        //            dr["BOX_QTY"] = parts[0]["BOX_QTY"].ToString();
        //            dr["UNIT"] = parts[0]["UNIT"].ToString();
        //        }
        //        DataTable dtStock = _GET_STOCK(DateTime.ParseExact(obj.startDate, "yyyyMMdd", null), partno);
        //        DataRow[] stocks = dtStock.Select(" PARTNO = '" + partno + "' AND CM = '" + dr["CM"] + "' ");
        //        if (stocks.Count() > 0)
        //        {
        //            dr["STOCK"] = stocks[0]["WBAL"].ToString();
        //            if (Convert.ToDouble(dr["STOCK"]) > 0 && Convert.ToDouble(dr["PLAN_QTY"]) > 0)
        //            {
        //                dr["STOCK_PERCENT"] = (Convert.ToDouble(dr["STOCK"]) / Convert.ToDouble(dr["PLAN_QTY"])) * 100;
        //            }
        //        }
        //        dr["STOCK_PERCENT"] = dr["STOCK_PERCENT"].ToString() != "" ? Math.Round(Convert.ToDouble(dr["STOCK_PERCENT"]), 2).ToString("0.00") : 0;
        //    }
        //    DataView dv = new DataView(dtParts);
        //    dv.Sort = "STOCK_PERCENT " + obj.sort;
        //    response = dv.ToTable();
        //    return Ok(ConvertDataTableasJSON(response));
        //}

        //[HttpPost]
        //[Route("/data/po")]
        //public IActionResult GetDataPO([FromBody] M_PO_DATA param)
        //{
        //    DataTable dt = Methods.GetListPartBySupplier(param.vender, param.startDate);
        //    DataTable dtResp = Methods.GetPlan(param);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        string _Part = dr["PARTNO"].ToString().Trim();
        //        dr["CM"] = "";
        //        dr["PLAN_QTY"] = 0;
        //        DataRow[] dtPlan = dtResp.Select("PARTNO = '" + _Part + "'");
        //        if (dtPlan.Count() > 0)
        //        {
        //            dr["CM"] = dtPlan[0]["CM"].ToString().Trim();
        //            dr["PLAN_QTY"] = Convert.ToDouble(dtPlan[0]["PLAN_QTY"].ToString());
        //            dr["PERIOD_START"] = dtPlan[0]["PERIOD_START"].ToString();
        //            dr["PERIOD_END"] = dtPlan[0]["PERIOD_END"].ToString();
        //            dr["BOX_QTY"] = dtPlan[0]["BOX_QTY"].ToString();
        //            dr["UNIT"] = dtPlan[0]["UNIT"].ToString();
        //        }
        //        DataTable _PoItem = Methods.GetPo(_Part, param.startDate, param.endDate);
        //        dr["PO_PERCENT"] = 0;
        //        dr["PO"] = 0;
        //        dr["STOCK_PERCENT"] = 0;
        //        dr["STOCK"] = 0;
        //        if (_PoItem.Rows.Count > 0)
        //        {
        //            dr["PO"] = _PoItem.Rows[0]["WHBLBQTY"];
        //            if (Convert.ToDouble(dr["PO"]) > 0 && Convert.ToDouble(dr["PLAN_QTY"]) > 0)
        //            {
        //                dr["PO_PERCENT"] = (Convert.ToDouble(dr["PO"]) / Convert.ToDouble(dr["PLAN_QTY"])) * 100;
        //            }
        //        }
        //        DataTable _Stock = _GET_STOCK(DateTime.ParseExact(param.startDate, "yyyyMMdd", null), _Part);
        //        DataRow[] _FindStock = _Stock.Select(" PARTNO = '" + _Part + "' AND CM = '" + dr["CM"] + "' ");
        //        if (_FindStock.Count() > 0)
        //        {
        //            dr["STOCK"] = _FindStock[0]["WBAL"].ToString();
        //            if (Convert.ToDouble(dr["STOCK"]) > 0 && Convert.ToDouble(dr["PLAN_QTY"]) > 0)
        //            {
        //                dr["STOCK_PERCENT"] = (Convert.ToDouble(dr["STOCK"]) / Convert.ToDouble(dr["PLAN_QTY"])) * 100;
        //            }
        //        }
        //        dr["STOCK_PERCENT"] = Math.Round(Convert.ToDouble(dr["STOCK_PERCENT"]), 2).ToString("0.00");
        //        dr["PO_PERCENT"] = Math.Round(Convert.ToDouble(dr["PO_PERCENT"]), 2).ToString("0.00");
        //    }
        //    DataView dv = new DataView(dt);
        //    dv.Sort = "PO_PERCENT " + param.sort;
        //    dtResp = dv.ToTable();
        //    return Ok(ConvertDataTableasJSON(dtResp));
        //}

        [HttpPost]
        [Route("/do/update")]
        public IActionResult DO_UPDATE([FromBody] M_DO_UPDATE param)
        {
            if (param.empCode != "41256")
            {
                return Ok(new { status = false, message = "ปิดระบบการแก้ไขชั่วคราว ติดต่อ IT 611 เบียร์" });
            }
            double? stock = 0;
            double? _do = 0;
            int status = 1;
            var dataAfterUpdate = (dynamic)null;
            string message = "";
            double? DoEdit = Convert.ToInt32(param.doEdit);
            var PartData = _contextDBSCM.DoHistories.Where(x => x.Id == param.id).FirstOrDefault();
            if (param.doEdit < PartData.DoVal)
            {
                DateTime DateStart = DateTime.ParseExact(PartData.RunningCode, "yyyyMMdd", null);
                DateTime DateLoop = DateTime.ParseExact(PartData.DateVal, "yyyyMMdd", null);
                DateTime FixedDate = DateStart.AddDays(7);
                bool firstTime = true;
                if (DateLoop >= DateStart && DateLoop <= FixedDate)
                {
                    double StockSim = 0;
                    double PlanVal = (double)PartData.PlanVal;
                    double doVal = 0;
                    //doVal = (double)DoEdit - (double)doVal;
                    if (firstTime)
                    {
                        double diffDO = (double)DoEdit - (double)PartData.DoVal;
                        StockSim = (double)PartData.StockVal;
                        StockSim = StockSim - Math.Abs(diffDO);
                        firstTime = false;
                    }
                    while (DateLoop <= FixedDate)
                    {
                        if (StockSim < 0)
                        {
                            status = 0;
                            message = "ไม่สามารถลบจำนวน D/O ได้ เนื่องจาก ทำให้ยอดติดลบภายในช่วงวันที่ระบบกำหนดไว้";
                            break;
                        }
                        DateLoop = DateLoop.AddDays(1);
                        PartData = _contextDBSCM.DoHistories.Where(x => x.DateVal == DateLoop.ToString("yyyyMMdd") && x.RunningCode == DateStart.ToString("yyyyMMdd") && x.Rev == PartData.Rev && x.Partno == param.partNo).FirstOrDefault();
                        PlanVal = (double)PartData.PlanVal;
                        double DoVal = (double)PartData.DoVal;
                        StockSim = (StockSim + (double)DoVal) - PlanVal;
                    }
                }
            }
            if (status != 0)
            {
                var context = _contextDBSCM.DoHistories.Where(x => x.Id == param.id).FirstOrDefault();
                if (context != null)
                {
                    SqlCommand sql = new SqlCommand();
                    sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_HISTORY] WHERE RUNNING_CODE = @RUNNING_CODE AND REV = @REV AND PARTNO = @PARTNO AND REVISION = 999 ORDER BY ID ASC";
                    sql.Parameters.Add(new SqlParameter("@RUNNING_CODE", context.RunningCode));
                    sql.Parameters.Add(new SqlParameter("@REV", context.Rev));
                    sql.Parameters.Add(new SqlParameter("@PARTNO", context.Partno));
                    DataTable dt = dbSCM.Query(sql);
                    foreach (DataRow dr in dt.Rows)
                    {
                        int? Id = Convert.ToInt32(dr["ID"].ToString());
                        int? Rev = Convert.ToInt32(dr["REV"].ToString());
                        double? PlanVal = Convert.ToDouble(dr["PLAN_VAL"].ToString());
                        double? StockVal = Convert.ToDouble(dr["STOCK_VAL"].ToString());
                        double? StockDefault = Convert.ToDouble(dr["STOCK"].ToString());
                        double? DoVal = Convert.ToDouble(dr["DO_VAL"].ToString());
                        string? RunningCode = dr["RUNNING_CODE"].ToString();
                        string? DateVal = dr["DATE_VAL"].ToString();
                        string? PartNo = dr["PARTNO"].ToString();
                        //double? DoEdit = Convert.ToInt32(param.doEdit);
                        if (Id == param.id || Id > param.id)
                        {
                            if (Id == param.id && DoEdit == DoVal)
                            {
                                break;
                            }
                            int CountItem = _contextDBSCM.DoHistories.Where(x => x.RunningCode == RunningCode && x.Rev == Rev && x.DateVal == DateVal && x.Partno == PartNo).ToList().Count();
                            SqlCommand sqlUpdate = new SqlCommand();
                            sqlUpdate.CommandText = @"UPDATE [dbo].[DO_HISTORY] SET [REVISION] = @REVISION WHERE ID = @ID";
                            sqlUpdate.Parameters.Add(new SqlParameter("@REVISION", CountItem));
                            sqlUpdate.Parameters.Add(new SqlParameter("@ID", Id));
                            dbSCM.Query(sqlUpdate);
                            if (Id == param.id)
                            {
                                if (DoEdit > DoVal)
                                {
                                    stock = StockVal + (DoEdit - DoVal);
                                }
                                else if (DoEdit < DoVal)
                                {
                                    stock = StockVal;
                                    stock = stock - (DoVal - DoEdit);
                                }
                                _do = DoEdit;
                                SqlCommand sqlInsert = new SqlCommand();
                                sqlInsert.CommandText = @"INSERT INTO [dbo].[DO_HISTORY]([RUNNING_CODE],[REV],[PARTNO],[DATE_VAL],[PLAN_VAL],[DO_VAL],[STOCK_VAL],[STOCK],[VD_CODE],[INSERT_DT],[INSERT_BY],[STATUS],[REVISION]) VALUES (@RUNNING_CODE,@REV,@PARTNO,@DATE_VAL,@PLAN_VAL,@DO_VAL,@STOCK_VAL,@STOCK,@VD_CODE,@INSERT_DATE,@INSERT_BY,@STATUS,@REVISION)";
                                sqlInsert.Parameters.Add(new SqlParameter("@RUNNING_CODE", RunningCode));
                                sqlInsert.Parameters.Add(new SqlParameter("@REV", Rev));
                                sqlInsert.Parameters.Add(new SqlParameter("@PARTNO", PartNo));
                                sqlInsert.Parameters.Add(new SqlParameter("@DATE_VAL", DateVal));
                                sqlInsert.Parameters.Add(new SqlParameter("@PLAN_VAL", dr["PLAN_VAL"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@DO_VAL", _do));
                                sqlInsert.Parameters.Add(new SqlParameter("@STOCK_VAL", stock));
                                sqlInsert.Parameters.Add(new SqlParameter("@STOCK", StockDefault));
                                sqlInsert.Parameters.Add(new SqlParameter("@VD_CODE", dr["VD_CODE"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@INSERT_DATE", DateTime.Now));
                                sqlInsert.Parameters.Add(new SqlParameter("@INSERT_BY", dr["INSERT_BY"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@STATUS", dr["STATUS"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@REVISION", 999));
                                dbSCM.Query(sqlInsert);
                            }
                            else if (Id > param.id)
                            {
                                stock = (stock + DoVal) - PlanVal;
                                SqlCommand sqlInsert = new SqlCommand();
                                sqlInsert.CommandText = @"INSERT INTO [dbo].[DO_HISTORY]([RUNNING_CODE],[REV],[PARTNO],[DATE_VAL],[PLAN_VAL],[DO_VAL],[STOCK_VAL],[STOCK],[VD_CODE],[INSERT_DT],[INSERT_BY],[STATUS],[REVISION]) VALUES (@RUNNING_CODE,@REV,@PARTNO,@DATE_VAL,@PLAN_VAL,@DO_VAL,@STOCK_VAL,@STOCK,@VD_CODE,@INSERT_DATE,@INSERT_BY,@STATUS,@REVISION)";
                                sqlInsert.Parameters.Add(new SqlParameter("@RUNNING_CODE", RunningCode));
                                sqlInsert.Parameters.Add(new SqlParameter("@REV", Rev));
                                sqlInsert.Parameters.Add(new SqlParameter("@PARTNO", PartNo));
                                sqlInsert.Parameters.Add(new SqlParameter("@DATE_VAL", DateVal));
                                sqlInsert.Parameters.Add(new SqlParameter("@PLAN_VAL", dr["PLAN_VAL"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@DO_VAL", DoVal));
                                sqlInsert.Parameters.Add(new SqlParameter("@STOCK_VAL", stock));
                                sqlInsert.Parameters.Add(new SqlParameter("@STOCK", StockDefault));
                                sqlInsert.Parameters.Add(new SqlParameter("@VD_CODE", dr["VD_CODE"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@INSERT_DATE", DateTime.Now));
                                sqlInsert.Parameters.Add(new SqlParameter("@INSERT_BY", dr["INSERT_BY"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@STATUS", dr["STATUS"].ToString()));
                                sqlInsert.Parameters.Add(new SqlParameter("@REVISION", 999));
                                dbSCM.Query(sqlInsert);
                            }
                        }
                    }
                }
                dataAfterUpdate = _contextDBSCM.DoHistories.Where(x => x.Id >= param.id && x.Revision == 999).ToList();
            }
            return Ok(new { status = status, data = dataAfterUpdate, message = message });
        }

        [HttpGet]
        [Route("/do/{id}")]
        public IActionResult GetHistoryById(int id)
        {
            var context = _contextDBSCM.DoHistories.Where(x => x.Id == id).FirstOrDefault();
            return Ok(context);
        }

        [HttpPost]
        [Route("/master/get")]
        public IActionResult getMaster([FromBody] MGetMaster param)
        {
            DataTable dt = new DataTable();
            if (param.type == "part")
            {
                var part = (dynamic)null;
                if (param.vender == "")
                {
                    part = _contextDBSCM.DoPartMasters.ToList();
                }
                else
                {
                    part = _contextDBSCM.DoPartMasters.Where(x => x.VdCode == param.vender).ToList();
                }
                return Ok(part);
            }
            else if (param.type == "vender")
            {
                var venders = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "VENDER_BOX_CAPACITY").OrderBy(x => x.Description).ToList();
                return Ok(venders);
            }
            else
            {
                return Ok();
            }
        }

        [HttpPost]
        [Route("/vender/update/day")]
        public IActionResult UpdateVenderDay([FromBody] M_UPDATE_VENDER_DAY param)
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"UPDATE [dbSCM].[dbo].[DO_VENDER_MASTER] SET VD_" + param.day.ToUpper() + " = " + (param.check ? 1 : 0) + " WHERE VD_CODE = '" + param.vender + "'";
            DataTable dtRes = dbSCM.Query(sql);
            if (param.check == true)
            {
                string SetCondition = "";
                for (int i = 1; i <= 31; i++)
                {
                    SetCondition += " VD_DAY" + i + " = 0 ";
                    if (i < 31)
                    {
                        SetCondition += " , ";
                    }
                }
                Console.WriteLine(SetCondition);
                SqlCommand sqlUpdateDate = new SqlCommand();
                sqlUpdateDate.CommandText = @" UPDATE [dbSCM].[dbo].[DO_VENDER_MASTER] SET " + SetCondition + " WHERE VD_CODE = '" + param.vender + "'";
                DataTable dtUpdateDate = dbSCM.Query(sqlUpdateDate);
            }
            return Ok();
        }

        [HttpPost]
        [Route("/vender/update/date")]
        public IActionResult UpdateVenderDate([FromBody] M_UPDATE_VENDER_DAY param)
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"UPDATE [dbSCM].[dbo].[DO_VENDER_MASTER] SET VD_DAY" + param.date + " = " + (param.check ? 1 : 0) + " WHERE VD_CODE = '" + param.vender + "'";
            DataTable dtRes = dbSCM.Query(sql);
            if (param.check == true)
            {
                SqlCommand sqlUpdateDate = new SqlCommand();
                sqlUpdateDate.CommandText = @" UPDATE [dbSCM].[dbo].[DO_VENDER_MASTER] SET VD_MON = 0,VD_TUE = 0,VD_WED = 0,VD_THU = 0,VD_FRI = 0,VD_SAT = 0,VD_SUN = 0 WHERE VD_CODE = '" + param.vender + "'";
                DataTable dtUpdateDate = dbSCM.Query(sqlUpdateDate);
            }
            return Ok();
        }

        [HttpPost]
        [Route("/vender/update/detail")]
        public IActionResult UpdateVenderDetail([FromBody] M_UPDATE_VENDER_DETAIL param)
        {
            var contentVenderMaster = _contextDBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == param.vender);
            if (contentVenderMaster != null)
            {
                contentVenderMaster.VdMinDelivery = param.min;
                contentVenderMaster.VdMaxDelivery = param.max;
                contentVenderMaster.VdRound = param.round;
                contentVenderMaster.VdTimeScheduleDelivery = param.timeSchedule;
            }
            List<DoMaster> contentRound = _contextDBSCM.DoMasters.Where(x => x.VdCode == param.vender).ToList();
            contentRound.ForEach(x => { x.VdRound = param.round; });
            int active = _contextDBSCM.SaveChanges();
            return Ok(new { update = active });
        }

        [HttpGet]
        [Route("/vender/get/{vender}")]
        public IActionResult GetVenderByCode(string vender)
        {
            var content = _contextDBSCM.DoVenderMasters.Where(x => x.VdCode == vender).FirstOrDefault();
            return Ok(content);
        }

        [HttpPost]
        [Route("/part/get")]
        public IActionResult MasterGetPart([FromBody] M_MASTER_GET_PART param)
        {
            var content = _contextDBSCM.DoPartMasters.Where(x => x.Partno == param.part).FirstOrDefault();
            return Ok(content);
        }

        [HttpPost]
        [Route("/part/update")]
        public IActionResult MasterPartUpdate([FromBody] DoPartMaster param)
        {
            var content = _contextDBSCM.DoPartMasters.FirstOrDefault(x => x.Partno == param.Partno);
            if (content != null)
            {
                content.BoxQty = param.BoxQty;
                content.BoxMin = param.BoxMin;
                content.BoxMax = param.BoxMax;
                content.Pdlt = param.Pdlt;
                content.Unit = param.Unit;
                _contextDBSCM.Update(content);
            }
            int update = _contextDBSCM.SaveChanges();
            return Ok(new { update = update });
        }


        [HttpGet]
        [Route("/vender/debug")]
        public IActionResult VenderDebug()
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = "SELECT A.VD_CODE,B.VenderName FROM [dbSCM].[dbo].[DO_VENDER_MASTER] A LEFT JOIN [dbSCM].[dbo].[AL_Vendor] B ON A.VD_CODE = B.Vender";
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                SqlCommand sqlUpdate = new SqlCommand();
                sqlUpdate.CommandText = "UPDATE [dbSCM].[dbo].[DO_VENDER_MASTER] SET VD_DESC = '" + dr["VenderName"] + "' WHERE VD_CODE ='" + dr["VD_CODE"] + "'";
                dbSCM.Query(sqlUpdate);
            }
            return Ok();
        }

        [HttpGet]
        [Route("/license/update/expire")]
        public IActionResult LicenseUpdateExpire()
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = "SELECT * FROM [dbSCM].[dbo].[SKC_LicenseTraining] WHERE DICT_CODE = 'LIC107' AND EXPIRED_DATE < GETDATE()";
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                string trId = dr["TR_ID"].ToString();
                DateTime eff = DateTime.Parse(dr["EFFECTIVE_DATE"].ToString());
                DateTime expire = DateTime.Parse(dr["EXPIRED_DATE"].ToString());
                eff = new DateTime(DateTime.Now.Year, eff.Month, eff.Day, eff.Hour, eff.Minute, eff.Second);
                expire = new DateTime(DateTime.Now.AddYears(1).Year, expire.Month, expire.Day, expire.Hour, expire.Minute, expire.Second);
                SqlCommand sqlUpdate = new SqlCommand();
                sqlUpdate.CommandText = "UPDATE [dbSCM].[dbo].[SKC_LicenseTraining] SET EFFECTIVE_DATE = '" + eff.ToString("yyyy/MM/dd HH:mm:ss") + "' ,EXPIRED_DATE = '" + expire.ToString("yyyy/MM/dd HH:mm:ss") + "' WHERE TR_ID = '" + trId + "'";
                dbSCM.Query(sqlUpdate);
            }
            return Ok();
        }

        //public double NextDouble(Random rnd, double min, double max)
        //{
        //    return rnd.NextDouble() * (max - min) + min;
        //}

        [HttpGet]
        [Route("/building/simulate/andnoboard/edit")]
        public IActionResult BuildingSimulateAndnoboardEdit()
        {
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[Building_CycleTimeLog] 
WHERE BoardId = 303 AND InsertDate >= '2023-09-07 19:08:00' AND Insertdate <= '2023-09-07 19:20:00' ORDER BY InsertDate ASC";
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                string boardId = dr["BoardId"].ToString();
                string boardCode = dr["BoardCode"].ToString();
                DateTime insertDate = DateTime.Parse(dr["InsertDate"].ToString());
                double dtime = double.Parse(dr["CycleTime"].ToString());
                double newdt = dtime = dtime + new Random().Next(30, 38);
                SqlCommand sqlUpdate = new SqlCommand();
                sqlUpdate.CommandText = @"UPDATE [dbo].[Building_CycleTimeLog]  SET CycleTime = @CycleTime WHERE ID = @ID";
                sqlUpdate.Parameters.Add(new SqlParameter("ID", dr["ID"].ToString()));
                sqlUpdate.Parameters.Add(new SqlParameter("CycleTime", newdt));
                dbSCM.Query(sqlUpdate);
            }
            return Ok();
        }

        [HttpGet]
        [Route("/building/simulate/andnoboard")]
        public IActionResult BuildingSimulateAndnoboard()
        {
            Dictionary<string, string> listBoard = new Dictionary<string, string>();
            listBoard.Add("303", "3AFI10");
            listBoard.Add("301", "3AMA5");
            listBoard.Add("302", "3AMA5");
            listBoard.Add("312", "3ACP5");
            listBoard.Add("310", "3AST3");
            listBoard.Add("330", "3ARO6");
            listBoard.Add("306", "3MCS9");
            listBoard.Add("308", "3MPI11");
            listBoard.Add("305", "3MCY12");
            listBoard.Add("307", "3MFH5");
            listBoard.Add("304", "3MRH5");
            listBoard.Add("309", "3MLU3");
            foreach (KeyValuePair<string, string> item in listBoard)
            {
                string boardId = item.Key;
                string boardCode = item.Value;
                double cycletime = 10.5;
                DateTime sDate = new DateTime(2023, 9, 7, 8, 0, 0);
                DateTime fDate = new DateTime(2023, 9, 7, 20, 0, 0);
                while (sDate <= fDate)
                {
                    double cyctimeMinus = cycletime - new Random().Next(1, 3);
                    int second = int.Parse(cyctimeMinus.ToString().Substring(0, 1));
                    int mini = int.Parse(cyctimeMinus.ToString().Substring(2, 1));
                    sDate = sDate.AddSeconds(second);
                    sDate = sDate.AddMilliseconds(mini);
                    SqlCommand sqlInsert = new SqlCommand();
                    sqlInsert.CommandText = @"INSERT INTO [dbo].[Building_CycleTimeLog] ([BoardId],[BoardCode],[CycleTime],[InsertDate]) VALUES  (@BoardId,@BoardCode,@CycleTime,@InsertDate)";
                    sqlInsert.Parameters.Add(new SqlParameter("BoardId", boardId));
                    sqlInsert.Parameters.Add(new SqlParameter("BoardCode", boardCode));
                    sqlInsert.Parameters.Add(new SqlParameter("CycleTime", cyctimeMinus));
                    sqlInsert.Parameters.Add(new SqlParameter("InsertDate", sDate));
                    dbSCM.Query(sqlInsert);
                }
            }

            return Ok();
        }

        [HttpPost]
        [Route("/jwt")]
        public IActionResult getJWT([FromBody] MLogin param)
        {
            return Ok(new
            {
                jwt = serv.CreateToken(param.username)
            });
        }

        [HttpPost]
        [Route("/login/supplier")]
        public IActionResult loginSupplier([FromBody] MLogin param)
        {
            var content = _contextDBSCM.DoVenderMasters.Where(x => x.VdCode == param.username).FirstOrDefault();
            if (content != null)
            {
                string jwt = serv.CreateToken(param.username);
                return Ok(new
                {
                    status = true,
                    jwt = jwt,
                    vdName = content.VdDesc
                });
            }
            else
            {
                return Ok(new
                {
                    status = false,
                    jwt = "",
                    vdName = ""
                });
            }
        }

        [HttpPost]
        [Route("/login/employee")]
        public IActionResult loginEmployee([FromBody] MLogin param)
        {
            var content = _contextDBHRM.Employees.Where(x => x.Code == param.username).FirstOrDefault();
            if (content != null)
            {
                //string jwt = CreateToken(param.username);
                string jwt = "";
                return Ok(new
                {
                    status = true,
                    jwt = jwt,
                    vdName = content.Name + " " + content.Surn
                });
            }
            else
            {
                return Ok(new
                {
                    status = false,
                    jwt = "",
                    vdName = ""
                });
            }
        }

        [HttpGet]
        [Route("/privilege/{type}")] // type = employee, supplier
        public IActionResult getPrivilege(string type)
        {
            return Ok(_contextDBSCM.DoDictMstrs.Where(x => x.DictType == "PRIVILEGE" && x.Code == type).ToList());
        }

        [HttpPost]
        [Route("/test")]
        public IActionResult getTest([FromBody] MTest param)
        {
            return Ok(param);
        }

        [HttpGet]
        [Route("/dict/timeschedule")]
        public IActionResult dictTimeSchedule()
        {
            var content = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "TIME_SCHEDULE_PS").OrderBy(x => x.DictType);
            return Ok(content);
        }

        [HttpGet]
        [Route("/vender")]
        public IActionResult GetVender()
        {
            var content = _contextDBSCM.DoVenderMasters.ToList();
            return Ok(content);
        }

        [HttpPost]
        [Route("/partsupply/timescheduledelivery")]
        public IActionResult PartSupplyTimeScheduleDelivery([FromBody] MPartSupplyTimeScheduleDelivery param)
        {
            var content = _contextDBSCM.DoHistories.Where(x => x.DateVal == param.startDate && x.DoVal > 0 && x.TimeScheduleDelivery != null).ToList();
            var contentPart = _contextDBSCM.DoPartMasters.ToList();
            var res = from a in content.DefaultIfEmpty()
                      join b in contentPart
                      on a.Partno equals b.Partno
                      select new
                      {
                          a.TimeScheduleDelivery,
                          a.Partno,
                          a.VdCode,
                          a.DoVal,
                          b.Description,
                          vdDesc = _contextDBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == a.VdCode).VdDesc,
                      };
            return Ok(res);
        }


        [HttpGet]
        [Route("/license/user")]
        public IActionResult UseOfLincense()
        {
            List<MUserOfLicense> res = new List<MUserOfLicense>();
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT COURSE.ID,COURSE.COURSE_CODE,COURSE.COURSE_NAME,(SELECT TOP(1) SCHEDULE.SCHEDULE_CODE FROM [dbDCI].[dbo].[TR_Schedule] SCHEDULE 
WHERE COURSE_ID = COURSE.ID ORDER BY SCHEDULE_START DESC) AS SCHEDULE_CODE
FROM [dbDCI].[dbo].[TR_COURSE] COURSE 
GROUP BY COURSE.ID,COURSE.COURSE_CODE,COURSE.COURSE_NAME";
            DataTable dt = dbDCI.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                MUserOfLicense item = new MUserOfLicense();
                item.CourseCode = dr["COURSE_CODE"].ToString();
                item.CourseName = dr["COURSE_NAME"].ToString();
                item.SchuduleCode = dr["SCHEDULE_CODE"].ToString();
                res.Add(item);
            }
            return Ok(res);
        }

        //private MRESRESULT getPlan()
        //{
        //    List<MDORESULT> res = new List<MDORESULT>();
        //    List<MStockAlpha> StockAlpha = new List<MStockAlpha>();
        //    List<MDOAct> DoActs = new List<MDOAct>();
        //    List<DoPartMaster> PartMstrs = new List<DoPartMaster>();
        //    List<DoVenderMaster> SupplierMstrs = new List<DoVenderMaster>();
        //    int _dFixed = 7;
        //    int _dRun = 7;
        //    int _dForeCast = 0;
        //    DateTime dtNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
        //    DateTime dtEnd = dtNow.AddDays(_dRun + _dFixed + _dForeCast);
        //    string buyer = "41256";
        //    string param_supplier = "021022";
        //    var rSupplier = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.RefCode != "" && x.Code == buyer).ToList();
        //    foreach (var supplier in rSupplier)
        //    {
        //        DateTime dtLoop = dtNow;
        //        DateTime dtStart = dtNow.AddDays(-_dRun);
        //        string SupplierCode = supplier.RefCode;
        //        var PartMstr = _contextDBSCM.DoPartMasters.Where(x => x.VdCode == SupplierCode).ToList();
        //        PartMstrs = PartMstrs.Concat(PartMstr).ToList();
        //        DataTable SupplierMstr = serv.GetSupplierMaster(SupplierCode);
        //        foreach (DataRow drSupplierMstr in SupplierMstr.Rows)
        //        {
        //            SupplierMstrs.Add(new DoVenderMaster()
        //            {
        //                VdMon = Boolean.Parse(drSupplierMstr["VD_MON"].ToString()),
        //                VdTue = Boolean.Parse(drSupplierMstr["VD_TUE"].ToString()),
        //                VdWed = Boolean.Parse(drSupplierMstr["VD_WED"].ToString()),
        //                VdThu = Boolean.Parse(drSupplierMstr["VD_THU"].ToString()),
        //                VdFri = Boolean.Parse(drSupplierMstr["VD_FRI"].ToString()),
        //                VdSat = Boolean.Parse(drSupplierMstr["VD_SAT"].ToString()),
        //                VdSun = Boolean.Parse(drSupplierMstr["VD_SUN"].ToString()),
        //            });
        //        }
        //        List<M_SQL_VIEW_DO_PLAN> Plans = SQLV_DOPLAN(SupplierCode, dtNow, dtEnd);
        //        var Parts = Plans.GroupBy(x => new { x.PartNo, x.Cm }).Select(x => new MParts
        //        {
        //            Part = x.Key.PartNo,
        //            Cm = x.Key.Cm
        //        });
        //        string PartJoin = string.Join("','", Parts.Select(x => x.Part).Distinct().ToList());
        //        DoActs = serv.GetDoActs(dtNow.AddDays(-7), dtEnd, PartJoin, SupplierCode);
        //        StockAlpha = serv.GetStockAlpha(dtNow, PartJoin);
        //        var Historys = _contextDBSCM.DoHistories.Where(x => x.VdCode == SupplierCode && x.Revision == 999).ToList();
        //        foreach (var itemPart in PartMstr)
        //        {
        //            dtLoop = dtNow;
        //            dtStart = dtNow.AddDays(-_dRun);
        //            string PartCode = itemPart.Partno;
        //            double PlanVal = 0;
        //            double DOVal = 0;
        //            double DOAct = 0;
        //            double StockVal = StockAlpha.FirstOrDefault(x => x.Part.Trim() == PartCode) != null ? StockAlpha.FirstOrDefault(x => x.Part == PartCode).Stock : 0;
        //            double StockSimVal = StockVal;

        //            while (dtStart < dtNow)
        //            {
        //                var history = Historys.FirstOrDefault(x => x.Partno == PartCode && x.DateVal == dtStart.ToString("yyyyMMdd"));
        //                if (history != null)
        //                {
        //                    DOVal = (double)history.DoVal;
        //                    StockSimVal = (double)history.StockVal;
        //                }
        //                res.Add(new MDORESULT()
        //                {
        //                    vdCode = SupplierCode,
        //                    date = dtStart,
        //                    part = PartCode,
        //                    plan = PlanVal,
        //                    doPlan = DOVal,
        //                    doAct = DOAct,
        //                    stock = StockVal,
        //                    stockSim = StockSimVal,
        //                    po = 0,
        //                });
        //                dtStart = dtStart.AddDays(1);
        //            }



        //            while (dtLoop <= dtEnd)
        //            {
        //                var ItemPartMstr = PartMstr.FirstOrDefault(x => x.Partno == PartCode);
        //                var Plan = Plans.FirstOrDefault(x => x.PartNo == itemPart.Partno && x.Date == dtLoop);
        //                PlanVal = Plan != null ? Plan.Qty : 0;
        //                StockSimVal -= PlanVal;
        //                res.Add(new MDORESULT()
        //                {
        //                    vdCode = SupplierCode,
        //                    date = dtLoop,
        //                    part = PartCode,
        //                    plan = PlanVal,
        //                    doPlan = DOVal,
        //                    doAct = DOAct,
        //                    stock = StockVal,
        //                    stockSim = StockSimVal,
        //                    po = 0,

        //                });
        //                if (StockSimVal < 0)
        //                {
        //                    DateTime dtDelivery = dtLoop.AddDays(-(int)ItemPartMstr.Pdlt);
        //                    if (dtDelivery < dtNow)
        //                    {
        //                        dtDelivery = dtNow;
        //                    }
        //                    dtDelivery = serv.checkDelivery(dtDelivery, SupplierMstr);
        //                    int index = res.FindIndex(x => x.vdCode == SupplierCode && x.part == PartCode && x.date == dtDelivery);
        //                    if (index != -1)
        //                    {
        //                        DOVal = serv.GetDoVal(Math.Abs(StockSimVal), ItemPartMstr);
        //                        res[index].doPlan += DOVal;
        //                        res[index].stockSim += DOVal;
        //                        MRefreshStock refreshStock = serv.refreshStockSim(res, dtDelivery, PartCode, SupplierCode);
        //                        res = refreshStock.Results;
        //                        StockSimVal = refreshStock.StockSim;
        //                        DOVal = 0;
        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine("123");
        //                    }
        //                    Console.WriteLine(res);
        //                }
        //                dtLoop = dtLoop.AddDays(1);
        //            }
        //        }
        //    }
        //    Console.WriteLine(res);
        //    _HOLIDAY = serv._GET_HOLIDAY();
        //    List<MGetPlan> PlanList = new List<MGetPlan>();
        //    foreach (MDORESULT item in res)
        //    {
        //        PlanList.Add(new MGetPlan()
        //        {
        //            id = 1,
        //            date = item.date.ToString("yyyyMMdd"),
        //            plan = item.plan.ToString(),
        //            picklist = item.plan.ToString(),
        //            doPlan = item.doPlan.ToString(),
        //            stock = item.stock.ToString(),
        //            part = item.part.ToString(),
        //            doBalance = "0",
        //            doAct = item.doAct.ToString(),
        //            stockSim = item.stockSim.ToString(),
        //            po = item.po.ToString(),
        //            doAddFixed = "",
        //            vdCode = item.vdCode.ToString(),
        //            planNow = item.planNow.ToString(),
        //            timeScheduleDelivery = "",
        //            model = "TEST"
        //        });
        //    }
        //    return new MRESRESULT()
        //    {
        //        Plan = PlanList,
        //        Holiday = _HOLIDAY,
        //        PartMasters = PartMstrs
        //    };
        //}

        [HttpPost]
        [Route("/getListBuyer")]
        public IActionResult GetListBuyer()
        {
            var ListBuyer = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.RefCode != "").ToList();
            var res = (from item in (from dict in ListBuyer
                                     select new
                                     {
                                         code = dict.Code
                                     }).GroupBy(x => x.code).ToList()
                       join emp in _contextDBHRM.Employees
                  on item.Key equals emp.Code
                       select new
                       {
                           empcode = emp.Code,
                           fullname = $"{emp.Pren!.ToUpper()}{emp.Name} {emp.Surn}"
                       }).ToList();
            return Ok(res);
        }

        [HttpPost]
        [Route("/getListSupplierByBuyer")]
        public IActionResult GetListSupplierByBuyer([FromBody] DoDictMstr param)
        {
            List<DoDictMstr> suppliers = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == param.Code).ToList();
            var res = (from vd in suppliers
                       join vdMstr in _contextDBSCM.DoVenderMasters.ToList()
                       on vd.RefCode equals vdMstr.VdCode
                       select new
                       {
                           vdcode = vd.RefCode,
                           vdname = vdMstr.VdDesc
                       }).ToList();
            return Ok(res);
        }
    }
}
