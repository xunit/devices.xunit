using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Runners
{
    interface INavigation
    {
        Task NavigateTo(NavigationPage page, object dataContext = null);
    }

    enum NavigationPage
    {
        Home,
        AssemblyTestList,
        TestResult,
        Credits
    }
}
