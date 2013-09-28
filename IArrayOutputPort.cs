using System;

namespace NTransit {
	public interface IArrayOutputPort {
		string Name { get; set; }
		Component Process { get; set; }
		bool Closed { get; }
		int[] ConnectedIndicies { get; } 

		bool HasCapacityOn(int portIndex);
		bool ConnectedOn(int portIndex);
		void Close();
		void ConnectPortIndexTo(int portIndex, IInputPort port);
		bool ConnectedPortOnIndexClosed(int portIndex);
		bool TrySend(InformationPacket ip, int portIndex);
		bool TrySend(InformationPacket ip, int portIndex, bool ignoreClosed);
	}
}