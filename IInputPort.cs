using System;

namespace nTransit {
	public interface IInputPort : IPort {
		bool HasPacketsWaiting { get; }

		InformationPacket Receive();
	}
}