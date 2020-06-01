using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    public class QuartzJobStoreSettings
    {

        public QuartzJobStoreSettings(bool IsServer)
        {
            this.IsServer = IsServer;

        }
        private bool IsServer { get; set; }
        private bool? useSqlServerQuartz;

        private string quartzServer;

        private string quartzPort;

        private string quartzConnectionString;
        public bool UseSqlServerQuartz
        {
            get
            {
                if(!useSqlServerQuartz.HasValue)
                {
                    useSqlServerQuartz = !string.IsNullOrEmpty(QuartzConnectionString);
                }
                return useSqlServerQuartz.Value;

            }
            set => useSqlServerQuartz = value;
        }
        public string QuartzServer 
        {
            get
            {
                if (string.IsNullOrEmpty(quartzServer))
                {
                    quartzServer = GetSetting("QuartzServer", Environment.MachineName);
                }
                return quartzServer;
            }
            set => quartzServer = value; 
        
        }

        public string QuartzTcpServer
        {
            get
            {
                return $"tcp://{QuartzServer}:{QuartzPort}/QuartzScheduler";
            }
          

        }
        public string QuartzPort
        {
            get
            {
                if (string.IsNullOrEmpty(quartzPort))
                {
                    quartzPort = GetSetting("QuartzPort", "555");
                }
                return quartzPort;
            }
            set => quartzPort = value;

        }
        public string QuartzConnectionString 
        {
            get
            {
                if(string.IsNullOrEmpty(quartzConnectionString))
                {
                   quartzConnectionString = "Data Source=localhost,1433;Database=Quartz;Initial Catalog=Quartz;User Id=brad;Password=bradley;";//
                   // quartzConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Quartz"]?.ConnectionString;
                }
                return quartzConnectionString;
            }

            set => quartzConnectionString = value; 
        }



        public static NameValueCollection GetSettings(bool IsServer=false)
        {
            return new QuartzJobStoreSettings(IsServer).GetQuartzSettings();
        }
        public NameValueCollection GetQuartzSettings()
        {

            if (UseSqlServerQuartz)
            {

                var properties = new NameValueCollection
                {
                {"quartz.scheduler.instanceName", QuartzServer },
                {"quartz.scheduler.instanceId", QuartzServer },
                {"quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
                {"quartz.jobStore.useProperties", "true" },
                {"quartz.jobStore.dataSource", "default" },
                {"quartz.jobStore.tablePrefix", "QRTZ_" },
                {"quartz.jobStore.clustered","true" },
                {"quartz.dataSource.default.connectionString",QuartzConnectionString },
                {"quartz.dataSource.default.provider", "SqlServer" },
                {"quartz.serializer.type", "binary" },
                {"quartz.threadPool.threadCount","5"},
                {"quartz.threadPool.threadPriority","Normal"}
                
                };

                if (IsServer)
                {
                    properties["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";
                    properties["quartz.scheduler.exporter.type"] = "Quartz.Simpl.RemotingSchedulerExporter, Quartz";
                    properties["quartz.scheduler.exporter.port"] = QuartzPort;
                    properties["quartz.scheduler.exporter.bindName"] = "QuartzScheduler";
                    properties["quartz.scheduler.exporter.channelType"] = "tcp";
                    properties["quartz.scheduler.exporter.channelName"] = "httpQuartz";
                }
                else
                {
                    properties["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quarz";

                }
                return properties;
            }
            else return new NameValueCollection();
        }

        public static string GetSetting(string Name, string Default)
        {
            string ReturnValue;
            try
            {
                ReturnValue = System.Configuration.ConfigurationManager.AppSettings.Get(Name);
                if (ReturnValue == null || ReturnValue == string.Empty)
                {
                    ReturnValue = Default;
                }
            }
            catch { ReturnValue = Default; }

            return ReturnValue;

        }
    }
}
