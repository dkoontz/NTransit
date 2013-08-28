using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class WaitForPacketOn {
		public IInputPort[] Ports { get; private set; }

		public WaitForPacketOn(IInputPort[] ports) {
			Ports = ports;
		}
	}

	public class WaitForCapacityOn {
		public IOutputPort[] Ports { get; private set; }

		public WaitForCapacityOn(IOutputPort[] ports) {
			Ports = ports;
		}
	}

	public class WaitForTime {
		public long Milliseconds { get; private set; }

		public long ElapsedTime { get; set; }

		public WaitForTime(int milliseconds) {
			Milliseconds = milliseconds;
		}
	}

	public abstract class Component {
		public string Name { get; protected set; }

		[OutputPort("Errors")]
		protected StandardOutputPort Errors;

		// This is set automatically during port creation
		protected List<IInputPort> InputPorts;

		protected Component(string name) {
			Name = name;
			ownedIps = new LinkedList<InformationPacket>();
		}

		LinkedList<InformationPacket> ownedIps;

		public abstract IEnumerator Execute();

		public void ClaimIp(InformationPacket ip) {
			ip.Owner = this;
			ownedIps.AddLast(ip);
		}

		public void ReleaseIp(InformationPacket ip) {
			ip.Owner = null;
			ownedIps.Remove(ip);
		}

		public bool HasPacketOnAnyNonIipInputPort() {
			foreach (var port in InputPorts) {
				if (!port.Connection.IsInitialInformationPacket && !port.Connection.Empty) {
					return true;
				}
			}

			return false;
		}

		// This method is called when the Network is told to shutdown
		// Override this method to add your own cleanup logic
		public virtual void Close() {}

		protected WaitForPacketOn WaitForPacketOn(params IInputPort[] ports) {
			return new WaitForPacketOn(ports);
		}

		protected WaitForCapacityOn WaitForCapacityOn(params IOutputPort[] ports) {
			return new WaitForCapacityOn(ports);
		}

		protected WaitForTime WaitForTime(int timeToWait) {
			return new WaitForTime(timeToWait);
		}
	}
}