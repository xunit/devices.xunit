namespace Xunit.Runners
{
    using System;
    using Xunit.Abstractions;

    public interface ITestExecutionSink
    {
        string AssemblyFileName
        {
            get;
        }

        ITestCase TestCase
        {
            get;
        }

        bool IsExecuting
        {
            get;
            set;
        }

        ITestResultMessage TestResult
        {
            get;
            set;
        }

        TimeSpan? ExecutionTime
        {
            get;
            set;
        }

        void Reset();
    }
}