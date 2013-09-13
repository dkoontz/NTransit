using System;
using System.Collections.Generic;

namespace NTransit {
	public class SingleThreadedScheduler {
		List<Component> registeredProcesses = new List<Component>();
		LinkedList<Component> runningProcesses = new LinkedList<Component>();
		Queue<Component> processesToMoveFromRunningToTerminating = new Queue<Component>();
		LinkedList<Component> terminatingProcesses = new LinkedList<Component>();
		Queue<Component> processesThatHaveFullyTerminated = new Queue<Component>();

		public void AddProcess(Component process) {
			registeredProcesses.Add(process);
		}

		public void Init() {
			foreach (var process in registeredProcesses) {
				if (process.Status == Component.ProcessStatus.Unstarted && process.AutoStart) {
					process.Startup();
					runningProcesses.AddLast(process);
				}
			}
		}

		public bool Tick() {
//			Console.WriteLine("Main Tick ------------------");
//			var runawayCounter = 0;
			bool processIsStillRunning;
			do {
//				Console.WriteLine("Internal Tick " + runawayCounter + " ****");
				processIsStillRunning = false;
				foreach (var process in registeredProcesses) {
					if (process.Status == Component.ProcessStatus.Unstarted &&
					    process.HasInputPacketWaiting) {
						runningProcesses.AddLast(process);
						process.Startup();
					} 
				}

				foreach (var process in terminatingProcesses) {
					if (process.HasOutputPacketWaiting) {
						process.Tick();
					}
					else {
						processesThatHaveFullyTerminated.Enqueue(process);
					}
				}

				foreach (var process in runningProcesses) {
//					Console.WriteLine(process.Name + " before: " + processIsStillRunning);
					processIsStillRunning = process.Tick() || processIsStillRunning;
//					Console.WriteLine(process.Name + " after: " + processIsStillRunning);

					if (process.Status == Component.ProcessStatus.Terminated) {
						processesToMoveFromRunningToTerminating.Enqueue(process);
					}
				}

				while (processesToMoveFromRunningToTerminating.Count > 0) {
					var process = processesToMoveFromRunningToTerminating.Dequeue();
					runningProcesses.Remove(process);
					terminatingProcesses.AddLast(process);
					process.Shutdown();
				}

				while (processesThatHaveFullyTerminated.Count > 0) {
					terminatingProcesses.Remove(processesThatHaveFullyTerminated.Dequeue());
				}
			}
			while (processIsStillRunning);

//			if (runawayCounter > 98) {
//				Console.WriteLine("Terminated due to runaway counter");
//			}
			return runningProcesses.Count > 0 || terminatingProcesses.Count > 0;
		}
	}
}