using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Xunit.Runners
{
    static class SecondaryView
    {
        static readonly CoreApplicationView view = CoreApplication.CreateNewView();

        public static CoreDispatcher Dispatcher => view.Dispatcher;
    }
}
