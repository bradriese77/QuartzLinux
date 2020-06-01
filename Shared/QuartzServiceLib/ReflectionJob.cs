using log4net;
using log4net.Config;
using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public class ReflectionJob : QuartzServiceJob<ReflectionJob>
    {
          public ReflectionJob()
        {

            JobTypeCronExpression = "59 59 23 31 12 ? 2099";
            JobDataMap map = new JobDataMap();
            map.Add("AssemblyPath", "");
            map.Add("TypeName", "");
 
            JobDetail.SetJobData(map);
            LoadConfigInfo();
        }

        public override void ExecuteJob(IJobExecutionContext context)
        {
            List<Exception> Exceptions = new List<Exception>();

            if(context.JobDetail!=null)
            {

                string AssemblyPath =context.JobDetail.JobDataMap["AssemblyPath"].ToString();
                string TypeName = context.JobDetail.JobDataMap["TypeName"].ToString();
                Logger.Info($"Calling {TypeName}");
                object o =CreateInstanceOfAssemblyType(AssemblyPath, TypeName);
                CallMethod(o, "ExecuteJob", o, context);
                Logger.Info($"Completed Calling {TypeName}");

            }
            if (Exceptions.Count > 0) throw new AggregateException(Exceptions);
        }


    }
}
