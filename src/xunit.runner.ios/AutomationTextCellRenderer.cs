using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xunit.Runner.iOS;
using Xunit.Runners.Utilities;

[assembly: ExportRenderer(typeof(AutomationTextCell), typeof(AutomationTextCellRenderer))]
namespace Xunit.Runner.iOS
{
    public class AutomationTextCellRenderer : TextCellRenderer
    {
        public AutomationTextCellRenderer()
        {
        }

        public override UIKit.UITableViewCell GetCell(Cell item, UIKit.UITableViewCell reusableCell, UIKit.UITableView tv)
        {
            var tableViewCell = base.GetCell(item, reusableCell, tv);
            tableViewCell.AccessibilityIdentifier = item.AutomationId;
            return tableViewCell;
        }
    }
}
