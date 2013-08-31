using System;
using System.Collections;

namespace NTransit {
	public class ConsoleWriter : Component {
		[InputPort("In")]
		public StandardInputPort Data { get; set; }

		public ConsoleWriter(string name) : base (name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(Data);
			var ip = Data.Receive();

			if (ip.Content is Exception) {
				var exception = ip.Content as Exception;
				Console.WriteLine(string.Format("Error ({0}): {1}\n{2}", exception.GetType(), exception.Message, exception.StackTrace));
			}
			else {
				Console.WriteLine(ip.Content);
			}
		}
	}
}