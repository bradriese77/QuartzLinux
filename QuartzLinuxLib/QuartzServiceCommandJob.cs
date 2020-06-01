using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public class QuartzServiceCommandJob : QuartzServiceJob<QuartzServiceCommandJob>
    {
        public QuartzServiceCommandJob()
        {

            JobTypeCronExpression = "59 59 23 31 12 ? 2099";
            JobDataMap map = new JobDataMap();
            map.Add("Command", "");
            map.Add("Args", "");
            JobDetail.SetJobData(map);
            LoadConfigInfo();
        }

        public override void ExecuteJob(IJobExecutionContext context)
        {
            List<Exception> Exceptions = new List<Exception>();

            if(context.JobDetail!=null)
            {
                Logger.Info($"Running QuartzServiceCommandJob");
                string Command = context.JobDetail.JobDataMap["Command"]?.ToString();
                string Args = context.JobDetail.JobDataMap["Args"]?.ToString();
                int? TimeOut = (int?) context.JobDetail.JobDataMap["TimeOut"];
                if(Command!=null)RunProcess(Command, Args, TimeOut);

            }
            if (Exceptions.Count > 0) throw new AggregateException(Exceptions);
        }


    }
}
