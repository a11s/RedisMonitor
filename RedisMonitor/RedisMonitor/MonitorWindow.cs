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
        }
        /// <summary>
        /// test chart
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
    }
}
