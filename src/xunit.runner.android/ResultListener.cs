using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using Xunit.Runners.UI;

namespace Xunit.Runners
{
    class ResultListener : IResultChannel
    {
        int failed;
        int passed;
        int skipped;
        TextWriter writer;

        public ResultListener(TextWriter writer)
        {
            this.writer = writer;
        }

        public void RecordResult(TestResultViewModel result)
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

        public Task<bool> OpenChannel(string message = null)
        {
            var r = OpenWriter(message);

            return Task.FromResult(r);
        }

        public Task CloseChannel()
        {
            var total = passed + failed; // ignored are *not* run
            writer.WriteLine("Tests run: {0} Passed: {1} Failed: {2} Skipped: {3}", total, passed, failed, skipped);

            writer.Dispose();
            writer = null;

            return Task.FromResult(true);
        }


        bool OpenWriter(string message)
        {
            var now = DateTime.Now;
            // let the application provide it's own TextWriter to ease automation with AutoStart property
            if (writer == null)
            {
                if (RunnerOptions.Current.ShowUseNetworkLogger)
                {
                    Console.WriteLine("[{0}] Sending '{1}' results to {2}:{3}", now, message, RunnerOptions.Current.HostName, RunnerOptions.Current.HostPort);
                    try
                    {
                        writer = new TcpTextWriter(RunnerOptions.Current.HostName, RunnerOptions.Current.HostPort);
                    }
                    catch (SocketException)
                    {
                        var msg = $"Cannot connect to {RunnerOptions.Current.HostName}:{RunnerOptions.Current.HostPort}. Start network service or disable network option";
                        Toast.MakeText(Application.Context, msg, ToastLength.Long)
                             .Show();
                        return false;
                    }
                }
            }

            if (writer == null)
                writer = Console.Out;

            writer.WriteLine("[Runner executing:\t{0}]", message);
            // FIXME
            writer.WriteLine("[M4A Version:\t{0}]", "???");

            writer.WriteLine("[Board:\t\t{0}]", Build.Board);
            writer.WriteLine("[Bootloader:\t{0}]", Build.Bootloader);
            writer.WriteLine("[Brand:\t\t{0}]", Build.Brand);
            writer.WriteLine("[CpuAbi:\t{0} {1}]", Build.CpuAbi, Build.CpuAbi2);
            writer.WriteLine("[Device:\t{0}]", Build.Device);
            writer.WriteLine("[Display:\t{0}]", Build.Display);
            writer.WriteLine("[Fingerprint:\t{0}]", Build.Fingerprint);
            writer.WriteLine("[Hardware:\t{0}]", Build.Hardware);
            writer.WriteLine("[Host:\t\t{0}]", Build.Host);
            writer.WriteLine("[Id:\t\t{0}]", Build.Id);
            writer.WriteLine("[Manufacturer:\t{0}]", Build.Manufacturer);
            writer.WriteLine("[Model:\t\t{0}]", Build.Model);
            writer.WriteLine("[Product:\t{0}]", Build.Product);
            writer.WriteLine("[Radio:\t\t{0}]", Build.Radio);
            writer.WriteLine("[Tags:\t\t{0}]", Build.Tags);
            writer.WriteLine("[Time:\t\t{0}]", Build.Time);
            writer.WriteLine("[Type:\t\t{0}]", Build.Type);
            writer.WriteLine("[User:\t\t{0}]", Build.User);
            writer.WriteLine("[VERSION.Codename:\t{0}]", Build.VERSION.Codename);
            writer.WriteLine("[VERSION.Incremental:\t{0}]", Build.VERSION.Incremental);
            writer.WriteLine("[VERSION.Release:\t{0}]", Build.VERSION.Release);
            writer.WriteLine("[VERSION.Sdk:\t\t{0}]", Build.VERSION.Sdk);
            writer.WriteLine("[VERSION.SdkInt:\t{0}]", Build.VERSION.SdkInt);
            writer.WriteLine("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output

            // FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, Linker options)

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