using System;

using Xamarin.Forms;

namespace Xunit.Runner.xunit.runner.devices.Platforms.Forms.Pages
{
    public class AssemblyTestListPage : ContentPage
    {
        public AssemblyTestListPage()
        {
            Content = new StackLayout
            {
                Children = {
                    new Label { Text = "Hello ContentPage" }
                }
            };
        }
    }
}

