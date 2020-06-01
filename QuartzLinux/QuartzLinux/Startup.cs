using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CrystalQuartz.AspNetCore;
using CrystalQuartz.Core.SchedulerProviders;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartzmin;

namespace QuartzLinux
{

    public class Startup
    {
        static readonly log4net.ILog _log =
log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddQuartzmin();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
           // var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            //XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            //_log.Info("Starting Quartz");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            QuartzServiceLib.QuartzJobStoreSettings s = new QuartzServiceLib.QuartzJobStoreSettings(true);
            QuartzServiceLib.QuartzWrapper quartz = new QuartzServiceLib.QuartzWrapper(s.GetQuartzSettings());

             QuartzServiceLib.QuartzProgram<Startup>.ScheduleReflectionJobTypes(QuartzServiceLib.QuartzProgram<Startup>.GetJobTypes());
             quartz.StartQuartz();
            IScheduler scheduler = quartz.Scheduler;//new CrystalQuartz.Core.SchedulerProviders.RemoteSchedulerProvider() {SchedulerHost=s.QuartzTcpServer }.CreateScheduler()
           //app.UseCrystalQuartz(new RemoteSchedulerProvider()
           //{
           //    SchedulerHost = "tcp://10.0.0.11:555/QuartzScheduler",
           //});


            app.UseCrystalQuartz(() => scheduler);
            app.UseQuartzmin(new QuartzminOptions()
            {
               // VirtualPathRoot = "QuartzMin",
                Scheduler = scheduler
            });
          
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
