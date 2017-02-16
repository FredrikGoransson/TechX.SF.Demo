using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using SpeakerActor.Interfaces;

namespace WebApiService.Controllers
{
	[ServiceRequestActionFilter]
	public class HomeController : ApiController
	{
		private static readonly object SpeakerActorServicePartitionsLock = new object();
		private static long[] _speakerActorServicePartitions;

		private readonly IActorProxyFactory _actorProxyFactory;
		private readonly IServiceProxyFactory _serviceProxyFactory;
		private readonly string _speakerActorApplicationName;
		private readonly string _speakerActorServiceName;
		private readonly Uri _speakerActorServiceUri;

		public HomeController()
		{
			_actorProxyFactory = new ActorProxyFactory();
			_serviceProxyFactory = new ServiceProxyFactory();//retrySettings: new OperationRetrySettings(TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(3), 1));

			_speakerActorApplicationName = FabricRuntime.GetActivationContext().ApplicationName;
			_speakerActorServiceName = $"{typeof(ISpeakerActor).Name.Substring(1)}Service";
			_speakerActorServiceUri = new Uri($"{_speakerActorApplicationName}/{_speakerActorServiceName}");

			GetSpeakerActorPartitionList();
		}
			
		private void GetSpeakerActorPartitionList()
		{
			lock (SpeakerActorServicePartitionsLock)
			{
				if (_speakerActorServicePartitions == null)
				{
					ServiceEventSource.Current.Message("WebApi service reading SpeakerActorService partitions");
					var fabricClient = new FabricClient();
					var partitions = new List<long>();
					var servicePartitionList = fabricClient.QueryManager.GetPartitionListAsync(_speakerActorServiceUri).GetAwaiter().GetResult();
					foreach (var servicePartition in servicePartitionList)
					{
						var partitionInformation = servicePartition.PartitionInformation as Int64RangePartitionInformation;
						if (partitionInformation != null)
						{
							partitions.Add(partitionInformation.LowKey);
						}
						else
						{
							ServiceEventSource.Current.Message($"Found partition {servicePartition.PartitionInformation.Id} but expected Int64, found {servicePartition.PartitionInformation.Kind.GetType().Name}");
						}
					}

					_speakerActorServicePartitions = partitions.ToArray();
					ServiceEventSource.Current.Message($"WebApi service found {partitions.Count} partitions for SpeakerActorService");
				}
			}
		}

		// GET /
		public async Task<IEnumerable<string>> Get()
		{
			var results = new List<string>();

			foreach (var servicePartition in _speakerActorServicePartitions)
			{
				var speakerActorService = _serviceProxyFactory.CreateServiceProxy<ISpeakerActorService>(
					_speakerActorServiceUri,
					new ServicePartitionKey(servicePartition));
				var foundResults = await speakerActorService.GetAllSpeakersAsync(CancellationToken.None);
				foreach (var foundResult in foundResults)
				{
					results.Add(foundResult.Name);
				}
			}

			return results;
		}

		// GET /home/Fredrik%20G%C3%B6ransson
		public async Task<SpeakerDetails> Get(string id)
		{
			var cancelationToken = CancellationToken.None;

			var speakerActor = _actorProxyFactory.CreateActorProxy<ISpeakerActor>(new ActorId(id));
			var speakerInfo = await speakerActor.GetSpeakerInfoAsync(cancelationToken);
			var sessionsInfo = await speakerActor.GetSessionsAsync(cancelationToken);
			var speaker = new SpeakerDetails()
			{
				Name = speakerInfo.Name,
				Bio = speakerInfo.Bio,
				Sessions = sessionsInfo,
			};

			return speaker;
		}
	}
}
