using CrystalQuartz.Core.SchedulerProviders;
using CrystalQuartz.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using System.Collections.Specialized;
using Quartz.Impl;
using Quartz;

[assembly: OwinStartupAttribute(typeof(QuartzAdmin.App_Start.Startup))]
namespace QuartzAdmin.App_Start
{
    
    public class Startup
    {
            public static SchedulerFacade Scheduler { get; set; }
     
        public void Configuration(IAppBuilder app)
        {
            Scheduler = new SchedulerFacade();
            
            QuartzServiceLib.QuartzJobStoreSettings quartzJobStoreSettings = new QuartzServiceLib.QuartzJobStoreSettings(true);
            NameValueCollection properties = quartzJobStoreSettings.GetQuartzSettings();
            properties["quartz.scheduler.proxy"] = "true";
            properties["quartz.scheduler.proxy.address"] = "tcp://localhost:555/QuartzScheduler";
            StdSchedulerFactory stdSchedulerFactory = new StdSchedulerFactory(properties);
            SchedulerFacade.Scheduler = stdSchedulerFactory.GetScheduler().Result;


            app.UseCrystalQuartz(()=>Scheduler);
 
        }
       
    }
}