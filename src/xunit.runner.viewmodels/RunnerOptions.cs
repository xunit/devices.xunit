

using System;
using Xunit.Runners;


namespace Xunit.Runners.UI {
	
	public class RunnerOptions {

		static public readonly RunnerOptions Current = new RunnerOptions ();
		
		public RunnerOptions ()
		{
            EnableNetwork = false;
            HostName = string.Empty;
            HostPort = 0;
		
		}

        public bool EnableNetwork { get; set; }

        public string HostName { get; set; }

        public int HostPort { get; set; }
		
		public bool AutoStart { get; set; }
		
		public bool TerminateAfterExecution { get; set; }

        public bool ShowUseNetworkLogger => (EnableNetwork && !string.IsNullOrWhiteSpace(HostName) && (HostPort > 0));

	}
}
