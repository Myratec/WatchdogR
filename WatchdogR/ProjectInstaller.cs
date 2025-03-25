using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace WatchdogR
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            // Prozess-Installer festlegen
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Dienst-Installer festlegen
            serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = "WatchdogR";
            serviceInstaller.DisplayName = "WatchdogR Service";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Beide Installer zur Installationskollektion hinzufügen
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
