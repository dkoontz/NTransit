using System;

namespace Transit
{
	public interface IInputPort : IPort
	{
		bool HasPacketsWaiting { get; }

		bool Receive (out InformationPacket packet);
	}
}