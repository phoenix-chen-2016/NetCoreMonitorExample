using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Tracing;

namespace SampleWeb
{
	public class SampleEventHostedService : IHostedService
	{
		private readonly IncrementingEventCounter m_Counter;
		private readonly CancellationTokenSource m_Cancellation = new CancellationTokenSource();

		public SampleEventHostedService(SampleEventSource eventSource)
		{
			m_Counter = new IncrementingEventCounter("test-incrementing", eventSource);
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Task.Run(async () =>
			{
				while (true && !m_Cancellation.IsCancellationRequested)
				{
					m_Counter.Increment();

					await Task.Delay(100).ConfigureAwait(false);
				}
			});

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Cancellation.Cancel();

			return Task.CompletedTask;
		}
	}
}
