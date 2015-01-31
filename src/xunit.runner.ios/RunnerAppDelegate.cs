using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

#if __UNIFIED__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xunit.Runners;
using Xunit.Runners.UI;

namespace Xunit.Runner
{
    public class RunnerAppDelegate : FormsApplicationDelegate
    {
        // class-level declarations
        UIWindow window;
        FormsRunner runner;

        private Assembly executionAssembly;
        private readonly List<Assembly> testAssemblies = new List<Assembly>();

        protected bool TerminateAfterExecution { get; set; }
        protected TextWriter Writer { get; set; }
        protected bool AutoStart { get; set; }

        protected void AddExecutionAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            if (!Initialized)
            {
                executionAssembly = assembly;
            }
        }

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();

            runner = new FormsRunner(executionAssembly, testAssemblies)
            {
                TerminateAfterExecution = TerminateAfterExecution,
                Writer = Writer,
                AutoStart = AutoStart,
            };

            Initialized = true;

            LoadApplication(runner);

            return base.FinishedLaunching(app, options);
        }

        protected bool Initialized { get; set; }
    }
}