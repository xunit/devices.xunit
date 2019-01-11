using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xunit.Runners;
using Xunit.Runners.UI;
using Xunit.Runners.ResultChannels;
using Xamarin.Forms.Platform.MacOS;
using AppKit;
#if __UNIFIED__
using Foundation;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Xunit.Runners
{
    public class RunnerAppDelegate : FormsApplicationDelegate
    {
        readonly List<Assembly> testAssemblies = new List<Assembly>();
        Assembly executionAssembly;
        FormsRunner runner;
        // class-level declarations

        protected bool AutoStart { get; set; }

        protected bool Initialized { get; set; }

        protected bool TerminateAfterExecution { get; set; }
        [Obsolete("Use ResultChannel")]
        protected TextWriter Writer { get; set; }
        protected IResultChannel ResultChannel { get; set; }

        NSWindow window;
        public RunnerAppDelegate()
        {
            var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled;

            var rect = new CoreGraphics.CGRect(200, 1000, 1024, 768);
            window = new NSWindow(rect, style, NSBackingStore.Buffered, false);
            window.Title = "Xamarin.Forms on Mac!"; // choose your own Title here
            window.TitleVisibility = NSWindowTitleVisibility.Hidden;
        }

        public override NSWindow MainWindow
        {
            get { return window; }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            Forms.Init();

            RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
            RunnerOptions.Current.AutoStart = AutoStart;

#pragma warning disable CS0618 // Type or member is obsolete
            runner = new FormsRunner(executionAssembly, testAssemblies, ResultChannel ?? new TextWriterResultChannel(Writer));
#pragma warning restore CS0618 // Type or member is obsolete

            Initialized = true;

            LoadApplication(runner);
            base.DidFinishLaunching(notification);
        }

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
    }
}