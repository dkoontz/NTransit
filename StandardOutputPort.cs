using System;
using System.Collections.Generic;

namespace NTransit {
	public class StandardOutputPort {
		public string Name { get; set; }

		StandardInputPort connectedPort;

		public void ConnectTo(StandardInputPort port) {
			connectedPort = port;
		}

		public bool TrySend(InformationPacket ip) {
			return connectedPort.TryReceive(ip);
		}
	}
}