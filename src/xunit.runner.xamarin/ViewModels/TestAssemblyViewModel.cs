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
using Xamarin.Forms;
using Xunit.Runners.Utilities;

namespace Xunit.Runners.ViewModels
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        private readonly INavigation navigation;
        private readonly ITestRunner runner;
        private readonly Command runAllTestsCommand;
        private readonly Command runFilteredTestsCommand;
        private string detailText;
        private Color displayColor;
        private string displayName;
        private TestState result;
        private volatile bool isBusy;
        private string searchQuery;
        private TestState resultFilter;
        private readonly FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>> filteredTests;
        private readonly ObservableCollection<TestCaseViewModel> allTests; 
        private CancellationTokenSource filterCancellationTokenSource;

        internal TestAssemblyViewModel(INavigation navigation, IGrouping<string, TestCaseViewModel> @group, ITestRunner runner)
        {
            this.navigation = navigation;
            this.runner = runner;

            runAllTestsCommand = new Command(() => ExecuteAllAsync(), () => !isBusy);
            runFilteredTestsCommand = new Command(() => ExecuteFilteredAsync(), () => !isBusy);

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


            UpdateCaption();

        }
   
        private void UpdateCaption()
        {
            var count = allTests.Count;
            
            if (count == 0)
            {
                DetailText = "No tests were found inside this assembly";
                DetailColor = Colors.NoTests;
            }
            else if (!isBusy)
            {
                DetailText = string.Format(
                    "{0} tests awaiting execution",
                    allTests.Count);

                DetailColor = Colors.NotRun;
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

                var haveFailures = failure > 0;
                var haveSkipped = skipped > 0;
                var havePendingRun = notRun > 0;

                DetailText = string.Format(
                    "{0} successful, {1} failed, {2} skipped, {3} not run",
                    positive,
                    failure,
                    skipped,
                    notRun);

                if (!haveFailures && !havePendingRun)
                {
                    DetailColor = Colors.Success;
                    Result = TestState.Passed;
                }
                else if (haveFailures)
                {
                    DetailColor = Colors.Failure;
                    Result = TestState.Failed;
                }
                else if (haveSkipped)
                {
                    DetailColor = Colors.RunningWithSkipped;
                    Result = TestState.Skipped;
                }
                else if (havePendingRun)
                {
                    DetailColor = Colors.Running;
                }
            }
        }

        private static bool IsTestFilterMatch(TestCaseViewModel test, Tuple<string, TestState> query)
        {
            if (test == null) throw new ArgumentNullException("test");
            if (query == null) throw new ArgumentNullException("query");

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
                    this.runAllTestsCommand.ChangeCanExecute();
                    this.runFilteredTestsCommand.ChangeCanExecute();
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

        public Color DetailColor
        {
            get { return displayColor; }
            private set { Set(ref displayColor, value); }
        }

        public string DetailText
        {
            get { return detailText; }
            private set { Set(ref detailText, value); }
        }


        public IList<TestCaseViewModel> TestCases
        {
            get { return filteredTests; }
        }

        public ICommand RunAllTestsCommand
        {
            get { return runAllTestsCommand; }
        }

        public ICommand RunFilteredTestsCommand
        {
            get { return runFilteredTestsCommand; }
        }

        public async Task ExecuteAllAsync()
        {
            if (allTests.Count == 0)
            {
                return;
            }

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

        public async Task ExecuteFilteredAsync()
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