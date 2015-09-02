using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;
using Xunit.Runners.Pages;
using Xunit.Runners.UI;

namespace Xunit.Runners
{
    public class FormsRunner : Xamarin.Forms.Application
    {
        readonly Assembly executionAssembly;
        readonly IReadOnlyCollection<Assembly> testAssemblies;
    
        public FormsRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies)
        {
            this.executionAssembly = executionAssembly;
            this.testAssemblies = testAssemblies;


            MainPage = GetMainPage();
        }

        public bool TerminateAfterExecution
        {
            get { return RunnerOptions.Current.TerminateAfterExecution; }
            set
            {
                RunnerOptions.Current.TerminateAfterExecution = value;
                OnPropertyChanged();
            }
        }

        public TextWriter Writer { get; set; }

        public bool AutoStart
        {
            get { return RunnerOptions.Current.AutoStart; }
            set
            {
                RunnerOptions.Current.AutoStart = value;
                OnPropertyChanged();
            }
        }

        Page GetMainPage()
        {
            var hp = new HomePage();
            var nav = new Navigator(hp.Navigation);

            var runner = new DeviceRunner(executionAssembly, testAssemblies, nav, new ResultListener(() => Writer));
            
            var vm = new HomeViewModel(nav, runner);
            
            
            hp.BindingContext = vm;

            return new Xamarin.Forms.NavigationPage(hp);
        }
    }
}
