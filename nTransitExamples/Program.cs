using System;
using NTransit;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NTransitExamples {
	class MainClass {
		public static void Main() {
			var reverser = new TextReverser("Text reverser");
			var inPort = new StandardInputPort(1);
			var outPort = new StandardOutputPort();
			reverser.SetInputPort("In", inPort);
			reverser.SetOutputPort("Out", outPort);
			reverser.Init();

			var testReceiver = new StandardInputPort(1);
			outPort.ConnectTo(testReceiver);

			testReceiver.Receive = ipOffer => {
				Console.WriteLine("Got data from reverser");
				Console.WriteLine(ipOffer.Accept().ContentAs<string>());
			};

			Console.WriteLine(inPort.TryReceive(new InformationPacket("hello world")));
			reverser.Tick();

			Console.WriteLine(inPort.TryReceive(new InformationPacket("foo bar baz")));
			reverser.Tick();

			testReceiver.Tick();
			reverser.Tick();
			testReceiver.Tick();

			Console.ReadKey();
		}
	}
}