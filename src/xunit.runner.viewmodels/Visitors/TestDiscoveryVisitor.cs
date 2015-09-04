using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit
{
    class TestDiscoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        public TestDiscoveryVisitor()
        {
            TestCases = new List<ITestCase>();
        }

        public List<ITestCase> TestCases { get; }


        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            TestCases.Add(testCaseDiscovered.TestCase);

            return true;
        }
    }
}