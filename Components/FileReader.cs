using System;
using System.Collections;
using System.IO;

namespace NTransit {
	public class FileReader : Component {
		[InputPort("File Name")]
		StandardInputPort fileNamePort;

		[OutputPort("File Contents")]
		StandardOutputPort fileContentsPort;

		public FileReader(string name) : base(name) {}

		public override IEnumerator Execute() {
			yield return WaitForPacketOn(fileNamePort);
			var fileName = fileNamePort.Receive().Content as string;

			string contents;
			try {
				var reader = new StreamReader(fileName);
				contents = reader.ReadToEnd();
			}
			catch (Exception ex) {
				Errors.TrySend(ex);
				yield break;
			}

			while (!fileContentsPort.TrySend(contents)) yield return WaitForCapacityOn(fileContentsPort);
		}
	}
}