using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Xamarin.Forms;
using Xunit.Runners.Pages;
using Xunit.Runners.UI;
using Xunit.Runners.Utilities;
using Xunit.Runners.ViewModels;
using Xunit.Abstractions;

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

using Xunit.Runners.Visitors;

#if ANDROID
using Android.App;
using Android.OS;
using Android.Widget;
#endif


namespace Xunit.Runners
{
    public class FormsRunner : Xamarin.Forms.Application, ITestListener, ITestRunner
    {
        private readonly Assembly executionAssembly;
        private readonly IReadOnlyCollection<Assembly> testAssemblies;
        private bool terminateAfterExecution;
        private TextWriter writer;
        private bool autoStart;

        private readonly AsyncLock executionLock = new AsyncLock();

        private int failed;
        private int skipped;
        private int passed;
        private bool cancelled;

        private SynchronizationContext context;

        public FormsRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies)
        {
            this.executionAssembly = executionAssembly;
            this.testAssemblies = testAssemblies;
            context = SynchronizationContext.Current;

            MainPage = GetMainPage();
        }

        public bool TerminateAfterExecution
        {
            get { return terminateAfterExecution; }
            set
            {
                terminateAfterExecution = value;
                OnPropertyChanged();
            }
        }

        public TextWriter Writer
        {
            get { return writer; }
            set
            {
                writer = value;
                OnPropertyChanged();
            }
        }

        public bool AutoStart
        {
            get { return autoStart; }
            set
            {
                autoStart = value;
                OnPropertyChanged();
            }
        }

        private Page GetMainPage()
        {
//            var mainPage = new HomePage();
//            var vm = new HomeViewModel(mainPage.Navigation, testAssemblies, this);
//            mainPage.BindingContext = vm;

            var mainPage = new TestsPage();
            var mainViewModel = new TestsViewModel(mainPage.Navigation, this, this.testAssemblies);
            mainPage.BindingContext = mainViewModel;

            return new NavigationPage(mainPage);
        }

        public async Task Run(IEnumerable<ITestExecutionSink> testExecutionSinks)
        {
            foreach (var testExecutionSink in testExecutionSinks)
            {
                testExecutionSink.Reset();
            }

            var groupedTestExecutionSinks = testExecutionSinks
                .GroupBy(x => x.AssemblyFileName);
            var testExecutionSinkDictionary = testExecutionSinks
                .ToDictionary(x => x.TestCase);
            var visitor = new ResultCollectionVisitor(testExecutionSinkDictionary, this.context);

            using (await executionLock.LockAsync())
            {
                await RunTestsAsync(groupedTestExecutionSinks, visitor);
            }
        }

        public Task Run(TestCaseViewModel test)
        {
            return Run(new[] { test });
        }

        public async Task Run(IEnumerable<TestCaseViewModel> tests, string message = null)
        {

            var stopWatch = Stopwatch.StartNew();

            var groups = tests.GroupBy(t => t.AssemblyFileName);
            using (await executionLock.LockAsync())
            {
                if (message == null)
                    message = tests.Count() > 1 ? "Run Multiple Tests" : tests.First()
                                                                              .DisplayName;

                if (!OpenWriter(message))
                    return;
                try
                {
                    await RunTestsAsync(groups, stopWatch);
                }
                finally
                {
                    CloseWriter();
                }
            }

            stopWatch.Stop();
        }

        async Task RunTestsAsync(IEnumerable<IGrouping<string, ITestExecutionSink>> testExecutionSinksByAssembly, ResultCollectionVisitor visitor)
        {
            var toDispose = new List<IDisposable>();

            try
            {
                cancelled = false;

                using (AssemblyHelper.SubscribeResolve())
                {
                    foreach (var assemblyGroup in testExecutionSinksByAssembly)
                    {
//                        if (RunnerOptions.Current.ParallelizeAssemblies)
//                        {
                            await this.RunTestsInAssemblyAsync(assemblyGroup.Key, assemblyGroup, visitor);
//                        }
//                        else
//                        {
//                            this.RunTestsInAssembly(assemblyGroup.Key, assemblyGroup, visitor);
//                        }
                    }
                }
            }
            finally
            {
                toDispose.ForEach(disposable => disposable.Dispose());
            }
        }

        Task RunTestsInAssemblyAsync(string assemblyFileName, IEnumerable<ITestExecutionSink> testExecutionSinks, ResultCollectionVisitor visitor)
        {
            return Task.Run(() => this.RunTestsInAssembly(assemblyFileName, testExecutionSinks, visitor));
        }

        void RunTestsInAssembly(string assemblyFileName, IEnumerable<ITestExecutionSink> testExecutionSinks, ResultCollectionVisitor visitor)
        {
            using (var controller = new XunitFrontController(assemblyFileName, configFileName: null, shadowCopy: true))
            {
                var executionOptions = TestFrameworkOptions.ForExecution();
                controller.RunTests(testExecutionSinks.Select(x => x.TestCase), visitor, executionOptions);
                visitor.Finished.WaitOne();
            }
        }

        Task RunTestsAsync(IEnumerable<IGrouping<string, TestCaseViewModel>> testCaseAccessor, Stopwatch stopwatch)
        {
            var tcs = new TaskCompletionSource<object>(null);

            ThreadPool.QueueUserWorkItem(state =>
            {
                var toDispose = new List<IDisposable>();

                try
                {
                    cancelled = false;
                    
                    using (AssemblyHelper.SubscribeResolve())
                        if (RunnerOptions.Current.ParallelizeAssemblies)
                            testCaseAccessor
                                .Select(testCaseGroup => RunTestsInAssemblyAsync(toDispose, testCaseGroup.Key, testCaseGroup, stopwatch))
                                .ToList()
                                .ForEach(@event => @event.WaitOne());
                        else
                            testCaseAccessor
                                .ToList()
                                .ForEach(testCaseGroup => RunTestsInAssembly(toDispose, testCaseGroup.Key, testCaseGroup, stopwatch));
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
                finally
                {
                    toDispose.ForEach(disposable => disposable.Dispose());
                //    OnTestRunCompleted();
                    tcs.SetResult(null);
                }
            });

            return tcs.Task;
        }

        ManualResetEvent RunTestsInAssemblyAsync(List<IDisposable> toDispose,
                                            string assemblyFileName,
                                            IEnumerable<TestCaseViewModel> testCases,
                                            Stopwatch stopwatch)
        {
            var @event = new ManualResetEvent(initialState: false);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    RunTestsInAssembly(toDispose, assemblyFileName, testCases, stopwatch);
                }
                finally
                {
                    @event.Set();
                }
            });

            return @event;
        }

        void RunTestsInAssembly(List<IDisposable> toDispose,
                                string assemblyFileName,
                                IEnumerable<TestCaseViewModel> testCases,
                                Stopwatch stopwatch)
        {
            if (cancelled)
                return;

            var controller = new XunitFrontController(assemblyFileName, configFileName: null, shadowCopy: true);

            lock (toDispose)
                toDispose.Add(controller);

            var xunitTestCases = testCases.ToDictionary(tc => tc.TestCase);

            using (var executionVisitor = new TestExecutionVisitor(xunitTestCases, this, () => cancelled, context))
            {
                var executionOptions = TestFrameworkOptions.ForExecution();

                controller.RunTests(xunitTestCases.Keys.ToList(), executionVisitor, executionOptions);
                executionVisitor.Finished.WaitOne();

            }

        }



        //private void OnTestRunCompleted()
        //{
        //    window.BeginInvokeOnMainThread(
        //        () =>
        //        {
        //            foreach (var ts in suite_elements.Values)
        //            {
        //                // Recalc the status
        //                ts.Update();
        //            }
        //        });
        //}

        void ITestListener.RecordResult(TestResultViewModel result)
        {
            // TODO: Find out why this is happening on WP8
            var writer = Writer;
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
            if (!String.IsNullOrEmpty(message))
            {
                writer.Write(" : {0}", message.Replace("\r\n", "\\r\\n"));
            }
            writer.WriteLine();

            var stacktrace = result.ErrorStackTrace;
            if (!String.IsNullOrEmpty(result.ErrorStackTrace))
            {
                var lines = stacktrace.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                    writer.WriteLine("\t\t{0}", line);
            }
        }

#if WINDOWS_PHONE
        public bool OpenWriter(string message)
        {
            if (Writer == null)
            {
                // TODO: Add options support and use TcpTextWriter
                Writer = Console.Out;
            }
            return true;
        }
#endif

#if __IOS__
        public bool OpenWriter(string message)
        {
            RunnerOptions options = RunnerOptions.Current;
            DateTime now = DateTime.Now;
            // let the application provide it's own TextWriter to ease automation with AutoStart property
            if (Writer == null)
            {
                if (options.ShowUseNetworkLogger)
                {
                    var hostname = SelectHostName(options.HostName.Split(','), options.HostPort);

                    if (hostname != null)
                    {
                        Console.WriteLine("[{0}] Sending '{1}' results to {2}:{3}", now, message, hostname, options.HostPort);
                        try
                        {
                            Writer = new TcpTextWriter(hostname, options.HostPort);
                        }
                        catch (SocketException)
                        {
                            UIAlertView alert = new UIAlertView("Network Error",
                                String.Format("Cannot connect to {0}:{1}. Continue on console ?", hostname, options.HostPort),
                                null, "Cancel", "Continue");
                            int button = -1;
                            alert.Clicked += delegate(object sender, UIButtonEventArgs e)
                            {
                                button = (int)e.ButtonIndex;
                            };
                            alert.Show();
                            while (button == -1)
                                NSRunLoop.Current.RunUntil(NSDate.FromTimeIntervalSinceNow(0.5));
                            Console.WriteLine(button);
                            Console.WriteLine("[Host unreachable: {0}]", button == 0 ? "Execution cancelled" : "Switching to console output");
                            if (button == 0)
                                return false;
                            else
                                Writer = Console.Out;
                        }
                    }
                }
                else
                {
                    Writer = Console.Out;
                }
            }

            Writer.WriteLine("[Runner executing:\t{0}]", message);
            Writer.WriteLine("[MonoTouch Version:\t{0}]", Constants.Version);
            //Writer.WriteLine("[GC:\t{0}{1}]", GC.MaxGeneration == 0 ? "Boehm" : "sgen",
            //    NSObject.IsNewRefcountEnabled() ? "+NewRefCount" : String.Empty);
            UIDevice device = UIDevice.CurrentDevice;
            Writer.WriteLine("[{0}:\t{1} v{2}]", device.Model, device.SystemName, device.SystemVersion);
            Writer.WriteLine("[Device Name:\t{0}]", device.Name);
            Writer.WriteLine("[Device UDID:\t{0}]", UniqueIdentifier);
            Writer.WriteLine("[Device Locale:\t{0}]", NSLocale.CurrentLocale.Identifier);
            Writer.WriteLine("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output

            Writer.WriteLine("[Bundle:\t{0}]", NSBundle.MainBundle.BundleIdentifier);
            // FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, GC and Linker options)
            passed = 0;
            skipped = 0;
            failed = 0;
            return true;
        }

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        private static string UniqueIdentifier
        {
            get
            {
                var handle = UIDevice.CurrentDevice.Handle;
                if (UIDevice.CurrentDevice.RespondsToSelector(new Selector("uniqueIdentifier")))
                    return NSString.FromHandle(objc_msgSend(handle, Selector.GetHandle("uniqueIdentifier")));
                return "unknown";
            }
        }

#endif

#if ANDROID
        private bool OpenWriter(string message)
        {
            var now = DateTime.Now;
            // let the application provide it's own TextWriter to ease automation with AutoStart property
            if (Writer == null)
            {
                if (RunnerOptions.Current.ShowUseNetworkLogger)
                {
                    Console.WriteLine("[{0}] Sending '{1}' results to {2}:{3}", now, message, RunnerOptions.Current.HostName, RunnerOptions.Current.HostPort);
                    try
                    {
                        Writer = new TcpTextWriter(RunnerOptions.Current.HostName, RunnerOptions.Current.HostPort);
                    }
                    catch (SocketException)
                    {
                        var msg = String.Format("Cannot connect to {0}:{1}. Start network service or disable network option", RunnerOptions.Current.HostName, RunnerOptions.Current.HostPort);
                        Toast.MakeText(Android.App.Application.Context, msg, ToastLength.Long)
                             .Show();
                        return false;
                    }
                }
                else
                {
                    Writer = Console.Out;
                }
            }

            Writer.WriteLine("[Runner executing:\t{0}]", message);
            // FIXME
            Writer.WriteLine("[M4A Version:\t{0}]", "???");

            Writer.WriteLine("[Board:\t\t{0}]", Build.Board);
            Writer.WriteLine("[Bootloader:\t{0}]", Build.Bootloader);
            Writer.WriteLine("[Brand:\t\t{0}]", Build.Brand);
            Writer.WriteLine("[CpuAbi:\t{0} {1}]", Build.CpuAbi, Build.CpuAbi2);
            Writer.WriteLine("[Device:\t{0}]", Build.Device);
            Writer.WriteLine("[Display:\t{0}]", Build.Display);
            Writer.WriteLine("[Fingerprint:\t{0}]", Build.Fingerprint);
            Writer.WriteLine("[Hardware:\t{0}]", Build.Hardware);
            Writer.WriteLine("[Host:\t\t{0}]", Build.Host);
            Writer.WriteLine("[Id:\t\t{0}]", Build.Id);
            Writer.WriteLine("[Manufacturer:\t{0}]", Build.Manufacturer);
            Writer.WriteLine("[Model:\t\t{0}]", Build.Model);
            Writer.WriteLine("[Product:\t{0}]", Build.Product);
            Writer.WriteLine("[Radio:\t\t{0}]", Build.Radio);
            Writer.WriteLine("[Tags:\t\t{0}]", Build.Tags);
            Writer.WriteLine("[Time:\t\t{0}]", Build.Time);
            Writer.WriteLine("[Type:\t\t{0}]", Build.Type);
            Writer.WriteLine("[User:\t\t{0}]", Build.User);
            Writer.WriteLine("[VERSION.Codename:\t{0}]", Build.VERSION.Codename);
            Writer.WriteLine("[VERSION.Incremental:\t{0}]", Build.VERSION.Incremental);
            Writer.WriteLine("[VERSION.Release:\t{0}]", Build.VERSION.Release);
            Writer.WriteLine("[VERSION.Sdk:\t\t{0}]", Build.VERSION.Sdk);
            Writer.WriteLine("[VERSION.SdkInt:\t{0}]", Build.VERSION.SdkInt);
            Writer.WriteLine("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output

            // FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, Linker options)

            return true;
        }
#endif
        public void CloseWriter()
        {
            var total = passed + failed; // ignored are *not* run
            Writer.WriteLine("Tests run: {0} Passed: {1} Failed: {2} Skipped: {3}", total, passed, failed, skipped);

            Writer.Close();
            Writer = null;
        }

#if !WINDOWS_PHONE
        private static string SelectHostName(string[] names, int port)
        {
            if (names.Length == 0)
                return null;

            if (names.Length == 1)
                return names[0];

            var lock_obj = new object();
            string result = null;
            var failures = 0;

            using (var evt = new ManualResetEventSlim(false))
            {
                for (var i = names.Length - 1; i >= 0; i--)
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
                                if (failures == names.Length)
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
#endif
    }
}
