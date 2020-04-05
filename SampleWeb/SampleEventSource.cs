using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class SampleEventSource : EventSource
	{
		private Dictionary<string, DiagnosticCounter> _counters = new Dictionary<string, DiagnosticCounter>();

		public SampleEventSource() : base("Sample")
		{
		}

		public void AddPollingCounterDef(
			string key,
			string displayName,
			Func<double> merticProvider,
			string displayUnits = null)
		{
			_counters.Add(
				key,
				new PollingCounter(key, this, merticProvider)
				{
					DisplayName = displayName,
					DisplayUnits = displayUnits
				});
		}
	}
}
