using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace Xunit.Runners.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigation navigation;

        public HomeViewModel(INavigation navigation)
        {
            this.navigation = navigation;
        }

        public ObservableCollection<TestAssemblyViewModel> TestAssemblies { get; private set; }  
        
    }
}
