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
using test.xunit.pcltestlib;
using Xunit.Runners.UI;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace test.xunit.runner.wpa81
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : RunnerApplication
    {
        protected override void OnInitializeRunner()
        {
            // tests can be inside the main assembly
            AddTestAssembly(GetType().GetTypeInfo().Assembly);
            // otherwise you need to ensure that the test assemblies will 
            // become part of the app bundle
            AddTestAssembly(typeof(PortableTests).GetTypeInfo().Assembly);
        }
    }
}