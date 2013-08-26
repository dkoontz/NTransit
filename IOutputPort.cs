using System;

namespace Transit
{
	public interface IOutputPort : IPort
	{
		bool SendSequenceStart ();

		bool SendSequenceEnd ();

		bool Send (object content);

		bool Send (InformationPacket ip);
	}
}