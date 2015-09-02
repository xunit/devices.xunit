using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace Xunit.Runners.UI
{
    public abstract class RunnerApplication : Application
    {
        Assembly executionAssembly;
        readonly List<Assembly> testAssemblies = new List<Assembly>();

        public bool TerminateAfterExecution { get; set; }
        public TextWriter Writer { get; set; }
        public bool AutoStart { get; set; }
        public bool Initialized { get; private set; }

        protected abstract void OnInitializeRunner();

        protected void AddExecutionAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            if (!Initialized)
            {
                executionAssembly = assembly;
            }
        }

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

            var rootFrame = Window.Current.Content ;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window

                OnInitializeRunner();

                Initialized = true;

                //var runner = new FormsRunner(executionAssembly, testAssemblies)
                //{
                //    TerminateAfterExecution = TerminateAfterExecution,
                //    Writer = Writer,
                //    AutoStart = AutoStart,
                //};

            //    var page = new RunnerPage(runner);

              //  Window.Current.Content = page;
            }

            // Ensure the current window is active
            Window.Current.Activate();

        }
    }
}
