using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xunit.Runners
{
    class RunStatusToColorConverter : IValueConverter
    {
        internal static readonly Color NoTestColor = Color.FromHex("#ff7f00");
        internal static readonly Color SkippedColor = Color.FromHex("#ff7700");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RunStatus status)
            {
                switch (status)
                {
                    case RunStatus.Ok:
                        return Color.Green;
                    case RunStatus.Failed:
                        return Color.Red;
                    case RunStatus.NoTests:
                        return NoTestColor;
                    case RunStatus.Skipped:
                        return SkippedColor;
                    case RunStatus.NotRun:
                        return Color.DarkGray;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return Color.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}