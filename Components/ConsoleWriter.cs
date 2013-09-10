using System;

namespace NTransit {
	public class ConsoleWriter : EndpointComponent {
		public ConsoleWriter(string name) : base(name) {
			SequenceStart["In"] = data => Console.WriteLine("Starting sequence {0}", data.Accept().Content);
			Receive["In"] = data => Console.WriteLine(data.Accept().Content);
			SequenceEnd["In"] = data => Console.WriteLine("Ending sequence {0}", data.Accept().Content);
		}
	}
}