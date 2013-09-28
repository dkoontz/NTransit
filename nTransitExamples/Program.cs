using System;
using NTransit;
using NTransitTest;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

namespace NTransitExamples {
	[InputPort("In")]
	[ArrayOutputPort("Out")]
	public class ArrayOutTest : Component {
		public ArrayOutTest(string name) : base(name) { }
		public override void Setup() { }

		int counter;

		protected override bool Update() {
			foreach (var port in ArrayOutPorts.Values) {
				foreach (var index in port.ConnectedIndicies) {
					port.TrySend(new InformationPacket(string.Format("{0}:{1}",index, counter)), index);
				}
			}
			++counter;
			return false;
		}
	}

	public class MockComponent : Component {
		public MockComponent(string name) : base(name) { }
		public override void Setup() { }
	}

	class MainClass {
		public static void Main() {
			var m = new MockComponent("");
			var inPort = new MockInputPort();
			m.SetInputPort("In", inPort);

			var m2 = new MockComponent("");
			var inPort2 = new MockInputPort();
			m2.SetInputPort("In", inPort2);

			var t = new ArrayOutTest("");
			t.ConnectArrayOutputPortTo("Out", 0, m, "In");
			t.ConnectArrayOutputPortTo("Out", 1, m2, "In");

			Console.WriteLine(inPort.ReceivedPackets.Count);
			Console.WriteLine(inPort2.ReceivedPackets.Count);
			t.Tick();
			t.Tick();
			Console.WriteLine(inPort.ReceivedPackets.Count);
			Console.WriteLine(inPort2.ReceivedPackets.Count);
		}
	}
}