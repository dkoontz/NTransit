using System;
using NUnit.Framework;
using NTransit;

namespace NTransitTest {
	public static class NTransitTestHelpers {
		
	}


	[TestFixture]
	public class IpQueueTests {

		[Test]
		public void IpQueue_should_be_able_to_receive_and_forward_unlimited_packets() {
			// 'Unlimited' is bound by memory constraints of course

			var queue = new IpQueue("Queue");
			var inPort = new StandardInputPort();
			inPort.Process = queue;
			var outPort = new StandardOutputPort();
			outPort.Process = queue;

			inPort.Connection = new MockConnection();
			var outputConnection = new MockConnection(1000);
			outPort.Connection = outputConnection;
			queue.SetInputPort("In", inPort);
			queue.SetOutputPort("Out", outPort);

			var iterator = queue.Execute();

			for(var i = 0; i < 100; ++i) {
				inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(i));
				do {
					iterator.MoveNext();
				}
				while (!(iterator.Current is WaitForPacketOrCapacityOnAny));
			}
			Assert.AreEqual(100, outputConnection.Packets.Count);
			Assert.AreEqual(0, outputConnection.Packets[0].Content);
			Assert.AreEqual(50, outputConnection.Packets[50].Content);
			Assert.AreEqual(99, outputConnection.Packets[99].Content);
		}

		[Test]
		public void IpQueue_should_be_able_to_receive_packets_while_out_connection_is_full() {
			var queue = new IpQueue("Queue");

			var inPort = new StandardInputPort();
			inPort.Process = queue;
			inPort.Connection = new MockConnection();
			queue.SetInputPort("In", inPort);

			var outPort = new StandardOutputPort();
			outPort.Process = queue;
			outPort.Connection = new MockConnection(0);
			queue.SetOutputPort("Out", outPort);


			var iterator = queue.Execute();

			for(var i = 0; i < 100; ++i) {
				Assert.True(inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(i)));
				do {
					iterator.MoveNext();
				}
				while (!(iterator.Current is WaitForPacketOrCapacityOnAny));
			}
		}

		[Test]
		public void IpQueue_should_be_able_to_receive_a_sequence_and_forward_it() {
			var queue = new IpQueue("Queue");
			var inPort = new StandardInputPort();
			inPort.Process = queue;
			var outPort = new StandardOutputPort();
			outPort.Process = queue;

			inPort.Connection = new MockConnection(100);
			var outputConnection = new MockConnection(100);
			outPort.Connection = outputConnection;
			queue.SetInputPort("In", inPort);
			queue.SetOutputPort("Out", outPort);

			var iterator = queue.Execute();

			inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(InformationPacket.PacketType.StartSequence, null));
			for (var i = 0; i < 5; ++i) {
				inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket("Test data " + i));
			}
			inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(InformationPacket.PacketType.EndSequence, null));

			// Receive all the packets waiting on the in connection
			do {
				iterator.MoveNext();
			}
			while (!(iterator.Current is WaitForPacketOrCapacityOnAny));

			// Send the packets to the out connection 
			for (var i = 0; i < 7; ++i) {
				iterator.MoveNext();
			}

			Assert.AreEqual(InformationPacket.PacketType.StartSequence, outputConnection.Packets[0].Type);
			Assert.AreEqual("Test data 0", outputConnection.Packets[1].Content);
			Assert.AreEqual("Test data 1", outputConnection.Packets[2].Content);
			Assert.AreEqual("Test data 2", outputConnection.Packets[3].Content);
			Assert.AreEqual("Test data 3", outputConnection.Packets[4].Content);
			Assert.AreEqual("Test data 4", outputConnection.Packets[5].Content);
			Assert.AreEqual(InformationPacket.PacketType.EndSequence, outputConnection.Packets[6].Type);
		}

		[Test]
		public void IpQueue_should_be_able_to_receive_an_empty_sequence_and_forward_it() {
			var queue = new IpQueue("Queue");
			var inPort = new StandardInputPort();
			inPort.Process = queue;
			var outPort = new StandardOutputPort();
			outPort.Process = queue;

			inPort.Connection = new MockConnection(100);
			var outputConnection = new MockConnection(100);
			outPort.Connection = outputConnection;
			queue.SetInputPort("In", inPort);
			queue.SetOutputPort("Out", outPort);

			var iterator = queue.Execute();

			inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(InformationPacket.PacketType.StartSequence, null));
			inPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(InformationPacket.PacketType.EndSequence, null));

			// Receive all the packets waiting on the in connection
			do {
				iterator.MoveNext();
			}
			while (!(iterator.Current is WaitForPacketOrCapacityOnAny));

			// Send the packets to the out connection 
			for (var i = 0; i < 2; ++i) {
				iterator.MoveNext();
			}

			Assert.AreEqual(InformationPacket.PacketType.StartSequence, outputConnection.Packets[0].Type);
			Assert.AreEqual(InformationPacket.PacketType.EndSequence, outputConnection.Packets[1].Type);
		}

	}
}