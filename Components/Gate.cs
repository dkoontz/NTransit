using System;
using System.Collections;

namespace NTransit {
	public class Gate : Component {
		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[InputPort("Trigger")]
		public StandardInputPort TriggerPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		public Gate(string name) : base(name) {}

		public override IEnumerator Execute() {
			while (true) {
				yield return WaitForPacketOn(TriggerPort);
				TriggerPort.Receive();
//				UnityEngine.Debug.Log("Gate - Got trigger for gate");

				if (InPort.HasPacketsWaiting) {
//					UnityEngine.Debug.Log("Gate - packet(s) waiting for gate");
					var ip = InPort.Receive();
					if (InformationPacket.PacketType.StartSequence == ip.Type) {

//						UnityEngine.Debug.Log("Gate - Sequence was waiting");
						do {
//							UnityEngine.Debug.Log("Gate - sending (" + ip.Type + ") " + ip.Content);
							while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
							yield return WaitForPacketOn(InPort);
							ip = InPort.Receive();
						}
						while (InformationPacket.PacketType.EndSequence != ip.Type);
//						UnityEngine.Debug.Log("Gate - Done sending sequence");
					}
					UnityEngine.Debug.Log("Gate - sending (" + ip.Type + ") " + ip.Content);
					while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
//					UnityEngine.Debug.Log("Gate - Done sending packet(s)");
				}
			}
		}
	}
}