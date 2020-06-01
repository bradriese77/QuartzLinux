using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using QuartzServiceLib;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzUtilityLib
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
            scheduleFactory=new StdSchedulerFactory(Settings);
        }
        public void StartQuartz()
        {
            _log.Info("Starting Quartz Scheduler");

            Scheduler.Start();

            Scheduler.ListenerManager.AddJobListener(JobListener, GroupMatcher<JobKey>.AnyGroup());
        }
#pragma warning disable 649
        [ImportMany("QuartzServiceJob", typeof(IQuartzServiceJob))]
        private List<IQuartzServiceJob> _jobs;
#pragma warning restore 649
        public IReadOnlyList<IQuartzServiceJob> GetJobsFromFolder(string Folder,string WildCard)
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
