using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.WinRT;
using Xunit.Runners;

namespace Xunit.Runners
{
    class RunnerPage : WindowsPage
    {
        public RunnerPage(FormsRunner runner)
        {
            LoadApplication(runner);
        }
    }
}
