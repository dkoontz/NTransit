using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Transit {
	public class Connection {
		// TODO: Remove this and have a config object that can be configured at runtime
		static int DEFAULT_CONNECTION_CAPACITY = 1;

		public int Capacity { get; set; }

		public bool Full {
			get { return packets.Count == Capacity; }
		}

		public bool Empty {
			get { return packets.Count == 0; }
		}

		Queue<InformationPacket> packets;
		List<IOutputPort> senders;
		IInputPort receiver;

		readonly object lockObject = new object();

		public Connection() : this(DEFAULT_CONNECTION_CAPACITY) {}

		public Connection(int capacity) {
			Capacity = capacity;
			packets = new Queue<InformationPacket>(Capacity);
			senders = new List<IOutputPort>(1);
		}

		public bool SendPacketIfCapacityAllows(InformationPacket ip) {
			bool hasCapacity = false;
			lock(lockObject) {
				if(packets.Count < Capacity) {
					hasCapacity = true;
					ip.Owner = this;
					packets.Enqueue(ip);
				}
			}
			return hasCapacity;
		}

		public InformationPacket Receieve() {
			InformationPacket ip;
			lock(lockObject) {
				ip = packets.Dequeue();
			}
			return ip;
		}

		public void SetReceiver(IInputPort receiver) {
			this.receiver = receiver;
		}

		public void AddSender(IOutputPort sender) {
			senders.Add(sender);
		}
	}
}