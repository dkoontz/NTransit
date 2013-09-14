using System;
using System.Collections.Generic;

namespace NTransit {
	public class IpOffer {
		public bool Accepted { get { return accepted; } }
		public InformationPacket.PacketType Type { get { return ip.Type; } }

		InformationPacket ip;
		bool accepted;

		public IpOffer(InformationPacket ip) {
			this.ip = ip;
		}

		public InformationPacket Accept() {
			accepted = true;
			return ip;
		}
	}

	public class InformationPacket {
		public enum PacketType {
			Data,
			StartSequence,
			EndSequence,
			Auto
		}

		public Dictionary<string, object> Attributes;

		public object Content { get; set; }
		public PacketType Type { get; private set; }

		public T ContentAs<T>() {
			return (T)Content;
		}

		public InformationPacket(object content) : this(content, PacketType.Data) {}

		public InformationPacket(object content, Dictionary<string, object> attributes) : this(content, PacketType.Data, attributes) {}

		public InformationPacket(object content, PacketType type) {
			Content = content;
			Attributes = new Dictionary<string, object>();
			Type = type;
		}

		public InformationPacket(object content, PacketType type, Dictionary<string, object> attributes) : this(content, type) {
			foreach (var kvp in attributes) {
				Attributes[kvp.Key] = kvp.Value;
			}
		}
	}
}