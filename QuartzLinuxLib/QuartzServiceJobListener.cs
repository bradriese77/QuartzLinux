using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
                try
                {
                   
                    using (SqlConnection conn = new SqlConnection(quartzJobStoreSettings.QuartzConnectionString))
                    {
                        IQuartzServiceJob job = (IQuartzServiceJob)context.JobInstance;
                        string LogFile = job.GetLogFile();
                        string JobDataStr=JsonConvert.SerializeObject(context.JobDetail.JobDataMap);
                        Console.WriteLine($"Logfile is {LogFile}");
                        string Sql = $@"INSERT INTO[dbo].[QRTZ_JOB_HISTORY]
           ([SCHED_NAME]
           ,[JOB_NAME]
           ,[JOB_GROUP]
           ,[STATUSDETAIL]
           ,[STARTDATE]
           ,[ENDDATE]
           ,[RECORDDATE]
           ,[ISEXCEPTION]
           ,[LOGFILE]
           ,[TRIGGER_NAME]
           ,[TRIGGER_GROUP]
           ,[JOB_DATA]) 

           select '{context.Scheduler.SchedulerName}' [SCHED_NAME]
           ,'{context.JobDetail.Key.Name}'[JOB_NAME]
           ,'{context.JobDetail.Key.Group}'[JOB_GROUP]
           ,'{Status}' [STATUSDETAIL]
           ,'{context.FireTimeUtc.ToLocalTime().DateTime.ToString("MM-dd-yyyy HH:mm:ss")}' [STARTDATE]
           ,'{(context.FireTimeUtc.ToLocalTime().DateTime + context.JobRunTime).ToString("MM-dd-yyyy HH:mm:ss")}' [ENDDATE]
           ,'{DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss")}' [RECORDDATE]
           ,{Convert.ToInt32((Status != "Successful")).ToString()} [ISEXCEPTION]
           ,'{LogFile}' [LOGFILE]
           ,'{context.Trigger.Key.Name}' [TRIGGER_NAME]
           ,'{context.Trigger.Key.Group}' [TRIGGER_GROUP]
           ,'{JobDataStr}' [JOB_DATA]";
                        conn.Open();
                        //Console.WriteLine(Sql);
                        SqlCommand command = new SqlCommand(Sql, conn);
                        command.ExecuteNonQuery();

                        conn.Close();

                    };

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }
        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            return SaveHistory(context, context?.Result?.ToString());
        }
    }
}
