using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class StandardInputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool Greedy { get; set; }
		public int ConnectionCapacity { get; set; }
		public bool HasCapacity { get { return queue.Count < ConnectionCapacity; } }
		public int QueuedPacketCount { get { return queue.Count; } }

		public Action<IpOffer> Receive;
		public Action<int> SequenceStart;
		public Action<int> SequenceEnd;

		object lockObject = new object();
		Queue<InformationPacket> queue;
		IpOffer ipOffer;
		InformationPacket initialIp;
		bool initialIpSent;

		public StandardInputPort(int connectionCapacity, Component process) {
			this.ConnectionCapacity = connectionCapacity;
			queue = new Queue<InformationPacket>(connectionCapacity);
			Process = process;
		}

		public void SetInitialData(InformationPacket ip) {
			initialIp = ip;
		}

		public bool TrySend(InformationPacket ip) {
			lock (lockObject) {
				if (queue.Count < ConnectionCapacity) {
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
			else if (!initialIpSent && initialIp != null) {
				ipOffer = new IpOffer(initialIp);
				Receive(ipOffer);
			}
			else if (queue.Count > 0) {
				ipOffer = new IpOffer(queue.Peek());
				Receive(ipOffer);
			}

			if (ipOffer != null && ipOffer.Accepted) {
				if (!initialIpSent && initialIp != null) initialIpSent = true;
				else queue.Dequeue();

				ipOffer = null;
			}
		}
	}
}