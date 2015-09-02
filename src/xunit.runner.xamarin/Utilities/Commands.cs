using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Xunit.Runners.Utilities
{
    static class Commands
    {
        static Commands()
        {
            LaunchUrl = new DelegateCommand<string>(OnLaunchUrl);
        }

        public static ICommand LaunchUrl { get; private set; }

        static void OnLaunchUrl(string str)
        {
            Device.OpenUri(new Uri(str));
        }
    }
}