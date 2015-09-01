using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xamarin.Forms;
using Xunit.Runners.Pages;
using Xunit.Runners.UI;

#if __IOS__ && !__UNIFIED__
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
#elif __IOS__ && __UNIFIED__
using ObjCRuntime;
using UIKit;
#endif

namespace Xunit.Runners.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigation navigation;
        private readonly ITestRunner runner;
        private readonly Command runEverythingCommand;

        public event EventHandler ScanComplete;
        private ManualResetEventSlim mre = new ManualResetEventSlim(false);
        private bool isBusy;

        internal HomeViewModel(INavigation navigation, ITestRunner runner)
        {
            this.navigation = navigation;
            this.runner = runner;
            TestAssemblies = new ObservableCollection<TestAssemblyViewModel>();

            OptionsCommand = new Command(OptionsExecute);
            CreditsCommand = new Command(CreditsExecute);
            runEverythingCommand = new Command(RunEverythingExecute, () => !isBusy);
            NavigateToTestAssemblyCommand = new Command(async vm => await navigation.PushAsync(new AssemblyTestListPage()
            {
                BindingContext = vm
            }));




            StartAssemblyScan();
        }


        public ObservableCollection<TestAssemblyViewModel> TestAssemblies { get; private set; }


        private void OptionsExecute()
        {
            Debug.WriteLine("Options");
        }

        private async void CreditsExecute()
        {
            await navigation.PushAsync(new CreditsPage());
        }

        private async void RunEverythingExecute()
        {
            try
            {
                IsBusy = true;
                await Run();
            }
            finally
            {
                IsBusy = false;
            }                       
        }


        public ICommand OptionsCommand { get; private set; }
        public ICommand	CreditsCommand { get; private set; }
        public ICommand RunEverythingCommand
        {
            get { return runEverythingCommand; }
        }
        public ICommand NavigateToTestAssemblyCommand { get; private set; }

        public bool IsBusy
        {
            get { return isBusy; }
            private set
            {
                if (Set(ref isBusy, value))
                {
                    runEverythingCommand.ChangeCanExecute();
                }
            }
        }

        public async void StartAssemblyScan()
        {
            IsBusy = true;
            try
            {
                var allTests = await Task.Run(() => DiscoverTestsInAssemblies());

                // Back on UI thread
                foreach (var group in allTests)
                {
                    var vm = new TestAssemblyViewModel(navigation, group, runner);
                    TestAssemblies.Add(vm);
                }

                var evt = ScanComplete;
                if (evt != null)
                    evt(this, EventArgs.Empty);

                mre.Set();

            }
            finally
            {
                IsBusy = false;
            }

            if (RunnerOptions.Current.AutoStart)
            {
                await Task.Run(() => mre.Wait());
                await Run();

                if (RunnerOptions.Current.TerminateAfterExecution)
                    TerminateWithSuccess();
            }
        }

#if __IOS__
        private static void TerminateWithSuccess()
        {
            var selector = new Selector("terminateWithSuccess");
            UIApplication.SharedApplication.PerformSelector(selector, UIApplication.SharedApplication, 0);
        }
#endif

#if ANDROID
        private static void TerminateWithSuccess()
        {
            Environment.Exit(0);
        }
#endif


#if WINDOWS_PHONE
        private static void TerminateWithSuccess()
        {
            System.Windows.Application.Current.Terminate();   
        }
#endif

#if NETFX_CORE
        private static void TerminateWithSuccess()
        {
            Windows.UI.Xaml.Application.Current.Exit();
        }
#endif
        private Task Run()
        {
            return runner.Run(TestAssemblies.SelectMany(t => t.TestCases), "Run Everything");
        }

        private IEnumerable<IGrouping<string, TestCaseViewModel>> DiscoverTestsInAssemblies()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new List<IGrouping<string, TestCaseViewModel>>();

            try
            {
                using (AssemblyHelper.SubscribeResolve())
                {
                    foreach (var assm in runner.TestAssemblies)
                    {
                        // Xunit needs the file name
#if __UNIFIED__
                        var fileName = assm.Location;
#elif !WINDOWS_PHONE && !NETFX_CORE
                        var fileName = Path.GetFileName(assm.Location);
#else
                        var fileName = assm.GetName().Name + ".dll";
#endif

                        try
                        {
                            using (var framework = new XunitFrontController(AppDomainSupport.Denied, fileName))
                            using (var sink = new TestDiscoveryVisitor())
                            {
                                framework.Find(includeSourceInformation: true, messageSink: sink, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                                sink.Finished.WaitOne();

                                result.Add(
                                    new Grouping<string, TestCaseViewModel>(
                                        fileName,
                                        sink.TestCases
                                            .GroupBy(tc => String.Format("{0}.{1}", tc.TestMethod.TestClass.Class.Name, tc.TestMethod.Method.Name))
                                            .SelectMany(group =>
                                                        group.Select(testCase =>
                                                                     new TestCaseViewModel(fileName, testCase, forceUniqueNames: group.Count() > 1, navigation: navigation, runner: runner)))
                                            .ToList()
                                        )
                                    );
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            stopwatch.Stop();

            return result;
        }



        private class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly IEnumerable<TElement> elements;

            public Grouping(TKey key, IEnumerable<TElement> elements)
            {
                Key = key;
                this.elements = elements;
            }

            public TKey Key { get; private set; }

            public IEnumerator<TElement> GetEnumerator()
            {
                return elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return elements.GetEnumerator();
            }
        }
    }
}
