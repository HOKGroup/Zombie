using System.ComponentModel;
using System.Configuration.Install;

namespace ZombieService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            serviceInstaller1.DelayedAutoStart = true;
        }
    }
}
