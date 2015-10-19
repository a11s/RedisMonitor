using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RedisPerformanceCounter
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main(string[] args)
        {


            if ((!IsMono && !Environment.UserInteractive)//Windows Service
                || (IsMono && !AppDomain.CurrentDomain.FriendlyName.Equals(Path.GetFileName(Assembly.GetEntryAssembly().CodeBase))))//MonoService
            {
                RunAsService();
                return;
            }

            string exeArg = string.Empty;

            if (args == null || args.Length < 1)
            {
                exeArg = "r";
                Run(exeArg, null);

            }
            else if (args != null && args.Any(a => a.Contains("?")))
            {
                Console.WriteLine("Welcome to Redismonitor Service!");

                Console.WriteLine("Please press a key to continue...");
                Console.WriteLine("-[r]: Run this application as a console application;");
                Console.WriteLine("-[i]: Install this application as a Windows Service;");
                Console.WriteLine("-[u]: Uninstall this Windows Service application;");

                while (true)
                {
                    exeArg = Console.ReadKey().KeyChar.ToString();
                    Console.WriteLine();

                    if (Run(exeArg, null))
                        break;
                }
            }
            else
            {
                exeArg = args[0];

                if (!string.IsNullOrEmpty(exeArg))
                    exeArg = exeArg.TrimStart('-');

                Run(exeArg, args);
            }
        }
        static bool IsMono { get { return RedisPerformanceCounter.PCHelper.IsMono; } }
        private static void RunAsService()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new RPCService() 
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static bool Run(string exeArg, string[] startArgs)
        {
            switch (exeArg.ToLower())
            {
                case ("i"):
                    SelfInstaller.InstallMe();
                    return true;

                case ("u"):
                    SelfInstaller.UninstallMe();
                    return true;

                case ("r"):
                    RunAsConsole();
                    return true;

                default:
                    Console.WriteLine("Invalid argument!");
                    return false;
            }
        }

        private static void RunAsConsole()
        {
            RPCService r = new RPCService();
            Console.WriteLine("Starting...");
            r.StartNewClient();
            Console.WriteLine("press q to exit");
            while (true)
            {
                var q = Console.ReadKey();
                if (q.Key == ConsoleKey.Q)
                {
                    r.Stop();
                    Environment.Exit(0);
                }
            }

        }
    }
}
