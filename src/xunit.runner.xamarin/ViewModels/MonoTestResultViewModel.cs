using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit.Runners
{
    public class MonoTestResultViewModel : ViewModelBase
    {
        private TimeSpan duration;
        private string errorMessage;
        private string errorStackTrace;
        private MonoTestCaseViewModel testCase;
        private ITestResultMessage testResultMessage;

        public MonoTestResultViewModel(MonoTestCaseViewModel testCase, ITestResultMessage testResult)
        {
            if (testCase == null) throw new ArgumentNullException("testCase");
            TestCase = testCase;
            TestResultMessage = testResult;

            if (testResult != null)
                testCase.UpdateTestState(this);
        }

        public MonoTestCaseViewModel TestCase
        {
            get { return testCase; }
            private set { Set(ref testCase, value); }
        }

        public ITestResultMessage TestResultMessage
        {
            get { return testResultMessage; }
            private set { Set(ref testResultMessage, value); }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set { Set(ref duration, value); }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { Set(ref errorMessage, value); }
        }

        public string ErrorStackTrace
        {
            get { return errorStackTrace; }
            set { Set(ref errorStackTrace, value); }
        }

        internal void RaiseTestUpdated()
        {
            TestCase.RaiseTestCaseUpdated();
        }
    }
}