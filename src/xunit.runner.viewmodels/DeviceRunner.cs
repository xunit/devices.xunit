using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runners.UI;
using Xunit.Runners.Utilities;
using Xunit.Runners.Visitors;

namespace Xunit.Runners
{
    class DeviceRunner : ITestListener, ITestRunner
    {
        readonly Assembly executionAssembly;
        readonly INavigation navigation;
        readonly IResultChannel resultChannel;
        readonly SynchronizationContext context = SynchronizationContext.Current;
        readonly AsyncLock executionLock = new AsyncLock();
        

        int failed;
        int skipped;
        int passed;
        volatile bool cancelled;

        public DeviceRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies, INavigation navigation, IResultChannel resultChannel)
        {
            this.executionAssembly = executionAssembly;
            this.navigation = navigation;
            TestAssemblies = testAssemblies;
            this.resultChannel = resultChannel;
        }



        public Task Run(TestCaseViewModel test)
        {
            return Run(new[] { test });
        }

        public async Task Run(IEnumerable<TestCaseViewModel> tests, string message = null)
        {

            Func<List<AssemblyRunInfo>> groups = () => 
                tests.GroupBy(t => t.AssemblyFileName)
                     .Select(g => new AssemblyRunInfo
                     {
                         AssemblyFileName = g.Key,
                         Configuration = ConfigReader.Load(g.Key),
                         TestCases = g.ToList()
                     })
                     .ToList();


            using (await executionLock.LockAsync())
            {
                if (message == null)
                    message = tests.Count() > 1 ? "Run Multiple Tests" : tests.First()
                                                                              .DisplayName;

                if (! await resultChannel.OpenChannel(message))
                    return;
                try
                {
                    await RunTests(groups);
                }
                finally
                {
                    await resultChannel.CloseChannel();
                }
            }
;
        }

        public Task<IReadOnlyList<TestAssemblyViewModel>> Discover()
        {
            return Task.Run<IReadOnlyList<TestAssemblyViewModel>>(() =>
                            {
                                var runInfos = DiscoverTestsInAssemblies();
                                return runInfos.Select(ri => new TestAssemblyViewModel(ri, this)).ToList();
                            });
            
        }

        public IReadOnlyCollection<Assembly> TestAssemblies { get; }


        IEnumerable<AssemblyRunInfo> DiscoverTestsInAssemblies()
        {
            var result = new List<AssemblyRunInfo>();

            try
            {
                using (AssemblyHelper.SubscribeResolve())
                {
                    foreach (var assm in TestAssemblies)
                    {
                        // Xunit needs the file name
                        var assemblyFileName = assm.GetName().Name + ".dll";
                        var assembly = new XunitProjectAssembly { AssemblyFilename = assemblyFileName };
                        var configuration = ConfigReader.Load(assemblyFileName);
                        var fileName = Path.GetFileNameWithoutExtension(assemblyFileName);

                        var discoveryOptions = TestFrameworkOptions.ForDiscovery(configuration);

                        try
                        {

                            if (cancelled)
                                break;

                            using (var framework = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName: assemblyFileName, configFileName: null, shadowCopy: false))
                            using (var sink = new TestDiscoveryVisitor())
                            {
                                framework.Find(includeSourceInformation: true, messageSink: sink, discoveryOptions: discoveryOptions);
                                sink.Finished.WaitOne();


                                result.Add(new AssemblyRunInfo
                                {
                                    AssemblyFileName = assemblyFileName,
                                    Configuration = configuration,
                                    TestCases = sink.TestCases.Select(tc => new TestCaseViewModel(assemblyFileName, tc, navigation, this)).ToList()
                                });

                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            

            return result;
        }

        Task RunTests(Func<List<AssemblyRunInfo>> testCaseAccessor)
        {
            var tcs = new TaskCompletionSource<object>(null);

            Action handler = () =>
            {
                var toDispose = new List<IDisposable>();

                try
                {
                    cancelled = false;
                    var assemblies = testCaseAccessor();
                    var parallelizeAssemblies = assemblies.All(runInfo => runInfo.Configuration.ParallelizeAssemblyOrDefault);


                    using (AssemblyHelper.SubscribeResolve())
                    {
                        if (parallelizeAssemblies)
                            assemblies
                                .Select(runInfo => RunTestsInAssemblyAsync(toDispose, runInfo))
                                .ToList()
                                .ForEach(@event => @event.WaitOne());
                        else
                            assemblies
                                .ForEach(runInfo => RunTestsInAssembly(toDispose, runInfo));
                    }
                        
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
            };

#if WINDOWS_APP || WINDOWS_PHONE || WINDOWS_PHONE_APP
            var fireAndForget = Windows.System.Threading.ThreadPool.RunAsync(_ => handler());
#else
            ThreadPool.QueueUserWorkItem(_ => handler());
#endif

            return tcs.Task;
        }

        ManualResetEvent RunTestsInAssemblyAsync(List<IDisposable> toDispose, AssemblyRunInfo runInfo)
        {
            var @event = new ManualResetEvent(initialState: false);

            Action handler = () =>
            {
                try
                {
                    RunTestsInAssembly(toDispose, runInfo);
                }
                finally
                {
                    @event.Set();
                }
            };

#if WINDOWS_APP || WINDOWS_PHONE || WINDOWS_PHONE_APP
            var fireAndForget = Windows.System.Threading.ThreadPool.RunAsync(_ => handler());
#else
            ThreadPool.QueueUserWorkItem(_ => handler());
#endif

            return @event;
        }

        void RunTestsInAssembly(List<IDisposable> toDispose, AssemblyRunInfo runInfo)
        {
            if (cancelled)
                return;
            
            var assemblyFileName = runInfo.AssemblyFileName;

            var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName);

            lock (toDispose)
                toDispose.Add(controller);



            var xunitTestCases = runInfo.TestCases.Select(tc => new { vm = tc, xunit = tc.TestCase })
                                                  .Where(tc => tc.xunit != null)
                                                  .ToDictionary(tc => tc.xunit, tc => tc.vm);
            var executionOptions = TestFrameworkOptions.ForExecution(runInfo.Configuration);


            using (var executionVisitor = new TestExecutionVisitor(xunitTestCases, this, executionOptions, () => cancelled, context))
            {
                controller.RunTests(xunitTestCases.Keys.ToList(), executionVisitor, executionOptions);
                executionVisitor.Finished.WaitOne();
            }
        }

        public void RecordResult(TestResultViewModel result)
        {
            resultChannel.RecordResult(result);
        }
    }
}
