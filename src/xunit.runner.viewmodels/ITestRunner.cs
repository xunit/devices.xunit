using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Runners
{
    interface ITestRunner
    {
        Task Run(TestCaseViewModel test);
        Task Run(IEnumerable<TestCaseViewModel> tests, string message = null);

        Task<IReadOnlyList<TestAssemblyViewModel>> Discover();

        IReadOnlyCollection<Assembly> TestAssemblies { get; }
    }
}
