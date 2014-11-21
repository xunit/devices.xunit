using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Runners
{
    interface ITestRunner
    {
        Task Run(TestCaseViewModel test);
        Task Run(IEnumerable<TestCaseViewModel> tests, string message = null);
        bool TerminateAfterExecution { get; }
        bool AutoStart { get; }
    }
}
