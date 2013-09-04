using System;
using NUnit.Framework;
using NTransit;

namespace NTransitTest {
	[TestFixture]
	public class CheckpointTests {

		[Test]
		public void Checkpoint_should_allow_through_one_individual_packet_per_activation() {
			var checkpoint = new Checkpoint("Checkpoint");

			var inPort = new StandardInputPort();
			var inputConnection = new MockConnection(5);
			inPort.Connection = inputConnection;
			inPort.Process = checkpoint;
			checkpoint.SetInputPort("In", inPort);

			var triggerPort = new StandardInputPort();
			triggerPort.Connection = new MockConnection();
			triggerPort.Process = checkpoint;
			checkpoint.SetInputPort("Trigger", triggerPort);

			var outPort = new StandardOutputPort();
			var outputConnection = new MockConnection(100);
			outPort.Connection = outputConnection;
			outPort.Process = checkpoint;
			checkpoint.SetOutputPort("Out", outPort);

			var iterator = checkpoint.Execute();

			var ip = new InformationPacket("Test data1");
			var ip2 = new InformationPacket("Test data2");
			inPort.Connection.SendPacketIfCapacityAllows(ip);
			inPort.Connection.SendPacketIfCapacityAllows(ip2);
			triggerPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(null));

			iterator.MoveNext();
			do {
				iterator.MoveNext();
			}
			while (!(iterator.Current is WaitForPacketOn && (iterator.Current as WaitForPacketOn).Ports[0] == triggerPort));

			Assert.AreEqual(1, outputConnection.NumberOfPacketsHeld);
			Assert.AreEqual(1, inputConnection.NumberOfPacketsHeld);
			Assert.AreEqual(ip, outputConnection.Packets[0]);
			Assert.AreEqual(ip2, inputConnection.Packets[0]);
		}

		[Test]
		public void Checkpoint_should_allow_through_one_sequence_per_activation() {
			var checkpoint = new Checkpoint("Checkpoint");

			var inPort = new StandardInputPort();
			var inputConnection = new MockConnection(5);
			inPort.Connection = inputConnection;
			inPort.Process = checkpoint;
			checkpoint.SetInputPort("In", inPort);

			var triggerPort = new StandardInputPort();
			triggerPort.Connection = new MockConnection();
			triggerPort.Process = checkpoint;
			checkpoint.SetInputPort("Trigger", triggerPort);

			var outPort = new StandardOutputPort();
			var outputConnection = new MockConnection(100);
			outPort.Connection = outputConnection;
			outPort.Process = checkpoint;
			checkpoint.SetOutputPort("Out", outPort);

			var iterator = checkpoint.Execute();

			var start = new InformationPacket(InformationPacket.PacketType.StartSequence, null);
			var end = new InformationPacket(InformationPacket.PacketType.EndSequence, null);
			var ip1 = new InformationPacket("data1");
			var ip2 = new InformationPacket("data2");
			var ip3 = new InformationPacket("data3");

			inPort.Connection.SendPacketIfCapacityAllows(start);
			inPort.Connection.SendPacketIfCapacityAllows(ip1);
			inPort.Connection.SendPacketIfCapacityAllows(ip2);
			inPort.Connection.SendPacketIfCapacityAllows(end);

			inPort.Connection.SendPacketIfCapacityAllows(ip3);
			triggerPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(null));

			iterator.MoveNext();
			do {
				iterator.MoveNext();
			}
			while (!(iterator.Current is WaitForPacketOn && (iterator.Current as WaitForPacketOn).Ports[0] == triggerPort));

			Assert.AreEqual(4, outputConnection.NumberOfPacketsHeld);
			Assert.AreEqual(1, inputConnection.NumberOfPacketsHeld);
			Assert.AreSame(start, outputConnection.Packets[0]);
			Assert.AreSame(ip1, outputConnection.Packets[1]);
			Assert.AreSame(ip2, outputConnection.Packets[2]);
			Assert.AreSame(end, outputConnection.Packets[3]);
			Assert.AreSame(ip3, inputConnection.Packets[0]);
		}

		[Test]
		public void Checkpoint_should_allow_through_an_empty_sequence() {
			var checkpoint = new Checkpoint("Checkpoint");

			var inPort = new StandardInputPort();
			var inputConnection = new MockConnection(5);
			inPort.Connection = inputConnection;
			inPort.Process = checkpoint;
			checkpoint.SetInputPort("In", inPort);

			var triggerPort = new StandardInputPort();
			triggerPort.Connection = new MockConnection();
			triggerPort.Process = checkpoint;
			checkpoint.SetInputPort("Trigger", triggerPort);

			var outPort = new StandardOutputPort();
			var outputConnection = new MockConnection(100);
			outPort.Connection = outputConnection;
			outPort.Process = checkpoint;
			checkpoint.SetOutputPort("Out", outPort);

			var iterator = checkpoint.Execute();

			var start = new InformationPacket(InformationPacket.PacketType.StartSequence, null);
			var end = new InformationPacket(InformationPacket.PacketType.EndSequence, null);
			var ip1 = new InformationPacket("data1");

			inPort.Connection.SendPacketIfCapacityAllows(start);
			inPort.Connection.SendPacketIfCapacityAllows(end);
			inPort.Connection.SendPacketIfCapacityAllows(ip1);

			triggerPort.Connection.SendPacketIfCapacityAllows(new InformationPacket(null));

			iterator.MoveNext();
			do {
				iterator.MoveNext();
			}
			while (!(iterator.Current is WaitForPacketOn && (iterator.Current as WaitForPacketOn).Ports[0] == triggerPort));

			Assert.AreEqual(2, outputConnection.NumberOfPacketsHeld);
			Assert.AreEqual(1, inputConnection.NumberOfPacketsHeld);
			Assert.AreSame(start, outputConnection.Packets[0]);
			Assert.AreSame(end, outputConnection.Packets[1]);
			Assert.AreSame(ip1, inputConnection.Packets[0]);
		}
	}
}