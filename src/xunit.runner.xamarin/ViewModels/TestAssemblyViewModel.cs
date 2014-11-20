using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Xunit.Runners.ViewModels
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        private readonly INavigation navigation;
        private readonly ITestRunner runner;
        private string detailText;
        private Color displayColor;
        private string displayName;
        private TestState result;

        internal TestAssemblyViewModel(INavigation navigation, IGrouping<string, TestCaseViewModel> @group, ITestRunner runner)
        {
            this.navigation = navigation;
            this.runner = runner;

            RunTestsCommand = new Command(RunTests);

            DisplayName = @group.Key;

            TestCases = new ObservableCollection<TestCaseViewModel>(@group);
            Result = TestState.NotRun;


            foreach (var tc in TestCases)
                WireTest(tc);

            UpdateCaption();
        }

        private void WireTest(TestCaseViewModel tc)
        {
            tc.PropertyChanged += TestCasePropertyChanged;
        }

        private void TestCasePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCaption();
        }

        private void UnwireTest(TestCaseViewModel tc)
        {
            tc.PropertyChanged -= TestCasePropertyChanged;
        }

        private void UpdateCaption()
        {
            var count = TestCases.Count;
            
            if (count == 0)
            {
                DetailText = "no test was found inside this assembly";
                DetailColor = Color.FromHex("#ff7f00");
            }
            else
            {
                var outcomes = TestCases.GroupBy(r => r.Result);

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
                    DetailText = string.Format("Success! {0} test{1}",
                                             positive, positive == 1 ? string.Empty : "s");
                    DetailColor = Color.Green;

                    Result = TestState.Passed;

                }
                else if (failure > 0 || (notRun > 0 && notRun < count))
                {
                    // we either have failures or some of the tests are not run
                    DetailText = string.Format("{0} success, {1} failure{2}, {3} skip{4}, {5} not run",
                                             positive, failure, failure > 1 ? "s" : String.Empty,
                                             skipped, skipped > 1 ? "s" : String.Empty,
                                             notRun);

                    DetailColor = Color.Red;

                    Result = TestState.Failed;
                }
                else if (Result == TestState.NotRun)
                {
                    DetailText = string.Format("{0} test case{1}, {2}",
                        count, count == 1 ? String.Empty : "s", Result);
                    DetailColor = Color.Green;
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


        public ObservableCollection<TestCaseViewModel> TestCases { get; private set; }

        public ICommand RunTestsCommand { get; private set; }

        private void RunTests()
        {
            runner.Run(TestCases);
        }
    }
}