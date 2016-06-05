using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PyKiller
{
    internal static class Program
    {
        /// <summary>
        /// Preserved memory size, in bytes.
        /// </summary>
        public static long PreservedMemory = 10*1024*1024; // 10 MB

        public static readonly string[] TargetPrcesses = {"Python", "Py"};

        static void WriteLine(string format, params object[] args)
        {
            Console.Write(DateTime.Now + "\t");
            Console.WriteLine(format, args);
        }

        static void WriteLine(string v)
        {
            Console.Write(DateTime.Now + "\t");
            Console.WriteLine(v);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("PyKiller, a simple memory preservation utility.");
            Console.WriteLine("by CXuesong, 2016");
            Console.WriteLine();
            AdjustPriority();
            LoadSettings();
            Console.WriteLine("Press any key to exit.");
            var t = new Thread(CheckMemoryEnterPoint)
            {
                Priority = ThreadPriority.Highest,
            };
            t.Start();
            while (true)
            {
                Console.ReadKey(true);
                Console.Write("Enter EXIT to exit:");
                if (Console.ReadLine()?.Trim().ToUpperInvariant() == "EXIT")
                {
                    t.Abort();
                    t.Join();
                    return;
                }
            }
        }

        /// <summary>
        /// Determines whether the current process is running on
        /// a higher priority, and elevates the priority if not.
        /// This is usually needed especially  when physical
        /// memory is almost used up and OS begins to be not responsive.
        /// </summary>
        static void AdjustPriority()
        {
            using (var p = Process.GetCurrentProcess())
            {
                switch (p.PriorityClass)
                {
                    case ProcessPriorityClass.Idle:
                    case ProcessPriorityClass.BelowNormal:
                    case ProcessPriorityClass.Normal:
                        p.PriorityClass = ProcessPriorityClass.High;
                        WriteLine("Priority successfully elevated.", p.PriorityClass);
                        break;
                }
                WriteLine("Current priority: {0} .", p.PriorityClass);
            }
        }

        static void LoadSettings()
        {
            var settings = ConfigurationManager.AppSettings;
            PreservedMemory = settings["PreservedMemory"]?.ToInt64()* 1024 * 1024 ?? PreservedMemory;
            WriteLine("PreservedMemory = {0:##,###} MB", PreservedMemory/1024/1024);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public static MEMORYSTATUSEX Create()
            {
                return new MEMORYSTATUSEX {dwLength = (uint) Marshal.SizeOf(typeof (MEMORYSTATUSEX))};
            }
        }

        static void CheckMemoryEnterPoint()
        {
            CHECK:
            var status = MEMORYSTATUSEX.Create();
            if (GlobalMemoryStatusEx(ref status))
            {
                if (status.ullAvailPhys < (ulong)PreservedMemory)
                {
                    WriteLine("Avaliable Memory = {0:##,###} < {1:##,###} .", status.ullAvailPhys, PreservedMemory);
                    KillApplications(TargetPrcesses);
                }
                Console.Title = string.Format("Avaliable Memory: {0:##,###}/{1:##,###}",
                    status.ullAvailPhys, status.ullTotalPhys);
            }
            // Check memory every 5 seconds.
            Thread.Sleep(5000);
            goto CHECK;
        }

        static void KillApplications(params string[] imageNames)
        {
            foreach (var name in imageNames)
            {
                var ps = Process.GetProcessesByName(name);
                foreach (var p in ps)
                {
                    var pid = p.Id;
                    try
                    {
                        p.Kill();
                    }
                    catch (Win32Exception ex)
                    {
                        // This happens especailly when the process is being killed.
                        WriteLine(ex.Message);
                    }
                    WriteLine("Killing PID = {0} .", pid);
                }
                // Wait for Exit
                foreach (var p in ps)
                {
                    p.WaitForExit(5000);
                    p.Dispose();
                }
            }
        }

        static long ToInt64(this string s) => Convert.ToInt64(s);
    }
}
