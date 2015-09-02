using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Xunit.Runners.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssemblyTestListPage : Page
    {
        public AssemblyTestListPage()
        {
            this.InitializeComponent();

            DataContextChanged += (sender, args) => { ViewModel = DataContext as TestAssemblyViewModel; };
        }

        public TestAssemblyViewModel ViewModel { get; set; }

        void resultFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var i = resultFilter.SelectedIndex;

            var state = TestState.All;


            switch (i)
            {
                case 0:
                    state = TestState.All;
                    break;

                case 1:
                    state = TestState.Passed;
                    break;

                case 2:
                    state = TestState.Failed;
                    break;

                case 3:
                    state = TestState.Skipped;
                    break;

                case 4:
                    state = TestState.NotRun;
                    break;

            }

            if (ViewModel != null)
                ViewModel.ResultFilter = state;
        }

        void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = e.AddedItems.Cast<TestCaseViewModel>().FirstOrDefault();
            vm?.NavigateToResultCommand.Execute(null);
        }
    }
}
