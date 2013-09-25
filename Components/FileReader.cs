using System;
using System.IO;

namespace NTransit {
	[InputPort("FileName")]
	public class FileReader : SourceComponent {
		StreamReader reader;

		public FileReader(string name) : base(name) { }

		public override void Setup() {
			InPorts["FileName"].Receive = data => {
				var fileName = data.Accept().ContentAs<string>();
				try {
					reader = new StreamReader(fileName);
				}
				catch (FileNotFoundException) {
					throw new ArgumentException(string.Format("File name '{0}' sent to {1}.FileName does not exist", fileName, Name));
				}
			};
		}

		protected override bool Update() {
			while (HasCapacity("Out") && !reader.EndOfStream) {
				SendNew("Out", reader.ReadLine()); 
				if (reader.EndOfStream) Status = ProcessStatus.Terminated;
			}
			return false;
		}

		protected override void End() {
			reader.Close();
		}
	}
}