using System;

namespace NTransit {
	public class StandardInputPort : IInputPort {
		public string Name { get; set; }

		public bool Closed { get; protected set; }

		public IConnection Connection { get; set; }

		public bool HasConnection { get { return Connection != null; } }

		public Component Process { get; set; }

		public bool HasComponent { get { return Process != null; } }

		public bool HasPacketsWaiting { get { return !Connection.Empty; } }

		public InformationPacket Receive() {
			if (Closed) throw new InvalidOperationException(string.Format("Cannot receive data from port {0}, it is closed", Name));
			else if (null == Connection) throw new InvalidOperationException(string.Format("Cannot receive data from port {0}, it does not have a connection", Name));
			else if (Connection.Empty) throw new InvalidOperationException(string.Format("Cannot receive data from port {0}, it does not have any waiting packets", Name));

			var packet = Connection.Receieve();
			Process.ClaimIp(packet);
			return packet;
		}

		public void Close() {
			Closed = true;
		}
	}
}