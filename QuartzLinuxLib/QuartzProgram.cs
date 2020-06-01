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
using AppUtilityLib;
using System.Collections.Specialized;
using log4net.Config;
using System.IO;
using log4net;

namespace QuartzServiceLib
{
    public abstract class QuartzProgram<T> : DataUtility
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
            //object o = Activator.CreateInstance(typeof(T));
            //object ot = o.GetType().GetProperty("JobTypes").GetValue(o);
            //return (Type[])ot;
            return GetQuartzServiceJobs().ToArray();
        }
        #endregion
        public static IEnumerable<Type> GetQuartzServiceJobs()
        {
            List<Type> types = new List<Type>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        if (typeof(IQuartzServiceJob).IsAssignableFrom(t))
                        {
                            try
                            {
                                if(t.IsAbstract==false)
                                types.Add(t);
                            }
                            catch (Exception ex)
                            {
                                string s = ex.ToString();

                            }
                        }
                    }
                } catch (Exception ex2)
                {

                    string s = ex2.ToString();
                }
            }
            return types;
        }
        public void StartLogging()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        }
        public static void ServiceMain(string[] args)
        {

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

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
                

        }
        public void AddDefaultQuartzJobs()
        {
            ScheduleReflectionJobTypes(GetJobTypes());

        }
        public void AddDefaultQuartzJob(Type t,bool UseReflectionJobType=true)
        {
            if(UseReflectionJobType)
            ScheduleReflectionJobTypes(new Type[] { t });
            else
            {
                ScheduleJobTypes(new Type[] { t });
            }
        }
        public void AddQuartzJobs()
        {
            ScheduleJobTypes(GetJobTypes());

        }
        public void AddQuartzJob(string TypeName)
        {
            ScheduleReflectionJobTypes(GetJobTypes().Where(t => t.Name == TypeName).ToArray());
        }
       
        public static ITrigger ScheduleCustomJobType(Type jobType, string JobName, string JobGroup, string TriggerName, string TriggerGroup, DateTimeOffset StartAt, IDictionary JobData,bool UseReflectionJobType)
        {

            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            ITrigger Schedule = TriggerBuilder.Create().StartAt(StartAt).WithIdentity(TriggerName, TriggerGroup).Build();
            JobDataMap map = new JobDataMap(JobData);
            if(UseReflectionJobType)return quartz.ScheduleCustomReflectionJob(jobType, JobName, JobGroup, map, Schedule);
            return quartz.ScheduleCustomJob(jobType, JobName, JobGroup, map, Schedule);
        }
        public static ITrigger ScheduleCustomJobTypeWithCronExpression(Type jobType, string JobName, string JobGroup, string TriggerName, string TriggerGroup, string CronExpression, IDictionary JobData, bool UseReflectionJobType)
        {

            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            ITrigger Schedule = TriggerBuilder.Create().WithCronSchedule(CronExpression).WithIdentity(TriggerName, TriggerGroup).Build();
            JobDataMap map = new JobDataMap(JobData);
            if (UseReflectionJobType) return quartz.ScheduleCustomReflectionJob(jobType, JobName, JobGroup, map, Schedule);
            return quartz.ScheduleCustomJob(jobType, JobName, JobGroup, map, Schedule);
        }

       
        public static void AddJobDetailsFromTypes(Type[] types, bool Replace)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            quartz.AddJobDetails(quartz.GetJobsFromTypes(types), Replace);


        }
        public static void ScheduleReflectionJobTypes(Type[] types)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            quartz.ScheduleReflectionJobTypes(types);

        }

        public static void ScheduleJobTypes(Type[] types)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            quartz.ScheduleJobTypes(quartz.GetJobsFromTypes(types));

        }
        protected static void Start(string[] args)
        {
            try
            {
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
                _log.Info("Starting");
                quartz = new QuartzWrapper(new QuartzJobStoreSettings(true).GetQuartzSettings());

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
        //public static QuartzScheduler scheduler = new QuartzScheduler(quartzJobStoreSettings.QuartzConnectionString);
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

        public static void AddJobData(string JobName, string Key, object Value,string JobGroup = "")
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            List<IJobDetail> Jobs = GetJobDetail(JobName, JobGroup);
            Jobs.ForEach(j => { j.JobDataMap[Key] = Value; if (j.JobDataMap[Key] == null) { j.JobDataMap.Put(Key, Value); quartz.Scheduler.AddJob(j, false, true).GetAwaiter().GetResult(); ; } });
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
            
            quartz?.StopQuartz();
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

