using System;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out1")]
	[OutputPort("Out2")]
	public class Clone : Component {
		public Clone(string name) : base(name) { }

		public override void Setup() {
			InPorts["In"].Receive = data => {
				var ip = data.Accept();
				SendNew("Out1", ip.Content);
				SendNew("Out2", ip.Content);
			};
		}
	}
}