using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    public class QuartzWrapper
    {
        private ISchedulerFactory scheduleFactory;
        public IScheduler Scheduler
        {
            get
            {
                if (scheduler == null)
                {
                    scheduler = scheduleFactory.GetScheduler().GetAwaiter().GetResult();
                }
                return scheduler;
            }
            set
            {
                scheduler = value;

            }
        }



        private QuartzServiceJobListener JobListener = new QuartzServiceJobListener();
        private IScheduler scheduler;
        protected static readonly log4net.ILog _log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public QuartzWrapper(NameValueCollection Settings)
        {
            scheduleFactory = new StdSchedulerFactory(Settings);
        }
        public void StartQuartz()
        {
           // _log.Info("Starting Quartz Scheduler");

            Scheduler.Start();

            Scheduler.ListenerManager.AddJobListener(JobListener, GroupMatcher<JobKey>.AnyGroup());
        }
#pragma warning disable 649
        [ImportMany("QuartzServiceJob", typeof(IQuartzServiceJob))]
        private List<IQuartzServiceJob> _jobs;
#pragma warning restore 649
        public IReadOnlyList<IQuartzServiceJob> GetJobsFromFolder(string Folder, string WildCard)
        {
            var catalog = new DirectoryCatalog(Folder, WildCard);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
            return _jobs;
        }

#pragma warning restore 649
        public IReadOnlyList<IQuartzServiceJob> GetJobsFromTypes(Type[] types)
        {
            AggregateCatalog catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new TypeCatalog(types));
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
            return _jobs;
        }
        public void StopQuartz()
        {
            if (Scheduler != null)
            {
                _log.Info(string.Format("Stopping Quartz Scheduler, waiting for {0} jobs to stop", Scheduler.GetCurrentlyExecutingJobs().GetAwaiter().GetResult().Count));

                Scheduler.Shutdown(true);

                _log.Info("Scheduler Stopped");
            }
        }



        public bool DoesJobExist(IQuartzServiceJob job)
        {
            List<IJobDetail> jobs = GetJobs();
            return jobs.Count(j => j.GetType().GetProperty("Name").GetValue(j).ToString() == job.GetType().Name) > 0;
        }
        public bool DoesJobExist(string JobName, string JobGroup,bool ThrowIfExists=false)
        {
            var ExistingJobs = GetJobs();

            bool Exists=ExistingJobs.Count(j => j.Key.Name == JobName && j.Key.Group == JobGroup) > 0;
            if(Exists && ThrowIfExists)
            {
                throw new Exception($"Job with Name {JobName} for Group {JobGroup} already exists");
            }
            return Exists;
        }

   
        public List<IJobDetail> GetJobs()
        {
            List<IJobDetail> jobs = new List<IJobDetail>();

            foreach (JobKey jobKey in Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).Result)
            {
                jobs.Add(Scheduler.GetJobDetail(jobKey).Result);
            }

            return jobs;
        }
        public List<IJobDetail> AddJobDetails(IEnumerable<IQuartzServiceJob> jobs, bool Replace, JobDataMap data = null)
        {
            Exception LastException = null;
            List<IJobDetail> Response = new List<IJobDetail>();
            foreach (var item in jobs)
            {

                if (data != null)
                {
                    item.JobDetail = item.JobDetail.SetJobData(data);
                }
                try
                {
                    IJobDetail d = item.JobDetail.Build();

                    Scheduler.AddJob(d, Replace, true).GetAwaiter().GetResult();
                    Response.Add(d);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    LastException = ex;
                }

            }
            if (LastException != null) throw LastException;
            return Response;
        }
        public ITrigger ScheduleCustomReflectionJob(Type job, string JobName, string JobGroup, JobDataMap map, ITrigger Schedule)
        {
           
            IQuartzServiceJob ReflectionJob = GetJobsFromTypes(new Type[] { typeof(ReflectionJob) }).First();
            IQuartzServiceJob TargetJob = GetJobsFromTypes(new Type[] { job }).First();

            map["AssemblyPath"] = job.Assembly.Location;
            map["TypeName"] = job.Namespace + "." + job.Name;

            ReflectionJob.JobDetail = ReflectionJob.JobDetail.SetJobData(map);

            IJobDetail d = ReflectionJob.JobDetail.WithIdentity(JobName, JobGroup).StoreDurably(false).Build();
            Console.WriteLine(d.ConcurrentExecutionDisallowed);
            d.Key.Group = JobGroup;
            d.Key.Name = JobName;

            return ScheduleJob(d, Schedule,true);
        }

        public ITrigger ScheduleJob(IJobDetail JobDetail,ITrigger Schedule, bool ThrowIfExists = false)
        {

            if (!DoesJobExist(JobDetail.Key.Name, JobDetail.Key.Group, ThrowIfExists))
            {
               
                DateTimeOffset dto = Scheduler.ScheduleJob(JobDetail, Schedule).GetAwaiter().GetResult();
                return Schedule;

            }
            else return null;
        }
        public ITrigger ScheduleCustomJob(Type job, string JobName, string JobGroup, JobDataMap map, ITrigger Schedule)
        {
            var ExistingJobs = GetJobs();
    
            IQuartzServiceJob TargetJob = GetJobsFromTypes(new Type[] { job }).First();

            TargetJob.JobDetail = TargetJob.JobDetail.SetJobData(map);

            IJobDetail d = TargetJob.JobDetail.WithIdentity(JobName,JobGroup).StoreDurably(false).Build();
           
            d.Key.Group = JobGroup;
            d.Key.Name = JobName;
            return ScheduleJob(d, Schedule, true);
        }
        public void ScheduleReflectionJobTypes(Type[] JobTypes)
        {
            Exception LastException = null;

            var ExistingJobs = GetJobs();
            foreach (var job in JobTypes)
            {
                try
                {
                IQuartzServiceJob ReflectionJob = GetJobsFromTypes(new Type[] { typeof(ReflectionJob) }).First();
                IQuartzServiceJob TargetJob = GetJobsFromTypes(new Type[] { job }).First();

                JobDataMap map = new JobDataMap();
                map["AssemblyPath"] = job.Assembly.Location;
                map["TypeName"] = job.Namespace + "." + job.Name;

                ReflectionJob.JobDetail = ReflectionJob.JobDetail.SetJobData(map);
             

                    IJobDetail d = ReflectionJob.JobDetail.Build();
                    d.Key.Group = job.Assembly.GetName().Name;
                    d.Key.Name = job.Name;
                    Console.WriteLine(d.ConcurrentExecutionDisallowed);
                    if (ExistingJobs.Count(j => j.Key.Name == d.Key.Name && j.Key.Group == d.Key.Group)>0)
                    {
                        Console.WriteLine($"Job with Name {d.Key.Name} for Group {d.Key.Group} already exists");
                    }
                    else
                    {
                        DateTimeOffset dto = Scheduler.ScheduleJob(d, TargetJob.Schedule).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    LastException = ex;
                }
               
            }
            if (LastException != null) throw LastException;
        }

        public void ScheduleJobTypes(IEnumerable<IQuartzServiceJob> jobs, JobDataMap data = null)
        {
            Exception LastException = null;
            foreach (var item in jobs)
            {

                if (data != null)
                {
                    item.JobDetail = item.JobDetail.SetJobData(data);
                }
                try
                {

                    IJobDetail d = item.JobDetail.Build();
                    DateTimeOffset dto = Scheduler.ScheduleJob(d, item.Schedule).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    LastException = ex;
                }
                if (item.Schedule.GetNextFireTimeUtc().HasValue)
                    _log.Info(string.Format("Loading Job type: {0} Next Schedule fire time (UTC): {1}", item.GetType(), item.Schedule.GetNextFireTimeUtc()));
            }
            if (LastException != null) throw LastException;
        }
    }
}
