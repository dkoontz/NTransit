using System;

namespace Transit {
	public class StandardOutputPort : IOutputPort {
		public string Name { get; set; }
		public bool Closed { get; protected set; }

		public Connection Connection { get; set; }
		public bool HasConnection { get { return Connection != null; } }

		public Component Component { protected get; set; }
		public bool HasComponent { get { return Component != null; } }

		public bool SendSequenceStart() {
			return Send(new InformationPacket(InformationPacket.PacketType.StartSequence, null));
		}

		public bool SendSequenceEnd() {
			return Send(new InformationPacket(InformationPacket.PacketType.EndSequence, null));
		}

		public bool Send(object content) {
			return Send(new InformationPacket(InformationPacket.PacketType.Content, content));
		}

		public bool Send(InformationPacket ip) {
			return Connection.SendPacketIfCapacityAllows(ip);
		}
	}
}