using System;
using System.Collections.Generic;

namespace NTransit {
	public class IpQueue : PropagatorComponent {
		Queue<InformationPacket> queue = new Queue<InformationPacket>();

		public IpQueue(string name) : base(name) {
			Receive["In"] = data => queue.Enqueue(data.Accept());
			Update = () => {
				if (queue.Count > 0 && TrySend("Out", queue.Peek())) {
					queue.Dequeue();
				}
			};
		}
	}
}