using System;
using System.Collections;
using System.IO;

namespace NTransit {
	public class FileReader : Component {
		[InputPort("File Name")]
		public StandardInputPort FileNamePort { get; set; }

		[OutputPort("File Contents")]
		public StandardOutputPort FileContentsPort { get; set; }

		public FileReader(string name) : base(name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(FileNamePort);
			var fileName = FileNamePort.Receive().Content as string;

			string contents;
			try {
				var reader = new StreamReader(fileName);
				contents = reader.ReadToEnd();
			}
			catch (Exception ex) {
				ErrorsPort.TrySend(ex);
				yield break;
			}

			while (!FileContentsPort.TrySend(contents)) yield return WaitForCapacityOn(FileContentsPort);
		}
	}
}