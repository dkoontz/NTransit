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
			InformationPacket ip;

			while (!secondsBetweenPackets.Receive (out ip)) {
				yield return WaitForPacketOn(secondsBetweenPackets);
			}
			var delay = Convert.ToSingle(ip.Content);

			while (true) {
				while (!input.Receive (out ip)) {
					yield return WaitForPacketOn(input);
				}
				
				yield return WaitForTime((int)(delay * 1000));

				while (!output.Send (ip)) {
					yield return WaitForCapacityOn(output);
				}
			}
		}
	}
}