using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace WebScraperService
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class WebScraperService : StatelessService
	{
		public WebScraperService(StatelessServiceContext context)
			: base(context)
		{ }

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[0];
		}

		/// <summary>
		/// This is the main entry point for your service instance.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			long iterations = 0;

			var actorProxyFactory = new ActorProxyFactory();
			var readSpeakers = new List<string>();

			var scraper = new Scraper(this.Context);

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var speakers = await scraper.ScanSpeakers();

				var tasks = new List<Task>();
				foreach (var speaker in speakers)
				{
					if (!readSpeakers.Contains(speaker.GetShortHash()))
					{
						ServiceEventSource.Current.ServiceMessage(this.Context, $"Found new/updated speaker {speaker.Name}");
						readSpeakers.Add(speaker.GetShortHash());
					}
				}
				await Task.WhenAll(tasks);

				var sessions = await scraper.ScanSessions();
				tasks = new List<Task>();

				scraper.AddSessionsToSpeakers(speakers, sessions);

				foreach (var speaker in speakers)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"Updating {speaker.Sessions.Count} sessions for speaker {speaker.Name}");
				}
				await Task.WhenAll(tasks);


				ServiceEventSource.Current.ServiceMessage(this.Context, "Scanning sessions-{0}", ++iterations);

				await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
			}
		}
	}
}
