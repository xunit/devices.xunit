using Xamarin.Forms;
using System.IO;

namespace Xunit.Runners.ViewModels
{
    using System;
    using Xunit.Abstractions;
    using Xunit.Runners.UI;

    public sealed class TestViewModel : ViewModelBase, ITestExecutionSink
    {
        private static readonly ImageSource pendingExecutionImageSource;
        private static readonly ImageSource executingImageSource;
        private static readonly ImageSource failedImageSource;
        private static readonly ImageSource passedImageSource;
        private static readonly ImageSource skippedImageSource;
        private readonly ITestCase testCase;
        private readonly string assemblyFileName;
        private bool isExecuting;
        private ITestResultMessage testResult;
        private TimeSpan? executionTime;

        static TestViewModel()
        {
            pendingExecutionImageSource = ImageSource.FromStream(() => typeof(TestViewModel).Assembly.GetManifestResourceStream("Xunit.Runner.iOS.PendingExecution.png"));
            executingImageSource = ImageSource.FromStream(() => typeof(TestViewModel).Assembly.GetManifestResourceStream("Xunit.Runner.iOS.Executing.png"));
            failedImageSource = ImageSource.FromStream(() => typeof(TestViewModel).Assembly.GetManifestResourceStream("Xunit.Runner.iOS.Failed.png"));
            passedImageSource = ImageSource.FromStream(() => typeof(TestViewModel).Assembly.GetManifestResourceStream("Xunit.Runner.iOS.Passed.png"));
            skippedImageSource = ImageSource.FromStream(() => typeof(TestViewModel).Assembly.GetManifestResourceStream("Xunit.Runner.iOS.Skipped.png"));
        }

        public TestViewModel(ITestCase testCase, string assemblyFileName)
        {
            this.testCase = testCase;
            this.assemblyFileName = assemblyFileName;
        }

        public ITestCase TestCase
        {
            get { return this.testCase; }
        }

        public string AssemblyFileName
        {
            get { return this.assemblyFileName; }
        }

        public bool IsExecuting
        {
            get { return this.isExecuting; }
            set
            {
                if (this.Set(ref this.isExecuting, value))
                {
                    this.RaisePropertyChanged("StatusImageSource");
                }
            }
        }

        public bool IsPendingExecution
        {
            get { return this.TestResult == null; }
        }

        public bool IsFailed
        {
            get
            {
                var result = this.TestResult;
                return result != null && result is ITestFailed;
            }
        }

        public bool IsSkipped
        {
            get
            {
                var result = this.TestResult;
                return result != null && result is ITestSkipped;
            }
        }

        public bool IsPassed
        {
            get
            {
                var result = this.TestResult;
                return result != null && result is ITestPassed;
            }
        }

        public ITestResultMessage TestResult
        {
            get { return this.testResult; }
            set
            {
                if (this.Set(ref this.testResult, value))
                {
                    this.RaisePropertyChanged("IsPendingExecution");
                    this.RaisePropertyChanged("IsFailed");
                    this.RaisePropertyChanged("IsSkipped");
                    this.RaisePropertyChanged("IsPassed");

                    this.RaisePropertyChanged("StatusImageSource");
                }
            }
        }

        public TimeSpan? ExecutionTime
        {
            get { return this.executionTime; }
            set
            {
                if (this.Set(ref this.executionTime, value))
                {
                    this.RaisePropertyChanged("Detail");
                }
            }
        }

        public ImageSource StatusImageSource
        {
            get
            {
                if (this.IsPendingExecution)
                {
                    return pendingExecutionImageSource;
                }
                else if (this.IsExecuting)
                {
                    return executingImageSource;
                }
                else if (this.IsPassed)
                {
                    return passedImageSource;
                }
                else if (this.IsSkipped)
                {
                    return skippedImageSource;
                }

                return failedImageSource;
            }
        }

        public string Detail
        {
            get
            {
                if (!this.ExecutionTime.HasValue)
                {
                    return null;
                }

                return string.Format("{0}ms", this.ExecutionTime.GetValueOrDefault().TotalMilliseconds);
            }
        }

        public string FullyQualifiedName
        {
            get { return testCase.TestMethod.TestClass.Class.Name + "." + testCase.TestMethod.Method.Name; }
        }

        public string DisplayName
        {
            get { return RunnerOptions.Current.GetDisplayName(this.testCase.DisplayName, this.testCase.TestMethod.Method.Name, this.FullyQualifiedName); }
        }

        public void Reset()
        {
            this.TestResult = null;
            this.ExecutionTime = null;
        }
    }
}