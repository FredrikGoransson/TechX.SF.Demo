using System.Runtime.Serialization;

namespace SpeakerActor.Interfaces
{
	[DataContract]
	public class SpeakerDetails : SpeakerInfo
	{
		[DataMember]
		public SessionInfo[] Sessions { get; set; }
	}
}