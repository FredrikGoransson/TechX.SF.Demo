using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using SpeakerActor.Interfaces;

namespace SpeakerActor
{
	[StatePersistence(StatePersistence.Persisted)]
	internal class SpeakerActor : Actor, ISpeakerActor
	{
		internal const string StateKeySpeakerInfo = "speaker_info";
		internal const string StateKeySessionInfo = "session_info";

		public SpeakerActor(SpeakerActorService actorService, ActorId actorId)
			: base(actorService, actorId)
		{
		}

		protected override Task OnActivateAsync()
		{
			ActorEventSource.Current.ActorMessage(this, "Actor activated.");
			return this.StateManager.TryAddStateAsync("count", 0);
		}

		public async Task<SpeakerInfo> GetSpeakerInfoAsync(CancellationToken cancellationToken)
		{
			try
			{
				var speakerInfoState = await this.StateManager.TryGetStateAsync<SpeakerInfo>(StateKeySpeakerInfo, cancellationToken);
				if (speakerInfoState.HasValue)
				{
					return speakerInfoState.Value;
				}

			}
			catch (Exception ex)
			{
				ActorEventSource.Current.ActorMessage(this, $"Failed to read SpeakerInfo state: {ex.Message}");
				return null;
			}
			ActorEventSource.Current.ActorMessage(this, $"Failed to read SpeakerInfo state: It doesn't exist in the StateManager");
			return null;
		}

		public async Task SetSpeakerInfoAsync(SpeakerInfo speakerInfo, CancellationToken cancellationToken)
		{
			try
			{
				await this.StateManager.SetStateAsync(StateKeySpeakerInfo, speakerInfo, cancellationToken);
				ActorEventSource.Current.ActorMessage(this, $"Saved SpeakerInfo state");
			}
			catch (Exception ex)
			{
				ActorEventSource.Current.ActorMessage(this, $"Failed to save SpeakerInfo state: {ex.Message}");
			}
		}

		public async Task SetSessionsAsync(SessionInfo[] sessionInfo, CancellationToken cancellationToken)
		{
			try
			{
				await this.StateManager.SetStateAsync(StateKeySessionInfo, sessionInfo, cancellationToken);
				ActorEventSource.Current.ActorMessage(this, $"Saved SessionInfo state");
			}
			catch (Exception ex)
			{
				ActorEventSource.Current.ActorMessage(this, $"Failed to save SessionInfo state: {ex.Message}");
			}
		}

		public async Task<SessionInfo[]> GetSessionsAsync(CancellationToken cancellationToken)
		{
			try
			{
				var sessionInfoState = await this.StateManager.TryGetStateAsync<SessionInfo[]>(StateKeySessionInfo, cancellationToken);
				if (sessionInfoState.HasValue)
				{
					return sessionInfoState.Value;
				}

			}
			catch (Exception ex)
			{
				ActorEventSource.Current.ActorMessage(this, $"Failed to read SessionInfo state: {ex.Message}");
				return null;
			}
			ActorEventSource.Current.ActorMessage(this, $"Failed to read SessionInfo state: It doesn't exist in the StateManager");
			return null;
		}

		public async Task<int> GetMultiState()
		{
			var state = await this.StateManager.GetStateAsync<int>("multi");
			return state;
		}

		public async Task SetMultiState()
		{
			await this.StateManager.SetStateAsync("multi", 1);
			await this.StateManager.SetStateAsync("multi", 2);
			await this.StateManager.SetStateAsync("multi", 3);
			await this.StateManager.SaveStateAsync();
			await this.StateManager.SetStateAsync("multi", 4);
			await this.StateManager.SetStateAsync("multi", 5);
			throw new MultiStateException("I has the fail");
			await this.StateManager.SetStateAsync("multi", 6);
			await this.StateManager.SetStateAsync("multi", 7);
			await this.StateManager.SetStateAsync("multi", 8);
		}
	}

	[Serializable]
	public class MultiStateException : Exception
	{
		public MultiStateException()
		{
		}

		public MultiStateException(string message) : base(message)
		{
		}

		public MultiStateException(string message, Exception inner) : base(message, inner)
		{
		}

		protected MultiStateException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
