using System;
using NUnit.Framework;
using Transit;

namespace TransitTest
{
	[TestFixture]
	public class ConnectionTests
	{
		[Test]
		public void Empty_is_reported_when_there_is_no_initial_data_and_no_data_is_in_the_connection ()
		{
			var connection = new Connection ();
			Assert.True (connection.Empty);
		}

		[Test]
		public void Empty_is_not_reported_when_initial_data_is_present_and_has_not_been_received ()
		{
			var connection = new Connection ();
			connection.SetInitialData (new InformationPacket ("Test data"));
			Assert.False (connection.Empty);
		}

		[Test]
		public void Empty_is_reported_when_initial_data_has_been_received_and_no_data_is_in_the_connection ()
		{
			var connection = new Connection ();
			connection.SetInitialData (new InformationPacket ("Test data"));
			connection.Receieve ();
			Assert.True (connection.Empty);
		}

		[Test]
		public void Full_is_reported_when_capacity_is_equal_to_number_of_packets ()
		{
			var connection = new Connection (2);
			connection.SetReceiver (new StandardInputPort ());
			var ip1 = new InformationPacket ("Test data1");
			var ip2 = new InformationPacket ("Test data2");
			connection.SendPacketIfCapacityAllows (ip1);
			Assert.False (connection.Full);
			connection.SendPacketIfCapacityAllows (ip2);
			Assert.True (connection.Full);
		}

		[Test]
		public void Initial_data_is_recieved_first ()
		{
			var connection = new Connection ();
			connection.SetReceiver (new StandardInputPort ());
			var initialIp = new InformationPacket ("Initial data");
			var normalIp = new InformationPacket ("Regular data");
			connection.SetInitialData (initialIp);
			connection.SendPacketIfCapacityAllows (normalIp);

			Assert.AreEqual (initialIp, connection.Receieve ());
			Assert.AreEqual (normalIp, connection.Receieve ());
			Assert.True (connection.Empty);
		}

		[Test]
		public void Initial_data_is_only_received_once ()
		{
			var connection = new Connection ();
			var initialIp = new InformationPacket ("Initial data");
			connection.SetInitialData (initialIp);

			Assert.AreEqual (initialIp, connection.Receieve ());
			Assert.Throws<InvalidOperationException> (() => connection.Receieve ());
		}

		[Test]
		public void InvalidOperationException_is_thrown_when_receiving_from_an_empty_connection ()
		{
			var connection = new Connection ();
			Assert.Throws<InvalidOperationException> (() => connection.Receieve ());
		}

		[Test]
		public void Sending_to_a_full_connection_returns_false ()
		{
			var connection = new Connection (0);
			connection.SetReceiver (new StandardInputPort ());
			Assert.False (connection.SendPacketIfCapacityAllows (new InformationPacket ("Test data")));
		}

		[Test]
		public void Sending_to_a_connection_changes_the_packets_owner_to_the_connection ()
		{
			var connection = new Connection ();
			connection.SetReceiver (new StandardInputPort ());
			var ip = new InformationPacket ("Test data");
			var originalOwnerObject = new object ();
			ip.Owner = originalOwnerObject;
			connection.SendPacketIfCapacityAllows (ip);
			Assert.AreEqual (ip.Owner, connection);
			Assert.AreNotEqual (ip.Owner, originalOwnerObject);
		}

		[Test]
		public void Connection_executes_callback_when_a_packet_is_received ()
		{
			var connection = new Connection ();
			connection.SetReceiver (new StandardInputPort ());
			var callbackCalled = false;
			connection.NotifyWhenPacketReceived += component => callbackCalled = true;
			connection.SendPacketIfCapacityAllows (new InformationPacket ("test"));
			Assert.True (callbackCalled);
		}
	}
}