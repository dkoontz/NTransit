using System;
using System.Collections;
using System.IO;

namespace Transit
{
	public class FileReader : Component
	{
		[InputPort("File Name")]
		StandardInputPort fileNamePort;
		[OutputPort("File Contents")]
		StandardOutputPort fileContentsPort;

		public FileReader ()
		{
			fileNamePort = new StandardInputPort ();
			fileContentsPort = new StandardOutputPort ();
		}

		public override IEnumerator Execute ()
		{
			InformationPacket packet;
			Console.WriteLine ("Reading in packet");
			while (!fileNamePort.Receive(out packet)) {
				Console.WriteLine ("waiting for packet");
				yield return WaitForPacketOn (fileNamePort);
			}

			var fileName = packet.Content as string;

			var reader = new StreamReader (fileName);
			var contents = reader.ReadToEnd ();

			Console.WriteLine ("sending packet to out port");
			while (!fileContentsPort.Send(contents)) {
				yield return WaitForCapacityOn (fileContentsPort);
			}
		}
	}
}