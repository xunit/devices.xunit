using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using test.xunit.pcltestlib;

namespace test.xunit.runner.uwp
{
    partial class App
    {
        partial void InitializeRunner()
        {
            // otherwise you need to ensure that the test assemblies will 
            // become part of the app bundle
            AddTestAssembly(typeof(PortableTests).GetTypeInfo().Assembly);
        }
    }
}
