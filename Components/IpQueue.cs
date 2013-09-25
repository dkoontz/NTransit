using System;
using System.Collections.Generic;

namespace NTransit {
	public class IpQueue : PropagatorComponent {
		Queue<InformationPacket> queue = new Queue<InformationPacket>();

		public IpQueue(string name) : base(name) {
			DoNotTerminateWhenInputPortsAreClosed = true;
		}

		public override void Setup() {
			base.Setup();

			InPorts["In"].SequenceStart = Enqueue;
			InPorts["In"].SequenceEnd = Enqueue;
			InPorts["In"].Receive = Enqueue;
		}

		protected override bool Update() {
			if (queue.Count > 0 && TrySend("Out", queue.Peek())) {
				queue.Dequeue();
				return true;
			}
			
			if (queue.Count == 0) {
				if (AllUpstreamPortsAreClosed()) {
					Status = ProcessStatus.Terminated;
				}
			}
			
			return false;
		}

		void Enqueue(IpOffer offer) {
			var ip = offer.Accept();
			queue.Enqueue(ip);
		}
	}
}