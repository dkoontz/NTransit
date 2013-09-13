using System;
using NTransit;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

namespace NTransitExamples {
	class MainClass {
		public static void Main() {
			var reader = new FileReader("File reader");
			reader.SetInitialData("File Name", new InformationPacket("test.txt"));

//			var converter = new ConvertIEnumerableToInformationPacketStream("Convert IEnumerable");
//			converter.SetInitialData("IEnumerable", new InformationPacket(new []
//			{
//				"The",
//				"quick",
//				"brown",
//				"fox"
//			}));
			var queue = new IpQueue("Queue");
			var gate = new Gate("Gate");
			var reverser = new TextReverser("Text reverser");

			var writer = new ConsoleWriter("Log");

			reader.ConnectTo("Out", queue, "In");
			queue.ConnectTo("Out", gate, "In");
//			converter.ConnectTo("Out", gate, "In", 10);
			gate.ConnectTo("Out", reverser, "In");
			reverser.ConnectTo("Out", writer, "In");
//			converter.ConnectTo("AUTO", gate, "Open");
			reader.ConnectTo("AUTO", gate, "Open");

			var scheduler = new BasicScheduler();
			scheduler.AddProcess(reader);
			scheduler.AddProcess(queue);
			scheduler.AddProcess(gate);
			scheduler.AddProcess(reverser);
			scheduler.AddProcess(writer);

			while (scheduler.Tick()) {

			}
		}
	}

	public class BasicScheduler {
		List<Component> registeredProcesses = new List<Component>();
		LinkedList<Component> runningProcesses = new LinkedList<Component>();
		Queue<Component> processesToMoveFromRunningToTerminating = new Queue<Component>();
		LinkedList<Component> terminatingProcesses = new LinkedList<Component>();
		Queue<Component> processesThatHaveFullyTerminated = new Queue<Component>();

		public void AddProcess(Component process) {
			registeredProcesses.Add(process);
		}

		public bool Tick() {
			foreach (var process in registeredProcesses) {
				if (process.Status == Component.ProcessStatus.Unstarted && process.AutoStart) {
					process.Startup();
					runningProcesses.AddLast(process);
				}
			}

//			Console.WriteLine("Tick-----------");
			foreach (var process in registeredProcesses) {
				if (process.Status == Component.ProcessStatus.Unstarted &&
				    process.HasInputPacketWaiting) {
					runningProcesses.AddLast(process);
//						Console.WriteLine ("input packets for " + process.Name + " starting up");
					process.Startup();
				} 
			}

			foreach (var process in terminatingProcesses) {
				if (process.HasOutputPacketWaiting) {
					process.Tick();
				}
				else {
//					Console.WriteLine("Process " + process.Name + " has sent all packets, terminating");
					processesThatHaveFullyTerminated.Enqueue(process);
				}
			}

			foreach (var process in runningProcesses) {
				process.Tick();
				if (process.Status == Component.ProcessStatus.Terminated) {
//					Console.WriteLine("Shutting down process: " + process.Name + ", waiting to finish sending packets");
					processesToMoveFromRunningToTerminating.Enqueue(process);
				}
			}

			while (processesToMoveFromRunningToTerminating.Count > 0) {
				var process = processesToMoveFromRunningToTerminating.Dequeue();
				runningProcesses.Remove(process);
				terminatingProcesses.AddLast(process);
//					Console.WriteLine("process " + process.Name + " completed, shutting down");
				process.Shutdown();
			}

			while (processesThatHaveFullyTerminated.Count > 0) {
				terminatingProcesses.Remove(processesThatHaveFullyTerminated.Dequeue());
			}

			return runningProcesses.Count > 0 || terminatingProcesses.Count > 0;
		}
	}
}