using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using SpeakerActor.Interfaces;

namespace SpeakerActor
{
	public class SpeakerActorService : ActorService, ISpeakerActorService
	{
		public SpeakerActorService(
			StatefulServiceContext context,
			ActorTypeInformation actorTypeInfo,
			Func<ActorService, ActorId, ActorBase> actorFactory = null,
			Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
			IActorStateProvider stateProvider = null,
			ActorServiceSettings settings = null) :
			base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
		{
		}

		public async Task<IEnumerable<SpeakerNameInfo>> GetAllSpeakersAsync(CancellationToken cancellationToken)
		{
			ContinuationToken continuationToken = null;
			var info = new List<SpeakerNameInfo>();

			do
			{
				var page = await this.StateProvider.GetActorsAsync(100, continuationToken, cancellationToken);

				foreach (var actor in page.Items)
				{
					var speakerInfoState = await this.StateProvider.LoadStateAsync<SpeakerInfo>(actor, SpeakerActor.StateKeySpeakerInfo, cancellationToken);
					info.Add(new SpeakerNameInfo() { Name = speakerInfoState.Name });
				}

				continuationToken = page.ContinuationToken;
			}
			while (continuationToken != null);

			return info;
		}

		public async Task<IEnumerable<SpeakerDetails>> FindSpeakersByKeyword(string keyword, CancellationToken cancellationToken)
		{
			ContinuationToken continuationToken = null;
			var info = new List<SpeakerDetails>();

			var keywordLowerCase = keyword.Trim().ToLowerInvariant();

			do
			{
				var page = await this.StateProvider.GetActorsAsync(100, continuationToken, cancellationToken);

				foreach (var actor in page.Items)
				{
					var matches = false;
					var speakerInfoState = await this.StateProvider.LoadStateAsync<SpeakerInfo>(actor, SpeakerActor.StateKeySpeakerInfo, cancellationToken);

					if (speakerInfoState.Bio.ToLowerInvariant().Contains(keywordLowerCase))
					{
						matches = true;
					}

					var sessionInfoState = await this.StateProvider.LoadStateAsync<SessionInfo[]>(actor, SpeakerActor.StateKeySessionInfo, cancellationToken);

					if (sessionInfoState.Any(session => session.Content.ToLowerInvariant().Contains(keywordLowerCase) || session.Title.ToLowerInvariant().Contains(keywordLowerCase)))
					{
						matches = true;
					}

					if (matches)
					{
						info.Add(new SpeakerDetails()
						{
							Name = speakerInfoState.Name,
							Bio = speakerInfoState.Bio,
							Sessions = sessionInfoState,
						});
					}
				}

				continuationToken = page.ContinuationToken;
			}
			while (continuationToken != null);


			return info.Take(10);
		}
	}
}