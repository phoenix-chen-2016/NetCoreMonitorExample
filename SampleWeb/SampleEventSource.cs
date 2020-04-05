using System.Diagnostics.Tracing;

namespace SampleWeb
{
	public class SampleEventSource : EventSource
	{
		public SampleEventSource() : base("Sample")
		{
		}
	}
}
