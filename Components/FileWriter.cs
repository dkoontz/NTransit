using System;
using System.Collections;
using System.IO;

namespace nTransit {
	public class FileWriter : Component {
		[InputPort("File Name")]
		StandardInputPort fileNamePort;

		[InputPort("Text To Write")]
		StandardInputPort fileContentsPort;

		public FileWriter(string name) : base(name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(fileNamePort);
			var fileName = fileNamePort.Receive().Content as string;

			yield return WaitForPacketOn(fileContentsPort);
			var contents = fileContentsPort.Receive().Content as string;

			try {
				using (var writer = new StreamWriter(fileName)) {
					writer.Write(contents);
				}
			}
			catch (IOException ex) {
				Errors.TrySend(ex);
				yield break;
			}
		}
	}
}