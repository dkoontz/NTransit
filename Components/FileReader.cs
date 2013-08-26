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
			Console.WriteLine ("At beginning of FileReader.Execute");
			InformationPacket packet;
			Console.WriteLine ("Reading in packet");
			while (!fileNamePort.Receive(out packet)) {
				Console.WriteLine ("waiting for packet");
				yield return WaitForPacketOn (fileNamePort);
			}

			var fileName = packet.Content as string;

			string contents;
			try 
			{
				var reader = new StreamReader (fileName);
				contents = reader.ReadToEnd ();
			}
			catch(Exception e) {
				Errors.Send (e);
				yield break;
			}

			Console.WriteLine ("sending packet to out port");
			while (!fileContentsPort.Send(contents)) {
				yield return WaitForCapacityOn (fileContentsPort);
			}
		}
	}
}