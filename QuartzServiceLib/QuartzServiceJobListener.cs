using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    public class QuartzServiceJobListener : IJobListener
    {
        public string Name => "QuartzServiceJobListener";

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            //return SaveHistory(context,"Vetoed");
            return Task.Run(() =>
            {
            });
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            //return SaveHistory(context, "ToBeExecuted");
            return Task.Run(() =>
            {
            });
        }
        private QuartzJobStoreSettings quartzJobStoreSettings = new QuartzJobStoreSettings(false);
        public Task SaveHistory(IJobExecutionContext context,string Status)
        {
            return Task.Run(() =>
            {

                //QuartzScheduler db = new QuartzScheduler(quartzJobStoreSettings.QuartzConnectionString);
                //QRTZ_JOB_HISTORY history = new QRTZ_JOB_HISTORY();
                //history.JOB_GROUP = context.JobDetail.Key.Group;
                //history.SCHED_NAME = context.Scheduler.SchedulerName;
                //history.JOB_NAME = context.JobDetail.Key.Name;
                //history.DETAIL = Status;
                //history.RECORDDATE = DateTime.Now;
                //history.STARTDATE = context.FireTimeUtc.ToLocalTime().DateTime;
                //history.ENDDATE = history.STARTDATE + context.JobRunTime;
                //history.ISEXCEPTION = (Status != "Successful");
                //history.TRIGGER_GROUP = context.Trigger.Key.Group;
                //history.TRIGGER_NAME = context.Trigger.Key.Name;
         
                //IQuartzServiceJob job = (IQuartzServiceJob)context.JobInstance;
                //history.LOGFILE = job.GetLogFile();
                //db.QRTZ_JOB_HISTORY.InsertOnSubmit(history);
                //db.SubmitChanges();
            });
        }
        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            return SaveHistory(context, context?.Result?.ToString());
        }
    }
}
