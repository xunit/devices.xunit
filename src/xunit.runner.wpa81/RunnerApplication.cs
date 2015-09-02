using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Xamarin.Forms.Platform.WinRT;

namespace Xunit.Runners.UI
{
    public abstract class RunnerApplication : Windows.UI.Xaml.Application
    {
        private TransitionCollection transitions;
        private Assembly executionAssembly;
        private readonly List<Assembly> testAssemblies = new List<Assembly>();

        public bool TerminateAfterExecution { get; set; }
        public TextWriter Writer { get; set; }
        public bool AutoStart { get; set; }
        public bool Initialized { get; private set; }
  
        protected abstract void OnInitializeRunner();


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
            Xamarin.Forms.Forms.Init(e);

            var rootFrame = Window.Current.Content as RunnerPage;

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

                RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
                RunnerOptions.Current.AutoStart = AutoStart;

                var runner = new FormsRunner(executionAssembly, testAssemblies, Writer);

                var page = new RunnerPage(runner);

                Window.Current.Content = page;
            }
            
            // Ensure the current window is active
            Window.Current.Activate();

        }
        
    }
}
