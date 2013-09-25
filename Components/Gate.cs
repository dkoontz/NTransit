using System;

namespace NTransit {
	[InputPort("Open")]
	[InputPort("Close")]
	public class Gate : PropagatorComponent {
		bool open;

		public Gate(string name) : base(name) { }

		public override void Setup() {
			base.Setup();

			InPorts["Open"].Receive = data => {
				data.Accept();
				open = true;
			};
			
			InPorts["Close"].Receive = data => {
				data.Accept();
				open = false;
			};
			
			InPorts["In"].SequenceStart = data => {
				if (open) Send("Out", data.Accept());
			};
			
			InPorts["In"].SequenceEnd = data => {
				if (open) Send("Out", data.Accept());
			};
			
			InPorts["In"].Receive = data => {
				if (open) Send("Out", data.Accept());
			};
		}
	}
}