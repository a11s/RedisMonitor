using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorClient
{
    public enum Err
    {
        Success=0,
        /// <summary>
        /// 工作线程开启失败
        /// </summary>
        ThreadStartFailed=1000,

    }
}
