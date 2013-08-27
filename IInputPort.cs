using System;

namespace nTransit {
	public interface IInputPort : IPort {
		bool HasPacketsWaiting { get; }

		bool Receive(out InformationPacket packet);
	}
}