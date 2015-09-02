using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WinPhone;

namespace Xunit.Runners.UI
{
    public abstract class RunnerApplicationPage : FormsApplicationPage
    {

        private Assembly executionAssembly;
        private readonly List<Assembly> testAssemblies = new List<Assembly>();

        public bool TerminateAfterExecution { get; set; }
        public TextWriter Writer { get; set; }
        public bool AutoStart { get; set; }
        public bool Initialized { get; private set; }
        protected RunnerApplicationPage()
        {
            Forms.Init();

            OnInitializeRunner();

            Initialized = true;

            RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
            RunnerOptions.Current.AutoStart = AutoStart;

            var runner = new FormsRunner(executionAssembly, testAssemblies, Writer);

            LoadApplication(runner);
        }

        protected abstract void OnInitializeRunner();

        //protected void AddExecutionAssembly(Assembly assembly)
        //{
        //    if (assembly == null) throw new ArgumentNullException("assembly");

        //    if (!Initialized)
        //    {
        //        executionAssembly = assembly;
        //    }
        //}

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }
    }
}
