using System;

namespace NTransit {
	public class StandardInputPort : IInputPort {
		public string Name { get; set; }

		public bool Closed { get; protected set; }

		public IConnection Connection { get; set; }

		public bool HasConnection { get { return Connection != null; } }

		public Component Process { get; set; }

		public bool HasComponent { get { return Process != null; } }

		public bool HasPacketsWaiting { 
			get { 
				if (null == Connection) throw new InvalidOperationException(string.Format("Cannot check for waiting packets from port '{0}' on '{1}' it does not have a connection", Name, Process.Name));

				return !Connection.Empty; 
			} 
		}

		public InformationPacket Receive() {
			if (null == Process) throw new InvalidOperationException(string.Format("Cannot receive data from port '{0}', it is not assigned to a process", Name));
			if (Closed) throw new InvalidOperationException(string.Format("Cannot receive data from port '{0}' on '{1}', it is closed", Name, Process.Name));
			else if (null == Connection) throw new InvalidOperationException(string.Format("Cannot receive data from port '{0}' on '{1}', it does not have a connection", Name, Process.Name));
			else if (Connection.Empty) throw new InvalidOperationException(string.Format("Cannot receive data from port '{0} on '{1}', it does not have any waiting packets", Name, Process.Name));

			var packet = Connection.Receieve();
			Process.ClaimIp(packet);
			return packet;
		}

		public void Close() {
			Closed = true;
		}
	}
}