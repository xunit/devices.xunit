namespace Xunit.Runners.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Xunit.Abstractions;

    public sealed class ResultCollectionVisitor : TestMessageVisitor<ITestAssemblyFinished>
    {
        private readonly IDictionary<ITestCase, ITestExecutionSink> executionSinks;
        private readonly SynchronizationContext synchronizationContext;

        public ResultCollectionVisitor(IDictionary<ITestCase, ITestExecutionSink> executionSinks, SynchronizationContext synchronizationContext)
        {
            this.executionSinks = executionSinks;
            this.synchronizationContext = synchronizationContext;
        }

        protected override bool Visit(ITestCaseStarting testCaseStarting)
        {
            this.synchronizationContext
                .Post(
                    _ =>
                    {
                        this.GetTestExecutionSink(testCaseStarting.TestCase).IsExecuting = true;
                    },
                    null);

            return base.Visit(testCaseStarting);
        }
            
        protected override bool Visit(ITestFailed testFailed)
        {
            this.SetTestResult(testFailed);
            return base.Visit(testFailed);
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            this.SetTestResult(testPassed);
            return base.Visit(testPassed);
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            this.SetTestResult(testSkipped);
            return base.Visit(testSkipped);
        }

        private ITestExecutionSink GetTestExecutionSink(ITestCase testCase)
        {
            return this.executionSinks[testCase];
        }

        private void SetTestResult(ITestResultMessage result)
        {
            var testExecutionSink = this.GetTestExecutionSink(result.TestCase);

            this.synchronizationContext
                .Post(
                    _ =>
                    {
                        testExecutionSink.IsExecuting = false;
                        testExecutionSink.TestResult = result;
                        testExecutionSink.ExecutionTime = TimeSpan.FromSeconds((double)result.ExecutionTime);
                    },
                    null);
        }
    }
}