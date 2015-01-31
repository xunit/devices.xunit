//
// Copyright 2011-2012 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.OS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Xunit.Runners.UI
{
    public class RunnerActivity : FormsApplicationActivity
    {
        private readonly List<Assembly> testAssemblies = new List<Assembly>();

        private FormsRunner runner;


        private Assembly executionAssembly;
        protected bool Initialized { get; private set; }

        protected bool TerminateAfterExecution { get; set; }
        protected TextWriter Writer { get; set; }
        protected bool AutoStart { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            
            runner = new FormsRunner(executionAssembly, testAssemblies)
            {
                TerminateAfterExecution = TerminateAfterExecution,
                Writer = Writer,
                AutoStart = AutoStart
            };

            Initialized = true;

            LoadApplication(runner);
        }

        protected void AddExecutionAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            if (!Initialized)
            {
                executionAssembly = assembly;
            }
        }

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }
    }
}