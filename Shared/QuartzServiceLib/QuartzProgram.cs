using System;
using System.Linq;
using System.Data.Linq;
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
using Newtonsoft.Json;
using System.IO;

namespace QuartzServiceLib
{
    public abstract class QuartzProgram<T> : DataUtility
    {

        static readonly log4net.ILog _log =
      log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Nested classes to support running as service

        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = QuartzServiceInstaller.ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                log4net.Config.XmlConfigurator.Configure();
                Start(args);
            }

            protected override void OnStop()
            {
                log4net.Config.XmlConfigurator.Configure();
                StopIt();
            }
        }

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
        public static void ServiceMain(string[] args)
        {

            log4net.Config.XmlConfigurator.Configure();


            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {

                    case "-install":
                        QuartzServiceInstaller.InstallService();
                        break;
                    case "-uninstall":
                        StopIt();
                        QuartzServiceInstaller.UninstallService();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            else
            {

                if (!Environment.UserInteractive)
                    // running as service
                    using (var service = new Service())
                        ServiceBase.Run(service);
                else
                {
                    // running as console app
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
            }
        }
        public void AddDefaultQuartzJobs()
        {
            ScheduleReflectionJobTypes(GetJobTypes());

        }
        public void AddQuartzJobs()
        {
            ScheduleJobTypes(GetJobTypes());

        }
        public void UpdateQuartzJobData()
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            IReadOnlyList<IQuartzServiceJob> jobs = quartz.GetJobsFromTypes(GetJobTypes());
            foreach (IQuartzServiceJob job in jobs)
            {
                IJobDetail d = job.JobDetail.Build();
                IJobDetail d2=quartz.Scheduler.GetJobDetail(d.Key).Result;
                bool DoUpdate = false;
                if (d2 != null)
                {
                    foreach(string Key in d.JobDataMap.Keys)
                    {
                        d2.JobDataMap[Key] = d.JobDataMap[Key];
                        DoUpdate = true;
                    }
                    if(DoUpdate)
                    quartz.Scheduler.AddJob(d2, true).GetAwaiter().GetResult();


                }
            }
        }
        public void BackupQuartzJobDetails(string Path)
        {
            QuartzWrapper quartz = new QuartzWrapper(quartzJobStoreSettings.GetQuartzSettings());
            IReadOnlyList<IQuartzServiceJob> jobs = quartz.GetJobsFromTypes(GetJobTypes());
            List<IJobDetail> JobDetails = new List<IJobDetail>();
            foreach (IJobDetail d in quartz.GetJobs())
            {
                JobDetails.Add(d);
                
            }
            string Json=JsonConvert.SerializeObject(JobDetails);
            File.WriteAllText(Path, Json);
        }
        public void AddQuartzJob(string TypeName)
        {
            ScheduleReflectionJobTypes(GetJobTypes().Where(t => t.Name == TypeName).ToArray());
        }
        public void UnInstall()
        {
            StopIt();
            QuartzServiceInstaller.UninstallService();
        }
        public void Install()
        {

            QuartzServiceInstaller.InstallService();

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

        public void CreateQuartzSchedulerDatabase(string ConnectionStr)
        {
            if (ConnectionStr == string.Empty)
            {
                ConnectionStr = "data source=localhost;initial catalog=Quartz;integrated security=True";
            }
            QuartzScheduler context = new QuartzScheduler(ConnectionStr);
            context.CreateDatabase();

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
        public static QuartzScheduler scheduler = new QuartzScheduler(quartzJobStoreSettings.QuartzConnectionString);
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
            var jobs = from j in quartz.GetJobs() where (string.IsNullOrEmpty(JobName) || j.Key.Name == JobName) && (string.IsNullOrEmpty(JobGroup) || j.Key.Group == JobGroup)  select j;
            List<IJobDetail> jobsfound = new List<IJobDetail>();
            if (!string.IsNullOrEmpty(TriggerGroup) || !string.IsNullOrEmpty(TriggerName))
            {
                foreach (IJobDetail job in jobs)
                {
                    if(quartz.Scheduler.GetTriggersOfJob(job.Key).Result.Where(t => (string.IsNullOrEmpty(TriggerName) || t.Key.Name == TriggerName) && (string.IsNullOrEmpty(TriggerGroup) || t.Key.Group == TriggerGroup)).Any())
                    {
                        jobsfound.Add(job);
                    }
                }
            }
            else
            {

                jobsfound = jobs.ToList();
            }

            var response = jobsfound;
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
        public static List<QRTZ_JOB_HISTORY> GetJobHistory(string JobName="",string TriggerName="",DateTime? StartDate=null,DateTime? EndDate=null,string JobGroup="",string TriggerGroup="",bool? IsException=null)
        {
       
            var history = from h in scheduler.QRTZ_JOB_HISTORY.ToList() where (string.IsNullOrEmpty(JobName) || h.JOB_NAME == JobName) && (string.IsNullOrEmpty(TriggerName) || h.TRIGGER_NAME == TriggerName) && (!StartDate.HasValue || h.STARTDATE>=StartDate.Value) && (!EndDate.HasValue || h.ENDDATE <= EndDate.Value) && (string.IsNullOrEmpty(JobGroup) || h.JOB_GROUP == JobGroup) && (string.IsNullOrEmpty(TriggerGroup) || h.TRIGGER_GROUP == TriggerGroup) && (!IsException.HasValue || h.ISEXCEPTION==IsException) select h;
            List<QRTZ_JOB_HISTORY> list = history.ToList<QRTZ_JOB_HISTORY>();
            return list;
        }
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

