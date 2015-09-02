using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xunit.Runners.Pages;

namespace Xunit.Runners
{
    class FormsRunner : Application
    {
        readonly Assembly executionAssembly;
        readonly IReadOnlyCollection<Assembly> testAssemblies;
        readonly TextWriter writer;

        public FormsRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies, TextWriter writer)
        {
            this.executionAssembly = executionAssembly;
            this.testAssemblies = testAssemblies;
            this.writer = writer;


            MainPage = GetMainPage();
        }


        Page GetMainPage()
        {
            var hp = new HomePage();
            var nav = new Navigator(hp.Navigation);

            var runner = new DeviceRunner(executionAssembly, testAssemblies, nav, new ResultListener(writer));

            var vm = new HomeViewModel(nav, runner);


            hp.BindingContext = vm;

            return new Xamarin.Forms.NavigationPage(hp);
        }
    }
}