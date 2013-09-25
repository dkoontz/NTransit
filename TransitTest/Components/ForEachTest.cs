using NTransit;
using NTransitTest;
using NUnit.Framework;
using System;

namespace NTransitTest {
	[TestFixture()]
	public class ForEachTest {
		Component component;
		MockInputPort inPort;
		MockOutputPort outPort;
		MockOutputPort originalPort;

		[SetUp]
		public void Init() {
			component = new ForEach("");
			inPort = new MockInputPort();
			outPort = new MockOutputPort();
			originalPort = new MockOutputPort();
			component.SetInputPort("In", inPort);
			component.SetOutputPort("Out", outPort);
			component.SetOutputPort("Original", originalPort);
			component.Setup();
			component.Startup();
		}

		[Test()]
		public void IP_passed_to_In_is_sent_to_Original() {
			var list = new [] { 4, 5, 6, 7, 8, 9 };
			inPort.TrySend(new InformationPacket(list));
			component.Tick();

			Assert.AreEqual(1, originalPort.SentPackets.Count);
			Assert.AreEqual(list, originalPort.SentPackets[0].Content);
		}

		[Test()]
		public void IP_is_sent_to_Out_for_each_element_in_IEnumerable() {
			var list = new [] { 4, 5, 6, 7, 8, 9 };
			inPort.TrySend(new InformationPacket(list));
			component.Tick();

			Assert.AreEqual(list.Length, outPort.SentPackets.Count);
			Assert.AreEqual(list[0], outPort.SentPackets[0].Content);
			Assert.AreEqual(list[1], outPort.SentPackets[1].Content);
			Assert.AreEqual(list[2], outPort.SentPackets[2].Content);
			Assert.AreEqual(list[3], outPort.SentPackets[3].Content);
			Assert.AreEqual(list[4], outPort.SentPackets[4].Content);
			Assert.AreEqual(list[5], outPort.SentPackets[5].Content);
		}

		[Test()]
		public void Empty_IEnumerable_sends_zero_packets_to_Out() {
			var list = new int[0];
			inPort.TrySend(new InformationPacket(list));
			component.Tick();

			Assert.AreEqual(list.Length, outPort.SentPackets.Count);
		}
	}
}