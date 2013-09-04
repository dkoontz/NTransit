using System;
using System.Collections;
using System.IO;

namespace NTransit {
	public class FileWriter : Component {
		[InputPort("File Name")]
		public StandardInputPort FileNamePort { get; set; }

		[InputPort("Text To Write")]
		public StandardInputPort FileContentsPort { get; set; }

		public FileWriter(string name) : base(name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(FileNamePort);
			var fileName = FileNamePort.Receive().Content as string;

			yield return WaitForPacketOn(FileContentsPort);
			var contents = FileContentsPort.Receive().Content as string;

			try {
				using (var writer = new StreamWriter(fileName)) {
					writer.Write(contents);
				}
			}
			catch (IOException ex) {
				ErrorsPort.TrySend(ex);
				yield break;
			}
		}
	}
}