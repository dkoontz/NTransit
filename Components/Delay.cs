using System;
using System.Collections;

namespace NTransit {
	public class Delay : Component {
		[InputPort("Seconds Between Packets")]
		public StandardInputPort SecondsBetweenPackets  { get; set; }

		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		public Delay(string name) : base (name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(SecondsBetweenPackets);
			var delay = Convert.ToSingle(SecondsBetweenPackets.Receive().Content);

			while (true) {
				yield return WaitForPacketOn(InPort);
				var ip = InPort.Receive();
				
				yield return WaitForTime((int)(delay * 1000));

				while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
			}
		}
	}
}