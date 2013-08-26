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

		public FileReader (string name) : base(name)
		{
			fileNamePort = new StandardInputPort ();
			fileContentsPort = new StandardOutputPort ();
		}

		public override IEnumerator Execute ()
		{
			InformationPacket packet;
			while (!fileNamePort.Receive(out packet)) {
				yield return WaitForPacketOn (fileNamePort);
			}

			var fileName = packet.Content as string;

			string contents;
			try 
			{
				var reader = new StreamReader (fileName);
				contents = reader.ReadToEnd ();
			}
			catch(Exception ex) {
				Errors.Send (ex);
				yield break;
			}

			while (!fileContentsPort.Send(contents)) {
				yield return WaitForCapacityOn (fileContentsPort);
			}
		}
	}
}