using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorClient
{
    public interface IResponse
    {
        string ToProtocolString();
        byte[] ToBinary();
    }


    public abstract class BaseResponse : IResponse
    {
        public abstract string ToProtocolString();

        public virtual byte[] ToBinary()
        {
            return System.Text.UTF8Encoding.UTF8.GetBytes(ToProtocolString());
        }
    }
    public class SingleLineResponse : BaseResponse
    {
        string arg = "";
        public SingleLineResponse(string resstring)
        {
            arg = resstring;
        }

        public override string ToProtocolString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("$");
            if (arg == null)
            {
                sb.Append("=1");
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
    }

    public class MultiLineResponse : BaseResponse
    {
        string[] _args = null;

        public MultiLineResponse(params string[] args)
        {
            _args = args;
        }

        public MultiLineResponse(List<string> list)
        {
            // TODO: Complete member initialization
            this._args = list.ToArray();
        }

        public override string ToProtocolString()
        {
            RCommand cmd = new RCommand(_args);
            return cmd.ToProtocolString();
        }
    }

    public class IntergerResponse : BaseResponse
    {
        string _arg = "";
        public IntergerResponse(long arg)
        {
            _arg = arg.ToString();
        }
        public IntergerResponse(string arg)
        {
            _arg = arg;
        }
        public override string ToProtocolString()
        {
            return ":" + _arg + "\r\n";
        }
    }

    public class EventOKResponse : BaseResponse
    {
        string _errstr;

        public EventOKResponse(string errstring = "OK")
        {
            _errstr = errstring;
        }
        public override string ToProtocolString()
        {
            return "+" + _errstr + "\r\n";
        }
    }

    public class EventFailedResponse : BaseResponse
    {
        string _errstr;
        public EventFailedResponse(string errstring = "Operation failed")
        {
            _errstr = errstring;
        }
        public override string ToProtocolString()
        {
            return "-" + _errstr + "\r\n";
        }
    }
}
