using NUnit.Framework;
using System;
using NTransit;
using System.Collections.Generic;

namespace NTransitTest {
	[InputPort("In1")]
	[InputPort("In2", Type = typeof(MockInputPort))]
	[OutputPort("Out1")]
	[OutputPort("Out2", Type = typeof(MockOutputPort))]
	public class ComponentWithPublicData : Component {
		public Dictionary<string, IInputPort> InputPortsForTesting { get { return InPorts; } }
		public Dictionary<string, IOutputPort> OutputPortsForTesting { get { return OutPorts; } }

		public int PendingPacketCount { get { return pendingPackets.Count; } }
		public void ClearPendingPackets() { pendingPackets.Clear(); }
		public void AddPendingPacket(string port, InformationPacket ip) {
			pendingPackets.Enqueue(new PendingPacket(port, ip));
		}

		public ComponentWithPublicData(string name) : base(name) { }

		public override void Setup() { }
	}

	[TestFixture]
	public class ComponentInstantiationTest {

		[Test]
		public void Should_create_the_ports_declared_as_attributes() {
			var c = new ComponentWithPublicData("");
			c.InputPortsForTesting.ContainsKey("In1").ShouldBeTrue();
			c.InputPortsForTesting.ContainsKey("In2").ShouldBeTrue();
			c.OutputPortsForTesting.ContainsKey("Out1").ShouldBeTrue();
			c.OutputPortsForTesting.ContainsKey("Out2").ShouldBeTrue();
		}

		[Test]
		public void Should_create_StandardInputPort_and_StandardOutputPort_ports_by_default() {
			var c = new ComponentWithPublicData("");
			c.InputPortsForTesting.ContainsKey("In1").ShouldBeTrue();
			c.InputPortsForTesting["In1"].GetType().ShouldEqual(typeof(StandardInputPort));
			c.OutputPortsForTesting["Out1"].GetType().ShouldEqual(typeof(StandardOutputPort));
		}

		[Test]
		public void Should_create_its_ports_base_on_their_declared_type() {
			var c = new ComponentWithPublicData("");
			c.InputPortsForTesting["In2"].GetType().ShouldEqual(typeof(MockInputPort));
			c.OutputPortsForTesting["Out2"].GetType().ShouldEqual(typeof(MockOutputPort));
		}
	}

	[TestFixture]
	public class ComponentPacketTest {

		[Test]
		public void Should_not_Tick_ports_if_outgoing_packets_are_queued() {
			var c = new ComponentWithPublicData("");
			(c.OutputPortsForTesting["Out2"] as MockOutputPort).ResponseToUseForTrySend = false;

			c.InputPortsForTesting["In2"].TrySend(new InformationPacket(null));
			c.AddPendingPacket("Out2", new InformationPacket(null));
			(c.InputPortsForTesting["In2"] as MockInputPort).TickCalled.ShouldBeFalse();
			c.Tick();
			(c.InputPortsForTesting["In2"] as MockInputPort).TickCalled.ShouldBeFalse();

			c.ClearPendingPackets();
			c.Tick();
			(c.InputPortsForTesting["In2"] as MockInputPort).TickCalled.ShouldBeTrue();
		}

		[Test]
		public void Should_hold_sent_packets_until_receiving_ports_accept_them_or_close() {
			var c = new ComponentWithPublicData("");
			(c.OutputPortsForTesting["Out2"] as MockOutputPort).ResponseToUseForTrySend = false;

			// receiving port accepts them
			c.AddPendingPacket("Out2", new InformationPacket(null));
			c.AddPendingPacket("Out2", new InformationPacket(null));

			c.PendingPacketCount.ShouldEqual(2);
			c.Tick();
			c.PendingPacketCount.ShouldEqual(2);

			(c.OutputPortsForTesting["Out2"] as MockOutputPort).ResponseToUseForTrySend = true;
			c.Tick();
			c.PendingPacketCount.ShouldEqual(0);

			// receiving port closes
			c.AddPendingPacket("Out2", new InformationPacket(null));
			c.AddPendingPacket("Out2", new InformationPacket(null));
			(c.OutputPortsForTesting["Out2"] as MockOutputPort).ResponseToUseForTrySend = false;
			(c.OutputPortsForTesting["Out2"] as MockOutputPort).ConnectedPortClosed = true;
			c.PendingPacketCount.ShouldEqual(2);
			c.Tick();
			c.PendingPacketCount.ShouldEqual(0);
		}
	}
}