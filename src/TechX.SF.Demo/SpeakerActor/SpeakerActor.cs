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
		public SpeakerActor(ActorService actorService, ActorId actorId)
			: base(actorService, actorId)
		{
		}

		protected override Task OnActivateAsync()
		{
			ActorEventSource.Current.ActorMessage(this, "Actor activated.");
			return this.StateManager.TryAddStateAsync("count", 0);
		}
	}
}
