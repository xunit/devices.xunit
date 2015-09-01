using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;
using Xunit.Abstractions;
using Xunit.Runners.Pages;
using Xunit.Runners.UI;

namespace Xunit.Runners
{
    public class TestCaseViewModel : ViewModelBase
    {
        readonly INavigation navigation;
        readonly ITestRunner runner;

        public ICommand NavigateToResultCommand { get; private set; }

        readonly string fqTestMethodName;
        ITestCase testCase;
        string assemblyFileName;
        TestResultViewModel testResult;
        string uniqueName;
        TestState result;
        string message;
        string output;
        string stackTrace;
        Color detailColor;
        string detailText;

        public string AssemblyFileName
        {
            get { return assemblyFileName; }
            private set { Set(ref assemblyFileName, value); }
        }

        public ITestCase TestCase
        {
            get { return testCase; }
            private set
            {
                if (Set(ref testCase, value))
                {
                    RaisePropertyChanged("DisplayName");
                }
            }
        }

        public TestResultViewModel TestResult
        {
            get { return testResult; }
            private set { Set(ref testResult, value); }
        }
        public string DisplayName { get { return RunnerOptions.Current.GetDisplayName(TestCase.DisplayName, TestCase.TestMethod.Method.Name, fqTestMethodName); } }


        public string UniqueName
        {
            get { return uniqueName; }
            private set { Set(ref uniqueName, value); }
        }

        internal TestCaseViewModel(string assemblyFileName, ITestCase testCase, bool forceUniqueNames, INavigation navigation, ITestRunner runner)
        {
            if (assemblyFileName == null) throw new ArgumentNullException(nameof(assemblyFileName));
            if (testCase == null) throw new ArgumentNullException(nameof(testCase));
            
            this.navigation = navigation;
            this.runner = runner;

            fqTestMethodName = $"{testCase.TestMethod.TestClass.Class.Name}.{testCase.TestMethod.Method.Name}";
            UniqueName = forceUniqueNames ? $"{fqTestMethodName} ({testCase.UniqueID})" : fqTestMethodName;
            AssemblyFileName = assemblyFileName;
            TestCase = testCase;

            Result = TestState.NotRun;
            Message = "not run";

            // Create an initial result representing not run
            TestResult = new TestResultViewModel(this, null);

            NavigateToResultCommand = new DelegateCommand(NavigateToResultsPage);
        }

        async void NavigateToResultsPage(){
            // run again
            await runner.Run(this);

            if (Result == TestState.Failed)
            {
                await navigation.PushAsync(new TestResultPage()
                {
                    BindingContext = TestResult
                });   
            }

        }

        public Color DetailColor
        {
            get { return detailColor; }
            set { Set(ref detailColor, value); }
        }

        public TestState Result
        {
            get { return result; }
            private set { Set(ref result, value); }
        }


        internal void UpdateTestState(TestResultViewModel message)
        {
            TestResult = message;

            Output = message.TestResultMessage.Output;
            Message = null;
            StackTrace = null;

            if (message.TestResultMessage is ITestPassed)
            {
                Result = TestState.Passed;
                Message = $"Success! {TestResult.Duration.TotalMilliseconds} ms";
                DetailColor = Color.Green;
            }
            if (message.TestResultMessage is ITestFailed)
            {
                Result = TestState.Failed;
                var failedMessage = (ITestFailed)(message.TestResultMessage);
                Message = ExceptionUtility.CombineMessages(failedMessage);
                StackTrace = ExceptionUtility.CombineStackTraces(failedMessage);
                DetailColor = Color.Red;
            }
            if (message.TestResultMessage is ITestSkipped)
            {
                Result = TestState.Skipped;

                var skipped = (ITestSkipped)(message.TestResultMessage);
                Message = skipped.Reason;
                DetailColor = Color.FromHex("#FF7700");
            }
        }

        // This should be raised on a UI thread as listeners will likely be
        // UI elements
   

        public string Message
        {
            get { return message; }
            private set { Set(ref message, value); }
        }

        public string Output
        {
            get { return output; }
            private set { Set(ref output, value); }
        }

        public string StackTrace
        {
            get { return stackTrace; }
            private set { Set(ref stackTrace, value); }
        }
    }
}