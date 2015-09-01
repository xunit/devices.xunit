using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !__UNIFIED__
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
#else 
using ObjCRuntime;
using UIKit;
#endif


namespace Xunit.Runners
{
    static class PlatformHelpers
    {
        public static void TerminateWithSuccess()
        {
            var selector = new Selector("terminateWithSuccess");
            UIApplication.SharedApplication.PerformSelector(selector, UIApplication.SharedApplication, 0);
        }
    }
}