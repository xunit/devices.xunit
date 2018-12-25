using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ObjCRuntime;
using Xunit.Runners.UI;
using Foundation;

#if __MACOS__
using AppKit;
#endif

namespace Xunit.Runners.ResultChannels
{
    public class TextWriterResultChannel : IResultChannel
    {
        int failed;
        int passed;
        int skipped;
        TextWriter writer;
        readonly object lockOjb = new object();

        public TextWriterResultChannel(TextWriter writer)
        {
            this.writer = writer;
        }

        static string UniqueIdentifier
        {
            get
            {
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
                            var alert = NSAlert.WithMessage("Network Error", "Cancel", "Continue", null, $"Cannot connect to {hostname}:{options.HostPort}.");
                            Console.WriteLine("[Host unreachable: {0}]", "Execution cancelled");
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

            var processInfo = NSProcessInfo.ProcessInfo;
            writer.WriteLine("[{0}:\t{1}]", processInfo.HostName, processInfo.OperatingSystemVersionString);
            writer.WriteLine("[Device Name:\t{0}]", processInfo.HostName);
            //writer.WriteLine("[Device UDID:\t{0}]", UniqueIdentifier);
            //writer.WriteLine("[Device Locale:\t{0}]", NSLocale.CurrentLocale.Identifier);
            writer.WriteLine("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output

            //writer.WriteLine("[Bundle:\t{0}]", NSBundle.MainBundle.BundleIdentifier);
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