using NUnit.Framework;
using NTransit;
using System;

namespace NTransitTest {
	[TestFixture]
	public class StandardInputPortTest {

		[Test]
		public void Should_return_false_when_capacity_is_full() {
			var port = new StandardInputPort(0, null);
			port.TrySend(new InformationPacket(null)).ShouldBeFalse();

			port = new StandardInputPort(10, null);
			var count = 0;
			var capacity = 10;
			while (count < capacity) {
				port.TrySend(new InformationPacket(null));
				++count;
			}
			// Should fail on sending the 11th packet to a connection with capacity 10
			port.TrySend(new InformationPacket(null)).ShouldBeFalse();
		}

		[Test]
		public void Should_return_true_when_capacity_is_not_full() {
			var port = new StandardInputPort(1, null);
			port.TrySend(new InformationPacket(null)).ShouldBeTrue();

			port = new StandardInputPort(10, null);
			var count = 0;
			var capacity = 10;
			while (count < capacity) {
				port.TrySend(new InformationPacket(null)).ShouldBeTrue();
				++count;
			}
		}

		[Test]
		public void Should_throw_exception_when_received_IP_content_does_not_match_specified_types() {
			var port = new StandardInputPort<int>(1, null);
			new TestDelegate(() => port.TrySend(new InformationPacket("1"))).ShouldThrow<ArgumentException>();
			new TestDelegate(() => port.TrySend(new InformationPacket(1.0))).ShouldThrow<ArgumentException>();
			new TestDelegate(() => port.TrySend(new InformationPacket(1.0f))).ShouldThrow<ArgumentException>();
			new TestDelegate(() => port.TrySend(new InformationPacket(new object()))).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void Should_not_throw_an_exception_when_received_IP_content_matches_any_type_specified() {
			var port = new StandardInputPort<string, float, double, int[]>(1, null);
			new TestDelegate(() => port.TrySend(new InformationPacket("1"))).ShouldNotThrow();
			new TestDelegate(() => port.TrySend(new InformationPacket(1.0))).ShouldNotThrow();
			new TestDelegate(() => port.TrySend(new InformationPacket(1.0f))).ShouldNotThrow();
			new TestDelegate(() => port.TrySend(new InformationPacket(new int[] {4, 5, 6, 7, 8}))).ShouldNotThrow();
		}

		[Test]
		public void Should_offer_one_IPs_if_queued_when_Tick_is_called() {
			var port = new StandardInputPort(1, null);
			var timesCalled = 0;
			port.TrySend(new InformationPacket(null));
			port.Receive = data => ++timesCalled;
			port.Tick();
			timesCalled.ShouldEqual(1);
		}

		[Test]
		public void Should_offer_all_IPs_if_queued_when_Tick_is_called_and_greedy_is_true() {
			var port = new StandardInputPort(4, null);
			port.Greedy = true;
			var timesCalled = 0;
			port.TrySend(new InformationPacket(null));
			port.TrySend(new InformationPacket(null));
			port.TrySend(new InformationPacket(null));
			port.TrySend(new InformationPacket(null));
			port.Receive = data => {
				++timesCalled;
				data.Accept();
			};
			port.Tick();
			timesCalled.ShouldEqual(4);
		}

		[Test]
		public void Should_return_false_from_Tick_when_no_packets_are_sent() {
			var port = new StandardInputPort(1, null);
			port.Tick().ShouldBeFalse();
		}

		[Test]
		public void Should_return_false_from_Tick_when_no_receiver_is_registered() {
			var port = new StandardInputPort(1, null);
			port.TrySend(new InformationPacket(null));
			port.Tick().ShouldBeFalse();
		}

		[Test]
		public void Should_return_true_from_Tick_when_at_least_one_packet_is_accepted() {
			var port = new StandardInputPort(1, null);
			port.Receive = data => data.Accept();
			port.TrySend(new InformationPacket(null));
			port.Tick().ShouldBeTrue();
		}
	}
}