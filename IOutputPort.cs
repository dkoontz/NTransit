using System;

namespace NTransit {
	public interface IOutputPort : IPort {
		bool SendSequenceStart();

		bool SendSequenceEnd();

		bool TrySend(object content);

		bool TrySend(InformationPacket ip);
	}
}