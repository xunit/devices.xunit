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
    /// </summary>
    public class DeviceRunner : ITestListener, ITestRunner
    {
        readonly SynchronizationContext context = SynchronizationContext.Current;
   
        readonly AsyncLock executionLock = new AsyncLock();
        readonly INavigation navigation;
        readonly IResultChannel resultChannel;

        volatile bool cancelled;

        public DeviceRunner(IReadOnlyCollection<Assembly> testAssemblies, INavigation navigation, IResultChannel resultChannel)
        {
            this.navigation = navigation;
            TestAssemblies = testAssemblies;
            this.resultChannel = resultChannel;
        }

        public IReadOnlyCollection<Assembly> TestAssemblies { get; }


        public void RecordResult(TestResultViewModel result)
        {
            resultChannel.RecordResult(result);
        }

        public Task Run(TestCaseViewModel test)
        {
            return Run(new[] {test});
        }

        public Task Run(IEnumerable<TestCaseViewModel> tests, string message = null)
        {
            var groups =
                tests.GroupBy(t => t.AssemblyFileName)
                     .Select(g => new AssemblyRunInfo
                     {
                         AssemblyFileName = g.Key,
                         Configuration = GetConfiguration(Path.GetFileNameWithoutExtension(g.Key)),
                         TestCases = g.ToList()
                     })
                     .ToList();


            return Run(groups, message);
        }

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

        public Task<IReadOnlyList<TestAssemblyViewModel>> Discover()
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<TestAssemblyViewModel>>();

            ThreadPoolHelper.RunAsync(() =>
                                    {

                                        try
                                        {
                                            var runInfos = DiscoverTestsInAssemblies();
                                            var list = runInfos.Select(ri => new TestAssemblyViewModel(ri, this))
                                                            .ToList();

                                            tcs.SetResult(list);
                                        }
                                        catch (Exception e)
                                        {
                                            tcs.SetException(e);
                                        }
                                    });

            return tcs.Task;
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

                        var configuration = GetConfiguration(assm.GetName()
                                                                 .Name);
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

        static TestAssemblyConfiguration GetConfiguration(string assemblyName)
        {
            var stream = GetConfigurationStreamForAssembly(assemblyName);
            if (stream != null)
            {
                using (stream)
                {
                    return ConfigReader.Load(stream);
                }
            }

            return new TestAssemblyConfiguration();
        }

        static Stream GetConfigurationStreamForAssembly(string assemblyName)
        {
#if ANDROID
            // Android needs to read the config from its asset manager
            return PlatformHelpers.ReadConfigJson(assemblyName);
#else

            // See if there's a directory with the assm name. this might be the case for appx
            if (Directory.Exists(assemblyName))
            {
                if (File.Exists(Path.Combine(assemblyName, $"{assemblyName}.xunit.runner.json")))
                {
                    return File.OpenRead(Path.Combine(assemblyName, $"{assemblyName}.xunit.runner.json"));
                }

                if (File.Exists(Path.Combine(assemblyName, "xunit.runner.json")))
                {
                    return File.OpenRead(Path.Combine(assemblyName, "xunit.runner.json"));
                }
            }

            // Fallback to working dir

            // look for a file called assemblyName.xunit.runner.json first 
            if (File.Exists($"{assemblyName}.xunit.runner.json"))
            {
                return File.OpenRead($"{assemblyName}.xunit.runner.json");
            }

            if (File.Exists("xunit.runner.json"))
            {
                return File.OpenRead("xunit.runner.json");
            }

            return null;
#endif
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
                                     tcs.TrySetResult(null);
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
                tc = tc.TestCase
            })
                                        .Where(tc => tc.tc.UniqueID != null)
                                        .ToDictionary(tc => tc.tc, tc => tc.vm);
            var executionOptions = TestFrameworkOptions.ForExecution(runInfo.Configuration);


            using (var executionVisitor = new TestExecutionVisitor(xunitTestCases, this, executionOptions, () => cancelled, context))
            {
                controller.RunTests(xunitTestCases.Select(tc => tc.Value.TestCase).ToList(), executionVisitor, executionOptions);
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
