using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Xunit.Runners.ViewModels
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        private readonly INavigation navigation;
        private string displayName;

        public TestAssemblyViewModel(INavigation navigation, IGrouping<string, TestCaseViewModel> @group)
        {
            this.navigation = navigation;

            RunTestsCommand = new Command(RunTests);

            DisplayName = @group.Key;

            TestCases = new ObservableCollection<TestCaseViewModel>();
        }

        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value); }
        }

        public ObservableCollection<TestCaseViewModel> TestCases { get; private set; }

        public ICommand RunTestsCommand { get; private set; }

        private void RunTests()
        {
         
        }
    }
}
