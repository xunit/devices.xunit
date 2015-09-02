using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Xunit.Runners.Pages;

namespace Xunit.Runners
{
    class Navigator : INavigation
    {
        readonly Frame frame;

        public Navigator(Frame frame)
        {
            this.frame = frame;
        }

        public Task NavigateTo(NavigationPage page, object dataContext = null)
        {
            Type t;
            switch (page)
            {
                case NavigationPage.Home:
                    t = typeof(HomePage);
                    break;
                case NavigationPage.AssemblyTestList:
                    t = typeof(AssemblyTestListPage);
                    break;
                case NavigationPage.TestResult:
                    t = typeof(TestResultPage);
                    break;
                case NavigationPage.Credits:
                    t = typeof(CreditsPage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            frame.Navigate(t, dataContext);

            return Task.CompletedTask;
        }
    }
}
