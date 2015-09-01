using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xunit.Runners
{
    public enum TestState
    {
        All = 0,
        Passed,
        Failed,
        Skipped,
        NotRun
    }


    public enum NameDisplay
    {
        Short = 1,
        Full = 2,
    }
}