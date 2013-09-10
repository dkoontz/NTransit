using System;
using System.IO;

namespace NTransit {
	[InputPort("File Name")]
	public class FileReader : SourceComponent {
		public FileReader(string name) : base(name) {}

		StreamReader reader;

		public override void Init() {
			Receive["File Name"] = data => reader = new StreamReader(data.Accept().ContentAs<string>());
			Update = () => {
				while (HasCapacity("Out") && !reader.EndOfStream) {
					SendNew("Out", reader.ReadLine()); 
					if (reader.EndOfStream) HasCompleted = true;
				}
			};
		}
	}
}