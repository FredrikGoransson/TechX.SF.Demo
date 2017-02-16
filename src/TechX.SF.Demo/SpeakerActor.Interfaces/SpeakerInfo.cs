using System.Runtime.Serialization;

namespace SpeakerActor.Interfaces
{
	[DataContract]
	public class SpeakerInfo : SpeakerNameInfo
	{
		[DataMember]
		public string Bio { get; set; }
	}
}