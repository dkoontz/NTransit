using System;
using NTransit;

namespace NTransitExamples {
	class MainClass {
		public static void Main() {
			var scheduler = new Scheduler();

			var consoleWriter = new ConsoleWriter("Console Output");
			scheduler.AddComponent(consoleWriter);

			//			var reader = new FileReader ("Get File Contents");
			//			scheduler.AddComponent (reader);
			//
			//			var writer = new FileWriter ("Write File Contents");
			//			scheduler.AddComponent (writer);

			var rng = new RandomNumberGenerator("Number generator");
			scheduler.AddComponent(rng);

			var delay = new Delay("Delayer");
			scheduler.AddComponent(delay);

			scheduler.SetupPorts();

			//			scheduler.Connect (reader, "File Contents", writer, "Text To Write");
			//			scheduler.Connect (reader, "Errors", consoleWriter, "In");
			//			scheduler.Connect (writer, "Errors", consoleWriter, "In");
			//			scheduler.SetInitialData (reader, "File Name", "test1.txt");
			//			scheduler.SetInitialData (writer, "File Name", "test2.txt");

			scheduler.Connect(rng, "Number", delay, "In");
			scheduler.Connect(delay, "Out", consoleWriter, "In");
			scheduler.SetInitialData(delay, "Seconds Between Packets", .5f);

			scheduler.Go();
		}
	}
}