using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RedisPerformanceCounter
{
    public partial class RPCService : ServiceBase
    {
        public RPCService()
        {
            InitializeComponent();
        }
        public static MonitorClient.InfoClient client;
        //bool running = false;
        //bool pausing = false;
        //System.Threading.Thread loopthread;
        protected override void OnStart(string[] args)
        {
            StartNewClient();
            //running = true;
            //loopthread = new System.Threading.Thread(mainloop);
            //loopthread.Start();
        }

        public void StartNewClient()
        {
            try
            {
                RedisPerformanceCounter.PCHelper.InstanceName = Properties.Settings.Default.IPPort;
                client = new MonitorClient.InfoClient(Properties.Settings.Default.IPPort, logcallback, Properties.Settings.Default.BufferSize, Properties.Settings.Default.Interval);
                client.DataChanged += client_DataChanged;
            }
            catch (Exception ex)
            {


            }
        }

        void client_DataChanged(object sender, MonitorClient.DataChangedEventArgs e)
        {
            try
            {
                RedisPerformanceCounter.PCHelper.CheckPCCategory(e.Data);
            }
            catch (Exception ex)
            {


            }
        }

        protected override void OnStop()
        {
            //running = false;
            try
            {
                client.Stop();
            }
            catch (Exception ex)
            {


            }
        }

        void logcallback(string s)
        {
            this.EventLog.WriteEntry(s, EventLogEntryType.Information);
        }

        //void mainloop()
        //{
        //    while (running)
        //    {
        //        if (pausing)
        //        {
        //            //do nothing.
        //        }
        //        else
        //        {
        //            client.Update();
        //        }
        //    }
        //}


    }
}
