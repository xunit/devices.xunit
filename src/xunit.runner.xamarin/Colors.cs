using System;
using Xamarin.Forms;

namespace Xunit.Runners
{
    internal static class Colors
    {
        public static Color NoTests = Color.FromHex("#ff7f00");

        public static Color NotRun = Color.Gray;

        public static Color Running = Color.Black;

        public static Color Success = Color.Green;

        public static Color Failure = Color.Red;

        public static Color RunningWithSkipped = Color.Yellow;
    }
}

