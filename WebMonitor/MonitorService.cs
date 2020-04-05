using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace WebMonitor
{
	public class MonitorService : IHostedService
	{
		private readonly MetricFactory m_MetricFactory;
		private readonly Dictionary<string, Gauge> m_Gauges = new Dictionary<string, Gauge>();
		private EventPipeEventSource m_EventSource;
		private Task m_RunningTask;

		public MonitorService(MetricFactory metricFactory)
		{
			m_MetricFactory = metricFactory ?? throw new ArgumentNullException(nameof(metricFactory));

			var pid = 1;
			var client = new DiagnosticsClient(pid);

			var providerArguments = new Dictionary<string, string>
			{
				["EventCounterIntervalSec"] = "10"
			};

			var provider = new EventPipeProvider(
				"System.Runtime",
				System.Diagnostics.Tracing.EventLevel.Verbose,
				0xffffffff,
				providerArguments);
			var customEventProvider = new EventPipeProvider(
				"Sample",
				System.Diagnostics.Tracing.EventLevel.Verbose,
				0xffffffff,
				providerArguments);

			var session = client.StartEventPipeSession(new[] { provider, customEventProvider });

			m_EventSource = new EventPipeEventSource(session.EventStream);

			m_EventSource.Dynamic.All += ProcessEvents;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_RunningTask = Task.Factory.StartNew(
				() => m_EventSource.Process(),
				TaskCreationOptions.LongRunning);

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_EventSource.StopProcessing();

			return Task.CompletedTask;
		}

		private Gauge GetGauge(string name, string displayName, string displayUnits)
		{
			if (m_Gauges.ContainsKey(name))
				return m_Gauges[name];

			var gauge = m_MetricFactory.CreateGauge(name, displayName);
			m_Gauges.Add(name, gauge);

			return gauge;
		}

		private void ProcessEvents(TraceEvent obj)
		{
			if (obj.EventName.Equals("EventCounters"))
			{
				try
				{
					IDictionary<string, object> payloadVal = (IDictionary<string, object>)(obj.PayloadValue(0));
					IDictionary<string, object> payloadFields = (IDictionary<string, object>)(payloadVal["Payload"]);

					Console.WriteLine(string.Join(", ", payloadFields.Select(p => $"{p.Key}=>{p.Value}")));

					var counterName = payloadFields["Name"].ToString();
					var displayName = payloadFields["DisplayName"].ToString();
					var displayUnits = payloadFields["DisplayUnits"].ToString();
					var value = 0D;

					if (payloadFields["CounterType"].Equals("Mean"))
					{
						value = (double)payloadFields["Mean"];
					}
					else if (payloadFields["CounterType"].Equals("Sum"))
					{
						var intervalSec = (float)payloadFields["IntervalSec"];
						var increment = (double)payloadFields["Increment"];
						value = increment / intervalSec;

						if (string.IsNullOrEmpty(displayUnits))
						{
							displayUnits = "count";
						}
						displayUnits += "/sec";
					}

					var gaugeName = $"{obj.ProviderName.Replace('.', ':')}:{counterName.Replace('-', '_')}";
					var gauge = GetGauge(gaugeName, displayName, displayUnits);

					gauge.Set(value);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}
	}
}
