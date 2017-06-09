using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Xunit.Runners.Utilities
{
    class RunStatusToColorConverter : IValueConverter
    {
        static readonly Brush SkippedColor = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x77, 0x00));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is RunStatus status)
            {
                switch (status)
                {
                    case RunStatus.Ok:
                        return new SolidColorBrush(Colors.Green);
                    case RunStatus.Failed:
                        return new SolidColorBrush(Colors.Red);
                    case RunStatus.NoTests:
                        return new SolidColorBrush(Colors.DimGray);
                    case RunStatus.Skipped:
                        return SkippedColor;
                    case RunStatus.NotRun:
                        return new SolidColorBrush(Colors.DimGray);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
