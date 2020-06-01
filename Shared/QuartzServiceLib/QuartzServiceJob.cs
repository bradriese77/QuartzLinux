using AppUtilityLib;
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
using System.Threading;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [Export("QuartzServiceJob", typeof(IQuartzServiceJob))]
    public abstract class QuartzServiceJob<T> : DataUtility, IQuartzServiceJob
    {

        protected static Configuration configAssembly = ConfigurationManager.OpenExeConfiguration(typeof(T).Assembly.Location);
        protected static AppSettingsSection configAppSettingsAssembly = (AppSettingsSection)configAssembly.GetSection("appSettings");
        protected static ConnectionStringsSection configConnectionStringsAssembly = (ConnectionStringsSection)configAssembly.GetSection("connectionStrings");
        public QuartzServiceJob()
        {
            
            JobDetail =JobBuilder.Create(JobType).WithIdentity(JobType.Name,JobType.Assembly.GetName().Name).StoreDurably(true);
            JobDetail.WithDescription(typeof(T).ToString());
            string configstr = typeof(T).Assembly.Location + ".config";
            var logRepository = LogManager.GetRepository(typeof(T).Assembly);
            XmlConfigurator.Configure(logRepository, new FileInfo(configstr));

        }
        public void LoadConfigInfo()
        {
            try
            {
                SqlCommandTimeOut = Convert.ToInt32(GetAppSetting("SqlCommandTimeOut", SqlCommandTimeOut.ToString()));
                JobTypeCronExpression = GetAppSetting(typeof(T).Name + "CronExpression", JobTypeCronExpression);
              //  _log.Info(string.Format("{0} CronExpression set to {1}",typeof(T).Name,JobTypeCronExpression));
               
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

        public override string GetSetting(string Name, string Default)
        {
            return GetAppSetting(Name, Default);

        }
        public static string GetAppSetting(string Name, string Default)
        {
            string ReturnValue;
            try
            {
                var value = configAppSettingsAssembly.Settings[Name]?.Value;

                return string.IsNullOrWhiteSpace(value) ? Default : value;
            
            }
            catch { ReturnValue = Default; }

            return ReturnValue;

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
                    StartSTATask(()=>ExecuteJob(context)).GetAwaiter().GetResult();
                    context.Result = "Successful";
                }
                catch(Exception ex)
                {
                    Logger.Error(ex);
                    context.Result = ex.ToString();

                }
            });
        

        }

        public virtual void ExecuteJob(IJobExecutionContext context)
        {
           

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
