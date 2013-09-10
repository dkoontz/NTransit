using System;

namespace NTransit {
	public class ConsoleWriter : EndpointComponent {
		public ConsoleWriter(string name) : base(name) {}

		public override void Init() {
			Receive["In"] = data => Console.WriteLine(data.Accept().Content);
		}
	}
}