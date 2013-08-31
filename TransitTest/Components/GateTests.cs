using System;
using NUnit.Framework;
using NTransit;

namespace NTransitTest {
	[TestFixture()]
	public class GateTests {

		[Test()]
		public void Gate_should_allow_through_one_individual_packet_per_activation() {
			var gate = new Gate("Gate");

			var inPort = new StandardInputPort();
			inPort.Connection = new MockConnection(5);
			inPort.Process = gate;
			gate.SetInputPort("In", inPort);

			var triggerPort = new StandardInputPort();
			triggerPort.Connection = new MockConnection();
			triggerPort.Process = gate;
			gate.SetInputPort("Trigger", triggerPort);

			var outPort = new StandardOutputPort();
			outPort.Connection = new MockConnection(100);
			outPort.Process = gate;
			gate.SetOutputPort("Out", outPort);


//			inPort.Connection.SendPacketIfCapacityAllows()
		}

		public void Gate_should_allow_through_one_sequence_per_activation() {
			Assert.Fail();
		}
	}
}