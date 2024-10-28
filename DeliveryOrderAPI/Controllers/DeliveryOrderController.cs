
using Azure;
using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using DeliveryOrderAPI.Params;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Globalization;
using System.Net;

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
        Services serv = new Services(new DBSCM(), new DBHRM());
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
        Helper oHelper = new Helper();
        public DeliveryOrderController(DBSCM contextDBSCM, DBHRM contextDBHRM)
        {
            _contextDBSCM = contextDBSCM;
            _contextDBHRM = contextDBHRM;
        }

        [HttpPost]
        [Route("/getPlans")]
        public IActionResult getPlans([FromBody] MGetPlan param)
        {
            bool? hiddenPartNoPlan = param.hiddenPartNoPlan;
            MODEL_GET_DO response = serv.CalDO(false, param.vdCode!, param, "", 0, hiddenPartNoPlan);
            return Ok(response);
        }

        [HttpGet]
        [Route("/distribute/{buyer}")]
        public async Task<IActionResult> Distribute(string buyer = "41256")
        {
            string YMDFormat = "yyyyMMdd";
            DateTime dtNow = DateTime.Now;
            //DateTime dtNow = DateTime.ParseExact("20241028", "yyyyMMdd", CultureInfo.InvariantCulture);
            string nbr = dtNow.ToString("yyyyMMdd");
            int rev = 0;
            DoHistoryDev prev = _contextDBSCM.DoHistoryDevs.FirstOrDefault(x => x.RunningCode == nbr && x.Revision == 999)!;
            if (prev != null)
            {
                rev = (int)prev.Rev!;
            }
            rev++;
            MODEL_GET_DO GroupDO = serv.CalDO(true, "", null, nbr, rev, false);
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
                    //InsertBy = param.empcode,
                    InsertBy = buyer,
                    Revision = 999
                };
                _contextDBSCM.DoHistoryDevs.Add(item);
            }
            int insert = _contextDBSCM.SaveChanges();
            if (insert > 0 && prev != null)
            {
                List<DoHistoryDev> listPrev = _contextDBSCM.DoHistoryDevs.Where(x => x.Revision == 999 && x.RunningCode == nbr && x.Rev != rev).ToList();
                listPrev.ForEach(a => a.Revision = prev.Rev);
                _contextDBSCM.SaveChanges();
            }
            if (insert > 0)
            {
                List<DoHistoryDev> listPrevDay = _contextDBSCM.DoHistoryDevs.Where(x => x.Revision == 999 && x.RunningCode != nbr).ToList();
                listPrevDay.ForEach(a => a.Revision = 1);
                _contextDBSCM.SaveChanges();
            }
            return Ok(new
            {
                status = insert,
                nbr = $"{nbr}{rev.ToString("D3")}"
            });
        }

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
            double? DoEdit = oHelper.ConvDBToInt(param.doEdit);
            List<DoHistory> rHistory = _contextDBSCM.DoHistories.Where(x => x.Id == param.id).ToList();
            if (rHistory.Count > 0)
            {
                DoHistory oHistory = rHistory.FirstOrDefault();
                if (oHistory != null && param.doEdit < oHistory.DoVal)
                {
                    DateTime DateStart = DateTime.ParseExact(oHistory.RunningCode, "yyyyMMdd", null);
                    DateTime DateLoop = DateTime.ParseExact(oHistory.DateVal, "yyyyMMdd", null);
                    DateTime FixedDate = DateStart.AddDays(7);
                    bool firstTime = true;
                    if (DateLoop >= DateStart && DateLoop <= FixedDate)
                    {
                        double StockSim = 0;
                        double PlanVal = oHelper.ConvDBEmptyToDB(oHistory.PlanVal);
                        if (firstTime)
                        {
                            double diffDO = (double)DoEdit - (double)oHistory.DoVal;
                            StockSim = oHelper.ConvDBEmptyToDB(oHistory.StockVal);
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
                            oHistory = _contextDBSCM.DoHistories.Where(x => x.DateVal == DateLoop.ToString("yyyyMMdd") && x.RunningCode == DateStart.ToString("yyyyMMdd") && x.Rev == oHistory.Rev && x.Partno == param.partNo).FirstOrDefault();
                            PlanVal = oHelper.ConvDBEmptyToDB(oHistory.PlanVal);
                            double DoVal = oHelper.ConvDBEmptyToDB(oHistory.DoVal);
                            StockSim = (StockSim + oHelper.ConvDBEmptyToDB(DoVal)) - PlanVal;
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
            DoVenderMaster oVdStd = _contextDBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == param.vender);
            try
            {
                if (oVdStd != null)
                {
                    oVdStd.VdBoxPeriod = param.vdBoxPeriod;
                    oVdStd.VdMinDelivery = param.min;
                    oVdStd.VdMaxDelivery = param.max;
                    oVdStd.VdRound = param.round;
                    oVdStd.VdProdLead = param.vdProdLead;
                    _contextDBSCM.DoVenderMasters.Update(oVdStd);
                }
                List<DoMaster> contentRound = _contextDBSCM.DoMasters.Where(x => x.VdCode == param.vender).ToList();
                contentRound.ForEach(x => { x.VdRound = param.round; });
                int active = _contextDBSCM.SaveChanges();
                return Ok(new { update = active, message = "" });
            }
            catch (Exception e)
            {
                return Ok(new { update = false, message = e.Message.ToString() });
            }
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

        //[HttpGet]
        //[Route("/vender/debug")]
        //public IActionResult VenderDebug()
        //{
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = "SELECT A.VD_CODE,B.VenderName FROM [dbSCM].[dbo].[DO_VENDER_MASTER] A LEFT JOIN [dbSCM].[dbo].[AL_Vendor] B ON A.VD_CODE = B.Vender";
        //    DataTable dt = dbSCM.Query(sql);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        SqlCommand sqlUpdate = new SqlCommand();
        //        sqlUpdate.CommandText = "UPDATE [dbSCM].[dbo].[DO_VENDER_MASTER] SET VD_DESC = '" + dr["VenderName"] + "' WHERE VD_CODE ='" + dr["VD_CODE"] + "'";
        //        dbSCM.Query(sqlUpdate);
        //    }
        //    return Ok();
        //}

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
                    jwt,
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

        //[HttpPost]
        //[Route("/test")]
        //public IActionResult getTest([FromBody] MTest param)
        //{
        //    return Ok(param);
        //}

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

        [HttpPost]
        [Route("/getListBuyer")]
        public IActionResult GetListBuyer()
        {
            return Ok(serv.GetListBuyer());
        }

        [HttpPost]
        [Route("/getListSupplierByBuyer")]
        public IActionResult GetListSupplierByBuyer([FromBody] DoDictMstr param)
        {
            List<DoDictMstr> suppliers = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == param.Code && x.DictStatus == "999").ToList();
            if (param.RefCode != "" && param.RefCode != null)
            {
                suppliers = suppliers.Where(x => x.RefCode == param.RefCode).ToList();
            }
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

        [HttpGet]
        [Route("/getVenderMasterOfVender/{vdcode}")]
        public IActionResult GetVenderMasterOfVender(string vdcode)
        {
            var vdMaster = _contextDBSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == vdcode);
            return Ok(vdMaster);
        }

        [HttpGet]
        [Route("/getVenderMasterOfVenders")]
        public IActionResult GetVenderMasterOfVenders()
        {
            var vdMaster = _contextDBSCM.DoVenderMasters.ToList();
            return Ok(vdMaster);
        }

        [HttpGet]
        [Route("/getSupplier/{buyer}")]
        public IActionResult GetSupplier(string buyer)
        {
            var supplierOfBuyer = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == "41256" && x.DictStatus == "999").ToList();
            var listSupplier = (from sp in supplierOfBuyer
                                join spDict in _contextDBSCM.DoVenderMasters.ToList()
                                on sp.RefCode equals spDict.VdCode
                                select new DoVenderMaster
                                {
                                    VdCode = spDict.VdCode,
                                    VdBox = spDict.VdBox,
                                    VdDesc = spDict.VdDesc,
                                    VdMinDelivery = spDict.VdMinDelivery,
                                    VdMaxDelivery = spDict.VdMaxDelivery,
                                    VdRound = spDict.VdRound,
                                    VdProdLead = spDict.VdProdLead,
                                });
            return Ok(listSupplier);
        }

        [HttpPost]
        [Route("/DOLog")]
        public IActionResult editDOLog([FromBody] MEditDO param)
        {
            List<MDOLog> res = new List<MDOLog>();
            string runningCode = param.runningCode;
            if (runningCode.Length == 11)
            {
                runningCode = runningCode.Substring(0, 8);
            }
            string partNo = param.partno;
            string ymd = param.ymd;
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT * FROM [dbo].[DO_LOG] WHERE RUNNING_CODE = '" + runningCode + " ' AND PARTNO =  '" + partNo + " ' AND DATE_VAL =  '" + ymd + " '  order by DT_INSERT DESC";
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                MDOLog item = new MDOLog();
                item.runningCode = dr["RUNNING_CODE"].ToString();
                item.partNo = dr["PARTNO"].ToString();
                item.dateVal = dr["DATE_VAL"].ToString();
                item.prevDO = double.Parse(dr["PREV_DO"].ToString());
                item.doVal = double.Parse(dr["DO"].ToString());
                item.status = dr["STATUS"].ToString();
                item.dtInsert = DateTime.Parse(dr["DT_INSERT"].ToString());
                item.dtUpdate = DateTime.Parse(dr["DT_UPDATE"].ToString());
                item.updateBy = dr["UPDATE_BY"].ToString();
                res.Add(item);
            }
            return Ok(res);
        }

        [HttpPost]
        [Route("/editDO")]
        public IActionResult EditDO([FromBody] MEditDO param)
        {
            int update = 0;
            string runningCode = param.runningCode;
            if (runningCode.Length == 11)
            {
                runningCode = runningCode.Substring(0, 8);
            }
            string partNo = param.partno;
            string ymd = param.ymd;
            string empCode = param.empCode;
            double doVal = param.doVal;
            double doPrev = param.doPrev;
            List<DoHistoryDev> PrevContent = _contextDBSCM.DoHistoryDevs.Where(x => x.RunningCode == runningCode && x.DateVal == ymd && x.Partno == partNo).ToList();
            if (PrevContent.Count > 0)
            {
                DoHistoryDev PrevItem = PrevContent.FirstOrDefault();
                PrevItem.DoVal = doVal;
                _contextDBSCM.DoHistoryDevs.Update(PrevItem);
                update = _contextDBSCM.SaveChanges();
                if (update > 0)
                {
                    SqlCommand sql = new SqlCommand();
                    sql.CommandText = @"INSERT [dbo].[DO_LOG]  ([RUNNING_CODE],[PARTNO],[DATE_VAL],[PREV_DO],[DO],[STATUS],[DT_INSERT],[DT_UPDATE],[UPDATE_BY]) VALUES ('" + runningCode + "','" + partNo + "','" + ymd + "','" + doPrev + "','" + doVal + "','CHANGE_DO','" + DateTime.Now + "','" + DateTime.Now + "','" + empCode + "')";
                    DataTable dt = dbSCM.Query(sql);
                }
            }
            return Ok(new
            {
                status = update
            });
        }

        [HttpPost]
        [Route("/GET_LIST_DRAWING_DELIVERY_OF_DAY")]
        public IActionResult GET_LIST_DRAWING_DELIVERY_OF_DAY([FromBody] M_GET_LIST_DRAWING_DELIVERY_OF_DAY param)
        {
            string dtTarget = param.dtTarget;
            string vdCode = param.vdCode;
            List<DoHistoryDev> ListDO = _contextDBSCM.DoHistoryDevs.Where(x => x.DateVal == dtTarget && x.Revision == 999 && x.DoVal > 0 && x.VdCode == vdCode).ToList();
            return Ok(ListDO);
        }

        //[HttpPost]
        //[Route("/INSERT_LIST_DRAWING_DELIVERY_OF_DAY")]
        //public IActionResult INSERT_LIST_DRAWING_DELIVERY_OF_DAY([FromBody] List<M_INSERT_LIST_DRAWING_DELIVERY_OF_DAY> param)
        //{
        //    foreach (M_INSERT_LIST_DRAWING_DELIVERY_OF_DAY item in param)
        //    {

        //    }
        //    return Ok();
        //}


        [HttpPost]
        [Route("/VIEW_HISTORY_PLAN")]
        public IActionResult VIEW_HISTORY_PLAN([FromBody] MVIEW_HISTORY_PLAN param)
        {
            string date = param.date;
            string part = param.part;
            var res = _contextDBSCM.DoHistoryDevs.Where(x => x.DateVal == date && x.Partno == part).OrderByDescending(x => x.InsertDt).ToList();
            return Ok(res);
        }

        [HttpPost]
        [Route("/CALENDAR/INSERT")]
        public IActionResult CalendarInsert([FromBody] DoDictMstr obj)
        {
            string dictType = "holiday";
            string dictCode = obj.Code;
            int action = 0;
            string message = "";
            DoDictMstr oDictHoliday = _contextDBSCM.DoDictMstrs.FirstOrDefault(x => x.DictType == dictType && x.Code == obj.Code);
            if (oDictHoliday != null)
            {
                oDictHoliday.Description = obj.Description;
                oDictHoliday.UpdateDate = DateTime.Now;
                _contextDBSCM.DoDictMstrs.Update(oDictHoliday);
                action = _contextDBSCM.SaveChanges();
            }
            else
            {
                oDictHoliday = new DoDictMstr();
                oDictHoliday.DictType = dictType;
                oDictHoliday.Code = dictCode;
                oDictHoliday.Description = obj.Description;
                oDictHoliday.Note = dictCode;
                oDictHoliday.CreateDate = DateTime.Now;
                oDictHoliday.UpdateDate = DateTime.Now;
                oDictHoliday.DictStatus = "pending";
                _contextDBSCM.DoDictMstrs.Add(oDictHoliday);
                action = _contextDBSCM.SaveChanges();
            }
            return Ok(new
            {
                status = action,
                message = message
            });
        }

        [HttpGet]
        [Route("/CALENDAR/GET/{yyyy}")]
        public IActionResult CalendarGetByYear(string yyyy)
        {
            List<DoDictMstr> rCalendar = _contextDBSCM.DoDictMstrs.Where(x => x.DictType == "holiday" && x.Code.StartsWith(yyyy)).ToList();
            return Ok(rCalendar);
        }

        [HttpGet]
        [Route("/CALENDAR/DATE/GET/{ymd}")]
        public IActionResult CalendarGetDateDetail(string ymd)
        {
            DoDictMstr oCalendar = new DoDictMstr();
            oCalendar = _contextDBSCM.DoDictMstrs.FirstOrDefault(x => x.DictType == "holiday" && x.Code == ymd);
            return Ok(oCalendar);
        }


        [HttpGet]
        [Route("/CALENDAR/DEL/{ymd}")]
        public IActionResult DictDelHoliday(string ymd)
        {
            DoDictMstr oCalendar = new DoDictMstr();
            int action = 0;
            oCalendar = _contextDBSCM.DoDictMstrs.FirstOrDefault(x => x.DictType == "holiday" && x.Code == ymd);
            if (oCalendar != null)
            {
                _contextDBSCM.DoDictMstrs.Remove(oCalendar);
                action = _contextDBSCM.SaveChanges();
            }
            return Ok(new
            {
                status = action
            });
        }

        [HttpPost]
        [Route("/HISTORY/DO")]
        public IActionResult GetHistoryDO([FromBody] DoLogDev obj)
        {
            List<DoLogDev> rLogDev = new List<DoLogDev>();
            string date = obj.logToDate;
            string part = obj.logPartNo;
            string doRunning = obj.doRunning;
            double doVal = obj.logDo.HasValue ? (double)obj.logDo : 0;
            int doRev = obj.doRev.HasValue ? (int)obj.doRev : 0;
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_LOG_DEV] where LOG_PART_NO = @part and LOG_TO_DATE = @log_to_date and DO_RUNNING = @DO_RUNNING and DO_REV = @DO_REV order by LOG_ID asc";
            //and LOG_DO = @LOG_DO
            sql.Parameters.Add(new SqlParameter("@part", part));
            sql.Parameters.Add(new SqlParameter("@log_to_date", date));
            sql.Parameters.Add(new SqlParameter("@DO_RUNNING", doRunning));
            sql.Parameters.Add(new SqlParameter("@DO_REV", doRev));
            //sql.Parameters.Add(new SqlParameter("@LOG_DO", doVal));
            DataTable dt = dbSCM.Query(sql);
            foreach (DataRow dr in dt.Rows)
            {
                DoLogDev item = new DoLogDev();
                item.logPartNo = dr["LOG_PART_NO"].ToString();
                item.logType = dr["LOG_TYPE"].ToString();
                item.logFromDate = dr["LOG_FROM_DATE"].ToString();
                item.logFromStock = Convert.ToDouble(dr["LOG_FROM_STOCK"].ToString());
                item.logFromPlan = Convert.ToDouble(dr["LOG_FROM_PLAN"].ToString());
                item.logNextDate = dr["LOG_NEXT_DATE"].ToString();
                item.logNextStock = Convert.ToDouble(dr["LOG_NEXT_STOCK"].ToString());
                item.logDo = Convert.ToDouble(dr["LOG_DO"].ToString());
                item.logBox = Convert.ToDouble(dr["LOG_BOX"].ToString());
                item.logState = dr["LOG_STATE"].ToString();
                item.logRemark = dr["LOG_REMARK"].ToString();
                item.logUpdateDate = Convert.ToDateTime(dr["LOG_UPDATE_DATE"].ToString());
                rLogDev.Add(item);
            }
            return Ok(rLogDev);
        }

        [HttpPost]
        [Route("/HISTORY/EDIT/DO")]
        public IActionResult HistoryEditDO([FromBody] MEditDO param)
        {
            int update = 0;
            string runningCode = param.runningCode;
            if (runningCode.Length == 11)
            {
                runningCode = runningCode.Substring(0, 8);
            }
            string partNo = param.partno;
            string ymd = param.ymd;
            string empCode = param.empCode;
            double doVal = param.doVal;
            double doPrev = param.doPrev;
            List<DoHistoryDev> PrevContent = _contextDBSCM.DoHistoryDevs.Where(x => x.RunningCode == runningCode && x.DateVal == ymd && x.Partno == partNo).ToList();
            if (PrevContent.Count > 0)
            {
                DoHistoryDev PrevItem = PrevContent.FirstOrDefault();
                PrevItem.DoVal = doVal;
                _contextDBSCM.DoHistoryDevs.Update(PrevItem);
                update = _contextDBSCM.SaveChanges();
                if (update > 0)
                {
                    SqlCommand sqlInsertLog = new SqlCommand();
                    sqlInsertLog.CommandText = @"INSERT INTO [dbo].[DO_LOG_DEV] ([DO_RUNNING],[DO_REV],[LOG_PART_NO],[LOG_VD_CODE] ,[LOG_PROD_LEAD],[LOG_TYPE],[LOG_FROM_DATE],[LOG_FROM_STOCK],[LOG_FROM_PLAN],[LOG_NEXT_DATE],[LOG_NEXT_STOCK],[LOG_TO_DATE],[LOG_DO],[LOG_BOX],[LOG_STATE],[LOG_REMARK],[LOG_CREATE_DATE],[LOG_UPDATE_DATE],[LOG_UPDATE_BY])
     VALUES
           ('" + runningCode + "','" + PrevItem.Rev + "','" + partNo + "','" + PrevItem.VdCode + "','2','EDIT_DO','" + PrevItem.DateVal + "','" + PrevItem.StockVal + "','" + PrevItem.PlanVal + "','" + PrevItem.DateVal + "','" + PrevItem.StockVal + "','" + PrevItem.DateVal + "','" + doVal + "','0','referent','edit_do',GETDATE(),GETDATE(),'" + empCode + "') ";
                    int insertLog = dbSCM.ExecuteNonCommand(sqlInsertLog);
                }
            }
            return Ok(new
            {
                status = update
            });
        }

        [HttpPost]
        [Route("/AddPartMaster")]
        public IActionResult AddPartMaster([FromBody] ParamAddPartMaster param)
        {
            bool status = false;
            string drawing = param.drawing.Trim();
            string cm = param.cm;
            string vender = param.vender;
            int boxMin = param.boxMin;
            int boxQty = param.boxQty;
            string desc = param.description;
            string unit = param.unit;
            DoPartMaster oPart = new DoPartMaster()
            {
                Active = "ACTIVE",
                BoxMax = 99999,
                BoxMin = boxMin,
                BoxQty = boxQty,
                Cm = cm,
                Description = desc,
                Partno = drawing,
                Diameter = "",
                VdCode = vender,
                Pdlt = 2,
                Unit = unit,
                UpdateBy = param.updateBy,
                UpdateDate = DateTime.Now
            };
            int exist = _contextDBSCM.DoPartMasters.Where(x => x.Partno == drawing && x.Cm == cm && x.VdCode == vender).Count();
            if (exist == 0)
            {
                _contextDBSCM.Add(oPart);
                int add = _contextDBSCM.SaveChanges();
                status = add > 0 ? true : false;
            }
            return Ok(new
            {
                status = status
            });
        }

        [HttpPost]
        [Route("/checkversion")]
        public IActionResult CheckVersionSystem([FromBody] ParamChkVer param)
        {
            string dictSystem = param.dictSystem;
            string dictType = param.dictType;
            string version = serv.ChkVer(dictSystem, dictType);
            return Ok(new
            {
                version
            });
        }
    }
}
