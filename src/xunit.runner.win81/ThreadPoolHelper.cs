using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace Xunit.Runners
{
    static class ThreadPoolHelper
    {
        public static async void RunAsync(Action action)
        {
            await ThreadPool.RunAsync(_ => action());
        }
    }
}
