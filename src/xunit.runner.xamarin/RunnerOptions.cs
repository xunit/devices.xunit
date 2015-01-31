

using System;
using Xunit.Runners;


namespace Xunit.Runners.UI {
	
	public class RunnerOptions {

		static public readonly RunnerOptions Current = new RunnerOptions ();

        // Normally this would be a bad thing, an event on a static class
        // given the lifespan of these elements, it doesn't matter.
	  //  public event EventHandler OptionsChanged;
		
		public RunnerOptions ()
		{
            EnableNetwork = false;
            HostName = string.Empty;
            HostPort = 0;
			//SortNames = defaults.BoolForKey ("display.sort");
		    ParallelizeAssemblies = false;
		    NameDisplay = NameDisplay.Short;
       

			
		
		}

        private bool EnableNetwork { get; set; }

        public string HostName { get; private set; }

        public int HostPort { get; private set; }
		
		public bool AutoStart { get; set; }
		
		public bool TerminateAfterExecution { get; set; }

        public bool ShowUseNetworkLogger
        {
            get { return (EnableNetwork && !String.IsNullOrWhiteSpace(HostName) && (HostPort > 0)); }
        }

		public bool SortNames { get; set; }
		public NameDisplay NameDisplay { get; set; }

        public bool ParallelizeAssemblies { get; set; }

        public string GetDisplayName(string displayName, string shortMethodName, string fullyQualifiedMethodName)
        {
            if (NameDisplay == NameDisplay.Full)
                return displayName;
            if (displayName == fullyQualifiedMethodName || displayName.StartsWith(fullyQualifiedMethodName + "("))
                return shortMethodName + displayName.Substring(fullyQualifiedMethodName.Length);
            return displayName;
        }
	}
}
