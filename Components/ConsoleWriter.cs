using System;
using System.Collections;

namespace nTransit {
	public class ConsoleWriter : Component {
		[InputPort("In")]
		StandardInputPort data;

		public ConsoleWriter(string name) : base (name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(data);
			var packet = data.Receive();

			if (packet.Content is Exception) {
				var exception = packet.Content as Exception;
				Console.WriteLine(string.Format("Error ({0}): {1}\n{2}", exception.GetType(), exception.Message, exception.StackTrace));
			}
			else {
				Console.WriteLine(packet.Content);
			}
		}
	}
}