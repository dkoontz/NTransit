using System;
using System.Collections.Generic;

namespace NTransit {
	public class IpOffer {
		public bool Accepted { get { return accepted; } }

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
		public Dictionary<string, object> Attributes;

		public object Content { get; set; }

		public T ContentAs<T>() {
			return (T)Content;
		}

		public InformationPacket(object content) {
			Content = content;
			Attributes = new Dictionary<string, object>();
		}

		public InformationPacket(object content, Dictionary<string, object> attributes) : this(content) {
			foreach (var kvp in attributes) {
				Attributes[kvp.Key] = kvp.Value;
			}
		}
	}
}