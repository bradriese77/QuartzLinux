using AppUtilityLib;
using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public abstract class QuartzServiceJob<T> : AppUtility, IQuartzServiceJob
    {


        public QuartzServiceJob()
        {
            
            JobDetail =JobBuilder.Create(JobType).WithIdentity(JobType.Name,JobType.Assembly.GetName().Name);
            JobDetail.WithDescription(typeof(T).ToString());
          
  
        }
        public void LoadConfigInfo()
        {
            try
            {
                SqlCommandTimeOut = Convert.ToInt32(GetSetting("SqlCommandTimeOut", SqlCommandTimeOut.ToString()));
                JobTypeCronExpression = GetSetting(typeof(T).Name + "CronExpression", JobTypeCronExpression);
                _log.Info(string.Format("{0} CronExpression set to {1}",typeof(T).Name,JobTypeCronExpression));
               
            }
            catch (Exception ex)
            {
                _log.Fatal(string.Format("LoadConfigInfo failed.", ex));
                throw ex;
            }
        }
        private Type jobType = typeof(T);
        private int sqlCommandTimeOut = 2400;
        public int SqlCommandTimeOut
        {
            get
            {
                return sqlCommandTimeOut;
            }

            set
            {
                sqlCommandTimeOut = value;
            }
        }
       
        protected string LoggingName 
        {
            get
            {
                return typeof(T).Name;
            }
        }
        public string GetLogFile()
        {

            return _log.Logger?.Repository?.GetAppenders()?.OfType<log4net.Appender.RollingFileAppender>()?.First()?.File;
        }
        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string cronExpression;
        public string CronExpression
        {
            get
            {
                return cronExpression;
            }
            set
            {
                cronExpression = value;
            }
        }

        public ITrigger Schedule
        {
            get
            {
                return TriggerBuilder.Create().WithSchedule(CronScheduleBuilder.CronSchedule(CronExpression).WithMisfireHandlingInstructionFireAndProceed()).WithIdentity(typeof(T).Name + "Cron", JobType.Assembly.GetName().Name).Build();
            }
        }
        public JobBuilder JobDetail { get; set; }
        public string JobTypeCronExpression
        {
            get
            {
                return cronExpression;
            }

            set
            {
                cronExpression = value;
            }
        }

        public Type JobType { get => jobType; set => jobType = value; }

        public virtual Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => {

                try
                {
                    StartSTATask(ExecuteJob).GetAwaiter().GetResult();
                    context.Result = "Successful";
                }
                catch(Exception ex)
                {
                    Logger.Error(ex);
                    context.Result = ex.ToString();

                }
            });
        

        }
         public virtual void ExecuteJob()
        {

        }
        public virtual void ExecuteJob(IJobExecutionContext context)
        {
            throw new NotImplementedException();

        }

        public static Task StartSTATask(System.Action func)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    func();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
