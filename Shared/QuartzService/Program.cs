using QuartzServiceLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzService
{

    public class Program : QuartzProgram<Program>
    {
        static readonly log4net.ILog _log =
  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        [STAThread]
        public static int Main(string[] args)
        {

            try
            {
                log4net.Config.XmlConfigurator.Configure();
                AppUtilityLib.AppUtility utility = new AppUtilityLib.AppUtility();
                QuartzServiceInstaller.ServiceName = utility.GetSetting("ServiceName", "QuartzService");
                QuartzServiceInstaller.QuartzServiceType = typeof(QuartzService);
              
                if (args.Length == 0)
                {


                    QuartzProgram<Program>.ServiceMain(args);
                    return 0;
                }
                else
                {
                    return ReflectCmdArgs(args);
                }
            }
            catch (Exception ex)
            {
                new Program().Logger.Error(ex);
                return -1;
            }
        }
    
        public Program()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
    }

}
