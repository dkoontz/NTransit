using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace NTransit {
	public class Connection : IConnection {
		// TODO: Remove this and have a config object that can be configured at runtime
		static int DEFAULT_CONNECTION_CAPACITY = 1;

		public int Capacity { get; set; }

		public bool Full {
			get { return packets.Count == Capacity; }
		}

		public bool Empty {
			get { 
				if (initialIp != null) return sentInitialData;
				else return packets.Count == 0;
			}
		}

		public int NumberOfPacketsHeld { 
			get { return packets.Count; }
		}

		public bool HasInitialInformationPacket { get; private set; }

		Queue<InformationPacket> packets;
		public IInputPort receiver;
		InformationPacket initialIp;
		bool sentInitialData;
		readonly object lockObject = new object();

		public Connection() : this(DEFAULT_CONNECTION_CAPACITY) {
		}

		public Connection(int capacity) {
			Capacity = capacity;
			packets = new Queue<InformationPacket>(Capacity);
		}

		public bool SendPacketIfCapacityAllows(InformationPacket ip) {
			if (null == receiver) throw new InvalidOperationException(string.Format("Cannot send packet from '{0}' to connection that has no reciever", ip.Owner));

			bool hasCapacity = false;
			lock (lockObject) {
				if (packets.Count < Capacity) {
					hasCapacity = true;
					ip.Owner = this;
					packets.Enqueue(ip);
				}
			}
			return hasCapacity;
		}

		public InformationPacket Receieve() {
			InformationPacket ip;
			lock (lockObject) {
				if (!sentInitialData && initialIp != null) {
					sentInitialData = true;
					ip = initialIp;
				}
				else ip = packets.Dequeue();
			}
			return ip;
		}

		public void SetReceiver(IInputPort receiver) {
			this.receiver = receiver;
		}

		public void SetInitialData(InformationPacket ip) {
			initialIp = ip;
			HasInitialInformationPacket = true;
		}

		public void ResetInitialDataAvailability() {
			if (HasInitialInformationPacket) sentInitialData = false;
			else throw new InvalidOperationException("Cannot reset initial data on a connection that has no initial data set");
		}
	}
}