using System;
using System.Linq;
//using System.Data.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using System.ServiceProcess;
using System.Configuration;
using System.Reflection;
using QuartzServiceLib;
using QuartzUtilityLib;
using AppUtilityLib;
using System.Collections.Specialized;
using log4net;
using log4net.Config;
using System.IO;

namespace QuartzServiceLib
{
    public abstract class QuartzProgram<T> : AppUtility
    {

        static readonly log4net.ILog _log =
      log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Nested classes to support running as service

        //public class Service : ServiceBase
        //{
        //    public Service()
        //    {
        //        ServiceName = QuartzServiceInstaller.ServiceName;
        //    }

        //    protected override void OnStart(string[] args)
        //    {
        //        log4net.Config.XmlConfigurator.Configure();
        //        Start(args);
        //    }

        //    protected override void OnStop()
        //    {
        //        log4net.Config.XmlConfigurator.Configure();
        //        StopIt();
        //    }
        //}

        public static string jobProviderAssembly = typeof(QuartzProgram<T>).Assembly.Location;
        public static string jobProviderType = typeof(QuartzProgram<T>).FullName;

        public static Type[] GetJobTypes()
        {
            object o = Activator.CreateInstance(typeof(T));
            object ot = o.GetType().GetProperty("JobTypes").GetValue(o);
            return (Type[])ot;
        }
        #endregion

        public static void ServiceMain(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            string configstr = Assembly.GetEntryAssembly().Location + ".config";
            Console.WriteLine(configstr);
            XmlConfigurator.Configure(logRepository, new FileInfo(configstr));
          


            //if (args.Length == 1)
            //{
            //    switch (args[0].ToLower())
            //    {

            //        case "-install":
            //            QuartzServiceInstaller.InstallService();
            //            break;
            //        case "-uninstall":
            //            StopIt();
            //            QuartzServiceInstaller.UninstallService();
            //            break;
            //        default:
            //            throw new NotImplementedException();
            //    }
            //}

            //else
            //{

            //    if (!Environment.UserInteractive)
            //        // running as service
            //        using (var service = new Service())
            //            ServiceBase.Run(service);
            //    else
            //    {
            //        // running as console app
                    try
                    {
                        Start(args);
                        Console.WriteLine("Press any key to stop...");
                        Console.ReadKey(true);
                        StopIt();
                    } catch (Exception ex)
                    {

                        _log.Error(ex);
                    }
            //    }
            //}
            }

        //public void UnInstall()
        //{
        //    StopIt();
        //    QuartzServiceInstaller.UninstallService();
        //}
        //public void Install()
        //{

        //    QuartzServiceInstaller.InstallService();

        //}
        public void CreateQuartzSchedulerDatabase(string ConnectionStr)
        {
            if (ConnectionStr == string.Empty)
            {
                ConnectionStr = "data source=localhost;initial catalog=Quartz;integrated security=True";
            }
            //QuartzScheduler context = new QuartzScheduler(ConnectionStr);
            //context.CreateDatabase();

        }
        public static void AddJobDetailsFromTypes(Type[] types,bool Replace)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings()); 
            quartz.AddJobDetails(quartz.GetJobsFromTypes(types),Replace);
            
            quartz.Scheduler = null;
            quartz = null;
        }
        
        public static void ScheduleJobTypes(Type[] types)
        {
            quartz.ScheduleJobTypes(quartz.GetJobsFromTypes(types));

        }
        protected static void Start(string[] args)
        {
            try
            {
                QuartzJobStoreSettings settings = new QuartzJobStoreSettings(true);
                Console.WriteLine(settings.QuartzTcpServer);
                quartz = new QuartzWrapper(settings.GetQuartzSettings());
                Console.WriteLine();
                quartz.StartQuartz();

                if (!quartzJobStoreSettings.UseSqlServerQuartz)
                    ScheduleJobTypes(GetJobTypes());
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw ex;
            }


        }
        private static readonly QuartzJobStoreSettings quartzJobStoreSettings = new QuartzJobStoreSettings(false);
        private static QuartzWrapper quartz; 
      //  public static QuartzScheduler scheduler = new QuartzScheduler(quartzJobStoreSettings.QuartzConnectionString);
        public static List<ITrigger> GetJobTriggers(JobKey jobkey)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            var response=quartz.Scheduler.GetTriggersOfJob(jobkey).GetAwaiter().GetResult().ToList();
            quartz.Scheduler = null;
            quartz = null;
            return response;
        }
        public static List<IJobDetail> GetJobDetail(string JobName = "", string TriggerName = "", string JobGroup = "", string TriggerGroup = "")
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            var jobs = from j in quartz.GetJobs() where (string.IsNullOrEmpty(JobName) || j.Key.Name == JobName) && (string.IsNullOrEmpty(JobGroup) || j.Key.Group == JobGroup) select j;
            var response = jobs.ToList();
            quartz.Scheduler = null;
            quartz = null;
            return response;
        }

        public static List<ITrigger> RunJobWithCronExpression(string JobName,string CronExpression,string TriggerName,string JobGroup = "",bool ReplaceExisting=true)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            List<ITrigger> TriggerList = new List<ITrigger>();
            List<IJobDetail> Jobs = GetJobDetail(JobName, JobGroup);

            Jobs.ForEach(j => {
                ITrigger t=TriggerBuilder.Create().WithSchedule(CronScheduleBuilder.CronSchedule(CronExpression).WithMisfireHandlingInstructionFireAndProceed()).WithIdentity(TriggerName,JobName).Build();
                TriggerList.Add(t);
                quartz.Scheduler.ScheduleJob(j, new ITrigger[] { t }, ReplaceExisting).GetAwaiter().GetResult();
            });

            quartz.Scheduler = null;
            quartz = null;
            return TriggerList;
        }
        public static List<ITrigger> RunJobNow(string JobName, string JobGroup = "",string TriggerName="",string TriggerGroup="")
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            List<ITrigger> TriggerList = new List<ITrigger>();
            List<IJobDetail> Jobs = GetJobDetail(JobName, JobGroup);
            Jobs.ForEach(j => {
            ITrigger t = TriggerBuilder.Create().WithIdentity(string.IsNullOrEmpty(TriggerName)?j.Key.Name + "Now":TriggerName, string.IsNullOrEmpty(TriggerGroup)?j.Key.Group:TriggerGroup).StartNow().Build();
            TriggerList.Add(t);
            quartz.Scheduler.ScheduleJob(j, t).GetAwaiter().GetResult();
            });

            quartz.Scheduler = null;
            quartz = null;
            return TriggerList;
        }
        public static void ResumeTriggers(string JobName = "", string TriggerName = "", string JobGroup = "", string TriggerGroup = "")
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            List<IJobDetail> Jobs = GetJobDetail(JobName, JobGroup);
            Jobs.ForEach(j => GetJobTriggers(j.Key).Where(t=> (string.IsNullOrEmpty(TriggerName) || t.Key.Name==TriggerName) && (string.IsNullOrEmpty(TriggerGroup) || t.Key.Group == TriggerGroup)).ToList().ForEach(t => quartz.Scheduler.ResumeTrigger(t.Key).GetAwaiter().GetResult()));
            quartz.Scheduler = null;
            quartz = null;
        }

        public static void PauseTriggers(string JobName = "", string TriggerName = "",string JobGroup = "", string TriggerGroup = "")
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            List<IJobDetail> Jobs = GetJobDetail(JobName, JobGroup);
            Jobs.ForEach(j => GetJobTriggers(j.Key).Where(t => (string.IsNullOrEmpty(TriggerName) || t.Key.Name == TriggerName) && (string.IsNullOrEmpty(TriggerGroup) || t.Key.Group == TriggerGroup)).ToList().ForEach(t => quartz.Scheduler.PauseTrigger(t.Key).GetAwaiter().GetResult()));
            quartz.Scheduler = null;
            quartz = null;
        }
        //public static List<QRTZ_JOB_HISTORY> GetJobHistory(string JobName="",string TriggerName="",DateTime? StartDate=null,DateTime? EndDate=null,string JobGroup="",string TriggerGroup="",bool? IsException=null)
        //{
       
        //    var history = from h in scheduler.QRTZ_JOB_HISTORY.ToList() where (string.IsNullOrEmpty(JobName) || h.JOB_NAME == JobName) && (string.IsNullOrEmpty(TriggerName) || h.TRIGGER_NAME == TriggerName) && (!StartDate.HasValue || h.STARTDATE>=StartDate.Value) && (!EndDate.HasValue || h.ENDDATE <= EndDate.Value) && (string.IsNullOrEmpty(JobGroup) || h.JOB_GROUP == JobGroup) && (string.IsNullOrEmpty(TriggerGroup) || h.TRIGGER_GROUP == TriggerGroup) && (!IsException.HasValue || h.ISEXCEPTION==IsException) select h;
        //    List<QRTZ_JOB_HISTORY> list = history.ToList<QRTZ_JOB_HISTORY>();
        //    return list;
        //}
        protected static void StopIt()
        {
            quartz.StopQuartz();
        }

        public static string JobProviderAssembly
        {
            get
            {
                return jobProviderAssembly;
            }

            set
            {
                jobProviderAssembly = value;
            }
        }

        public static string JobProviderType
        {
            get
            {
                return jobProviderType;
            }

            set
            {
                jobProviderType = value;
            }
        }

     
    }


}

