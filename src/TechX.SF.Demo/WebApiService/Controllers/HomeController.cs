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
		private readonly IActorProxyFactory _actorProxyFactory;

		public HomeController()
		{
			_actorProxyFactory = new ActorProxyFactory();
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
