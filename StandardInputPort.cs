using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class StandardInputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool Greedy { get; set; }
		public int ConnectionCapacity { get; set; }
		public bool Connected { get { return upstreamPorts.Count > 0; } }
		public bool AllUpstreamPortsClosed {
			get {
				var allClosed = upstreamPorts.Count > 0;
				foreach (var port in upstreamPorts) {
					if (port.Connected && !port.Closed) {
						allClosed = false;
					}
				}
				return allClosed;
			}
		}
		public bool HasPacketWaiting { get { return queue.Count > 0 || (HasInitialData && !initialIpSent); } }
		public bool HasCapacity { get { return queue.Count < ConnectionCapacity; } }
		public bool HasInitialData { get { return initialIp != null; } }
		public int QueuedPacketCount { get { return queue.Count; } }
		public bool Closed { get; private set; }

		public Action<IpOffer> Receive;
		public Action<IpOffer> SequenceStart;
		public Action<IpOffer> SequenceEnd;

		object lockObject = new object();
		Queue<InformationPacket> queue;
		IpOffer ipOffer;
		InformationPacket initialIp;
		bool initialIpSent;
		List<StandardOutputPort> upstreamPorts;

		public StandardInputPort(int connectionCapacity, Component process) {
			this.ConnectionCapacity = connectionCapacity;
			queue = new Queue<InformationPacket>(connectionCapacity);
			upstreamPorts = new List<StandardOutputPort>();
			Process = process;
			SequenceStart = data => data.Accept();
			SequenceEnd = data => data.Accept();
		}

		public void SetInitialData(InformationPacket ip) {
			initialIp = ip;
		}

		public bool TrySend(InformationPacket ip) {
			if (Closed)	{
				throw new InvalidOperationException(string.Format("Cannot send data to a closed port '{0}.{1}'", Process.Name, Name));
			}
			lock (lockObject) {
				if (queue.Count < ConnectionCapacity) {
					queue.Enqueue(ip);
					return true;
				}
				
				return false;
			}
		}

		public bool Tick() {
			if (Receive == null || Closed) {
				return false;
			}

			if (ipOffer != null) {
				DispatchOffer(ipOffer);
			}
			else if (!initialIpSent && initialIp != null) {
				ipOffer = new IpOffer(initialIp);
				DispatchOffer(ipOffer);
			}
			else if (queue.Count > 0) {
				ipOffer = new IpOffer(queue.Peek());
				DispatchOffer(ipOffer);
			}

			if (ipOffer != null && ipOffer.Accepted) {
				if (!initialIpSent && initialIp != null) {
					initialIpSent = true;
				}
				else {
					lock (lockObject) {
						queue.Dequeue();
					}
				}

				ipOffer = null;
				return true;
			}

			return false;
		}

		public void NotifyOfConnection(StandardOutputPort port) {
			upstreamPorts.Add(port);
		}

		public void Close() {
			Closed = true;
		}

		void DispatchOffer(IpOffer offer) {
			switch (offer.Type) {
				case InformationPacket.PacketType.Data:
					Receive(offer);
					break;
				case InformationPacket.PacketType.StartSequence:
					SequenceStart(offer);
					break;
				case InformationPacket.PacketType.EndSequence:
					SequenceEnd(offer);
					break;
				case InformationPacket.PacketType.Auto:
					Receive(offer);
					break;
			}
		}
	}
}