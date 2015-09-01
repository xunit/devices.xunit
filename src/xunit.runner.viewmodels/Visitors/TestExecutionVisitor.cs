using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Runners.UI;


namespace Xunit.Runners.Visitors
{
    class TestExecutionVisitor : TestMessageVisitor<ITestAssemblyFinished>
    {
        private readonly Dictionary<ITestCase, TestCaseViewModel> testCases;
        private readonly ITestListener listener;
        private readonly Func<bool> cancelledThunk;

        private readonly SynchronizationContext context;

        public TestExecutionVisitor(Dictionary<ITestCase, TestCaseViewModel> testCases, ITestListener listener, Func<bool> cancelledThunk, SynchronizationContext context)
        {
            if (testCases == null) throw new ArgumentNullException("testCases");
            if (listener == null) throw new ArgumentNullException("listener");
            if (cancelledThunk == null) throw new ArgumentNullException("cancelledThunk");

            this.testCases = testCases;
            this.listener = listener;
            this.cancelledThunk = cancelledThunk;
            this.context = context;
        }

        protected override bool Visit(ITestFailed testFailed)
        {
            MakeTestResultViewModel(testFailed, TestState.Failed);
            return !cancelledThunk();
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            MakeTestResultViewModel(testPassed, TestState.Passed);
            return !cancelledThunk();
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            MakeTestResultViewModel(testSkipped, TestState.Skipped);
            return !cancelledThunk();
        }

        private async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
        {
            var tcs = new TaskCompletionSource<TestResultViewModel>();
            var testCase = testCases[testResult.TestCase];
            var fqTestMethodName = String.Format("{0}.{1}", testResult.TestMethod.TestClass.Class.Name, testResult.TestMethod.Method.Name);
            var displayName = RunnerOptions.Current.GetDisplayName(testResult.Test.DisplayName, testResult.TestCase.TestMethod.Method.Name, fqTestMethodName);


            // Create the result VM on the UI thread as it updates properties
            context.Post(_ =>
            {
                var result = new TestResultViewModel(testCase, testResult)
                {
                    Duration = TimeSpan.FromSeconds((double)testResult.ExecutionTime),
                };

                // Work around VS considering a test "not run" when the duration is 0
                if (result.Duration.TotalMilliseconds == 0)
                    result.Duration = TimeSpan.FromMilliseconds(1);

                if (outcome == TestState.Failed)
                {
                    result.ErrorMessage = ExceptionUtility.CombineMessages((ITestFailed)testResult);
                    result.ErrorStackTrace = ExceptionUtility.CombineStackTraces((ITestFailed)testResult);
                }

                tcs.TrySetResult(result);

            }, null);

            var r = await tcs.Task;
            listener.RecordResult(r); // bring it back to the threadpool thread

        }
    }
}