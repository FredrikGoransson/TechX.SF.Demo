using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using SpeakerActor.Interfaces;

namespace WebScraperService
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class WebScraperService : StatelessService
	{
		private readonly IActorProxyFactory _actorProxyFactory;
		private readonly IServiceProxyFactory _serviceProxyFactory;
		private readonly string _speakerActorApplicationName;
		private readonly string _speakerActorServiceName;
		private readonly Uri _speakerActorServiceUri;

		public WebScraperService(StatelessServiceContext context)
			: base(context)
		{
			_actorProxyFactory = new ActorProxyFactory();
			_serviceProxyFactory = new ServiceProxyFactory();//retrySettings: new OperationRetrySettings(TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(3), 1));

			_speakerActorApplicationName = FabricRuntime.GetActivationContext().ApplicationName;
			_speakerActorServiceName = $"{typeof(ISpeakerActor).Name.Substring(1)}Service";
			_speakerActorServiceUri = new Uri($"{_speakerActorApplicationName}/{_speakerActorServiceName}");

		}

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
			var readSessions = new List<string>();

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

						var speakerActor = actorProxyFactory.CreateActorProxy<ISpeakerActor>(new ActorId(speaker.Name));
						var speakerInfoAsync = speakerActor.SetSpeakerInfoAsync(speaker.ToSpeakerInfo(), cancellationToken);
						tasks.Add(speakerInfoAsync);

						readSpeakers.Add(speaker.GetShortHash());
					}
				}
				await Task.WhenAll(tasks);

				var sessions = await scraper.ScanSessions();
				tasks = new List<Task>();

				scraper.AddSessionsToSpeakers(speakers, sessions);

				foreach (var speaker in speakers)
				{
					if (!readSessions.Contains(speaker.Sessions.GetShortHash()))
					{
						ServiceEventSource.Current.ServiceMessage(this.Context, $"Updating {speaker.Sessions.Count} sessions for speaker {speaker.Name}");

						var speakerActor = actorProxyFactory.CreateActorProxy<ISpeakerActor>(new ActorId(speaker.Name));
						var sessionsAsync = speakerActor.SetSessionsAsync(speaker.Sessions.Select(session => session.ToSessionInfo()).ToArray(), cancellationToken);
						tasks.Add(sessionsAsync);

						readSessions.Add(speaker.Sessions.GetShortHash());
					}
				}
				await Task.WhenAll(tasks);


				ServiceEventSource.Current.ServiceMessage(this.Context, "Scanning sessions-{0}", ++iterations);

				await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
			}
		}
	}
}
