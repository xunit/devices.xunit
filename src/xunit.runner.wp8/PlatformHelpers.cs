using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;


namespace Xunit.Runners
{
    static class PlatformHelpers
    {
        public static void TerminateWithSuccess()
        {
            Application.Current.Terminate();
        }
    }
}