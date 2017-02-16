using System;
using System.Runtime.Serialization;

namespace SpeakerActor.Interfaces
{
	[DataContract]
	public class SessionInfo
	{
		[DataMember]
		public string Title { get; set; }
		[DataMember]
		public string Content { get; set; }
		[DataMember]
		public DateTime Date { get; set; }
		[DataMember]
		public string[] Target { get; set; }
		[DataMember]
		public int Level { get; set; }
	}
}