using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Windows.UI.Xaml;


namespace Xunit.Runners
{
    static class PlatformHelpers
    {
        public static void TerminateWithSuccess()
        {
            Application.Current.Exit();
        }
    }
}