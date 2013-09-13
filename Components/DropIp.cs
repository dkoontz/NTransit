using System;

namespace NTransit {
	public class DropIp : EndpointComponent {
		public DropIp(string name) : base(name) {
			Receive["In"] = data => data.Accept();
		}
	}
}