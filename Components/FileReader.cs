using System;
using System.IO;

namespace NTransit {
	[InputPort("File Name")]
	public class FileReader : SourceComponent {
		StreamReader reader;

		public FileReader(string name) : base(name) {
			Receive["File Name"] = data => reader = new StreamReader(data.Accept().ContentAs<string>());
			Update = () => {
				while (HasCapacity("Out") && !reader.EndOfStream) {
					SendNew("Out", reader.ReadLine()); 
					if (reader.EndOfStream) Status = ProcessStatus.Terminated;
				}
				return false;
			};
			End = () => reader.Close();
		}
	}
}