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
        private readonly INavigation navigation;

        public ICommand NavigateToResultCommand { get; private set; }

        public event EventHandler TestCaseUpdated;

        private readonly string fqTestMethodName;
        private ITestCase testCase;
        private string assemblyFileName;
        private TestResultViewModel testResult;
        private string uniqueName;
        private TestState result;
        private string message;
        private string output;
        private string stackTrace;

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

#if __IOS__ || MAC
        public string DisplayName { get { return RunnerOptions.Current.GetDisplayName(TestCase.DisplayName, TestCase.TestMethod.Method.Name, fqTestMethodName); } }
#else
        public string DisplayName { get { return RunnerOptions.GetDisplayName(TestCase.DisplayName, TestCase.TestMethod.Method.Name, fqTestMethodName); } }
#endif

        public string UniqueName
        {
            get { return uniqueName; }
            private set { Set(ref uniqueName, value); }
        }

        public TestCaseViewModel(string assemblyFileName, ITestCase testCase, bool forceUniqueNames, INavigation navigation)
        {
            this.navigation = navigation;
            if (assemblyFileName == null) throw new ArgumentNullException("assemblyFileName");
            if (testCase == null) throw new ArgumentNullException("testCase");

            fqTestMethodName = String.Format("{0}.{1}", testCase.TestMethod.TestClass.Class.Name, testCase.TestMethod.Method.Name);
            UniqueName = forceUniqueNames ? String.Format("{0} ({1})", fqTestMethodName, testCase.UniqueID) : fqTestMethodName;
            AssemblyFileName = assemblyFileName;
            TestCase = testCase;

            Result = TestState.NotRun;

            // Create an initial result representing not run
            TestResult = new TestResultViewModel(this, null);

            NavigateToResultCommand = new Command(NavigateToResultsPage);
        }

        private async void NavigateToResultsPage()
        {
            await navigation.PushAsync(new TestResultPage()
            {
                BindingContext = TestResult
            });

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
                Message = "Passed";
            }
            if (message.TestResultMessage is ITestFailed)
            {
                Result = TestState.Failed;
                var failedMessage = (ITestFailed)(message.TestResultMessage);
                Message = ExceptionUtility.CombineMessages(failedMessage);
                StackTrace = ExceptionUtility.CombineStackTraces(failedMessage);
            }
            if (message.TestResultMessage is ITestSkipped)
            {
                Result = TestState.Skipped;

                var skipped = (ITestSkipped)(message.TestResultMessage);
                Message = skipped.Reason;
            }
        }

        // This should be raised on a UI thread as listeners will likely be
        // UI elements
        internal void RaiseTestCaseUpdated()
        {
            var evt = TestCaseUpdated;
            if (evt != null)
                evt(this, EventArgs.Empty);
        }

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