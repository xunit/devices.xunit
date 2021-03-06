﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.OS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xunit.Runners.ResultChannels;

namespace Xunit.Runners.UI
{
    public abstract class RunnerActivity : FormsApplicationActivity
    {
        readonly List<Assembly> testAssemblies = new List<Assembly>();

        FormsRunner runner;


        Assembly executionAssembly;
        protected bool Initialized { get; private set; }

        protected bool TerminateAfterExecution { get; set; }
        [Obsolete("Use ResultChannel")]
        protected TextWriter Writer { get; set; }
        protected IResultChannel ResultChannel { get; set; }
        protected bool AutoStart { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            PlatformHelpers.Assets = Assets;

            Forms.Init(this, bundle);

            RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
            RunnerOptions.Current.AutoStart = AutoStart;

#pragma warning disable CS0618 // Type or member is obsolete
            runner = new FormsRunner(executionAssembly, testAssemblies, ResultChannel ?? new TextWriterResultChannel(Writer));
#pragma warning restore CS0618 // Type or member is obsolete

            Initialized = true;

            LoadApplication(runner);
        }

        protected void AddExecutionAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            if (!Initialized)
            {
                executionAssembly = assembly;
            }
        }

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }
    }
}