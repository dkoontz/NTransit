using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NTransit {
	public class StandardInputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool Greedy { get; set; }

		public Action<IpOffer> Receive;

		object lockObject = new object();
		int connectionCapacity;
		Queue<InformationPacket> queue;
		IpOffer ipOffer;

		public StandardInputPort(int connectionCapacity) : this(connectionCapacity, true) {}

		public StandardInputPort(int connectionCapacity, bool greedy) {
			this.connectionCapacity = connectionCapacity;
			queue = new Queue<InformationPacket>(connectionCapacity);
			Greedy = greedy;
		}

		public bool TryReceive(InformationPacket ip) {
			lock (lockObject) {
				if (queue.Count < connectionCapacity) {
					queue.Enqueue(ip);
					return true;
				}
				
				return false;
			}
		}

		public void Tick() {
			if (null == Receive) return;

			if (ipOffer != null) {
				Receive(ipOffer);
			}
			else if (queue.Count > 0) {
				ipOffer = new IpOffer(queue.Peek());
				Receive(ipOffer);
			}

			if (ipOffer != null && ipOffer.Accepted) {
				ipOffer = null;
				queue.Dequeue();
			}
		}
	}
}