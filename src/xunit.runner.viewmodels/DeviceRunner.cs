using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        readonly IResultChannel resultChannel;
        readonly SynchronizationContext context = SynchronizationContext.Current;
        readonly AsyncLock executionLock = new AsyncLock();
        

        int failed;
        int skipped;
        int passed;
        bool cancelled;

        public DeviceRunner(Assembly executionAssembly, IReadOnlyCollection<Assembly> testAssemblies, IResultChannel resultChannel)
        {
            this.executionAssembly = executionAssembly;
            TestAssemblies = testAssemblies;
            this.resultChannel = resultChannel;
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

                if (! await resultChannel.OpenChannel(message))
                    return;
                try
                {
                    await RunTests(groups, stopWatch);
                }
                finally
                {
                    await resultChannel.CloseChannel();
                }
            }

            stopWatch.Stop();
        }

        public IReadOnlyCollection<Assembly> TestAssemblies { get; }

        Task RunTests(IEnumerable<IGrouping<string, TestCaseViewModel>> testCaseAccessor, Stopwatch stopwatch)
        {
            var tcs = new TaskCompletionSource<object>(null);

            Task.Run(() =>
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

            Task.Run(() =>
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

            var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName);

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

        public void RecordResult(TestResultViewModel result)
        {
            resultChannel.RecordResult(result);
        }
    }
}
