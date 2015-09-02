using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xunit.Runners.Pages;

namespace Xunit.Runners
{
    class Navigator : INavigation
    {
        readonly Xamarin.Forms.INavigation navigation;

        public Navigator(Xamarin.Forms.INavigation navigation)
        {
            this.navigation = navigation;
        }

        public Task NavigateTo(NavigationPage page, object dataContext)
        {
            ContentPage p;
            switch (page)
            {
                case NavigationPage.Home:
                    p = new HomePage();
                    break;
                case NavigationPage.AssemblyTestList:
                    p = new AssemblyTestListPage();
                    break;
                case NavigationPage.TestResult:
                    p = new TestResultPage();
                    break;
                case NavigationPage.Credits:
                    p = new CreditsPage();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            p.BindingContext = dataContext;

            return navigation.PushAsync(p);
        }
    }
}