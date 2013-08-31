using System;
using System.Collections.Generic;
using NTransit;

namespace NTransitTest {
	public class MockConnection : IConnection {
		public List<InformationPacket> Packets { get; set; }

		public MockConnection() : this(1) {}

		public MockConnection(int capacity) {
			Packets = new List<InformationPacket>(capacity);
			Capacity = capacity;
		}

		public bool SendPacketIfCapacityAllows(InformationPacket ip) {
			if(Packets.Count < Capacity) {
				Packets.Add(ip);
				return true;
			}
			return false;
		}

		public InformationPacket Receieve() {
			var ip = Packets[0];
			Packets.RemoveAt(0);
			return ip;
		}

		public void SetReceiver(IInputPort receiver) {
			throw new NotImplementedException("Mock connection does not support a receiver");
		}

		public void SetInitialData(InformationPacket ip) {
			throw new NotImplementedException("Mock connection does not support initial IPs");
		}

		public int Capacity { get; set; }

		public bool Full {
			get { return Packets.Count == Capacity; }
		}

		public bool Empty {
			get { return 0 == Packets.Count; }
		}

		public int NumberOfPacketsHeld {
			get { return Packets.Count; }
		}

		public bool HasInitialInformationPacket {
			get { throw new NotImplementedException("Mock connection does not support initial IPs"); }
		}
	}
}

