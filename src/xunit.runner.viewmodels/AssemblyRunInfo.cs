using System;
using System.Collections.Generic;
using System.Text;

namespace Xunit.Runners
{
    class AssemblyRunInfo
    {
        public string AssemblyFileName { get; set; }
        public TestAssemblyConfiguration Configuration { get; set; }
        public IList<TestCaseViewModel> TestCases { get; set; }
    }
}
