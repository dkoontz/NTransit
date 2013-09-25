using System;

namespace NTransit {
	public class DropIp : EndpointComponent {
		public DropIp(string name) : base(name) { }

		public override void Setup() {
			InPorts["In"].Receive = data => data.Accept();
		}
	}
}