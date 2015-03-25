using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorClient
{
    public class RCommand
    {
        public static byte[] CRLF = Encoding.ASCII.GetBytes("\r\n");
        List<string> _Args;

        public List<string> Args
        {
            get { return _Args; }
            set { _Args = value; }
        }
        public RCommand(params string[] args)
        {
            _Args = args.ToList();
        }
        public RCommand(List<string> args)
        {
            _Args = args.ToArray().ToList();
        }

        public string this[int i]
        {
            get
            {
                return _Args[i];
            }
        }
        private string GetBlockCountString()
        {
            if (_Args == null)
            {
                return "-no content";
            }
            return "*" + _Args.Count.ToString();
        }
        string GetArgLine(string arg)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("$");//block
            if (arg == null)
            {
                sb.Append("-1");
            }
            else
            {
                sb.Append(arg.Length);
            }
            sb.Append("\r\n");
            sb.Append(arg);
            sb.Append("\r\n");
            return sb.ToString();

        }
        public string ToProtocolString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetBlockCountString());
            for (int i = 0; i < _Args.Count; i++)
            {
                sb.Append(GetArgLine(_Args[i]));
            }
            return sb.ToString();
        }
    }
}
