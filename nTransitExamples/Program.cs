using System;
using NTransit;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NTransitExamples {
	class MainClass {
		public static void Main() {
			var reader = new FileReader("File reader");
			var fileNamePort = new StandardInputPort(1, reader);
			reader.SetInputPort("File Name", fileNamePort);
			reader.SetOutputPort("Out", new StandardOutputPort());
			reader.Init();

			var reverser = new TextReverser("Text reverser");
			reverser.CreatePorts();
			reverser.Init();

			var writer = new ConsoleWriter("Log");
			writer.CreatePorts();
			writer.Init();

			reader.ConnectPorts("Out", reverser, "In", 3);
			reverser.ConnectPorts("Out", writer, "In");

			fileNamePort.SetInitialData(new InformationPacket("test.txt"));

			var processes = new Component[] { reader, reverser, writer };

			for (var i = 0; i < 10; ++i) {
				Console.WriteLine("Tick ==========");
				foreach (var process in processes) process.Tick();
			}
		}
	}
}