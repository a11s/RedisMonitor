using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace RedisPerformanceCounter
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }
        public override void Uninstall(IDictionary savedState)
        {
            try
            {
                RedisPerformanceCounter.PCHelper.RemovePCCategoryUNSAFE();
            }
            catch (Exception xx)
            {


            }
            base.Uninstall(savedState);
        }
    }
}
