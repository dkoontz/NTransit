using System;
using NTransit;
using NTransitTest;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

namespace NTransitExamples {
	[InputPort("In1")]
	[InputPort("In2", Type = typeof(MockInputPort))]
	[OutputPort("Out1")]
	[OutputPort("Out2", Type = typeof(MockOutputPort))]
	public class ComponentWithPublicData : Component {
		public Dictionary<string, IInputPort> InputPortsForTesting { get { return InPorts; } }
		public Dictionary<string, IOutputPort> OutputPortsForTesting { get { return OutPorts; } }
		public void ClearPendingPackets() { pendingPackets.Clear(); }
		public void AddPendingPacket(string port, InformationPacket ip) {
			pendingPackets.Enqueue(new PendingPacket(port, ip));
		}

		public ComponentWithPublicData(string name) : base(name) { }

		public override void Setup() { }
	}

	class MainClass {
		public static void Main() {
			var c = new ComponentWithPublicData("");
			c.InputPortsForTesting["In2"].TrySend(new InformationPacket(null));
			c.AddPendingPacket("Out2", new InformationPacket(null));
			Console.WriteLine((c.InputPortsForTesting["In2"] as MockInputPort).TickCalled);
			c.Tick();
			Console.WriteLine((c.InputPortsForTesting["In2"] as MockInputPort).TickCalled);

			c.ClearPendingPackets();
			c.Tick();
			Console.WriteLine((c.InputPortsForTesting["In2"] as MockInputPort).TickCalled);
		}
	}
}