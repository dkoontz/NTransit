using System;

namespace NTransit {
	[InputPort("Open")]
	[InputPort("Close")]
	public class Gate : PropagatorComponent {
		bool open;

		public Gate(string name) : base(name) {
			Receive["Open"] = data => {
				data.Accept();
				open = true;
			};

			Receive["Close"] = data => {
				data.Accept();
				open = false;
			};

			SequenceStart["In"] = data => {
				if (open) Send("Out", data.Accept());
			};

			SequenceEnd["In"] = data => {
				if (open) Send("Out", data.Accept());
			};

			Receive["In"] = data => {
				if (open) Send("Out", data.Accept());
			};
		}
	}
}