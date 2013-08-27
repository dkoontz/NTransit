using System;

namespace nTransit {
	public interface IOutputPort : IPort {
		bool SendSequenceStart();

		bool SendSequenceEnd();

		bool TrySend(object content);

		bool TrySend(InformationPacket ip);
	}
}