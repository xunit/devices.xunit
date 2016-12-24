using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xunit.Runners
{
    static class ThreadPoolHelper
    {
        public static void RunAsync(Action action)
        {
            ThreadPool.QueueUserWorkItem(_ => action(), null);
        }
    }
}