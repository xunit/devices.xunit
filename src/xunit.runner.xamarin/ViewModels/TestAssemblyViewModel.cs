using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace Xunit.Runners.ViewModels
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        private readonly INavigation navigation;

        public TestAssemblyViewModel(INavigation navigation)
        {
            this.navigation = navigation;
        }

        public ObservableCollection<TestCaseViewModel> TestCases { get; private set; }
    }
}
