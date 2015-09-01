using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Runners
{
    internal interface IResultChannel : ITestListener
    {
        Task<bool> OpenChannel(string message = null);

        Task CloseChannel();
        

    }
}
