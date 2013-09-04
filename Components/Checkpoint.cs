using System;
using System.Collections;

namespace NTransit {
	public class Checkpoint : Component {
		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[InputPort("Trigger")]
		public StandardInputPort TriggerPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		public Checkpoint(string name) : base(name) {}

		public override IEnumerator Execute() {
			while (true) {
				yield return WaitForPacketOn(TriggerPort);
				TriggerPort.Receive();
				UnityEngine.Debug.Log("Checkpoint - Got trigger for Checkpoint");
//				Console.WriteLine("Checkpoint - Got trigger for Checkpoint");

				if (InPort.HasPacketsWaiting) {
					UnityEngine.Debug.Log("Checkpoint - packet(s) waiting for Checkpoint");
//					Console.WriteLine("Checkpoint - packet(s) waiting for Checkpoint");
					var ip = InPort.Receive();
					if (InformationPacket.PacketType.StartSequence == ip.Type) {
						UnityEngine.Debug.Log("Checkpoint - Sequence was waiting");
//						Console.WriteLine("Checkpoint - Sequence was waiting");
						do {
							UnityEngine.Debug.Log("Checkpoint - sending sequence element (" + ip.Type + ") " + ip.Content);
//							Console.WriteLine ("Checkpoint - sending sequence element (" + ip.Type + ") " + ip.Content);
							while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
							UnityEngine.Debug.Log("Checkpoint - waiting for next packet in sequence");
//							Console.WriteLine ("Checkpoint - waiting for next packet in sequence");
							yield return WaitForPacketOn(InPort);
							ip = InPort.Receive();
						}
						while (InformationPacket.PacketType.EndSequence != ip.Type);
						UnityEngine.Debug.Log("Checkpoint - Done sending sequence");
//						Console.WriteLine("Checkpoint - Done sending sequence");
					}
					UnityEngine.Debug.Log("Checkpoint - sending last/only packet (" + ip.Type + ") " + ip.Content);
//					Console.WriteLine("Checkpoint - sending last/only packet (" + ip.Type + ") " + ip.Content);
					while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
					UnityEngine.Debug.Log("Checkpoint - Done sending packet(s)");
//					Console.WriteLine("Checkpoint - Done sending packet(s)");
				}
			}
		}
	}
}