using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("Xunit.Runners.Sdk.UITheoryDiscoverer", "xunit.runner.devices")]
    public class UITheoryAttribute : FactAttribute
    {
    }
}
