using System;
using System.Collections;
using System.IO;

namespace Transit
{
	public class FileWriter : Component
	{
		[InputPort("File Name")]
		StandardInputPort fileNamePort;
		[InputPort("Text To Write")]
		StandardInputPort fileContentsPort;

		public FileWriter(string name) : base(name) {}

		public override IEnumerator Execute ()
		{
			InformationPacket packet;
			while (!fileNamePort.Receive(out packet)) {
				yield return WaitForPacketOn (fileNamePort);
			}

			var fileName = packet.Content as string;

			while (!fileContentsPort.Receive(out packet)) {
				yield return WaitForPacketOn (fileContentsPort);
			}

			var contents = packet.Content as string;

			try
			{
				using (var writer = new StreamWriter(fileName)) {
					writer.Write (contents);
				}
			}
			catch(IOException ex)
			{
				Errors.Send (ex);
				yield break;
			}
		}
	}
}