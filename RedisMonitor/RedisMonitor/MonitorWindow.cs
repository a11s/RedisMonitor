using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace RedisMonitor
{
    public partial class MonitorWindow : Form
    {
        public MonitorWindow()
        {
            InitializeComponent();
        }

        MonitorClient.InfoClient client;

        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {            
            client = new MonitorClient.InfoClient(textBox1.Text, (s) => Console.WriteLine(s), 4096, 0);
            client.DataChanged += client_DataChanged;

            chart1.Series.Clear();
            Series series = new Series("xxxxx");
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 1;
            series.ShadowOffset = 1;
            series.IsValueShownAsLabel = true;
            series.CustomProperties = "LabelStyle=Top";
            series.Label = "#VAL";
            chart1.Series.Add(series);
            button1.Enabled = false;
            timer1.Interval = (int)numericUpDown2.Value * 1000;
            timer1.Enabled = true;

        }

        void prepairListBox(MonitorClient.DataChangedEventArgs e)
        {
            bool flushedCombobox = false;
            if (comboBox1.Items.Count == 0)
            {
                foreach (var item in e.Data)
                {
                    comboBox1.Items.Add(item.Key);
                }
                flushedCombobox = true;
                comboBox1.SelectedIndex = 0;
                listBox1.Items.Clear();
            }

            if (listBox1.Items.Count == 0)
            {
                foreach (var item in e.Data[comboBox1.Text])
                {
                    listBox1.Items.Add(item.Key);
                }
            }

        }

        void client_DataChanged(object sender, MonitorClient.DataChangedEventArgs e)
        {
            if (testdata == true)
            {
                //testdata = false;
                try
                {
                    RedisPerformanceCounter.PCHelper.CheckPCCategory(e.Data);

                }
                catch (Exception ex)
                {


                }

            }
            var a = new Action(() =>
            {
                prepairListBox(e);

                //var sv = e.Data["# Stats"]["total_commands_processed"];
                if (comboBox1.SelectedIndex < 0)
                {
                    return;
                }
                if (listBox1.SelectedIndex < 0)
                {
                    return;
                }
                if (!e.Data[comboBox1.Text].ContainsKey(listBox1.Text))
                {
                    return;
                }
                var sv = e.Data[comboBox1.Text][listBox1.Text];

                var arr = sv.Split(SPLITOR2);
                if (arr.Length > 1)
                {
                    //db0:keys=13,expires=13,avg_ttl=158233845
                    var subItems = MonitorClient.InfoClient.GetSubItems(sv);
                    if (chart1.Series.Count == 0)
                    {
                        PrepareChart(subItems.Select(o => o.Key));
                    }
                    for (int i = 0; i < subItems.Count; i++)
                    {
                        var kv = subItems[i];

                        double v;
                        bool isNum = MonitorClient.InfoClient.StringToNumber(kv.Value, out v);
                        var series = chart1.Series[i];
                        if (!isNum)
                        {
                            series.LegendText = kv.Value;
                            return;
                        }
                        series.IsValueShownAsLabel = checkBox1.Checked;
                        ////series.CustomProperties = "LabelStyle=Top";
                        //series.Label = "#VAL";
                        DateTime date = DateTime.Now;
                        series.Points.AddXY(date.ToShortTimeString(), (float)v);
                        while (series.Points.Count > (int)numericUpDown1.Value)
                        {
                            series.Points.RemoveAt(0);
                            chart1.ResetAutoValues();
                        }

                    }
                }
                else
                {
                    if (chart1.Series.Count == 0)
                    {
                        PrepareChart(new string[] { listBox1.Text });
                    }
                    double v;
                    bool isNum = MonitorClient.InfoClient.StringToNumber(sv, out v);
                    var series = chart1.Series[0];
                    if (!isNum)
                    {
                        series.LegendText = sv;
                        return;
                    }
                    series.IsValueShownAsLabel = checkBox1.Checked;
                    //series.ResetIsValueShownAsLabel();
                    //series.CustomProperties = "LabelStyle=Top";
                    //series.Label = "#VAL";
                    //series.Label = null;
                    DateTime date = DateTime.Now;
                    series.Points.AddXY(date.ToShortTimeString(), (float)v);
                    while (series.Points.Count > (int)numericUpDown1.Value)
                    {
                        series.Points.RemoveAt(0);
                        chart1.ResetAutoValues();
                    }
                }
            });
            try
            {
                this.Invoke(a);

            }
            catch
            {
                client.Stop();
            }


        }



        #region TESTS
        Random r = new Random((int)DateTime.Now.Ticks);
        private char[] SPLITOR2 = ",".ToCharArray();
        private void button2_Click(object sender, EventArgs e)
        {
            chart1.Series.Clear();
            Series series = new Series("xxxxx");
            series.ChartType = SeriesChartType.Area;
            series.BorderWidth = 1;
            series.ShadowOffset = 1;

            // Populate new series with data
            //series.Points.AddXY(DateTime.Now.AddSeconds(1), 67);
            //series.Points.AddXY(DateTime.Now.AddSeconds(2), 27);
            //series.Points.AddXY(DateTime.Now.AddSeconds(3), 57);
            //series.Points.AddXY(DateTime.Now.AddSeconds(4), 47);
            //series.Points.AddXY(DateTime.Now.AddSeconds(5), 67);
            //series.Points.AddXY(DateTime.Now.AddSeconds(6), 37);
            //series.Points.AddXY(DateTime.Now.AddSeconds(7), 47);
            //series.Points.AddXY(DateTime.Now.AddSeconds(8), 27);



            //for (int i = 0; i < 20000; i++)
            //{
            //    series.Points.AddXY(DateTime.Now.AddSeconds(i), r.Next(1,100));
            //}            
            // Add series into the chart's series collection
            chart1.Series.Add(series);
            timer1.Enabled = true;

        }
        bool testdata = false;

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                client.Update();
            }
            finally
            {
            }
        }

        #endregion

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                ResetChart(listBox1.Text);
            }
        }

        private void ResetChart(string p)
        {
            chart1.Series.Clear();
        }

        void PrepareChart(IEnumerable<string> ps)
        {
            foreach (var p in ps)
            {
                Series series = new Series(p);
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 1;
                series.ShadowOffset = 1;
                chart1.Series.Add(series);
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)numericUpDown2.Value * 1000;
        }

        private void MonitorWindow_Load(object sender, EventArgs e)
        {
            timer1.Interval = (int)numericUpDown2.Value * 1000;
#if DEBUG
            button2.Visible = true;
            button3.Visible = true;
            button4.Visible = true;
#endif
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var t = @"# Server
redis_version:2.8.12
redis_git_sha1:00000000
redis_git_dirty:0
redis_build_id:ff040dde4a39b4ff
redis_mode:standalone
os:Windows  
arch_bits:64
multiplexing_api:winsock_IOCP
gcc_version:0.0.0
process_id:1336
run_id:72273425c7dbf8b90c14eb92eea39cd2b07a8883
tcp_port:6379
uptime_in_seconds:200574
uptime_in_days:2
hz:10
lru_clock:2149769
config_file:

# Clients
connected_clients:1
client_longest_output_list:0
client_biggest_input_buf:0
blocked_clients:0

# Memory
used_memory:4360672
used_memory_human:4.16M
used_memory_rss:4327072
used_memory_peak:4443128
used_memory_peak_human:4.24M
used_memory_lua:33792
mem_fragmentation_ratio:0.99
mem_allocator:dlmalloc-2.8

# Persistence
loading:0
rdb_changes_since_last_save:0
rdb_bgsave_in_progress:0
rdb_last_save_time:1444808298
rdb_last_bgsave_status:ok
rdb_last_bgsave_time_sec:1444808299
rdb_current_bgsave_time_sec:-1
aof_enabled:0
aof_rewrite_in_progress:0
aof_rewrite_scheduled:0
aof_last_rewrite_time_sec:-1
aof_current_rewrite_time_sec:-1
aof_last_bgrewrite_status:ok
aof_last_write_status:ok

# Stats
total_connections_received:4
total_commands_processed:1767
instantaneous_ops_per_sec:0
rejected_connections:0
sync_full:0
sync_partial_ok:0
sync_partial_err:0
expired_keys:3
evicted_keys:0
keyspace_hits:3
keyspace_misses:0
pubsub_channels:0
pubsub_patterns:0
latest_fork_usec:0

# Replication
role:master
connected_slaves:0
master_repl_offset:0
repl_backlog_active:0
repl_backlog_size:1048576
repl_backlog_first_byte_offset:0
repl_backlog_histlen:0

# CPU
used_cpu_sys:4.38
used_cpu_user:9.66
used_cpu_sys_children:0.00
used_cpu_user_children:0.00

# Keyspace
";
            //RedisPerformanceCounter.PCHelper.CheckPCCategory();
            RedisPerformanceCounter.PCHelper.RemovePCCategoryUNSAFE();
        }

        private void MonitorWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            //RedisPerformanceCounter.PCHelper.CleanUp();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            testdata = true;
        }
        /// <summary>
        /// test chart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
    }
}
