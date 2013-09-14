using System;
using NTransit;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

namespace NTransitExamples {
	class MainClass {
		static SingleThreadedScheduler scheduler;

		public static void Main() {
//			var converter = new ConvertIEnumerableToInformationPacketStream("Convert IEnumerable");
//			converter.SetInitialData("IEnumerable", new InformationPacket(new []
//			{
//				"The",
//				"quick",
//				"brown",
//				"fox"
//			}));
//			var queue = new IpQueue("Queue");
//			var gate = new Gate("Gate");
//			var reverser = new TextReverser("Text reverser");
//
//			var writer = new ConsoleWriter("Log");
//
//			converter.ConnectTo("Out", queue, "In");
//			queue.ConnectTo("Out", gate, "In");
//			gate.ConnectTo("Out", reverser, "In");
//			reverser.ConnectTo("Out", writer, "In");
//			converter.ConnectTo("AUTO", gate, "Open");
//
//			scheduler = new SingleThreadedScheduler();
//			scheduler.AddProcess(converter);
//			scheduler.AddProcess(queue);
//			scheduler.AddProcess(gate);
//			scheduler.AddProcess(reverser);
//			scheduler.AddProcess(writer);


			var program = @"
				GetWordList(FileReader).Out => Queue(IpQueue).In
				Queue.Out => WaitUntilAllWordsAreReceived(Gate).In
				WaitUntilAllWordsAreReceived.Out => ReverseLines(TextReverser).In
				ReverseLines.Out => Writer(ConsoleWriter).In
				GetWordList.AUTO => WaitUntilAllWordsAreReceived.Open
				'test.txt' => GetWordList.FileName
			";
			scheduler = new FbpParser().Parse(program);

			while (scheduler.Tick()) {

			}
		}
	}
}