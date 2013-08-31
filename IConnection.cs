using System;

namespace NTransit {
	public interface IConnection {
		int Capacity { get; set; }

		bool Full { get; }

		bool Empty { get; }

		int NumberOfPacketsHeld { get; }

		bool HasInitialInformationPacket { get; }

		bool SendPacketIfCapacityAllows(InformationPacket ip);

		InformationPacket Receieve();

		void SetReceiver(IInputPort receiver);

		void SetInitialData(InformationPacket ip);

		void ResetInitialDataAvailability();
	}
}