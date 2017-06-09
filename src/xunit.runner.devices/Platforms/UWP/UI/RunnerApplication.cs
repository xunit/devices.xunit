using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Xunit.Runners.UI
{
    public abstract class RunnerApplication : Application
    {
        readonly List<Assembly> testAssemblies = new List<Assembly>();

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
            Resources.MergedDictionaries.Add(new DeviceResources());

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.Navigated += OnNavigated;

                // Place the frame in the current Window

                OnInitializeRunner();

                Initialized = true;

                RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
                RunnerOptions.Current.AutoStart = AutoStart;

                var nav = new Navigator(rootFrame);

                var runner = new DeviceRunner(testAssemblies, nav, new ResultListener(Writer));
                var hvm = new HomeViewModel(nav, runner);

                nav.NavigateTo(NavigationPage.Home, hvm);

              

                Window.Current.Content = rootFrame;
            }

            // Ensure the current window is active
            Window.Current.Activate();
            // Hook up the default Back handler
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, args) =>
            {
                if (rootFrame.CanGoBack)
                {
                    args.Handled = true;
                    rootFrame.GoBack();
                }
            };
        }

        void OnNavigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;

            var page = e.Content as Page;
            if(page != null)
                page.DataContext = e.Parameter;
        }
    }
}
