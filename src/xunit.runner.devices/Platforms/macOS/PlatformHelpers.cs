using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AppKit;
using ObjCRuntime;

namespace Xunit.Runners
{
    static class PlatformHelpers
    {
        public static void TerminateWithSuccess()
        {
            var selector = new Selector("terminateWithSuccess");
            NSApplication.SharedApplication.PerformSelector(selector, NSApplication.SharedApplication, 0);
        }
    }
}