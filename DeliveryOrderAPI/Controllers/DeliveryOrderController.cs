
using Azure;
using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
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

        [HttpGet]
        [Route("/insertDO/{buyer}")]
        //public async Task<IActionResult> InsertDO([FromBody] MRUNDO param)
        public async Task<IActionResult> InsertDO(string buyer = "41256")
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
