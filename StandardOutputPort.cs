using System;

namespace nTransit {
	public class StandardOutputPort : IOutputPort {
		public string Name { get; set; }

		public bool Closed { get; protected set; }

		public Connection Connection { get; set; }

		public bool HasConnection { get { return Connection != null; } }

		public Component Component { get; set; }

		public bool HasComponent { get { return Component != null; } }

		public bool SendSequenceStart() {
			return TrySend(new InformationPacket(InformationPacket.PacketType.StartSequence, null), false);
		}

		public bool SendSequenceEnd() {
			return TrySend(new InformationPacket(InformationPacket.PacketType.EndSequence, null), false);
		}

		public bool TrySend(object content) {
			return TrySend(new InformationPacket(content), false);
		}

		public bool TrySend(InformationPacket ip) {
			return TrySend(ip, true);
		}

		public bool TrySend(InformationPacket ip, bool releaseIpFromComponent) {
			if (Closed)	throw new InvalidOperationException(string.Format("Cannot send data on port {0}, it is closed", Name));
			else if (null == Connection) throw new InvalidOperationException(string.Format("Cannot send data on port {0}, it does not have a connection", Name));

			if (Connection.SendPacketIfCapacityAllows(ip)) {
				if (releaseIpFromComponent) Component.ReleaseIp(ip);
				return true;
			}
			return false;
		}

		public void Close() {
			Closed = true;
		}
	}
}