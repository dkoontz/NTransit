using System;
using NTransit;
using NTransitTest;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

namespace NTransitExamples {
	class MainClass {

		static void Main(string[] args) {
			var program = @"
'test.txt' -> ReadConfigFile(FileReader).FileName
ReadConfigFile.Out -> Reverse(TextReverser).In
Reverse.Out -> Output(ConsoleWriter).In
";
			var scheduler = FbpParser.Parse(program);
			scheduler.Init();

			while (scheduler.Tick()) { }
		}
	}
}