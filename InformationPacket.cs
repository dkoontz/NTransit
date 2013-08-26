using System;

namespace Transit {
	public class InformationPacket {
		public enum PacketType {
			StartSequence,
			EndSequence,
			Content,
		}

		public object Content { get; private set; }
		public PacketType Type { get; private set; }
		public object Owner { get; set; }

		public InformationPacket(PacketType type, object content) {
			Type = type;
			Content = content;
		}

		// attributes
		// children
	}
}