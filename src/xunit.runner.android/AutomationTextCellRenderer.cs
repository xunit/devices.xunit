using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xunit.Runners;
using Xunit.Runners.Utilities;

[assembly: ExportRenderer(typeof(AutomationTextCell), typeof(AutomationTextCellRenderer))]
namespace Xunit.Runners
{
    public class AutomationTextCellRenderer : TextCellRenderer
    {
        protected override Android.Views.View GetCellCore(Cell item, Android.Views.View convertView, Android.Views.ViewGroup parent, Android.Content.Context context)
        {
            var view = base.GetCellCore(item, convertView, parent, context);
            view.ContentDescription = Cell.AutomationId;
            return view;
        }
    }
}
