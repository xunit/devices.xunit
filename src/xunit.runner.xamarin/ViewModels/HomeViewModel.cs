﻿using System;
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
        private readonly IReadOnlyCollection<Assembly> testAssemblies;
        private readonly ITestRunner runner;
        private readonly Command runEverythingCommand;

        public event EventHandler ScanComplete;
        private ManualResetEventSlim mre = new ManualResetEventSlim(false);
        private volatile bool isBusy;

        internal HomeViewModel(INavigation navigation, IReadOnlyCollection<Assembly> testAssemblies, ITestRunner runner)
        {
            this.navigation = navigation;
            this.testAssemblies = testAssemblies;
            this.runner = runner;
            TestAssemblies = new ObservableCollection<TestAssemblyViewModel>();

            OptionsCommand = new Command(OptionsExecute);
            CreditsCommand = new Command(CreditsExecute);
            runEverythingCommand = new Command(() => ExecuteAllAsync(), () => !isBusy);
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

        public async Task ExecuteAllAsync()
        {
            try
            {
                IsBusy = true;

                foreach (var testAssembly in TestAssemblies)
                {
                    await testAssembly.ExecuteAllAsync();
                }
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

            if (runner.AutoStart)
            {
                await Task.Run(() => mre.Wait());
                await ExecuteAllAsync();

                if (runner.TerminateAfterExecution)
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
#if __UNIFIED__
                        var fileName = assm.Location;
#elif !WINDOWS_PHONE
                        var fileName = Path.GetFileName(assm.Location);
#else
                        var fileName = assm.GetName().Name + ".dll";
#endif

                        try
                        {
                            using (var framework = new XunitFrontController(fileName, configFileName: null, shadowCopy: true))
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
