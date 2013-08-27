using System;
using NUnit.Framework;
using nTransit;

namespace nTransit {
	[TestFixture]
	public class StandardOutputPortTests {
		[Test]
		public void InvalidOperationException_is_thrown_when_sending_a_packet_to_a_closed_port() {
			var port = new StandardOutputPort();
			port.Connection = new Connection();
			port.Close();

			Assert.Throws<InvalidOperationException>(() => {
				port.TrySend("test");
			});

			Assert.Throws<InvalidOperationException>(() => {
				port.SendSequenceStart();
			});

			Assert.Throws<InvalidOperationException>(() => {
				port.SendSequenceEnd();
			});
		}

		[Test]
		public void InvalidOperationException_is_thrown_when_sending_a_packet_to_a_port_with_no_connection() {
			var port = new StandardOutputPort();

			Assert.Throws<InvalidOperationException>(() => {
				port.TrySend("test");
			});

			Assert.Throws<InvalidOperationException>(() => {
				port.SendSequenceStart();
			});

			Assert.Throws<InvalidOperationException>(() => {
				port.SendSequenceEnd();
			});
		}

		[Test]
		public void Send_creates_a_new_InformationPacket_and_sends_it_to_the_connection() {
			var port = new StandardOutputPort();
			port.Connection = new Connection();
			port.Connection.SetReceiver(new StandardInputPort());

			Assert.True(port.Connection.Empty);
			port.TrySend("Test data");
			Assert.False(port.Connection.Empty);

			var receivedPacket = port.Connection.Receieve();
			Assert.AreEqual("Test data", receivedPacket.Content);
		}

		[Test]
		public void SendSequenceStart_creates_a_new_InformationPacket_with_type_StartSequence_and_sends_it_to_the_connection() {
			var port = new StandardOutputPort();
			port.Connection = new Connection();
			port.Connection.SetReceiver(new StandardInputPort());

			Assert.True(port.Connection.Empty);
			port.SendSequenceStart();
			Assert.False(port.Connection.Empty);

			var receivedPacket = port.Connection.Receieve();
			Assert.AreEqual(InformationPacket.PacketType.StartSequence, receivedPacket.Type);
			Assert.IsNull(receivedPacket.Content);
		}

		[Test]
		public void SendSequenceEnd_creates_a_new_InformationPacket_with_type_EndSequence_and_sends_it_to_the_connection() {
			var port = new StandardOutputPort();
			port.Connection = new Connection();
			port.Connection.SetReceiver(new StandardInputPort());

			Assert.True(port.Connection.Empty);
			port.SendSequenceEnd();
			Assert.False(port.Connection.Empty);

			var receivedPacket = port.Connection.Receieve();
			Assert.AreEqual(InformationPacket.PacketType.EndSequence, receivedPacket.Type);
			Assert.IsNull(receivedPacket.Content);
		}

		[Test]
		public void Successfully_sending_an_ip_causes_the_component_to_release_the_InformationPacket() {
			var port = new StandardOutputPort();
			port.Connection = new Connection();
			port.Connection.SetReceiver(new StandardInputPort());

			var component = new MockComponent();
			port.Component = component;

			var ip = new InformationPacket("Test data");
			component.ClaimIp(ip);

			port.TrySend(ip);

			Assert.IsNull(ip.Owner);
		}
	}
}