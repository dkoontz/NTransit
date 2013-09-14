using System;
using System.IO;

namespace NTransit {
	[InputPort("FileName")]
	public class FileReader : SourceComponent {
		StreamReader reader;

		public FileReader(string name) : base(name) {
			Receive["FileName"] = data => {
				var fileName = data.Accept().ContentAs<string>();
				try {
					reader = new StreamReader(fileName);
				}
				catch (FileNotFoundException) {
					throw new ArgumentException(string.Format("File name '{0}' sent to {1}.FileName does not exist", fileName, Name));
				}
			};

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