using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Xunit.Runners
{
    public interface ITestListener
    {
        void RecordResult(TestResultViewModel result);
    }
}