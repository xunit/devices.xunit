using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Xunit.Runners
{
    public interface ITestListener
    {
        void RecordResult(TestResultViewModel result);
    }
}