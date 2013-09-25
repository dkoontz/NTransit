using System;

namespace NTransit {
	public interface IInputPort {
		string Name { get; set; }
		Component Process { get; set; }
//		bool Greedy { get; set; }
		int ConnectionCapacity { get; set; }
		bool Connected { get; }
		bool AllUpstreamPortsClosed { get; }
		bool HasPacketWaiting { get; }
		bool HasCapacity { get; }
		bool HasInitialData { get; }
		int QueuedPacketCount { get; }
		bool Closed { get; }

		Action<IpOffer> Receive { get; set; }
		Action<IpOffer> SequenceStart { get; set; }
		Action<IpOffer> SequenceEnd { get; set; }
		void SetInitialData(InformationPacket ip);
		bool TrySend(InformationPacket ip);
		bool Tick();
		void NotifyOfConnection(IOutputPort port);
		void Close();
	}
}