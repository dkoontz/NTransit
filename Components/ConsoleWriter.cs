using System;

namespace NTransit {
	public class ConsoleWriter : EndpointComponent {
		public ConsoleWriter(string name) : base(name) { }

		public override void Setup() {
			InPorts["In"].SequenceStart = data => Console.WriteLine("Starting sequence {0}", data.Accept().Content);
			InPorts["In"].Receive = data => Console.WriteLine(data.Accept().Content);
			InPorts["In"].SequenceEnd = data => Console.WriteLine("Ending sequence {0}", data.Accept().Content);
		}
	}
}