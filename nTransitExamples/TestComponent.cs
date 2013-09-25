using System;
using NTransit;

namespace NTransitExamples {
	public class TestComponent : PropagatorComponent {
		public TestComponent(string name) : base(name) { }

		public override void Setup() {
			base.Setup();

			InPorts["In"].Receive = data => Send("Out", data.Accept());
		}
	}
}