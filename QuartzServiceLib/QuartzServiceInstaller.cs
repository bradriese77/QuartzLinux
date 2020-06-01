using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace QuartzServiceLib
{
    [RunInstaller(true)]
    public partial class QuartzServiceInstaller : System.Configuration.Install.Installer
    {
      
        public static string ServiceName { get; set; }
        public static Type QuartzServiceType { get; set; }

        public QuartzServiceInstaller()
        {
            InitializeComponent();
        }   
        private void SetServiceParams()
        {
            var serviceProcessInstaller1 = new ServiceProcessInstaller();
            var QuartzServiceInstaller = new ServiceInstaller();

            
            //This will prompt for user and password when installing.
            serviceProcessInstaller1.Account = ServiceAccount.LocalService;
            //this.serviceProcessInstaller1.Password = null;
            //this.serviceProcessInstaller1.Username = null;
           
            QuartzServiceInstaller.StartType = ServiceStartMode.Automatic;

            QuartzServiceInstaller.ServiceName = ServiceName;
            if (Context.Parameters.ContainsKey("ServiceName"))
            {
                QuartzServiceInstaller.ServiceName = Context.Parameters["ServiceName"];
            }

            QuartzServiceInstaller.DisplayName = ServiceName;
            if (Context.Parameters.ContainsKey("DisplayName"))
            {
                QuartzServiceInstaller.DisplayName = Context.Parameters["DisplayName"];
            }

            QuartzServiceInstaller.Description = ServiceName;
            if (Context.Parameters.ContainsKey("Description"))
            {
                QuartzServiceInstaller.Description = Context.Parameters["Description"];
            }

            // ProjectInstaller
            Installers.AddRange(new Installer[]
            {
                serviceProcessInstaller1,
                QuartzServiceInstaller
            });
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            SetServiceParams();
            base.OnBeforeInstall(savedState);
        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            SetServiceParams();
            base.OnBeforeUninstall(savedState);
        }

        public static void InstallService()
        {
            if (IsInstalled()) return;

            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void UninstallService()
        {
           
            if (!IsInstalled()) return;
            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Uninstall(state);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        private static bool IsInstalled()
        {
            using (ServiceController controller =
                new ServiceController(ServiceName))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        private static bool IsRunning()
        {
            using (ServiceController controller =
                new ServiceController(ServiceName))
            {
                if (!IsInstalled()) return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }

        private static AssemblyInstaller GetInstaller()
        {
            AssemblyInstaller installer = new AssemblyInstaller(
                QuartzServiceType.Assembly, null);
            installer.UseNewContext = true;
            return installer;
        }
    }
}
