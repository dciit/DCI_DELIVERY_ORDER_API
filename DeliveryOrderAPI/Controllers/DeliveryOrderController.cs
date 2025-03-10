﻿using Azure;
using DeliveryOrderAPI.Contexts;
using DeliveryOrderAPI.Models;
using DeliveryOrderAPI.Params;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text.Json;
using static DeliveryOrderAPI.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        private readonly DBSCM efSCM;
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


        private string redisHost = "192.168.226.85";
        private string redisPort = "6379";


        public DeliveryOrderController(DBSCM contextDBSCM, DBHRM contextDBHRM)
        {
            efSCM = contextDBSCM;
            _contextDBHRM = contextDBHRM;
        }

        [HttpPost]
        [Route("/getPlans")]
        public async Task<IActionResult> getPlans([FromBody] MGetPlan param)
        {
            bool? hiddenPartNoPlan = param.hiddenPartNoPlan;
            MODEL_GET_DO response = await serv.CalDO(false, param.vdCode!, param, "", 0, hiddenPartNoPlan, false);
            /* HttpContext.Response.Body = new MemoryStream();
             await using var writer = new StreamWriter(HttpContext.Response.Body);

             await writer.WriteAsync(JsonSerializer.Serialize(response));
             await writer.FlushAsync();*/

            //HttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            //return File(HttpContext.Response.Body, "application/json");
            //MODEL_GET_DO response = await getRedis("CAL_DO");
            return Ok(response);
        }

        //[HttpGet]
        //[Route("/calDO")]
        //public async Task<IActionResult> calDO()
        //{
        //    deleteRedis();

        //    DateTime dtNow = DateTime.Now;
        //    string nbr = dtNow.ToString("yyyyMMdd");
        //    int rev = 0;
        //    DoHistoryDev prev = efSCM.DoHistoryDevs.FirstOrDefault(x => x.RunningCode == nbr && x.Revision == 999)!;
        //    if (prev != null)
        //    {
        //        rev = (int)prev.Rev!;
        //    }
        //    rev++;
        //    MODEL_GET_DO response = await serv.CalDO(true, "", null, nbr, rev, false, false);
        //    string _key = "CAL_DO";
        //    setRedis(_key, response);
        //    return Ok(response);
        //}


        [HttpGet]
        [Route("/distribute/{buyer}")]
        public async Task<IActionResult> Distribute(string buyer = "41256")
        {
            string _key = "CAL_DO";
            int resultInsert = 0;
            DateTime dtNow = DateTime.Now;
            string nbr = dtNow.ToString("yyyyMMdd");
            int rev = 0;
            DoHistoryDev prev = efSCM.DoHistoryDevs.FirstOrDefault(x => x.RunningCode == nbr && x.Revision == 999)!;
            if (prev != null)
            {
                rev = (int)prev.Rev!;
            }
            rev++;
            MODEL_GET_DO DOInfo = await serv.CalDO(true, "", null, nbr, rev, false, false);
            foreach (MRESULTDO oDO in DOInfo.data)
            {
                SqlCommand sqlInsert = new SqlCommand();
                double Stock = double.IsNaN(oDO.Stock) ? 0 : oDO.Stock;
                double Do = double.IsNaN(oDO.Do) ? 0 : oDO.Do;
                sqlInsert.CommandText = $@"INSERT INTO DO_HISTORY_DEV (RUNNING_CODE,REV,MODEL,PARTNO,CM,DATE_VAL,PLAN_VAL,DO_VAL,STOCK_VAL,STOCK,VD_CODE, INSERT_DT,INSERT_BY,REVISION) 
                                      VALUES ('{nbr}','{rev}','','{oDO.PartNo}','{oDO.Cm}', '{oDO.Date.ToString("yyyyMMdd")}','{oDO.Plan}','{Do}','{Stock}','0','{oDO.Vender}',GETDATE(),'{buyer}','999')";
                int insert = dbSCM.ExecuteNonCommand(sqlInsert);
                if (insert > 0)
                {
                    resultInsert = resultInsert + 1;
                }
            }
            if (resultInsert > 0 && prev != null)
            {
                List<DoHistoryDev> listPrev = efSCM.DoHistoryDevs.Where(x => x.Revision == 999 && x.RunningCode == nbr && x.Rev != rev).ToList();
                listPrev.ForEach(a => a.Revision = prev.Rev);
                efSCM.SaveChanges();
            }
            if (resultInsert > 0)
            {
                List<DoHistoryDev> listPrevDay = efSCM.DoHistoryDevs.Where(x => x.Revision == 999 && x.RunningCode != nbr).ToList();
                listPrevDay.ForEach(a => a.Revision = 1);
                efSCM.SaveChanges();
            }
            setRedis(_key, DOInfo);
            return Ok(new
            {
                status = 1,
                nbr = $"{nbr}{rev.ToString("D3")}"
            });
        }

        [HttpPost]
        [Route("/do/update")]
        public IActionResult DO_UPDATE([FromBody] M_DO_UPDATE param)
        {
            //if (param.empCode != "41256")
            //{
            //    return Ok(new { status = false, message = "ปิดระบบการแก้ไขชั่วคราว ติดต่อ IT 611 เบียร์" });
            //}
            double? stock = 0;
            double? _do = 0;
            int status = 1;
            var dataAfterUpdate = (dynamic)null;
            string message = "";
            double? DoEdit = oHelper.ConvDBToInt(param.doEdit);
            List<DoHistory> rHistory = efSCM.DoHistories.Where(x => x.Id == param.id).ToList();
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
                            oHistory = efSCM.DoHistories.Where(x => x.DateVal == DateLoop.ToString("yyyyMMdd") && x.RunningCode == DateStart.ToString("yyyyMMdd") && x.Rev == oHistory.Rev && x.Partno == param.partNo).FirstOrDefault();
                            PlanVal = oHelper.ConvDBEmptyToDB(oHistory.PlanVal);
                            double DoVal = oHelper.ConvDBEmptyToDB(oHistory.DoVal);
                            StockSim = (StockSim + oHelper.ConvDBEmptyToDB(DoVal)) - PlanVal;
                        }
                    }
                }
                if (status != 0)
                {
                    var context = efSCM.DoHistories.Where(x => x.Id == param.id).FirstOrDefault();
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
                                int CountItem = efSCM.DoHistories.Where(x => x.RunningCode == RunningCode && x.Rev == Rev && x.DateVal == DateVal && x.Partno == PartNo).ToList().Count();
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
                    dataAfterUpdate = efSCM.DoHistories.Where(x => x.Id >= param.id && x.Revision == 999).ToList();
                }
            }
            return Ok(new { status = status, data = dataAfterUpdate, message = message });
        }

        [HttpGet]
        [Route("/do/{id}")]
        public IActionResult GetHistoryById(int id)
        {
            var context = efSCM.DoHistories.Where(x => x.Id == id).FirstOrDefault();
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
                    part = efSCM.DoPartMasters.ToList();
                }
                else
                {
                    part = efSCM.DoPartMasters.Where(x => x.VdCode == param.vender).ToList();
                }
                return Ok(part);
            }
            else if (param.type == "vender")
            {
                var venders = efSCM.DoDictMstrs.Where(x => x.DictType == "VENDER_BOX_CAPACITY").OrderBy(x => x.Description).ToList();
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
            DoVenderMaster oVdStd = efSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == param.vender);
            try
            {
                if (oVdStd != null)
                {
                    oVdStd.VdBoxPeriod = param.vdBoxPeriod;
                    oVdStd.VdMinDelivery = param.min;
                    oVdStd.VdMaxDelivery = param.max;
                    oVdStd.VdRound = param.round;
                    oVdStd.VdProdLead = param.vdProdLead;
                    efSCM.DoVenderMasters.Update(oVdStd);
                }
                List<DoMaster> contentRound = efSCM.DoMasters.Where(x => x.VdCode == param.vender).ToList();
                contentRound.ForEach(x => { x.VdRound = param.round; });
                int active = efSCM.SaveChanges();
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
            var content = efSCM.DoVenderMasters.Where(x => x.VdCode == vender).FirstOrDefault();
            return Ok(content);
        }

        [HttpPost]
        [Route("/part/get")]
        public IActionResult MasterGetPart([FromBody] M_MASTER_GET_PART param)
        {
            var content = efSCM.DoPartMasters.Where(x => x.Partno == param.part && x.VdCode == param.vdCode).FirstOrDefault();
            return Ok(content);
        }

        [HttpPost]
        [Route("/part/update")]
        public IActionResult MasterPartUpdate([FromBody] DoPartMaster param)
        {
            var content = efSCM.DoPartMasters.FirstOrDefault(x => x.Partno == param.Partno && x.VdCode == param.VdCode);
            if (content != null)
            {
                content.Cm = param.Cm;
                content.BoxQty = param.BoxQty;
                content.BoxMin = param.BoxMin;
                content.BoxMax = param.BoxMax;
                content.BoxPerPallet = param.BoxPerPallet;
                content.Description = param.Description;
                content.Pdlt = param.Pdlt;
                content.Unit = param.Unit;
                content.Active = param.Active;
                content.UpdateBy = param.UpdateBy;
                content.UpdateDate = param.UpdateDate;
                efSCM.Update(content);
            }
            int update = efSCM.SaveChanges();
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
            var content = efSCM.DoVenderMasters.Where(x => x.VdCode == param.username).FirstOrDefault();
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
            return Ok(efSCM.DoDictMstrs.Where(x => x.DictType == "PRIVILEGE" && x.Code == type).ToList());
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
            var content = efSCM.DoDictMstrs.Where(x => x.DictType == "TIME_SCHEDULE_PS").OrderBy(x => x.DictType);
            return Ok(content);
        }

        [HttpGet]
        [Route("/vender")]
        public IActionResult GetVender()
        {
            var content = efSCM.DoVenderMasters.ToList();
            return Ok(content);
        }

        [HttpPost]
        [Route("/partsupply/timescheduledelivery")]
        public IActionResult PartSupplyTimeScheduleDelivery([FromBody] MPartSupplyTimeScheduleDelivery param)
        {
            var content = efSCM.DoHistories.Where(x => x.DateVal == param.startDate && x.DoVal > 0 && x.TimeScheduleDelivery != null).ToList();
            var contentPart = efSCM.DoPartMasters.ToList();
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
                          vdDesc = efSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == a.VdCode).VdDesc,
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
            List<DoDictMstr> suppliers = efSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == param.Code && x.DictStatus == "999").ToList();
            if (param.RefCode != "" && param.RefCode != null)
            {
                suppliers = suppliers.Where(x => x.RefCode == param.RefCode).ToList();
            }
            var res = (from vd in suppliers
                       join vdMstr in efSCM.DoVenderMasters.ToList()
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
            var vdMaster = efSCM.DoVenderMasters.FirstOrDefault(x => x.VdCode == vdcode);
            return Ok(vdMaster);
        }

        [HttpGet]
        [Route("/getVenderMasterOfVenders")]
        public IActionResult GetVenderMasterOfVenders()
        {
            var vdMaster = efSCM.DoVenderMasters.ToList();
            return Ok(vdMaster);
        }

        [HttpGet]
        [Route("/getSupplier/{buyer}")]
        public IActionResult GetSupplier(string buyer)
        {
            var supplierOfBuyer = efSCM.DoDictMstrs.Where(x => x.DictType == "BUYER" && x.Code == "41256" && x.DictStatus == "999").ToList();
            var listSupplier = (from sp in supplierOfBuyer
                                join spDict in efSCM.DoVenderMasters.ToList()
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
            List<DoHistoryDev> PrevContent = efSCM.DoHistoryDevs.Where(x => x.RunningCode == runningCode && x.DateVal == ymd && x.Partno == partNo).ToList();
            if (PrevContent.Count > 0)
            {
                DoHistoryDev PrevItem = PrevContent.FirstOrDefault();
                PrevItem.DoVal = doVal;
                efSCM.DoHistoryDevs.Update(PrevItem);
                update = efSCM.SaveChanges();
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
            List<DoHistoryDev> ListDO = efSCM.DoHistoryDevs.Where(x => x.DateVal == dtTarget && x.Revision == 999 && x.DoVal > 0 && x.VdCode == vdCode).ToList();
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
            var res = efSCM.DoHistoryDevs.Where(x => x.DateVal == date && x.Partno == part).OrderByDescending(x => x.InsertDt).ToList();
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
            DoDictMstr oDictHoliday = efSCM.DoDictMstrs.FirstOrDefault(x => x.DictType == dictType && x.Code == obj.Code);
            if (oDictHoliday != null)
            {
                oDictHoliday.Description = obj.Description;
                oDictHoliday.UpdateDate = DateTime.Now;
                efSCM.DoDictMstrs.Update(oDictHoliday);
                action = efSCM.SaveChanges();
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
                efSCM.DoDictMstrs.Add(oDictHoliday);
                action = efSCM.SaveChanges();
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
            List<DoDictMstr> rCalendar = efSCM.DoDictMstrs.Where(x => x.DictType == "holiday" && x.Code.StartsWith(yyyy)).ToList();
            return Ok(rCalendar);
        }

        [HttpGet]
        [Route("/CALENDAR/DATE/GET/{ymd}")]
        public IActionResult CalendarGetDateDetail(string ymd)
        {
            DoDictMstr oCalendar = new DoDictMstr();
            oCalendar = efSCM.DoDictMstrs.FirstOrDefault(x => x.DictType == "holiday" && x.Code == ymd);
            return Ok(oCalendar);
        }


        [HttpGet]
        [Route("/CALENDAR/DEL/{ymd}")]
        public IActionResult DictDelHoliday(string ymd)
        {
            DoDictMstr oCalendar = new DoDictMstr();
            int action = 0;
            oCalendar = efSCM.DoDictMstrs.FirstOrDefault(x => x.DictType == "holiday" && x.Code == ymd);
            if (oCalendar != null)
            {
                efSCM.DoDictMstrs.Remove(oCalendar);
                action = efSCM.SaveChanges();
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
            string vdcode = obj.logVdCode;
            SqlCommand sql = new SqlCommand();
            sql.CommandText = @"SELECT * FROM [dbSCM].[dbo].[DO_LOG_DEV]
                                where LOG_VD_CODE = @VD_CODE and LOG_PART_NO = @part and LOG_TO_DATE = @log_to_date and DO_RUNNING = @DO_RUNNING and DO_REV = @DO_REV order by LOG_ID asc";
            //and LOG_DO = @LOG_DO
            sql.Parameters.Add(new SqlParameter("@part", part));
            sql.Parameters.Add(new SqlParameter("@log_to_date", date));
            sql.Parameters.Add(new SqlParameter("@DO_RUNNING", doRunning));
            sql.Parameters.Add(new SqlParameter("@DO_REV", doRev));
            sql.Parameters.Add(new SqlParameter("@VD_CODE", vdcode));
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
                item.logNextDo = Convert.ToDouble(dr["LOG_NEXT_DO"].ToString());
                item.logBox = Convert.ToDouble(dr["LOG_BOX"].ToString());
                item.logState = dr["LOG_STATE"].ToString();
                item.logRemark = dr["LOG_REMARK"].ToString();
                item.logUpdateDate = Convert.ToDateTime(dr["LOG_UPDATE_DATE"].ToString());
                item.logUpdateBy = dr["LOG_UPDATE_BY"].ToString();
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
            double doAdj = param.doVal;
            double doPrev = param.doPrev;
            string _vdcode = param.vdCode;
            List<DoHistoryDev> PrevContent = efSCM.DoHistoryDevs.Where(x => x.RunningCode == runningCode && x.DateVal == ymd && x.Partno == partNo && x.VdCode == _vdcode).ToList();
            if (PrevContent.Count > 0)
            {
                string vdCode = PrevContent[0].VdCode!;
                string ymdTarget = PrevContent[0].DateVal!;
                DateTime dtStart = DateTime.ParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture);
                //List<ViDoPlan> oPlanInfos = serv.GetPlans(vdCode, dtStart, dtStart.AddDays(15));
                SqlCommand SqlGetHistoryDOofPart = new SqlCommand();
                SqlGetHistoryDOofPart.CommandText = $@"SELECT [PARTNO] ,[DATE_VAL] ,[PLAN_VAL] ,[DO_VAL] ,[STOCK_VAL] FROM [dbSCM].[dbo].[DO_HISTORY_DEV] WHERE RUNNING_CODE = (  SELECT TOP(1) RUNNING_CODE FROM [dbSCM].[dbo].[DO_HISTORY_DEV] ORDER BY CAST(RUNNING_CODE AS INT ) DESC)  AND DATE_VAL >= '{DateTime.Now.ToString("yyyyMMdd")}' AND REVISION = '999' AND PARTNO = '{partNo}' AND VD_CODE = '{vdCode}' order by DATE_VAL ASC";
                DataTable dt = dbSCM.Query(SqlGetHistoryDOofPart);
                int StockSim = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    string YMDLoop = dr["DATE_VAL"].ToString()!;
                    int DOLoop = oHelper.ConvStr2Int(dr["DO_VAL"].ToString()!);
                    if (YMDLoop == ymdTarget)
                    {
                        DOLoop = oHelper.ConvDBToInt(doAdj);
                    }
                    int PlnLoop = oHelper.ConvStr2Int(dr["PLAN_VAL"].ToString()!);
                    //ViDoPlan oPlanInfo = oPlanInfos.FirstOrDefault(x=>x.Partno == partNo && x.Prdymd == PlnLoop.ToString("yyyyMMdd") && x.Vender == vdCode);
                    //if (oPlanInfo != null)
                    //{
                    //    PlnLoop = oHelper.ConvUnDec2Int(oPlanInfo.Qty);
                    //}
                    StockSim = (StockSim + DOLoop) - PlnLoop;
                    SqlCommand SqlUpdate = new SqlCommand();
                    SqlUpdate.CommandText = $@"UPDATE [dbSCM].[dbo].[DO_HISTORY_DEV]  SET DO_VAL = '{DOLoop}',STOCK_VAL = '{StockSim}' 
                                             WHERE RUNNING_CODE = '{param.runningCode}' AND DATE_VAL = '{YMDLoop}' AND PARTNO = '{partNo}' 
                                             AND REVISION = '999'  AND VD_CODE = '{vdCode}'";
                    int actUpdate = dbSCM.ExecuteNonCommand(SqlUpdate);
                    Console.WriteLine($"DATE : {YMDLoop} PLAN : {PlnLoop} , DO : {DOLoop} SIM : {StockSim})");

                    update = actUpdate;
                }
                DoHistoryDev PrevItem = PrevContent.FirstOrDefault();
                //PrevItem.DoVal = doVal;
                //efSCM.DoHistoryDevs.Update(PrevItem);
                //update = efSCM.SaveChanges();
                if (update > 0)
                {
                    SqlCommand sqlInsertLog = new SqlCommand();
                    sqlInsertLog.CommandText = @"INSERT INTO [dbo].[DO_LOG_DEV] ([DO_RUNNING],[DO_REV],[LOG_PART_NO],[LOG_VD_CODE] ,[LOG_PROD_LEAD],[LOG_TYPE],[LOG_FROM_DATE],[LOG_FROM_STOCK],[LOG_FROM_PLAN],[LOG_NEXT_DATE],[LOG_NEXT_STOCK],[LOG_TO_DATE],[LOG_DO],[LOG_NEXT_DO]
                                ,[LOG_BOX],[LOG_STATE],[LOG_REMARK],[LOG_CREATE_DATE],[LOG_UPDATE_DATE],[LOG_UPDATE_BY])
                VALUES
                      ('" + runningCode + "','" + PrevItem.Rev + "','" + partNo + "','" + PrevItem.VdCode + "','2','EDIT_DO','" + PrevItem.DateVal + "','" + PrevItem.StockVal + "','" + PrevItem.PlanVal + "','" + PrevItem.DateVal + "','" + PrevItem.StockVal + "','" + PrevItem.DateVal + "','" + doPrev + "','" + doAdj + "','0','referent','edit_do',GETDATE(),GETDATE(),'" + empCode + "') ";
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
            int exist = efSCM.DoPartMasters.Where(x => x.Partno == drawing && x.Cm == cm && x.VdCode == vender).Count();
            if (exist == 0)
            {
                efSCM.Add(oPart);
                int add = efSCM.SaveChanges();
                status = add > 0 ? true : false;
            }
            return Ok(new
            {
                status = status
            });
        }


        [HttpPost]
        [Route("/AddPartMasterAll")]
        public IActionResult AddPartMasterAll([FromBody] List<DOPartNumberForPU> param)
        {
            bool status = false;

            foreach (DOPartNumberForPU item in param)
            {
                string drawing = item.partno.Split(" ")[0];
                string cm = item.cm;
                string vender = item.vdcode;
                int boxMin = item.minQty;
                int boxQty = item.boxQty;
                string desc = item.desc;
                string unit = "PCS";
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
                    UpdateBy = item.updateBy,
                    UpdateDate = DateTime.Now
                };
                int exist = efSCM.DoPartMasters.Where(x => x.Partno == drawing && x.Cm == cm && x.VdCode == vender).Count();
                if (exist == 0)
                {
                    efSCM.Add(oPart);
                    int add = efSCM.SaveChanges();
                    //status = add > 0 ? true : false;
                }

            }

            return Ok(new
            {
                status = true
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



        private async void setRedis(string _Key, object _Val)
        {
            string redisConnectionString = $"{redisHost}:{redisPort}"; // Replace with your Redis server's address and port

            try
            {
                // Connect to the Redis server
                var redis = ConnectionMultiplexer.Connect(redisConnectionString);

                if (redis.IsConnected)
                {
                    MODEL_GET_DO oDos = _Val as MODEL_GET_DO;

                    // Get a database instance
                    IDatabase db = redis.GetDatabase();
                    // Set a key-value pair
                    await db.StringSetAsync($"{_Key}", JsonSerializer.Serialize(oDos));

                }

                // Close the connection
                redis.Close();
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        private async Task<MODEL_GET_DO> getRedis(string _Key)
        {
            // Connection string for your Redis server
            string redisConnectionString = $"{redisHost}:{redisPort}"; // Replace with your Redis server's address and port
            string result = "";
            MODEL_GET_DO oDos = new MODEL_GET_DO();

            try
            {
                // Connect to the Redis server
                var redis = ConnectionMultiplexer.Connect(redisConnectionString);

                if (redis.IsConnected)
                {
                    // Get a database instance
                    IDatabase db = redis.GetDatabase();

                    // Get the value for the key
                    result = await db.StringGetAsync($"{_Key}");

                    oDos = JsonSerializer.Deserialize<MODEL_GET_DO>(result);


                }

                // Close the connection
                redis.Close();
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return oDos;
        }


        private async void deleteRedis()
        {
            string redisConnectionString = $"{redisHost}:{redisPort}"; // Replace with your Redis server's address and port

            try
            {
                // Connect to the Redis server
                var redis = ConnectionMultiplexer.Connect(redisConnectionString);

                if (redis.IsConnected)
                {

                    // Get a database instance
                    IDatabase db = redis.GetDatabase();
                    bool wasDeleted = db.KeyDelete("CAL_DO");

                    if (wasDeleted)
                    {
                        Console.WriteLine("The 'name' key was deleted from Redis.");
                    }
                    else
                    {
                        Console.WriteLine("The 'name' key was not found or could not be deleted.");
                    }

                    // Try to get the value again after deletion
                    string nameAfterDelete = db.StringGet("CAL_DO");
                    Console.WriteLine($"After deletion: {nameAfterDelete}");


                }

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
            }


        }

        [HttpGet]
        [Route("/BatchDOWarining")]

        public async Task<IActionResult> BatchDOWarining()
        {


            DateTime dtNow = DateTime.Now;
            string nbr = dtNow.ToString("yyyyMMdd");

            string runningLog = dtNow.ToString("yyyyMMddHHmm");

            int rev = 0;
            DoHistoryDev prev = efSCM.DoHistoryDevs.FirstOrDefault(x => x.RunningCode == nbr && x.Revision == 999)!;


            if (prev != null)
            {
                rev = (int)prev.Rev!;
            }

            rev++;

            MODEL_GET_DO DOInfo = await serv.CalDO(true, "", null, nbr, rev, false, false);

            //&& x.Date.DayOfWeek != DayOfWeek.Saturday && x.Date.DayOfWeek != DayOfWeek.Sunday
            List<MRESULTDO> data = DOInfo.data.Where(x => x.Stock < 0).ToList();

            var finalData =
                          (from a in data
                           join b in DOInfo.PartMaster on new { X1 = a.PartNo, X2 = a.vdCode } equals new { X1 = b.Partno, X2 = b.VdCode }
                           select new
                           {
                               date = a.Date,
                               vdCode = a.vdCode,
                               vdName = a.vdName,
                               partNo = a.PartNo,
                               cm = b.Cm,
                               description = b.Description,
                               stock = a.Stock,
                               _do = a.Do
                           }).Distinct().ToList();



            SqlCommand sqlSelectPrev = new SqlCommand();
            sqlSelectPrev.CommandText = $@"SELECT TOP(1) [REVESION] FROM [dbSCM].[dbo].[DO_LOG_STOCK_WARNING]
                                           WHERE [REVESION] = '999'";
            DataTable dtPrev = dbSCM.Query(sqlSelectPrev);


            int resultInsert = 0;
            foreach (var item in finalData)
            {

                SqlCommand sqlInsert = new SqlCommand();
                sqlInsert.CommandText = $@"INSERT INTO DO_LOG_STOCK_WARNING ([LOG_RUNNING],[LOG_PARTNO],[LOG_CM],[LOG_DESC],[LOG_VD_CODE],[LOG_VD_NAME],[LOG_STOCK],[LOG_DO],[LOG_DATE],[REVESION],[LOG_CREATE_DATE],[LOG_CREATE_BY],[LOG_UPDATE_DATE],[LOG_UPADTE_BY]) 
                   VALUES ('{runningLog}','{item.partNo}','{item.cm}','{item.description}','{item.vdCode}','{item.vdName}','{Math.Ceiling(item.stock)}','{item._do}'
                            ,'{Convert.ToDateTime(item.date).ToString("yyyy-MM-dd HH:mm:ss")}',{999},GETDATE(),'system',GETDATE(),'system')";
                int insert = dbSCM.ExecuteNonCommand(sqlInsert);
                if (insert > 0)
                {
                    resultInsert = resultInsert + 1;
                }
            }

            if (resultInsert > 0 && dtPrev.Rows.Count > 0)
            {

                SqlCommand SqlUpdate = new SqlCommand();
                SqlUpdate.CommandText = $@"UPDATE [dbSCM].[dbo].[DO_LOG_STOCK_WARNING]  
                                    SET REVESION = '1',LOG_UPDATE_DATE = GETDATE() 
                                    WHERE REVESION = '999' and LOG_RUNNING != {runningLog}";
                int actUpdate = dbSCM.ExecuteNonCommand(SqlUpdate);
            }

            //if(resultInsert > 0)
            //{
            //    SqlCommand SqlUpdate = new SqlCommand();
            //    SqlUpdate.CommandText = $@"UPDATE [dbSCM].[dbo].[DO_LOG_STOCK_WARNING]  
            //                        SET REVESION = '1',LOG_UPDATE_DATE = GETDATE() 
            //                        WHERE REVESION = '999' and LOG_RUNNING != {runningLog}";
            //    int actUpdate = dbSCM.ExecuteNonCommand(SqlUpdate);
            //}




            return Ok();


        }










        //[HttpGet]
        //[Route("/DOWarining")]

        //public IActionResult DOWarining()
        //{

        //    int round = GetPeriod(DateTime.Now, 3);

        //    string[] color = new string[5] { "bg-red-100", "bg-green-100", "bg-yellow-100", "bg-purple-100", "bg-orange-100" };
        //    int holiday = 2;

        //    SqlCommand findHolidayCount = new SqlCommand();
        //    findHolidayCount.CommandText = $@" WITH DateRange AS (
        //                                        SELECT GETDATE() AS CurrentDate
        //                                        UNION ALL
        //                                        SELECT DATEADD(DAY, 1, CurrentDate)
        //                                        FROM DateRange
        //                                        WHERE CurrentDate < GETDATE() + 2
        //                                    )

        //                                    SELECT COUNT(CurrentDate) COUNT_H
        //                                    FROM DateRange
        //                                    WHERE DATEPART(WEEKDAY, CurrentDate) IN (1, 7)  
        //                                    OPTION (MAXRECURSION 0);";

        //    DataTable dtfindHolidayCount = dbSCM.Query(findHolidayCount);

        //    if (dtfindHolidayCount.Rows.Count > 0)
        //    {
        //        foreach (DataRow dr in dtfindHolidayCount.Rows)
        //        {
        //            holiday += Convert.ToInt32(dr["COUNT_H"]);
        //        }
        //    }



        //     List<StockWarning> stockWarnings = new List<StockWarning>();

        //    SqlCommand sqlSelectShortageStock = new SqlCommand();
        //    sqlSelectShortageStock.CommandText = $@" SELECT  LOG_RUNNING,[LOG_VD_CODE],LOG_VD_NAME 
        //         FROM [dbSCM].[dbo].[DO_LOG_STOCK_WARNING] 
        //         where [REVESION] = 999 and LOG_DATE >= CONVERT(DATE,GETDATE()) and LOG_DATE <= CONVERT(DATE,GETDATE() + {holiday}) 
        //GROUP BY LOG_VD_CODE,LOG_VD_NAME,LOG_RUNNING
        //         ORDER BY LOG_VD_CODE";
        //    DataTable dtSelectShortageStock = dbSCM.Query(sqlSelectShortageStock);

        //    if (dtSelectShortageStock.Rows.Count > 0)
        //    {
        //        foreach (DataRow dr in dtSelectShortageStock.Rows)
        //        {

        //            StockWarning mainWarining = new StockWarning();
        //            string format = "yyyyMMddHHmm";

        //            string vdCode = dr["LOG_VD_CODE"].ToString();


        //            mainWarining.dateRound = DateTime.ParseExact(dr["LOG_RUNNING"].ToString(), format, CultureInfo.InvariantCulture);

        //            mainWarining.vdCode = dr["LOG_VD_CODE"].ToString();
        //            mainWarining.vdName = dr["LOG_VD_NAME"].ToString();
        //            //mainWarining.item = Convert.ToInt16(dr["ITEM"]);

        //            int round = 0;
        //            int colorIndex = 0;
        //            SqlCommand sqlGroupVender = new SqlCommand();
        //            sqlGroupVender.CommandText = $@"
        //   				  SELECT [LOG_PARTNO],[LOG_CM],[LOG_DESC]
        //                ,[LOG_STOCK],[LOG_DO],[LOG_DATE]
        //                 FROM [dbSCM].[dbo].[DO_LOG_STOCK_WARNING] 
        //                 where [REVESION] = 999 and LOG_VD_CODE = '{vdCode}' and LOG_DATE >= CONVERT(DATE,GETDATE()) and LOG_DATE <= CONVERT(DATE,GETDATE() + {holiday}) ";

        //            DataTable dtGroupVender = dbSCM.Query(sqlGroupVender);

        //            if (dtGroupVender.Rows.Count > 0)
        //            {

        //                List<VenderGroup> groupList = new List<VenderGroup>();

        //                foreach (DataRow drGroupVender in dtGroupVender.Rows)
        //                {



        //                    VenderGroup venderGroup = new VenderGroup();




        //                    int _do = (Convert.IsDBNull(drGroupVender["LOG_DO"]) ? 0 : Convert.ToInt32(drGroupVender["LOG_DO"]));
        //                    int stock = 0;





        //                    if (Convert.ToDateTime(drGroupVender["LOG_DATE"]).ToString("yyyyMMdd") == DateTime.Now.ToString("yyyyMMdd"))
        //                    {
        //                        stock = Convert.ToInt32(drGroupVender["LOG_STOCK"]) - _do;
        //                    }
        //                    else
        //                    {
        //                        stock = Convert.ToInt32(drGroupVender["LOG_STOCK"]);
        //                    }




        //                    venderGroup.date = Convert.ToDateTime(drGroupVender["LOG_DATE"]);
        //                    venderGroup.partNo = drGroupVender["LOG_PARTNO"].ToString();
        //                    venderGroup.cm = drGroupVender["LOG_CM"].ToString();
        //                    venderGroup.description = drGroupVender["LOG_DESC"].ToString();
        //                    venderGroup.stock = stock;
        //                    venderGroup._do = _do;

        //                    if ((round == 0 ? venderGroup.partNo : groupList[round - 1].partNo) != venderGroup.partNo)
        //                    {
        //                        colorIndex++;
        //                    }

        //                    venderGroup.color = color[colorIndex];


        //                    groupList.Add(venderGroup);

        //                    round++;



        //                }

        //                mainWarining._venderGroup = groupList;
        //            }

        //            mainWarining.item = mainWarining._venderGroup.Count();
        //            stockWarnings.Add(mainWarining);

        //        }
        //    }


        //    return Ok(stockWarnings);


        //}



        [HttpGet]
        [Route("/DOWarining")]

        public IActionResult DOWarining()
        {
            int period = GetPeriod(DateTime.Now, 3);
            string[] color = new string[2] { "bg-white", "bg-gray-200" };
            List<StockWarning> stockWarnings = new List<StockWarning>();
            SqlCommand sqlSelectShortageStock = new SqlCommand();
            sqlSelectShortageStock.CommandText = $@" SELECT  LOG_RUNNING,[LOG_VD_CODE],LOG_VD_NAME 
                 FROM [dbSCM].[dbo].[DO_LOG_STOCK_WARNING] 
                 where [REVESION] = 999 and LOG_DATE >= CONVERT(DATE,GETDATE()) and LOG_DATE <= CONVERT(DATE,GETDATE() + {period}) 
			     GROUP BY LOG_VD_CODE,LOG_VD_NAME,LOG_RUNNING
                 ORDER BY LOG_VD_CODE";
            DataTable dtSelectShortageStock = dbSCM.Query(sqlSelectShortageStock);

            if (dtSelectShortageStock.Rows.Count > 0)
            {
                foreach (DataRow dr in dtSelectShortageStock.Rows)
                {
                    StockWarning mainWarining = new StockWarning();
                    string format = "yyyyMMddHHmm";
                    string vdCode = dr["LOG_VD_CODE"].ToString();
                    mainWarining.dateRound = DateTime.ParseExact(dr["LOG_RUNNING"].ToString(), format, CultureInfo.InvariantCulture);
                    mainWarining.vdCode = dr["LOG_VD_CODE"].ToString();
                    mainWarining.vdName = dr["LOG_VD_NAME"].ToString();
                    //mainWarining.item = Convert.ToInt16(dr["ITEM"]);

                    int round = 0;
                    int colorIndex = 0;
                    SqlCommand sqlGroupVender = new SqlCommand();
                    sqlGroupVender.CommandText = $@"
			        				  SELECT [LOG_PARTNO],[LOG_CM],[LOG_DESC]
				                    ,[LOG_STOCK],[LOG_DO],[LOG_DATE]
				                     FROM [dbSCM].[dbo].[DO_LOG_STOCK_WARNING] 
				                     where [REVESION] = 999 and LOG_VD_CODE = '{vdCode}' and LOG_DATE >= CONVERT(DATE,GETDATE()) and LOG_DATE <= CONVERT(DATE,GETDATE() + {period}) ";

                    DataTable dtGroupVender = dbSCM.Query(sqlGroupVender);

                    if (dtGroupVender.Rows.Count > 0)
                    {

                        List<VenderGroup> groupList = new List<VenderGroup>();

                        foreach (DataRow drGroupVender in dtGroupVender.Rows)
                        {



                            VenderGroup venderGroup = new VenderGroup();




                            int _do = (Convert.IsDBNull(drGroupVender["LOG_DO"]) ? 0 : Convert.ToInt32(drGroupVender["LOG_DO"]));
                            int stock = 0;





                            if (Convert.ToDateTime(drGroupVender["LOG_DATE"]).ToString("yyyyMMdd") == DateTime.Now.ToString("yyyyMMdd"))
                            {
                                stock = Convert.ToInt32(drGroupVender["LOG_STOCK"]) - _do;
                            }
                            else
                            {
                                stock = Convert.ToInt32(drGroupVender["LOG_STOCK"]);
                            }




                            venderGroup.date = Convert.ToDateTime(drGroupVender["LOG_DATE"]);
                            venderGroup.partNo = drGroupVender["LOG_PARTNO"].ToString();
                            venderGroup.cm = drGroupVender["LOG_CM"].ToString();
                            venderGroup.description = drGroupVender["LOG_DESC"].ToString();
                            venderGroup.stock = stock;
                            venderGroup._do = _do;

                            if ((round == 0 ? venderGroup.partNo : groupList[round - 1].partNo) != venderGroup.partNo)
                            {

                                if (round == 0 || colorIndex >= 1)
                                {
                                    colorIndex = 0;
                                }
                                else
                                {
                                    colorIndex++;
                                }

                            }

                            venderGroup.color = color[colorIndex];


                            groupList.Add(venderGroup);

                            round++;



                        }

                        mainWarining._venderGroup = groupList;
                    }

                    mainWarining.item = mainWarining._venderGroup.Count();
                    stockWarnings.Add(mainWarining);

                }
            }


            return Ok(stockWarnings);


        }




        [HttpGet]
        [Route("/getPartMstrPU/{vdcode}/{empcode}")]

        public IActionResult getPartMstrPU(string vdcode, string empcode)
        {

            List<DOPartNumberForPU> dOPartNumberForPUs = new List<DOPartNumberForPU>();

            OracleCommand cmd = new();
            cmd.CommandText = @"SELECT HTCODE , PARTNO, BRUSN as CM, KOUBHM as Description, SYUYO BOX_QTY, MINQTY MIN_BOX
                                FROM ND_ZUBGPU_WK
                                where HTCODE = '" + vdcode + "'";
            DataTable dt = dbAlpha.Query(cmd);

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    DOPartNumberForPU dOPartNumberForPU = new DOPartNumberForPU();

                    dOPartNumberForPU.vdcode = vdcode;
                    dOPartNumberForPU.partno = dr["PARTNO"].ToString().Trim() + " " + dr["CM"].ToString().Trim();
                    dOPartNumberForPU.cm = dr["CM"].ToString().Trim();
                    dOPartNumberForPU.desc = dr["Description"].ToString();
                    dOPartNumberForPU.boxQty = Convert.ToInt16(dr["BOX_QTY"]);
                    dOPartNumberForPU.minQty = Convert.ToInt16(dr["MIN_BOX"]);
                    dOPartNumberForPU.updateBy = empcode;

                    dOPartNumberForPUs.Add(dOPartNumberForPU);

                }

            }



            return Ok(dOPartNumberForPUs);
        }



        [HttpGet]
        [Route("/deleteRadis")]
        public IActionResult deleteRadis()
        {
            string redisConnectionString = $"{redisHost}:{redisPort}"; // Replace with your Redis server's address and port

            try
            {
                // Connect to the Redis server
                var redis = ConnectionMultiplexer.Connect(redisConnectionString);

                if (redis.IsConnected)
                {

                    // Get a database instance
                    IDatabase db = redis.GetDatabase();
                    bool wasDeleted = db.KeyDelete("CAL_DO");

                    if (wasDeleted)
                    {
                        Console.WriteLine("The 'name' key was deleted from Redis.");
                    }
                    else
                    {
                        Console.WriteLine("The 'name' key was not found or could not be deleted.");
                    }

                    // Try to get the value again after deletion
                    string nameAfterDelete = db.StringGet("DO_PrdPlan_20250121");
                    Console.WriteLine($"After deletion: {nameAfterDelete}");


                }

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return Ok();
        }




        [HttpGet]
        [Route("/update/partMaster/WH")]
        public IActionResult updateMasterWH()
        {



            SqlCommand sqlGetMasterWH = new SqlCommand();
            sqlGetMasterWH.CommandText = $@"
			         SELECT distinct [PART_ID],[PARTNO],[CM]
                     FROM [dbSCM].[dbo].[DO_PART_MASTER]
                     where ACTIVE = 'ACTIVE'";

            DataTable dtupdateMasterWH = dbSCM.Query(sqlGetMasterWH);
            foreach (DataRow dr in dtupdateMasterWH.Rows)
            {
                string cm = dr["CM"].ToString() == "" ? " " : dr["CM"].ToString();

                OracleCommand cmd = new();
                cmd.CommandText = @"SELECT PARTNO,CM,LOCA1 FROM DST_MSTSB1
                                where PARTNO = '" + dr["PARTNO"] + "' and CM = '" + cm + "'";
                DataTable dtcmd = dbAlpha2.Query(cmd);
                if (dtcmd.Rows.Count > 0)
                {
                    foreach (DataRow orcalCMD in dtcmd.Rows)
                    {
                        string compareLocal = orcalCMD["LOCA1"].ToString().StartsWith("A") ? "WH1" :
                            (orcalCMD["LOCA1"].ToString().StartsWith("B") ? "WH2" : (orcalCMD["LOCA1"].ToString().StartsWith("AX") ? "CANCE" :

                            (orcalCMD["LOCA1"].ToString().StartsWith("NEW") ? "NEW" : "")
                            ));


                        SqlCommand sqlupdateMasterWH = new SqlCommand();
                        sqlupdateMasterWH.CommandText = $@"UPDATE [dbSCM].[dbo].[DO_PART_MASTER]
                                                           SET WH_NO = '{compareLocal}'
                                                           WHERE PARTNO = '{orcalCMD["PARTNO"].ToString()}' and CM = '{orcalCMD["CM"]}'";
                        int insert = dbSCM.ExecuteNonCommand(sqlupdateMasterWH);



                    }
                }

            }
            return Ok();
        }


        public class PeriodInfo
        {

            public string startdate { set; get; }
            public string enddate { set; get; }
        }

        [NonAction]
        public int GetPeriod(DateTime dataDate, int period)
        {
            PeriodInfo mRes = new PeriodInfo();

            OracleCommand strGetPo = new OracleCommand();
            strGetPo.CommandText = $@"SELECT YYDAY, NENDO
                                     FROM ND_CAL_TBL_V1
                                     WHERE JIBU='64' AND CAL='CAL' AND YYDAY BETWEEN '{dataDate.ToString("yyyyMMdd")}' AND '{dataDate.AddDays(30).ToString("yyyyMMdd")}' 
                                            AND FUKAKU = 0  ";
            DataTable dt = dbAlpha.Query(strGetPo);

            int loop = 0, loopday = 0;
            while (loop < period)
            {
                DataRow[] dr = dt.Select($" YYDAY='{dataDate.AddDays(loopday).ToString("yyyyMMdd")}' ");
                //if (dr.Length > 0)
                //{
                //    if (loop == 0)
                //    {
                //        mRes.startdate = dataDate.AddDays(loopday).ToString("yyyyMMdd");
                //    }
                //    else if (loop == period)
                //    {
                //        mRes.enddate = dataDate.AddDays(loopday).ToString("yyyyMMdd");
                //    }
                //    loop++;
                //}

                //loopday++;

                if (dr.Length > 0)
                {

                }
                else
                {
                    loop++;
                }
                loopday++;
            }

            return loopday;

        }

//        [HttpGet]
//        [Route("/IT_TEST_RERESH_DO_FROM_LOH")]
//        public IActionResult IT_TEST()
//        {
//            SqlCommand sql = new SqlCommand();
//            sql.CommandText = $@"SELECT A.* FROM (SELECT ROW_NUMBER() OVER (PARTITION BY DO_RUNNING,[LOG_VD_CODE],LOG_PART_NO,LOG_TO_DATE ORDER BY LOG_ID desc)  RN
//, [LOG_ID]
//,[LOG_VD_CODE]
//      ,[DO_RUNNING] 
//      ,[LOG_PART_NO]  
//      ,[LOG_TO_DATE]  
//      ,[LOG_NEXT_DO] 
//	  ,[LOG_CREATE_DATE]
//  FROM [dbSCM].[dbo].[DO_LOG_DEV]
//  WHERE DO_RUNNING = '20250225'   
//  ) A
//  WHERE A.RN = 1 ";
//            DataTable dt = dbSCM.Query(sql);
//            foreach (DataRow dr in dt.Rows)
//            {
//                string running_code = dr["DO_RUNNING"].ToString();
//                string partno = dr["LOG_PART_NO"].ToString();
//                string date_val = dr["LOG_TO_DATE"].ToString();
//                string do_val = dr["LOG_NEXT_DO"].ToString();
//                string vd_code = dr["LOG_VD_CODE"].ToString();
//                SqlCommand sqlupdate = new SqlCommand();
//                sqlupdate.CommandText = $@"update [dbSCM].[dbo].[DO_HISTORY_DEV] set  do_val = '{do_val}' WHERE RUNNING_CODE = '20250226' AND PARTNO = '{partno}' AND DATE_VAL = '{date_val}' AND VD_CODE = '{vd_code}'";
//                int update = dbSCM.ExecuteNonCommand(sqlupdate);
//                if (update <= 0)
//                {
//                    Console.WriteLine("asdasd");
//                }
//            }
//            return Ok();
//        }
        //[HttpGet]
        //[Route("/GetListSupplierOfBuyer/{buyer}")]
        //public IActionResult GetListSupplierOfBuyer(string buyer)
        //{
        //    SqlCommand sql = new SqlCommand();
        //    sql.CommandText = $@"SELECT  CODE BUYER,REF_CODE SUPPLIER FROM [dbSCM].[dbo].[DO_DictMstr]   WHERE DICT_TYPE = 'BUYER' AND CODE = '{buyer}' AND DICT_STATUS = 'ACTIVE' GROUP BY CODE ,REF_CODE ";
        //    DataTable dt = dbSCM.Query(sql);
        //    return Ok(serv.DataTableToJson(dt));
        //}
        //[HttpGet]
        //[Route("/GetBuyers")]
        //public IActionResult GetBuyers()
        //{
        //    SqlCommand sql = new SqlCommand();
        //    List<string> buyerCode = new List<string>();
        //    List<string> buyerName = new List<string>();
        //    sql.CommandText = $@"SELECT  DICT.CODE BUYER,DICT.REF_CODE SUPPLIER,EM.PREN + EM.NAME + ' ' + EM.SURN FULLNAME FROM [dbSCM].[dbo].[DO_DictMstr] DICT  
        //                         LEFT JOIN [dbHRM].[dbo].[Employee] EM
        //                         ON EM.CODE  COLLATE Thai_CI_AS = DICT.CODE  COLLATE Thai_CI_AS
        //                         WHERE DICT.DICT_TYPE = 'BUYER' AND DICT.DICT_STATUS = 'ACTIVE'
        //                         AND EM.CODE IS NOT NULL";
        //    DataTable dt = dbSCM.Query(sql);
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        string buyer = dr["BUYER"].ToString();
        //        string supplier
        //    }
        //    return Ok(new
        //    {
        //        buyerCode = serv.DataTableToJson(),
        //        supplier = serv.DataTableToJson(dt)
        //    });
        //}
    }
}
