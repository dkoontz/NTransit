using System;
using System.Collections.Generic;

namespace NTransit {
	public class InformationPacket {
		public enum PacketType {
			StartSequence,
			EndSequence,
			Content,
		}

		public static InformationPacket AutoPacket {
			get { return new InformationPacket(null); }
		}

		public Dictionary<string, object> Attributes;

		public object Content { get; private set; }

		public PacketType Type { get; private set; }

		public object Owner { get; set; }

		public InformationPacket(object content) : this(InformationPacket.PacketType.Content, content) {}

		public InformationPacket(PacketType type, object content) {
			Type = type;
			Content = content;
		}
	}
}