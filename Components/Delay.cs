using System;
using System.Collections;

namespace nTransit {
	public class Delay : Component {
		[InputPort("Seconds Between Packets")]
		StandardInputPort secondsBetweenPackets;

		[InputPort("In")]
		StandardInputPort input;

		[OutputPort("Out")]
		StandardOutputPort output;

		public Delay(string name) : base (name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(secondsBetweenPackets);
			var delay = Convert.ToSingle(secondsBetweenPackets.Receive().Content);

			while (true) {
				yield return WaitForPacketOn(input);
				var ip = input.Receive();
				
				yield return WaitForTime((int)(delay * 1000));

				while (!output.TrySend(ip)) {
					yield return WaitForCapacityOn(output);
				}
			}
		}
	}
}