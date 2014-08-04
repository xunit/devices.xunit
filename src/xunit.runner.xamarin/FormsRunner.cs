using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Xamarin.Forms;
using Xunit.Runners.Pages;
using Xunit.Runners.ViewModels;

namespace Xunit.Runners
{
    public class FormsRunner : ViewModelBase
    {
        private readonly Assembly executionAssembly;
        private readonly IReadOnlyCollection<Assembly> testAssemblies;
        private bool terminateAfterExecution;
        private TextWriter writer;
        private bool autoStart;

        public FormsRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies)
        {
            this.executionAssembly = executionAssembly;
            this.testAssemblies = testAssemblies;
        }

        public bool TerminateAfterExecution
        {
            get { return terminateAfterExecution; }
            set { Set(ref terminateAfterExecution, value); }
        }

        public TextWriter Writer
        {
            get { return writer; }
            set { Set(ref writer, value); }
        }

        public bool AutoStart
        {
            get { return autoStart; }
            set { Set(ref autoStart, value); }
        }

        public Page GetMainPage()
        {

            var hp = new HomePage();
            var vm = new HomeViewModel(hp.Navigation, testAssemblies);
            
            
            hp.BindingContext = vm;

            return new NavigationPage(hp);
        }
    }
}
