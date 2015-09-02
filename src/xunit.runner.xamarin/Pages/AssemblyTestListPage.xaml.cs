using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xunit.Runners.Pages
{
	public partial class AssemblyTestListPage : ContentPage
	{
		public AssemblyTestListPage ()
		{
			InitializeComponent ();

            // There doesn't seem to be a way to add these in XAML 
            resultStatePicker.Items.Add("All");
            resultStatePicker.Items.Add("Passed");
            resultStatePicker.Items.Add("Failed");
            resultStatePicker.Items.Add("Skipped");
            resultStatePicker.Items.Add("Not run");
            
            // TwoWay binding doesn't seem to work for this from XAML
            resultStatePicker.SelectedIndexChanged += PickerOnSelectedIndexChanged;

            resultStatePicker.SelectedIndex = 0; // Default to All
         
		}

	    private void PickerOnSelectedIndexChanged(object sender, EventArgs eventArgs)
	    {
            var i = resultStatePicker.SelectedIndex;
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

            var vm = (TestAssemblyViewModel)BindingContext;

            if (vm != null)
                vm.ResultFilter = state;
            
	    }
	}
}
