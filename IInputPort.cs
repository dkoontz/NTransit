using System;

namespace NTransit {
	public interface IInputPort : IPort {
		bool HasPacketsWaiting { get; }

		InformationPacket Receive();
	}
}