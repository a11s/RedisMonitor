using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitorClient
{
    public class DataChangedEventArgs : EventArgs
    {
        public Dictionary<string, Dictionary<string, string>> Data;

    }
    public class InfoClient
    {
        public const int minsleepinterval = 100;
        public DateTime lastsleeptime;
        #region field

        TcpClient socket;
        Action<string> _mlog = null;
        bool _running = false;
        int _interval = 1000;
        Thread loopthread;
        private NetworkStream stream;

        byte[] INFOBUFF;
        byte[] READBUFF;
        CommandBuilder cb;
        char[] SPLITOR = new char[] { ':' };

        public bool Running
        {
            get { return _running; }
        }
        #endregion

        #region Events
        public event EventHandler<DataChangedEventArgs> DataChanged;
        #endregion

        void Log(string s)
        {
            if (_mlog != null)
            {
                try
                {
                    _mlog(s);
                }
                finally
                {

                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipport"></param>
        /// <param name="logcallback"></param>
        /// <param name="ReadBuffSize"></param>
        /// <param name="Interval"> ms,0 means never update</param>
        /// <remarks>通常说来,info返回的字节数都差不多,因此这里输入一个足够大的buffer可以一次性加载完整的数据</remarks>
        public InfoClient(string ipport, Action<string> logcallback = null, int ReadBuffSize = 4096, int Interval = 1000)
        {
            #region prepare data
            RCommand cmd = new RCommand("info");
            INFOBUFF = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.ToProtocolString());
            READBUFF = new byte[ReadBuffSize];
            _interval = Interval;
            cb = new CommandBuilder();
            #endregion
            if (logcallback != null)
            {
                _mlog = logcallback;
            }
            ipport = ipport.Trim();
            string ip = ipport;
            int port = 6379;
            var arr = ipport.Split(new char[] { ':' });

            if (arr.Length > 1)
            {
                ip = arr[0];
                port = int.Parse(arr[1]);
            }
            try
            {
                socket = new TcpClient(ip, port);
            }
            catch (SocketException se)
            {
                Log(se.ToString());
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            if (loopthread == null)
            {
                _running = true;
                loopthread = new Thread(monitorloop);
                loopthread.Start();
            }
            else
            {
                Log("Err:" + Err.ThreadStartFailed);
            }

            stream = socket.GetStream();

        }




        public void Stop()
        {
            _running = false;
        }

        private void monitorloop(object obj)
        {
            if (_interval < 1)
            {

            }
            else
            {
                while (Running)
                {

                    Update();
                    Thread.Sleep(minsleepinterval);
                    //break;
                }
            }

        }

        public async void Update()
        {
            if (DateTime.Now.Subtract(lastsleeptime).TotalMilliseconds > _interval)
            {
                lastsleeptime = DateTime.Now;
            }
            else
            {
                return;
            }
            int numberOfBytesRead = 0;

            if (stream.CanWrite)
            {
                await stream.WriteAsync(INFOBUFF, 0, INFOBUFF.Length);
            }
            bool hasCmd = false;

            List<RCommand> lst = null;

            while (stream.CanRead && stream.DataAvailable && Running)
            {
                int i;
                //一个4M的buff,意味着最大块长度不能大于4M,每连接
                //byte[] PeerBuff = new byte[1024 * 1024 * 4];

                i = stream.Read(READBUFF, 0, READBUFF.Length);
                if (i != 0)
                {
                    var sbuff = System.Text.UTF8Encoding.UTF8.GetString(READBUFF, 0, i);
                    hasCmd = cb.AppendData(sbuff, out lst);

                }
                else
                {
                    Close();
                }
            }
            if (hasCmd)
            {
                //dispatch cmd
                for (int i = 0; i < lst.Count; i++)
                {
                    DispatchCommand(lst[i]);
                }
            }
        }

        private void DispatchCommand(RCommand cmd)
        {
            switch (cmd.Args[0].ToUpper())
            {

                default:
                    //System.Diagnostics.Debug.WriteLine(cmd.Args[0]);
                    DispatchParagraph(cmd.Args[0]);
                    break;
            }
        }

        private void DispatchParagraph(string p)
        {
            Dictionary<string, Dictionary<string, string>> AllParagraph = new Dictionary<string, Dictionary<string, string>>();
            using (System.IO.StringReader sr = new System.IO.StringReader(p))
            {

                var s = sr.ReadLine();
                var currentParagraphKey = "";
                while (s != null)
                {
                    if (s.StartsWith("#"))
                    {
                        if (AllParagraph.ContainsKey(s))
                        {
                            currentParagraphKey = s;
                        }
                        else
                        {
                            AllParagraph[s] = new Dictionary<string, string>();
                            currentParagraphKey = s;
                        }
                    }
                    else
                    {
                        //get subitems

                        SplitSubItems(AllParagraph, s, currentParagraphKey);

                    }
                    s = sr.ReadLine();
                }
            }
            if (DataChanged != null)
            {
                DataChanged(this, new DataChangedEventArgs() { Data = AllParagraph });
            }
        }

        private void SplitSubItems(Dictionary<string, Dictionary<string, string>> AllParagraph, string s, string currentParagraphKey)
        {
            var arr = s.Split(SPLITOR);
            var currPara = AllParagraph[currentParagraphKey];
            //switch (currentParagraphKey.ToLower())
            //{
            //    #region #KeySpace
            //    case "#keyspace":
            //        {
            //            //db0:keys=18,expires=18,avg_ttl=9867155

            //        }
            //        break;
            //    #endregion
            //    default:
            //        break;
            //}
            if (arr.Length > 1)
            {
                currPara[arr[0]] = arr[1];
            }
        }



        private void Close()
        {
            cb = null;
            Stop();
        }

        public static bool StringToNumber(string sv, out double v)
        {
            bool isNum = false;
            double value = 0;
            if (sv.Length > 1)// like 12 1M
            {
                var lastChar = sv.Substring(sv.Length - 1, 1);
                if ("BKMGT".Contains(lastChar))//like 1M
                {

                    var shortv = sv.Substring(0, sv.Length - 1);
                    isNum = double.TryParse(shortv, out value);
                    if (isNum)
                    {

                        if (sv.EndsWith("M"))
                        {
                            value *= 1000000;
                        }
                        else if (sv.EndsWith("G"))
                        {
                            value *= 1000000000;
                        }
                        else if (sv.EndsWith("T"))
                        {
                            value *= 1000000000000;
                        }
                        else if (sv.EndsWith("K"))
                        {
                            value *= 1000;
                        }
                        else if (sv.EndsWith("B"))
                        {
                            //do nothing
                        }
                        else
                        {
                            //throw new Exception("not support");

                        }
                    }
                }
                else
                {
                    //other num , like 12
                    isNum = double.TryParse(sv, out value);
                }
            }
            else
            {
                //like "9"
                isNum = double.TryParse(sv, out value);
            }

            v = value;
            return isNum;
        }

        public static List<KeyValuePair<string, string>> GetSubItems(string sv, string splitor = ",", string eqchar = "=")
        {
            var eq = eqchar.ToCharArray();
            var ret = new List<KeyValuePair<string, string>>();
            var arr = sv.Split(splitor.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < arr.Length; i++)
            {
                var kv = arr[i];
                var arrkv = kv.Split(eq);
                if (arrkv.Length > 1)
                {
                    ret.Add(new KeyValuePair<string, string>(arrkv[0], arrkv[1]));
                }
                else
                {
                    ret.Add(new KeyValuePair<string, string>(arrkv[0], ""));
                }
            }

            return ret;
        }
    }
}
