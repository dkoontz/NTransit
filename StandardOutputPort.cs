using System;
using System.Collections.Generic;

namespace NTransit {
	public class StandardOutputPort {
		public string Name { get; set; }
		public bool HasCapacity { get { return connectedPort.HasCapacity; } }
		public bool Connected { get { return connectedPort != null; } }
		public bool Closed { get; private set; }
		public bool ConnectedPortClosed { get { return Connected && connectedPort.Closed; } }

		StandardInputPort connectedPort;

		public void ConnectTo(StandardInputPort port) {
			connectedPort = port;
		}

		public bool TrySend(InformationPacket ip) {
			return TrySend(ip, false);
		}

		public bool TrySend(InformationPacket ip, bool ignoreClosed) {
			if (!ignoreClosed && Closed) {
				throw new InvalidOperationException(string.Format("Cannot send data on closed port '{0}'", Name));
			}

			return !connectedPort.Closed && connectedPort.TrySend(ip);
		}

		public void Close() {
//			Console.WriteLine("Closing output port '{0}'", Name);
			Closed = true;
		}
	}
}