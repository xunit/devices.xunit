using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runners.Utilities;
using Xunit.Runners.Visitors;

namespace Xunit.Runners
{
    /// <summary>
    /// 
    /// </summary>
    public class DeviceRunner : ITestListener, ITestRunner
    {
        readonly SynchronizationContext context = SynchronizationContext.Current;
        readonly Assembly executionAssembly;
        readonly AsyncLock executionLock = new AsyncLock();
        readonly INavigation navigation;
        readonly IResultChannel resultChannel;

        volatile bool cancelled;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="executionAssembly"></param>
        /// <param name="testAssemblies"></param>
        /// <param name="navigation"></param>
        /// <param name="resultChannel"></param>
        public DeviceRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies, INavigation navigation, IResultChannel resultChannel)
        {
            this.executionAssembly = executionAssembly;
            this.navigation = navigation;
            TestAssemblies = testAssemblies;
            this.resultChannel = resultChannel;
        }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyCollection<Assembly> TestAssemblies { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void RecordResult(TestResultViewModel result)
        {
            resultChannel.RecordResult(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public Task Run(TestCaseViewModel test)
        {
            return Run(new[] {test});
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task Run(IEnumerable<TestCaseViewModel> tests, string message = null)
        {
            var groups =
                tests.GroupBy(t => t.AssemblyFileName)
                     .Select(g => new AssemblyRunInfo
                     {
                         AssemblyFileName = g.Key,
                         //Configuration = ConfigReader.Load(g.Key),
                         Configuration = new TestAssemblyConfiguration(),
                         TestCases = g.ToList()
                     })
                     .ToList();


            return Run(groups, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runInfos"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task Run(IReadOnlyList<AssemblyRunInfo> runInfos, string message = null)
        {
            using (await executionLock.LockAsync())
            {
                if (message == null)
                    message = runInfos.Count > 1 || runInfos.FirstOrDefault()
                                                           ?.TestCases.Count > 1 ? "Run Multiple Tests" :
                                  runInfos.FirstOrDefault()
                                         ?.TestCases.FirstOrDefault()
                                         ?.DisplayName;


                if (!await resultChannel.OpenChannel(message))
                    return;
                try
                {
                    await RunTests(() => runInfos);
                }
                finally
                {
                    await resultChannel.CloseChannel();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IReadOnlyList<TestAssemblyViewModel>> Discover()
        {
            return Task.Run<IReadOnlyList<TestAssemblyViewModel>>(() =>
                                                                  {
                                                                      var runInfos = DiscoverTestsInAssemblies();
                                                                      return runInfos.Select(ri => new TestAssemblyViewModel(ri, this))
                                                                                     .ToList();
                                                                  });
        }


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
                        var assemblyFileName = assm.GetName()
                                                   .Name + ".dll";

                        // var configuration = ConfigReader.Load(assemblyFileName);
                        var configuration = new TestAssemblyConfiguration();
                        var discoveryOptions = TestFrameworkOptions.ForDiscovery(configuration);

                        try
                        {
                            if (cancelled)
                                break;

                            using (var framework = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName, null, false))
                            using (var sink = new TestDiscoveryVisitor())
                            {
                                framework.Find(true, sink, discoveryOptions);
                                sink.Finished.WaitOne();


                                result.Add(new AssemblyRunInfo
                                {
                                    AssemblyFileName = assemblyFileName,
                                    Configuration = configuration,
                                    TestCases = sink.TestCases.Select(tc => new TestCaseViewModel(assemblyFileName, tc, navigation, this))
                                                    .ToList()
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

        Task RunTests(Func<IReadOnlyList<AssemblyRunInfo>> testCaseAccessor)
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

            ThreadPoolHelper.RunAsync(handler);

            return tcs.Task;
        }

        void RunTestsInAssembly(List<IDisposable> toDispose, AssemblyRunInfo runInfo)
        {
            if (cancelled)
                return;

            var assemblyFileName = runInfo.AssemblyFileName;

            var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName);

            lock (toDispose)
                toDispose.Add(controller);


            var xunitTestCases = runInfo.TestCases.Select(tc => new
                                                                {
                                                                    vm = tc,
                                                                    xunit = tc.TestCase
                                                                })
                                        .Where(tc => tc.xunit != null)
                                        .ToDictionary(tc => tc.xunit, tc => tc.vm);
            var executionOptions = TestFrameworkOptions.ForExecution(runInfo.Configuration);


            using (var executionVisitor = new TestExecutionVisitor(xunitTestCases, this, executionOptions, () => cancelled, context))
            {
                controller.RunTests(xunitTestCases.Keys.ToList(), executionVisitor, executionOptions);
                executionVisitor.Finished.WaitOne();
            }
        }

        ManualResetEvent RunTestsInAssemblyAsync(List<IDisposable> toDispose, AssemblyRunInfo runInfo)
        {
            var @event = new ManualResetEvent(false);

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

            ThreadPoolHelper.RunAsync(handler);

            return @event;
        }
    }
}