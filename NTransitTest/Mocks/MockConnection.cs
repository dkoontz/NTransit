using System;
using System.Collections.Generic;
using NTransit;

namespace NTransitTest {

	[InputPort("In")]
	public class MockConnection : Component {
		public List<InformationPacket> ReceivedSequenceStartPackets { get; set; }
		public List<InformationPacket> ReceivedSequenceEndPackets { get; set; }
		public List<InformationPacket> ReceivedPackets { get; set; }

		public MockConnection(string name) : base(name) {
			ReceivedSequenceStartPackets = new List<InformationPacket>();
			ReceivedSequenceEndPackets = new List<InformationPacket>();
			ReceivedPackets = new List<InformationPacket>();
		}

		public override void Setup() {
			InPorts["In"].SequenceStart = data => ReceivedSequenceStartPackets.Add(data.Accept());
			InPorts["In"].SequenceEnd = data => ReceivedSequenceEndPackets.Add(data.Accept());
			InPorts["In"].Receive = data => ReceivedPackets.Add(data.Accept());
		}
	}
}