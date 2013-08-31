using System;
using NTransit;

namespace NTransitExamples {
	class MainClass {
		public static void Main() {
			var scheduler = new Scheduler();

			var consoleWriter = new ConsoleWriter("Console Output");
			scheduler.AddProcess(consoleWriter);

			// File read and write example

			//			var reader = new FileReader ("Get File Contents");
			//			scheduler.AddComponent (reader);
			//
			//			var writer = new FileWriter ("Write File Contents");
			//			scheduler.AddComponent (writer);
			//
			//			scheduler.Connect (reader, "File Contents", writer, "Text To Write");
			//			scheduler.Connect (reader, "Errors", consoleWriter, "In");
			//			scheduler.Connect (writer, "Errors", consoleWriter, "In");
			//			scheduler.SetInitialData (reader, "File Name", "test1.txt");
			//			scheduler.SetInitialData (writer, "File Name", "test2.txt");


			// Random number generator with delay example
			var rng = new RandomNumberGenerator("Number generator");
			scheduler.AddProcess(rng);

			var delay = new Delay("Delayer");
			scheduler.AddProcess(delay);

			scheduler.Connect(rng, "Number", delay, "In");
			scheduler.Connect(delay, "Out", consoleWriter, "In");
			scheduler.SetInitialData(delay, "Seconds Between Packets", .5f);

			scheduler.AutoRun();
		}
	}
}