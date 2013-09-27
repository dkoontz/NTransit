using NTransit;
using NTransitTest;
using NUnit.Framework;
using System;

namespace NTransitTest {
	[TestFixture]
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

		[Test]
		public void IP_passed_to_In_is_sent_to_Original() {
			var list = new [] { 4, 5, 6, 7, 8, 9 };
			inPort.TrySend(new InformationPacket(list));
			component.Tick();

			originalPort.SentPackets.Count.ShouldEqual(1);
			originalPort.SentPackets[0].Content.ShouldEqual(list);
		}

		[Test]
		public void IP_is_sent_to_Out_for_each_element_in_IEnumerable() {
			var list = new [] { 4, 5, 6, 7, 8, 9 };
			inPort.TrySend(new InformationPacket(list));
			component.Tick();


			outPort.SentPackets.Count.ShouldEqual(list.Length);
			for (var i = 0; i < list.Length; ++i) {
				outPort.SentPackets[i].Content.ShouldEqual(list[i]);
			}
		}

		[Test]
		public void Empty_IEnumerable_sends_zero_packets_to_Out() {
			var list = new int[0];
			inPort.TrySend(new InformationPacket(list));
			component.Tick();

			outPort.SentPackets.Count.ShouldEqual(0);
		}
	}
}