using System;
using NTransit;
using System.Collections.Generic;

namespace NTransitTest {
	public class MockInputPort : IInputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
//		public bool Greedy { get; set; }
		public int ConnectionCapacity { get; set; }
		public bool Connected { get; set; }
		public bool AllUpstreamPortsClosed { get; set; }
		public bool HasPacketWaiting { get { return Queue.Count > 0; } }
		public bool HasCapacity { get; set; }
		public bool HasInitialData { get; set; }
		public int QueuedPacketCount { get; set; }
		public bool Closed { get; set; }

		Action<IpOffer> receive;
		public Action<IpOffer> Receive { 
			get { return receive; } 
			set { receive = value; } }
		public Action<IpOffer> SequenceStart { get; set; }
		public Action<IpOffer> SequenceEnd { get; set; }

		public Queue<InformationPacket> Queue { get; set; }

		public MockInputPort() {
			ConnectionCapacity = 1;
			Connected = true;
			AllUpstreamPortsClosed = false;
			HasCapacity = true;
			Closed = false;
			Queue = new Queue<InformationPacket>();
		}

		public void SetInitialData(InformationPacket ip) {
			Queue.Enqueue(ip);
		}

		public bool TrySend(InformationPacket ip) {
			Queue.Enqueue(ip);
			return true;
		}

		public bool Tick() {
			DispatchOffer(new IpOffer(Queue.Dequeue()));
			return false;
		}

		public void NotifyOfConnection(IOutputPort port) {	}

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

