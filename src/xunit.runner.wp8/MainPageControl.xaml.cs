using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Xamarin.Forms;
using Xunit.Runners;

namespace xunit.runner.wp8
{
    public partial class MainPageControl : UserControl
    {

        FormsRunner runner;

        private Assembly executionAssembly;
        private readonly List<Assembly> testAssemblies = new List<Assembly>();

        public bool TerminateAfterExecution { get; set; }
        public TextWriter Writer { get; set; }
        public bool AutoStart { get; set; }
        public bool Initialized { get; private set; }
        public MainPageControl()
        {
            InitializeComponent();

            Forms.Init();
        }


        public void AddExecutionAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            if (!Initialized)
            {
                executionAssembly = assembly;
            }
        }

        public void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }

        public void FinishInit(PhoneApplicationPage page)
        {
            runner = new FormsRunner(executionAssembly, testAssemblies)
            {
                TerminateAfterExecution = TerminateAfterExecution,
                Writer = Writer,
                AutoStart = AutoStart,
            };

            Content = runner.GetMainPage().ConvertPageToUIElement(page);

            Initialized = true;
        }
    }
}
