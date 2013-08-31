using System;
using System.Collections;

namespace NTransit {
	public class Drop : Component {
		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		public Drop(string name) : base(name) {}

		public override IEnumerator Execute() {
			while (true) {
				yield return WaitForPacketOn(InPort);
				while (InPort.HasPacketsWaiting) {
					InPort.Receive();
				}
			}
		}
	}
}