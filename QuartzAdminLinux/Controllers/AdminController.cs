using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http.Headers;
using Quartz.Impl;
using QuartzServiceLib;
using Quartz;
using Newtonsoft.Json;
using QuartzAdminLinux.Models;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace QuartzAdmin.Controllers
{
   // [Authorize]
    public class AdminController : System.Web.Http.ApiController
    {
        public class JobCriteria
        {
            public string JobName { get; set; }
            public string JobGroup { get; set; }
        }
        public class TriggerCriteria : JobCriteria
        {
            public string TriggerName { get; set; }
            public string TriggerGroup { get; set; }

        }
        public class CronScheduleCriteria : TriggerCriteria
        {
            public string CronExpression { get; set; }
        }

        public class JobHistoryCriteria:TriggerCriteria
        {
            public string StartDate { get; set; }
            public string EndtDate { get; set; }
            public bool IsException { get; set; }
        }


        [Route("ResumeTriggers")]
        public HttpResponseMessage ResumeTriggers([FromBody]TriggerCriteria triggerCriteria)
        {
            try

            {
                QuartzProgram<AdminController>.ResumeTriggers(triggerCriteria.JobName, triggerCriteria?.TriggerName, triggerCriteria.JobGroup, triggerCriteria?.TriggerGroup);
                return Request.CreateResponse(HttpStatusCode.OK, $"Successfully Resumed Triggers");
            }
            catch(Exception ex)
            {
               return Request.CreateResponse(HttpStatusCode.InternalServerError,$"Failed to ResumeTriggers due to {ex.ToString()}");

            }
        }
     
       
    

       [ Route("PauseTriggers")]
        public HttpResponseMessage PauseTriggers([FromBody]TriggerCriteria triggerCriteria)
        {
            try
            {
                QuartzProgram<AdminController>.PauseTriggers(triggerCriteria.JobName, triggerCriteria?.TriggerName, triggerCriteria.JobGroup, triggerCriteria?.TriggerGroup);

                return Request.CreateResponse(HttpStatusCode.OK, $"Successfully Paused Triggers");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to Pause Triggers due to {ex.ToString()}");

            }
        }
       [ Route("SearchJobHistory")]
        public HttpResponseMessage SearchJobHistory([FromBody]JobHistoryCriteria jobHistoryCriteria)
        {
            try
            {
            DateTime? StartDate = null;
            DateTime? EndDate = null;
            DateTime ParseDate;

            if (DateTime.TryParse(jobHistoryCriteria?.StartDate, out ParseDate)) StartDate = ParseDate;
            if (DateTime.TryParse(jobHistoryCriteria?.EndtDate, out ParseDate)) EndDate = ParseDate;

                return Request.CreateResponse(HttpStatusCode.OK, QuartzProgram<AdminController>.GetJobHistory(jobHistoryCriteria?.JobName, jobHistoryCriteria?.TriggerName, StartDate, EndDate, jobHistoryCriteria?.JobGroup, jobHistoryCriteria?.TriggerGroup));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to Search Job History due to {ex.ToString()}");

            }

        }
       [ Route("RunJobNow")]
        public HttpResponseMessage RunJobNow([FromBody]JobCriteria jobCriteria)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, QuartzProgram<AdminController>.RunJobNow(jobCriteria.JobName, jobCriteria.JobGroup));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to RunJobNow due to {ex.ToString()}");
            }
        }

        
       [ Route("RunJobWithCronExpression")]
        public HttpResponseMessage RunJobWithCronExpression([FromBody]CronScheduleCriteria cronScheduleCriteria)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, QuartzProgram<AdminController>.RunJobWithCronExpression(cronScheduleCriteria.JobName, cronScheduleCriteria.CronExpression, cronScheduleCriteria.TriggerName, cronScheduleCriteria.TriggerGroup, false));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to RunJobWithCronExpression due to {ex.ToString()}");
            }
         
        }
       [ Route("ChangeJobWithCronExpression")]
        public HttpResponseMessage ChangeJobWithCronExpression([FromBody]CronScheduleCriteria cronScheduleCriteria)
        {

            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, QuartzProgram<AdminController>.RunJobWithCronExpression(cronScheduleCriteria.JobName, cronScheduleCriteria.CronExpression, cronScheduleCriteria.TriggerName, cronScheduleCriteria.TriggerGroup, true));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to ChangeJobWithCronExpression due to {ex.ToString()}");
            }
        }
       [ Route("SearchJobDetail")]
        public HttpResponseMessage SearchJobDetail([FromBody] TriggerCriteria triggerCriteria)
        {
            try
            {

                return Request.CreateResponse(HttpStatusCode.OK, QuartzProgram<AdminController>.GetJobDetail(triggerCriteria.JobName, triggerCriteria.TriggerName, triggerCriteria.JobGroup, triggerCriteria.TriggerGroup));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to SearchJobDetail due to {ex.ToString()}");
            }
   
        }

        [Route("GetLogFile")]
        public HttpResponseMessage GetLogFile(string LogFilePath)
        {
            try
            {

                string Ext = Path.GetExtension(LogFilePath);

                if (Ext.ToLower() != ".log")
                {
                    throw new Exception($"{Ext} is not a valid Log File Extension");

                }
                else if (!File.Exists(LogFilePath))
                {
                    throw new Exception($"{LogFilePath} does not exist");

                }

                return Request.CreateResponse(HttpStatusCode.OK, File.ReadAllLines(LogFilePath));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to GetLogFile due to {ex.ToString()}");
            }
        }
       // [Route("GetJobTypes/{AssemblyName}")]
       // public HttpResponseMessage GetJobTypes(string AssemblyName)
       // {
       //     try
       //     {
       //         return Request.CreateResponse(HttpStatusCode.OK, QuartzService.Program.GetJobTypes().Where(jt => string.IsNullOrEmpty(AssemblyName) || AssemblyName == "All" || jt.Assembly.GetName().Name == AssemblyName).Select(jt => new { jt.Name, Group = jt.Assembly.GetName().Name }), Configuration.Formatters.JsonFormatter);
       //     }
       //     catch (Exception ex)
       //     {
       //         return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to SearchJobDetail due to {ex.ToString()}");
       //     }
       // }
       
       //[ Route("AddJobDetailsFromAssemblyClass/{AssemblyName}/{ClassName}")]
        //public HttpResponseMessage AddJobDetailsFromAssemblyClass(string AssemblyName,string ClassName)
        //{
        //    try
        //    {
        //        Type[] jobTypes = QuartzService.Program.GetJobTypes().Where(jt => jt.Name == ClassName && jt.Assembly.GetName().Name==AssemblyName).ToArray();
        //        if (jobTypes == null || jobTypes.Count() == 0) throw new Exception($"{ClassName} does not appear to be a valid PayrollReporting JobType");
        //        QuartzProgram<AdminController>.AddJobDetailsFromTypes(jobTypes,false);

        //    }catch(Exception ex)
        //    {
        //       return Request.CreateResponse(HttpStatusCode.InternalServerError,$"Failed to add {ClassName} due to {ex.ToString()}");

        //    }
        //    return Request.CreateResponse(HttpStatusCode.OK, $"Successfully Added {ClassName}");

        //}
    }
}
