using System;

namespace NTransit {
	[InputPort("Activate")]
	public class Checkpoint : PropagatorComponent {
		bool open;
		string sequenceId;

		public Checkpoint(string name) : base(name) { }

		public override void Setup() {
			base.Setup();

			InPorts["Activate"].Receive = data => {
				data.Accept();
				Console.WriteLine ("checkpoint opening");
				open = true;
			};
			
			InPorts["In"].SequenceStart = data => {
				if (open) {
					var ip = data.Accept();
					if (sequenceId == null) {
						Console.WriteLine ("checkpoint recording start of sequence");
						sequenceId = ip.ContentAs<string>();
					}
					Send("Out", ip);
				}
			};
			
			InPorts["In"].SequenceEnd = data => {
				var ip = data.Accept();
				var endingId = ip.ContentAs<string>();
				if (sequenceId == endingId) {
					Console.WriteLine("checkpoint closing after sequence");
					open = false;
					sequenceId = null;
				}
				Send("Out", ip);
			};
			
			InPorts["In"].Receive = data => {
				if (open) {
					Send("Out", data.Accept());
				}
				if (sequenceId == null) {
					Console.WriteLine ("checkpoint closing after single packet");
					open = false;
				}
			};
		}

		public void Open() {
			open = true;
		}

		public void Close() {
			open = false;
			sequenceId = null;
		}
	}
}