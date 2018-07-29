using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xunit.Runners.Pages;
using Xunit.Runners.ResultChannels;

namespace Xunit.Runners
{
    class FormsRunner : Application
    {

        // ReSharper disable once NotAccessedField.Local
        readonly Assembly executionAssembly;
        readonly IReadOnlyCollection<Assembly> testAssemblies;
        readonly IResultChannel resultChannel;

        public FormsRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies, TextWriter writer) : 
            this(executionAssembly, testAssemblies, new TextWriterResultChannel(writer))
        {
        }

        public FormsRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies, IResultChannel resultChannel)
        {
            this.executionAssembly = executionAssembly;
            this.testAssemblies = testAssemblies;
            this.resultChannel = resultChannel;

            MainPage = GetMainPage();
        }


        Page GetMainPage()
        {
            var hp = new HomePage();
            var nav = new Navigator(hp.Navigation);

            var runner = new DeviceRunner(testAssemblies, nav, resultChannel);

            var vm = new HomeViewModel(nav, runner);


            hp.BindingContext = vm;

            return new Xamarin.Forms.NavigationPage(hp);
        }
    }
}