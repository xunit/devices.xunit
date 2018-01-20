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
using Windows.UI.Xaml.Navigation;
using Xunit.Runners.UI;

namespace integration.uwp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : RunnerApplication
    {
        protected override void OnInitializeRunner()
        {
            AddTestAssembly(typeof(App).GetTypeInfo().Assembly);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Start the test runner if we have args
            if (!string.IsNullOrWhiteSpace(e.Arguments))
            {
                Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
                // Ensure the current window is active
                Window.Current.Activate();

                Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(e.Arguments);
            }
            else
            {
                base.OnLaunched(e);
            }
        }
    }
}
