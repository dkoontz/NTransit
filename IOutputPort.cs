using System;

namespace NTransit {
	public interface IOutputPort {
		string Name { get; set; }
		Component Process { get; set; }
		bool HasCapacity { get; }
		bool Connected { get; }
		bool Closed { get; }
		bool ConnectedPortClosed { get; }
		void ConnectTo(IInputPort port);
		bool TrySend(InformationPacket ip);
		bool TrySend(InformationPacket ip, bool ignoreClosed);
		void Close();
	}
}