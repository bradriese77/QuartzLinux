using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
//using CrystalQuartz.Owin;
using Microsoft.Owin;
using Owin;
using Quartz;
using Quartz.Impl;

[assembly: OwinStartupAttribute(typeof(QuartzAdminLinux.Startup))]
namespace QuartzAdminLinux
{
    using System.Diagnostics;
    using CrystalQuartz.Application;
    using CrystalQuartz.Core.SchedulerProviders;
    using QuartzServiceLib;

    public partial class Startup
    {
        QuartzWrapper quartz; 
        public void Configuration(IAppBuilder app)
        {
            QuartzServiceLib.QuartzJobStoreSettings s = new QuartzServiceLib.QuartzJobStoreSettings(true);

           
            StdSchedulerFactory scheduleFactory = new StdSchedulerFactory(s.GetQuartzSettings());
         //   
         quartz = new QuartzWrapper(s.GetQuartzSettings());
            quartz.StartQuartz();
            app.UseCrystalQuartz(()=>quartz.Scheduler);

        
        }
    }

}
