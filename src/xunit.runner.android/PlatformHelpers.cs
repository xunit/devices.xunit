using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Xunit.Runners
{
    static class PlatformHelpers
    {
        public static void TerminateWithSuccess()
        {
            Environment.Exit(0);
        }
    }
}