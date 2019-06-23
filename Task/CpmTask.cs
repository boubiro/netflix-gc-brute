using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Netflix_GC_Brute.Task
{
    public class CpmTask
    {
        
        private static readonly ConcurrentDictionary<long, long> Cps = new ConcurrentDictionary<long, long>();
        
        public static long GetCpm()
        {
            long checksPerMinute = 0;

            foreach (var check in Cps)
                if (check.Key >= DateTimeOffset.Now.ToUnixTimeSeconds() - 60)
                    checksPerMinute += check.Value;

            return checksPerMinute;
        }
        
        public static void Start()
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (Globals.Working)
                    {
                        Cps.TryAdd(DateTimeOffset.Now.ToUnixTimeSeconds(), Globals.LastChecks);
                        
                        Globals.LastChecks = 0;
                    }
                    else
                        break;

                    Thread.Sleep(1000);
                }
            });
        }
        
    }
}