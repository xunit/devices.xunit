using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runners.UI;
#if __IOS__ && !__UNIFIED__
using MonoTouch;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;

#elif __IOS__ && __UNIFIED__
using Foundation;
using ObjCRuntime;
using UIKit;
#endif

namespace Xunit.Runners
{
    class ResultListener : IResultChannel
    {
        int failed;
        int passed;
        int skipped;
        TextWriter writer;
        readonly object lockOjb = new object();

        public ResultListener(TextWriter writer)
        {
            this.writer = writer;
        }

        static string UniqueIdentifier
        {
            get
            {
                var handle = UIDevice.CurrentDevice.Handle;
                if (UIDevice.CurrentDevice.RespondsToSelector(new Selector("uniqueIdentifier")))
                    return NSString.FromHandle(objc_msgSend(handle, Selector.GetHandle("uniqueIdentifier")));
                return "unknown";
            }
        }

        public void RecordResult(TestResultViewModel result)
        {
            lock (lockOjb)
            {
                if (writer == null)
                    return;
                
                if (result.TestCase.Result == TestState.Passed)
                {
                    writer.Write("\t[PASS] ");
                    passed++;
                }
                else if (result.TestCase.Result == TestState.Skipped)
                {
                    writer.Write("\t[SKIPPED] ");
                    skipped++;
                }
                else if (result.TestCase.Result == TestState.Failed)
                {
                    writer.Write("\t[FAIL] ");
                    failed++;
                }
                else
                {
                    writer.Write("\t[INFO] ");
                }
                writer.Write(result.TestCase.DisplayName);

                var message = result.ErrorMessage;
                if (!string.IsNullOrEmpty(message))
                {
                    writer.Write(" : {0}", message.Replace("\r\n", "\\r\\n"));
                }
                writer.WriteLine();

                var stacktrace = result.ErrorStackTrace;
                if (!string.IsNullOrEmpty(result.ErrorStackTrace))
                {
                    var lines = stacktrace.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                        writer.WriteLine("\t\t{0}", line);
                }
            }
        }

        public Task<bool> OpenChannel(string message = null)
        {
            lock (lockOjb)
            {
                var r = OpenWriter(message);
                if (r)
                {
                    failed = passed = skipped = 0;
                }
                return Task.FromResult(r);
            }
        }

        public Task CloseChannel()
        {
            lock (lockOjb)
            {
                var total = passed + failed; // ignored are *not* run
                writer.WriteLine("Tests run: {0} Passed: {1} Failed: {2} Skipped: {3}", total, passed, failed, skipped);
                writer.Dispose();
                writer = null;
                return Task.FromResult(true);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        [DllImport("/usr/lib/libobjc.dylib")]
        static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);
#pragma warning restore IDE1006 // Naming Styles

        bool OpenWriter(string message)
        {
            var options = RunnerOptions.Current;
            var now = DateTime.Now;
            // let the application provide it's own TextWriter to ease automation with AutoStart property
            if (writer == null)
            {
                if (options.ShowUseNetworkLogger)
                {
                    var hostname = SelectHostName(options.HostName.Split(','), options.HostPort);

                    if (hostname != null)
                    {
                        Console.WriteLine("[{0}] Sending '{1}' results to {2}:{3}", now, message, hostname, options.HostPort);
                        try
                        {
                            writer = new TcpTextWriter(hostname, options.HostPort);
                        }
                        catch (SocketException)
                        {
                            var alert = new UIAlertView("Network Error",
                                                        $"Cannot connect to {hostname}:{options.HostPort}. Continue on console ?",
                                                        null, "Cancel", "Continue");
                            var button = -1;
                            alert.Clicked += delegate(object sender, UIButtonEventArgs e) { button = (int)e.ButtonIndex; };
                            alert.Show();
                            while (button == -1)
                                NSRunLoop.Current.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5));
                            Console.WriteLine(button);
                            Console.WriteLine("[Host unreachable: {0}]", button == 0 ? "Execution cancelled" : "Switching to console output");
                            if (button == 0)
                                return false;
                        }
                    }
                }
            }

            if (writer == null)
                writer = Console.Out;

            writer.WriteLine("[Runner executing:\t{0}]", message);
            writer.WriteLine("[MonoTouch Version:\t{0}]", Constants.Version);
            //Writer.WriteLine("[GC:\t{0}{1}]", GC.MaxGeneration == 0 ? "Boehm" : "sgen",
            //    NSObject.IsNewRefcountEnabled() ? "+NewRefCount" : String.Empty);
            var device = UIDevice.CurrentDevice;
            writer.WriteLine("[{0}:\t{1} v{2}]", device.Model, device.SystemName, device.SystemVersion);
            writer.WriteLine("[Device Name:\t{0}]", device.Name);
            writer.WriteLine("[Device UDID:\t{0}]", UniqueIdentifier);
            writer.WriteLine("[Device Locale:\t{0}]", NSLocale.CurrentLocale.Identifier);
            writer.WriteLine("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output

            writer.WriteLine("[Bundle:\t{0}]", NSBundle.MainBundle.BundleIdentifier);
            // FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, GC and Linker options)

            return true;
        }

        static string SelectHostName(IReadOnlyList<string> names, int port)
        {
            if (names.Count == 0)
                return null;

            if (names.Count == 1)
                return names[0];

            var lock_obj = new object();
            string result = null;
            var failures = 0;

            using (var evt = new ManualResetEventSlim(false))
            {
                for (var i = names.Count - 1; i >= 0; i--)
                {
                    var name = names[i];
                    Task.Run(() =>
                             {
                                 try
                                 {
                                     var client = new TcpClient(name, port);
                                     using (var writer = new StreamWriter(client.GetStream()))
                                     {
                                         writer.WriteLine("ping");
                                     }
                                     lock (lock_obj)
                                     {
                                         if (result == null)
                                             result = name;
                                     }
                                     evt.Set();
                                 }
                                 catch (Exception)
                                 {
                                     lock (lock_obj)
                                     {
                                         failures++;
                                         if (failures == names.Count)
                                             evt.Set();
                                     }
                                 }
                             });
                }

                // Wait for 1 success or all failures
                evt.Wait();
            }

            return result;
        }
    }
}