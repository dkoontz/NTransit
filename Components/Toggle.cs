using System;
using System.Collections;

namespace NTransit {
	public class Toggle : Component {

		[InputPort("Toggle")]
		public StandardInputPort TogglePort { get; set; }

		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[OutputPort("DefaultOut")]
		public StandardOutputPort DefaultOutPort { get; set; }

		[OutputPort("AlternateOut")]
		public StandardOutputPort AlternateOutPort { get; set; }

		bool useAlternatePort;

		public Toggle(string name) : base(name) {}

		public override IEnumerator Execute() {
			if (TogglePort.HasPacketsWaiting) {
				while (TogglePort.HasPacketsWaiting) {
					TogglePort.Receive();
					useAlternatePort = !useAlternatePort;
				}
			}

			if (InPort.HasPacketsWaiting) {
				while (InPort.HasPacketsWaiting) {
					var ip = InPort.Receive();
					if (useAlternatePort) {
						while (!AlternateOutPort.TrySend(ip)) yield return WaitForCapacityOn(AlternateOutPort);
					}
					else {
						while (!DefaultOutPort.TrySend(ip)) yield return WaitForCapacityOn(DefaultOutPort);
					}
				}
			}
		}
	}
}