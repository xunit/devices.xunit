using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Xunit.Runners.Utilities;

namespace Xunit.Runners.ViewModels
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        readonly ITestRunner runner;
        readonly DelegateCommand runAllTestsCommand;
        readonly DelegateCommand runFilteredTestsCommand;
        string detailText;
        RunStatus runStatus;
        string displayName;
        TestState result;
        bool isBusy;
        string searchQuery;
        TestState resultFilter;
        readonly FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>> filteredTests;
        readonly ObservableCollection<TestCaseViewModel> allTests; 
        CancellationTokenSource filterCancellationTokenSource;

        internal TestAssemblyViewModel(IGrouping<string, TestCaseViewModel> @group, ITestRunner runner)
        {
            this.runner = runner;

            runAllTestsCommand = new DelegateCommand(RunAllTests, () => !isBusy);
            runFilteredTestsCommand = new DelegateCommand(RunFilteredTests, () => !isBusy);

            DisplayName = Path.GetFileNameWithoutExtension(@group.Key);

            allTests = new ObservableCollection<TestCaseViewModel>(@group);

            filteredTests = new FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>>(
                allTests,
                IsTestFilterMatch,
                Tuple.Create(SearchQuery, ResultFilter),
                new TestComparer()
                );

            filteredTests.ItemChanged += (sender, args) => UpdateCaption();
            filteredTests.CollectionChanged += (sender, args) => UpdateCaption();

            Result = TestState.NotRun;
            RunStatus = RunStatus.Ok;

            UpdateCaption();

        }
   
        private void UpdateCaption()
        {
            var count = allTests.Count;
            
            if (count == 0)
            {
                DetailText = "no test was found inside this assembly";

                RunStatus = RunStatus.NoTests;
            }
            else
            {
                var outcomes = allTests.GroupBy(r => r.Result);

                var results = outcomes.ToDictionary(k => k.Key, v => v.Count());

                int positive;
                results.TryGetValue(TestState.Passed, out positive);

                int failure;
                results.TryGetValue(TestState.Failed, out failure);

                int skipped;
                results.TryGetValue(TestState.Skipped, out skipped);

                int notRun;
                results.TryGetValue(TestState.NotRun, out notRun);

                // No failures and all run
                if (failure == 0 && notRun == 0)
                {
                    DetailText = $"Success! {positive} test{(positive == 1 ? string.Empty : "s")}";
                    RunStatus = RunStatus.Ok;

                    Result = TestState.Passed;

                }
                else if (failure > 0 || (notRun > 0 && notRun < count))
                {
                    // we either have failures or some of the tests are not run
                    DetailText = $"{positive} success, {failure} failure{(failure > 1 ? "s" : String.Empty)}, {skipped} skip{(skipped > 1 ? "s" : String.Empty)}, {notRun} not run";

                    RunStatus = RunStatus.Failed;

                    Result = TestState.Failed;
                }
                else if (Result == TestState.NotRun)
                {
                    DetailText = $"{count} test case{(count == 1 ? String.Empty : "s")}, {Result}";
                    RunStatus = RunStatus.Ok;
                }
            }
            
        }

        static bool IsTestFilterMatch(TestCaseViewModel test, Tuple<string, TestState> query)
        {
            if (test == null) throw new ArgumentNullException(nameof(test));
            if (query == null) throw new ArgumentNullException(nameof(query));

            TestState? requiredTestState;
            switch (query.Item2)
            {
                case TestState.All:
                    requiredTestState = null;
                    break;
                case TestState.Passed:
                    requiredTestState = TestState.Passed;
                    break;
                case TestState.Failed:
                    requiredTestState = TestState.Failed;
                    break;
                case TestState.Skipped:
                    requiredTestState = TestState.Skipped;
                    break;
                case TestState.NotRun:
                    requiredTestState = TestState.NotRun;
                    break;
                default:
                    throw new ArgumentException();
            }

            if (requiredTestState.HasValue && test.Result != requiredTestState.Value)
            {
                return false;
            }

            var pattern = query.Item1;
            return string.IsNullOrWhiteSpace(pattern) || test.UniqueName.IndexOf(pattern.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public string SearchQuery
        {
            get { return searchQuery; }
            set
            {
                if (Set(ref searchQuery, value))
                {
                    this.FilterAfterDelay();
                }
            }
        }
        
        public TestState ResultFilter
        {
            get { return resultFilter; }
            set
            {
                if (Set(ref resultFilter, value))
                {
                    this.FilterAfterDelay();
                }
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            private set 
            {
                if (Set(ref isBusy, value))
                {
                    this.runAllTestsCommand.RaiseCanExecuteChanged();
                    this.runFilteredTestsCommand.RaiseCanExecuteChanged();
        }
            }
        }

        public TestState Result
        {
            get { return result; }
            set { Set(ref result, value); }
        }

        public string DisplayName
        {
            get { return displayName; }
            private set { Set(ref displayName, value); }
        }

        public RunStatus RunStatus
        {
            get { return runStatus; }
            private set { Set(ref runStatus, value); }
        }

        public string DetailText
        {
            get { return detailText; }
            private set { Set(ref detailText, value); }
        }


        public IList<TestCaseViewModel> TestCases => filteredTests;

        public ICommand RunAllTestsCommand => runAllTestsCommand;

        public ICommand RunFilteredTestsCommand => runFilteredTestsCommand;

        private async void RunAllTests()
        {
            try
            {
                IsBusy = true;
                await runner.Run(allTests);
            }
            finally
            {
                IsBusy = false;
            }
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
                    x =>
                    {
                        filteredTests.FilterArgument = Tuple.Create(SearchQuery, ResultFilter);
                    },
                    token,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void RunFilteredTests()
        {
            try
            {
                IsBusy = true;
                await runner.Run(filteredTests);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private class TestComparer : IComparer<TestCaseViewModel>
        {
            public int Compare(TestCaseViewModel x, TestCaseViewModel y)
            {
                int compare = string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
                if (compare != 0)
                {
                    return compare;
                }

                return string.Compare(x.UniqueName, y.UniqueName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}