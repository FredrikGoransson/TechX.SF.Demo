using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace SpeakerActor.Interfaces
{
	public interface ISpeakerActor : IActor
	{
		Task<SpeakerInfo> GetSpeakerInfoAsync(CancellationToken cancellationToken);

		Task SetSpeakerInfoAsync(SpeakerInfo speakerInfo, CancellationToken cancellationToken);

		Task SetSessionsAsync(SessionInfo[] sessionInfo, CancellationToken cancellationToken);

		Task<SessionInfo[]> GetSessionsAsync(CancellationToken cancellationToken);
	}
}
