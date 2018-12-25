using System.Reflection;
using AppKit;
using Foundation;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;
using Xunit.Sdk;

namespace Xunit.Runners
{
    [Register("AppDelegate")]
    public class AppDelegate : RunnerAppDelegate
    {
        public override void DidFinishLaunching(NSNotification notification)
        {
            // We need this to ensure the execution assembly is part of the app bundle
            AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);

            // tests can be inside the main assembly
            AddTestAssembly(Assembly.GetExecutingAssembly());

            base.DidFinishLaunching(notification);
        }
    }
}
