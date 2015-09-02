using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Xunit.Runners.Pages
{
	partial class HomePage : ContentPage
	{
        readonly static IValueConverter AssemblyRunStatusConverter = new RunStatusToColorConverter();
	    private HomeViewModel viewModel;
		public HomePage ()
		{
			InitializeComponent ();

            
        }

	    protected override void OnBindingContextChanged()
	    {
	        base.OnBindingContextChanged();
	        if (viewModel != null)
	        {
	            viewModel.ScanComplete -= ScanComplete;
	        }

	        // Wire up the sections
	        viewModel = (HomeViewModel)BindingContext;
	        viewModel.ScanComplete += ScanComplete;

	        root.Clear();

	    }

	   

	    private void ScanComplete(object sender, EventArgs e)
	    {
            // Xam Forms requires us to redraw the table root to add new content
	        var tr = new TableRoot();
	        var fs = new TableSection("Test Assemblies");

	        foreach (var ta in viewModel.TestAssemblies)
	        {
	            var ts = new TextCell {BindingContext = ta};
	            ts.SetBinding(TextCell.TextProperty, "DisplayName");
	            ts.SetBinding(TextCell.DetailProperty, "DetailText");
	            ts.SetBinding(TextCell.DetailColorProperty, "RunStatus", converter: AssemblyRunStatusConverter);

	            ts.Command = viewModel.NavigateToTestAssemblyCommand;
	            ts.CommandParameter = ts.BindingContext;
                
                fs.Add(ts);
	        }
	        tr.Add(fs); // add the first section

	        var run = new TextCell
	        {
	            Text = "Run Everything",
	            Command = viewModel.RunEverythingCommand,
	        };

	        table.Root.Skip(1)
                .First()
	             .Insert(0, run);
	        tr.Add(table.Root.Skip(1)); // Skip the first section and add the others

	        table.Root = tr;

	    }
	}
}
