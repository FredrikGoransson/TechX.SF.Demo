using System.Runtime.Serialization;

namespace SpeakerActor.Interfaces
{
	[DataContract]
	public class SpeakerNameInfo
	{
		[DataMember]
		public string Name { get; set; }
	}
}