using System;
using NTransit;

namespace NTransitExamples {
	public class TestComponent : PropagatorComponent {
		public TestComponent(string name) : base(name) {
			Receive["In"] = data => Send("Out", data.Accept());
		}
	}
}