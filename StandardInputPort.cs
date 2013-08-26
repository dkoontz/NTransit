using System;

namespace Transit {
	public class StandardInputPort : IInputPort {
		public string Name { get; set; }
		public bool Closed { get; protected set; }

		public Connection Connection { get; set; }
		public bool HasConnection { get { return Connection != null; } }

		public Component Component { protected get; set; }
		public bool HasComponent { get { return Component != null; } }

		public bool HasPacketsWaiting { get { return !Connection.Empty; } }

		public bool Receive(out InformationPacket packet) {
			packet = null;
			if (Closed) throw new InvalidOperationException(string.Format("Cannot receive data from port {0}, it is closed", Name));
			else if (Connection.Empty) return false;

			packet = Connection.Receieve();
			Component.ClaimIp(packet);
			return true;
		}
	}
}