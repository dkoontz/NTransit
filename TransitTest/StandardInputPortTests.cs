using System;
using NUnit.Framework;
using nTransit;

namespace nTransit {
	[TestFixture]
	public class StandardInputPortTests {
		[Test]
		public void InvalidOperationException_is_thrown_when_recieving_from_a_closed_port() {
			var port = new StandardInputPort();
			port.Connection = new Connection();
			port.Close();
			InformationPacket outValue;
			Assert.Throws<InvalidOperationException>(() => {
				port.Receive(out outValue);
			});
		}

		[Test]
		public void InvalidOperationException_is_thrown_when_receiving_from_a_port_with_no_connection() {
			var port = new StandardInputPort();
			InformationPacket outValue = new InformationPacket("this should be overwritten by Receive");
			Assert.Throws<InvalidOperationException>(() => {
				port.Receive(out outValue);
			});
		}

		[Test]
		public void When_a_receive_is_not_successful_the_out_value_is_null() {
			var port = new StandardInputPort();
			port.Connection = new Connection();
			InformationPacket outValue = new InformationPacket("this should be overwritten by Receive");
			port.Receive(out outValue);

			Assert.IsNull(outValue);
		}

		[Test]
		public void Receiving_a_packet_causes_the_associated_component_to_claim_the_packet() {
			var port = new StandardInputPort();
			port.Connection = new Connection();
			var packet = new InformationPacket("Packet to be claimed");
			packet.Owner = port.Connection;
			port.Connection.SetInitialData(packet);
			var component = new MockComponent();
			port.Component = component;

			InformationPacket outValue;
			port.Receive(out outValue);
			Assert.AreSame(component, packet.Owner);
		}
	}
}