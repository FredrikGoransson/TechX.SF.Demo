using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace SpeakerActor.Interfaces
{
	public interface ISpeakerActorService : IService
	{
		Task<IEnumerable<SpeakerNameInfo>> GetAllSpeakersAsync(CancellationToken cancellationToken);

		Task<IEnumerable<SpeakerDetails>> FindSpeakersByKeyword(string keyword, CancellationToken cancellationToken);
	}
}