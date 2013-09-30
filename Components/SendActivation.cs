using System;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out")]
	public class SendActivation : Component {
		public SendActivation(string name) : base(name) { }

		public override void Setup() {
			InPorts["In"].Receive = data => SendAuto("Out");
		}
	}
}