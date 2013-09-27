using System;
using NTransit;
using System.Collections.Generic;

namespace NTransitTest {
	public class MockInputPort : IInputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool Greedy { get; set; }
		public int ConnectionCapacity { get; set; }
		public bool Connected { get; set; }
		public bool AllUpstreamPortsClosed { get; set; }
		public bool HasPacketWaiting { get { return Queue.Count > 0; } }
		public bool HasCapacity { get; set; }
		public bool HasInitialData { get; set; }
		public int QueuedPacketCount { get; set; }
		public bool Closed { get; set; }

		public Action<IpOffer> Receive { get; set; }
		public Action<IpOffer> SequenceStart { get; set; }
		public Action<IpOffer> SequenceEnd { get; set; }

		// Exposed for testing /////////
		public Queue<InformationPacket> Queue { get; set; }
		public bool TickCalled { get; set; }
		////////////////////////////////

		public MockInputPort() : this(1, null) { }

		public MockInputPort(int connectionCapacity, Component process) {
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
			TickCalled = true;
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
					if (Receive != null) {
						Receive(offer);
					}
					break;
					case InformationPacket.PacketType.StartSequence:
					if (SequenceStart != null) {
						SequenceStart(offer);
					}
					break;
					case InformationPacket.PacketType.EndSequence:
					if (SequenceEnd != null) {
						SequenceEnd(offer);
					}
					break;
					case InformationPacket.PacketType.Auto:
					if (Receive != null) {
						Receive(offer);
					}
					break;
			}
		}
	}
}

