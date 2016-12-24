using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace Xunit.Runners
{
    static class ThreadPoolHelper
    {
        public static async void RunAsync(Action action)
        {

            var task = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            try
            {
                await task;
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                    Debug.WriteLine(e);
                }
            }

        }
    }
}
