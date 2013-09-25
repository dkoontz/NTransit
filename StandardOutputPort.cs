using System;
using System.Collections.Generic;

namespace NTransit {
	public class StandardOutputPort : IOutputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool HasCapacity { get { return connectedPort.HasCapacity; } }
		public bool Connected { get { return connectedPort != null; } }
		public bool Closed { get; private set; }
		public bool ConnectedPortClosed { get { return Connected && connectedPort.Closed; } }

		IInputPort  connectedPort;

		public StandardOutputPort(Component process) {
			Process = process;
		}

		public void ConnectTo(IInputPort port) {
			connectedPort = port;
		}

		public bool TrySend(InformationPacket ip) {
			return TrySend(ip, false);
		}

		public bool TrySend(InformationPacket ip, bool ignoreClosed) {
			if (!ignoreClosed && Closed) {
				throw new InvalidOperationException(string.Format("Cannot send data on closed port '{0}.{1}'", Process.Name, Name));
			}

			if (!Connected) {
				throw new InvalidOperationException(string.Format("Cannot send data on unconnected port '{0}.{1}'", Process.Name, Name));
			}

			return !connectedPort.Closed && connectedPort.TrySend(ip);
		}

		public void Close() {
			Closed = true;
		}
	}
}