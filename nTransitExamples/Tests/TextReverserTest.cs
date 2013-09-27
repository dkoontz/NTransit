using System;
using NUnit.Framework;
using NTransit;
using NTransitTest;

namespace NTransitExamples {
	[TestFixture]
	public class TextReverserTest {
		Component component;
		MockInputPort inPort;
		MockOutputPort outPort;

		[SetUp]
		public void Init() {
			component = new TextReverser("");
			inPort = new MockInputPort();
			outPort = new MockOutputPort();
			component.SetInputPort("In", inPort);
			component.SetOutputPort("Out", outPort);
			component.Setup();
			component.Startup();
		}

		[Test]
		public void Empty_string_input_outputs_the_same_string() {
			inPort.TrySend(new InformationPacket(""));

			component.Tick();

			Assert.AreEqual(1, outPort.SentPackets.Count);
			Assert.AreEqual("", outPort.SentPackets[0].Content);
		}

		[Test]
		public void One_character_string_outputs_the_same_string() {
			inPort.TrySend(new InformationPacket("a"));

			component.Tick();

			Assert.AreEqual(1, outPort.SentPackets.Count);
			Assert.AreEqual("a", outPort.SentPackets[0].Content);
		}

		[Test]
		public void Multi_character_string_outputs_reversed_string() {
			inPort.TrySend(new InformationPacket("this is a long sentence with lots of spaces"));

			component.Tick();

			Assert.AreEqual(1, outPort.SentPackets.Count);

			Assert.AreEqual("secaps fo stol htiw ecnetnes gnol a si siht", outPort.SentPackets[0].Content);
		}

		[Test]
		public void Strings_containing_symbols_and_escaped_characters_are_reversed() {
			inPort.TrySend(new InformationPacket("~!@#$%^&*()_\n\t1234567890"));

			component.Tick();

			Assert.AreEqual(1, outPort.SentPackets.Count);
			Assert.AreEqual("0987654321\t\n_)(*&^%$#@!~", outPort.SentPackets[0].Content);
		}
	}
}