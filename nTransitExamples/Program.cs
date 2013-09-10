using System;
using NTransit;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

namespace NTransitExamples {
	class MainClass {
		public static void Main() {
			var reader = new FileReader("File reader");
//			var fileNamePort = new StandardInputPort(1, reader);
//			reader.SetInputPort("File Name", fileNamePort);
//			reader.SetOutputPort("Out", new StandardOutputPort());
//			reader.Init();
			reader.SetInitialData("File Name", new InformationPacket("test.txt"));

			var converter = new ConvertIEnumerableToInformationPacketStream("Convert IEnumerable");
			converter.SetInitialData("IEnumerable", new InformationPacket(new []
			{
				"The",
				"quick",
				"brown",
				"fox"
			}));

			var reverser = new TextReverser("Text reverser");

			var writer = new ConsoleWriter("Log");

//			reader.ConnectPorts("Out", reverser, "In", 3);
			converter.ConnectPorts("Out", reverser, "In", 2);
			reverser.ConnectPorts("Out", writer, "In");



//			var processes = new Component[] { reader, reverser, writer };
			var registeredProcesses = new List<Component> { converter, reverser, writer };
			var runningProcesses = new LinkedList<Component>();
			var completedProcesses = new Queue<Component>();

			foreach (var process in registeredProcesses) {
				if (process.AutoStart) {
					process.Startup();
					runningProcesses.AddLast(process);
				}
			}

			for (var i = 0; i < 100; ++i) {
				Console.WriteLine("Tick-----------");
				foreach (var process in registeredProcesses) {
					if (process.Status == Component.ProcessStatus.Unstarted &&
					    process.HasInputPacketWaiting) {
						runningProcesses.AddLast(process);
						Console.WriteLine ("input packets for " + process.Name + " starting up");
						process.Startup();
					} 
				}

				foreach (var process in runningProcesses) {
					process.Tick();
					if (process.Status == Component.ProcessStatus.Completed) {
						completedProcesses.Enqueue(process);
					}
				}

				while (completedProcesses.Count > 0) {
					var process = completedProcesses.Dequeue();
					runningProcesses.Remove(process);
					Console.WriteLine("process " + process.Name + " completed, shutting down");
					process.Shutdown();
				}
			}
		}
	}
}