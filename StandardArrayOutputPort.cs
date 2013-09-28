using System;
using System.Collections.Generic;

namespace NTransit {
	public class StandardArrayOutputPort : IArrayOutputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool Closed { get; private set; }
		public int[] ConnectedIndicies {
			get {
				var keys = connectedPorts.Keys;
				var indices = new int[keys.Count];
				keys.CopyTo(indices, 0);

				return indices;
			}
		}

		Dictionary<int, IInputPort> connectedPorts;

		public StandardArrayOutputPort(Component process) {
			Process = process;
			connectedPorts = new Dictionary<int, IInputPort>();
		}

		public void ConnectPortIndexTo(int portIndex, IInputPort port) {
			connectedPorts[portIndex] = port;
		}

		public bool TrySend(InformationPacket ip, int portIndex) {
			return TrySend(ip, portIndex, false);
		}

		public bool TrySend(InformationPacket ip, int portIndex, bool ignoreClosed) {
			if (!ignoreClosed && Closed) {
				throw new InvalidOperationException(string.Format("Cannot send data on closed port '{0}.{1}'", Process.Name, Name));
			}

			if (ConnectedOn(portIndex)) {
				return !connectedPorts[portIndex].Closed && connectedPorts[portIndex].TrySend(ip);
			}
			else {
				throw new InvalidOperationException(string.Format("Cannot send data on unconnected port '{0}.{1}[{2}]'", Process.Name, Name, portIndex));
			}
		}

		public bool ConnectedOn(int portIndex) {
			return connectedPorts.ContainsKey(portIndex) && connectedPorts[portIndex] != null;
		}

		public bool ConnectedPortOnIndexClosed(int portIndex) {
			return connectedPorts[portIndex].Closed;
		}

		public bool HasCapacityOn(int portIndex) {
			return connectedPorts[portIndex].HasCapacity;
		}

		public void Close() {
			Closed = true;
		}
	}
}