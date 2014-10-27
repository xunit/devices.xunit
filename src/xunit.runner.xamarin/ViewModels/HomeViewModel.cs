using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Xamarin.Forms;
using Xunit.Runners.Pages;
using Xunit.Runners.UI;

#if __IOS__
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
#endif

namespace Xunit.Runners.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigation navigation;
        private readonly IReadOnlyCollection<Assembly> testAssemblies;
        private readonly ITestRunner runner;

        public event EventHandler ScanComplete;
        private ManualResetEventSlim mre = new ManualResetEventSlim(false);

        internal HomeViewModel(INavigation navigation, IReadOnlyCollection<Assembly> testAssemblies, ITestRunner runner)
        {
            this.navigation = navigation;
            this.testAssemblies = testAssemblies;
            this.runner = runner;
            TestAssemblies = new ObservableCollection<TestAssemblyViewModel>();

            OptionsCommand = new Command(OptionsExecute);
            CreditsCommand = new Command(CreditsExecute);
            RunEverythingCommand = new Command(RunEverythingExecute);
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

        private void CreditsExecute()
        {
            Debug.WriteLine("Credits");
        }

        private async void RunEverythingExecute()
        {
            await Run();
        }


        public ICommand OptionsCommand { get; private set; }
        public ICommand CreditsCommand { get; private set; }
        public ICommand RunEverythingCommand { get; private set; }
        public ICommand NavigateToTestAssemblyCommand { get; private set; }

        public bool AutoStart { get; set; }
        public bool TerminateAfterExecution { get; set; }

        public async void StartAssemblyScan()
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

            if (AutoStart)
            {
                await Task.Run(() => mre.Wait());
                 await Run();

                if (TerminateAfterExecution)
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
                    foreach (var assm in testAssemblies)
                    {
                        // Xunit needs the file name
                        var fileName = Path.GetFileName(assm.Location);

                        try
                        {
                            using (var framework = new XunitFrontController(fileName, configFileName: null, shadowCopy: true))
                            using (var sink = new TestDiscoveryVisitor())
                            {
                                framework.Find(includeSourceInformation: true, messageSink: sink, options: new TestFrameworkOptions());
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
