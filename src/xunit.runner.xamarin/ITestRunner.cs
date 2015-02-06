using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit.Runners.ViewModels;
using Xunit.Abstractions;

namespace Xunit.Runners
{
    public interface ITestRunner
    {
        Task Run(IEnumerable<ITestExecutionSink> testExecutionSinks);
        Task Run(TestCaseViewModel test);
        Task Run(IEnumerable<TestCaseViewModel> tests, string message = null);
        bool TerminateAfterExecution { get; }
        bool AutoStart { get; }
    }
}
