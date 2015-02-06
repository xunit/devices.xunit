using System.Linq;
using System.ComponentModel;

namespace Xunit.Runners.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Input;
    using Xunit.Runners.Utilities;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using System.Threading.Tasks;
    using Xamarin.Forms;
    using Xunit.Abstractions;

    public sealed class TestsViewModel : ViewModelBase
    {
        private readonly INavigation navigation;
        private readonly ITestRunner testRunner;
        private readonly ObservableCollection<TestViewModel> tests;
        private readonly FilteredCollectionView<TestViewModel, string> filteredTests;
        private readonly Command executeCommand;
        private readonly Command filterFailedCommand;
        private readonly Command filterPassedCommand;
        private readonly Command filterSkippedCommand;
        private volatile TestsViewModelStatus status;
        private string filterText;
        private CancellationTokenSource filterCancellationTokenSource;
        private TestExecutionProgressViewModel progress;

        public TestsViewModel(
            INavigation navigation,
            ITestRunner testRunner,
            IReadOnlyCollection<Assembly> testAssemblies)
        {
            this.navigation = navigation;
            this.testRunner = testRunner;
            this.tests = new ObservableCollection<TestViewModel>();
            this.filteredTests = new FilteredCollectionView<TestViewModel, string>(this.tests, this.FilterTest, null, new TestViewModelComparer());
            this.executeCommand = new Command(() => this.ExecuteAsync(), () => !this.IsExecutingTests);
            this.filterFailedCommand = new Command(() => { });
            this.filterPassedCommand = new Command(() => { });
            this.filterSkippedCommand = new Command(() => { });

            this.filteredTests.CollectionChanged += delegate
            {
                this.RaisePropertyChanged("ExecuteCommandText");
            };

            this.InitializeAsync(testAssemblies);
        }

        public TestsViewModelStatus Status
        {
            get { return this.status; }
            private set
            {
                if (this.Set(ref status, value))
                {
                    this.RaisePropertyChanged("IsScanningAssemblies");
                    this.RaisePropertyChanged("IsExecutingTests");
                    this.RaisePropertyChanged("IsIdle");
                    this.executeCommand.ChangeCanExecute();
                }
            }
        }

        public ICollection<TestViewModel> Tests
        {
            get { return this.tests; }
        }

        public ICollection<TestViewModel> FilteredTests
        {
            get { return this.filteredTests; }
        }

        public ICommand ExecuteCommand
        {
            get { return this.executeCommand; }
        }

        public ICommand FilterFailedCommand
        {
            get { return this.filterFailedCommand; }
        }

        public ICommand FilterPassedCommand
        {
            get { return this.filterPassedCommand; }
        }

        public ICommand FilterSkippedCommand
        {
            get { return this.filterSkippedCommand; }
        }

        public string ExecuteCommandText
        {
            get { return "Execute " + this.filteredTests.Count + " tests"; }
        }

        public string FilterFailedCommandText
        {
            get
            {
                if (this.progress == null)
                {
                    return null;
                }

                return this.progress.FailedTestCount + " failed";
            }
        }

        public string FilterPassedCommandText
        {
            get
            {
                if (this.progress == null)
                {
                    return null;
                }

                return this.progress.PassedTestCount + " passed";
            }
        }

        public string FilterSkippedCommandText
        {
            get
            {
                if (this.progress == null)
                {
                    return null;
                }

                return this.progress.SkippedTestCount + " skipped";
            }
        }

        public bool IsFilterFailedCommandVisible
        {
            get
            {
                var progress = this.Progress;
                return progress != null && progress.FailedTestCount > 0;
            }
        }

        public bool IsFilterPassedCommandVisible
        {
            get
            {
                var progress = this.Progress;
                return progress != null && progress.PassedTestCount > 0;
            }
        }

        public bool IsFilterSkippedCommandVisible
        {
            get
            {
                var progress = this.Progress;
                return progress != null && progress.SkippedTestCount > 0;
            }
        }

        public bool IsScanningAssemblies
        {
            get { return this.Status == TestsViewModelStatus.ScanningAssemblies; }
        }

        public bool IsExecutingTests
        {
            get { return this.Status == TestsViewModelStatus.ExecutingTests; }
        }

        public bool IsIdle
        {
            get { return this.Status == TestsViewModelStatus.Idle; }
        }

        public string FilterText
        {
            get { return this.filterText; }
            set
            {
                if (this.Set(ref this.filterText, value))
                {
                    this.FilterAfterDelay();
                }
            }
        }

        public TestExecutionProgressViewModel Progress
        {
            get { return this.progress; }
            private set
            {
                var progress = this.Progress;

                if (progress != null)
                {
                    this.DetachFromProgress(progress);
                    progress.Dispose();
                    this.Progress = null;
                }

                if (this.Set(ref this.progress, value))
                {
                    this.RaisePropertyChanged("HasProgress");

                    if (value != null)
                    {
                        this.AttachToProgress(value);
                    }
                }
            }
        }

        private void AttachToProgress(TestExecutionProgressViewModel progress)
        {
            progress.PropertyChanged += this.OnProgressPropertyChanged;
        }

        private void DetachFromProgress(TestExecutionProgressViewModel progress)
        {
            progress.PropertyChanged -= this.OnProgressPropertyChanged;
        }

        private void OnProgressPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FailedTestCount":
                    this.RaisePropertyChanged("FilterFailedCommandText");
                    this.RaisePropertyChanged("IsFilterFailedCommandVisible");
                    break;
                case "PassedTestCount":
                    this.RaisePropertyChanged("FilterPassedCommandText");
                    this.RaisePropertyChanged("IsFilterPassedCommandVisible");
                    break;
                case "SkippedTestCount":
                    this.RaisePropertyChanged("FilterSkippedCommandText");
                    this.RaisePropertyChanged("IsFilterSkippedCommandVisible");
                    break;
            }
        }

        public bool HasProgress
        {
            get { return this.progress != null; }
        }

        private void FilterAfterDelay()
        {
            if (this.filterCancellationTokenSource != null)
            {
                this.filterCancellationTokenSource.Cancel();
            }

            this.filterCancellationTokenSource = new CancellationTokenSource();
            var token = this.filterCancellationTokenSource.Token;

            Task
                .Delay(500, token)
                .ContinueWith(
                    x => filteredTests.FilterArgument = this.FilterText,
                    token,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task InitializeAsync(IEnumerable<Assembly> testAssemblies)
        {
            await this.ScanAssembliesAsync(testAssemblies);

            if (this.testRunner.AutoStart)
            {
                this.executeCommand.Execute(null);
            }
        }

        private async Task ScanAssembliesAsync(IEnumerable<Assembly> testAssemblies)
        {
            this.Status = TestsViewModelStatus.ScanningAssemblies;

            try
            {
                await Task.Run(() => this.ScanAssemblies(testAssemblies));
            }
            finally
            {
                this.Status = TestsViewModelStatus.Idle;
            }
        }

        private void ScanAssemblies(IEnumerable<Assembly> testAssemblies)
        {
            using (AssemblyHelper.SubscribeResolve())
            {
                foreach (var testAssembly in testAssemblies)
                {
                    this.ScanAssembly(testAssembly);
                }
            }
        }

        private void ScanAssembly(Assembly testAssembly)
        {
            var assemblyFileName = testAssembly.Location;

            using (var framework = new XunitFrontController(assemblyFileName, configFileName: null, shadowCopy: true))
            using (var sink = new TestDiscoveryVisitor())
            {
                framework.Find(includeSourceInformation: true, messageSink: sink, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                sink.Finished.WaitOne();

                foreach (var testCase in sink.TestCases)
                {
                    this.AddTestForCase(testCase, assemblyFileName);
                }
            }
        }

        private void AddTestForCase(ITestCase testCase, string assemblyFileName)
        {
            this.tests.Add(new TestViewModel(testCase, assemblyFileName));
        }

        private bool FilterTest(TestViewModel test, string filter)
        {
            return string.IsNullOrEmpty(filter) || test.FullyQualifiedName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) != -1;
        }

        private async Task ExecuteAsync()
        {
            this.Status = TestsViewModelStatus.ExecutingTests;

            var testsToExecute = this.filteredTests
                .ToList();
            this.Progress = new TestExecutionProgressViewModel(testsToExecute);

            try
            {
                await this.testRunner.Run(testsToExecute);
            }
            finally
            {
                this.Status = TestsViewModelStatus.Idle;
            }
        }

        private sealed class TestViewModelComparer : IComparer<TestViewModel>
        {
            public int Compare(TestViewModel x, TestViewModel y)
            {
                var compare = string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);

                if (compare != 0)
                {
                    return compare;
                }

                return string.Compare(x.FullyQualifiedName, y.FullyQualifiedName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}