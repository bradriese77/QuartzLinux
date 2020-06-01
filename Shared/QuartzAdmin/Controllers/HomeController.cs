using Newtonsoft.Json;
using Quartz;
using QuartzAdmin.Models;
using QuartzServiceLib;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace QuartzAdmin.Controllers
{



    public class HomeController : PagedController<QRTZ_JOB_HISTORY>
    {
        public ActionResult Index()
        {
            ViewBag.Title = "CrystalQuartz";

            return View();
        }

        public ActionResult LogViewer(string LogFilePath)
        {
            ViewBag.Title = "LogViewer";

            return View();
        }
        public ActionResult QuartzMin()
        {
            ViewBag.Title = "QuartzMin";

            return View();
        }
        public ActionResult JobHistory()
        {
            ViewBag.Title = "JobHistory";
           
            return View();
        }
        public ActionResult Exit()
        {
         
            return View("Exit");

        }
        QuartzJobStoreSettings quartzJobStoreSettings = new QuartzJobStoreSettings(false);

        [Route("GetLogFiles")]
        public  JsonResult GetLogFiles()
        {

            QuartzScheduler context = new QuartzScheduler(quartzJobStoreSettings.QuartzConnectionString);

            var list=QuartzProgram<AdminController>.GetJobHistory().OrderByDescending(l=>l.STARTDATE).Select(s => s.LOGFILE).Distinct().ToArray();

            return Json(list, JsonRequestBehavior.AllowGet);

        }

        [Route("GetJobHistory")]
        public async Task<JsonResult> GetJobHistory()
        {
            int count = 0;
            QuartzScheduler context = new QuartzScheduler(quartzJobStoreSettings.QuartzConnectionString);

            var dataTableData = new DataTableData<QRTZ_JOB_HISTORY>();
            var gridData = context.QRTZ_JOB_HISTORY.ToList();//new List<QRTZ_JOB_HISTORY>();


            PagingData pgData = new PagingData(Request.QueryString);
          

            Expression<Func<QRTZ_JOB_HISTORY, bool>> where = w => (
                 w.JOB_NAME.Contains(pgData.Search?? ""));
            /*   Expression<Func<Order, bool>> where = w => (
                w.OrderNumber != "");*/
            dataTableData.draw = pgData.Draw;


            //   
            count = gridData.Count;

            gridData = await GetList(gridData.Where(d => d.JOB_NAME != "").AsQueryable().OrderByDescending(d => d.STARTDATE).ThenBy(d=>d.JOB_NAME), pgData, where);
            dataTableData.data = gridData;
            dataTableData.recordsFiltered =count;
            dataTableData.recordsTotal = count;
            dataTableData.success = true;

            return Json(dataTableData, JsonRequestBehavior.AllowGet);

        }
    }
}
