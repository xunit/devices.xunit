using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;



namespace Xunit.Runners.Pages
{
    partial class CreditsPage : ContentPage
    {
        public CreditsPage()
        {
            this.InitializeComponent();

            // Load about text
            var html = "<html><body><b>xUnit Device Runner</b><br>Copyright &copy; 2015<br>Outercurve Foundation<br>All rights reserved.<br><br>Author: Oren Novotny<hr /></body></html>";

            WebView.Source = new HtmlWebViewSource { Html = html };

        }

    }
}
