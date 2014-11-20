using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Forms;
using Xunit.Runners;
using Xunit.Runners.UI;

namespace Xunit.Runner
{
    public class RunnerAppDelegate : UIApplicationDelegate
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

            // create a new window instance based on the screen size
            window = new UIWindow(UIScreen.MainScreen.Bounds);

            runner = new FormsRunner(executionAssembly, testAssemblies)
            {
                TerminateAfterExecution = TerminateAfterExecution,
                Writer = Writer,
                AutoStart = AutoStart,
            };

            window.RootViewController = runner.GetMainPage()
                                              .CreateViewController();

            // make the window visible
            window.MakeKeyAndVisible();

            Initialized = true;

            return true;
        }

        protected bool Initialized { get; set; }
    }
}