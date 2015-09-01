using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit.Runners
{
    public class TestResultViewModel : ViewModelBase
    {
        TimeSpan duration;
        string errorMessage;
        string errorStackTrace;
        TestCaseViewModel testCase;
        ITestResultMessage testResultMessage;

        public TestResultViewModel(TestCaseViewModel testCase, ITestResultMessage testResult)
        {
            if (testCase == null) throw new ArgumentNullException(nameof(testCase));
            TestCase = testCase;
            TestResultMessage = testResult;

            if (testResult != null)
                testCase.UpdateTestState(this);
        }

        public TestCaseViewModel TestCase
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
            set 
            {
                if (Set(ref duration, value))
                {
                    testCase?.UpdateTestState(this);
                }
            }
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
        
    }
}