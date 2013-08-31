using System;
using System.Collections;

namespace NTransit {
	public class PacketLogger : Component {
		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		[OutputPort("Log")]
		public StandardOutputPort LogPort { get; set; }

		public PacketLogger(string name) : base(name) {}

		public override IEnumerator Execute() {
			while (true) {
				yield return WaitForPacketOn(InPort);
				var ip = InPort.Receive();
				var logIp = new InformationPacket(string.Format("* (Time {0}) Packet ({1}) - {2}", DateTime.Now.Millisecond, ip.Type, ip.Content));
				while (!LogPort.TrySend(logIp)) yield return WaitForCapacityOn(LogPort);
				while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
				UnityEngine.Debug.Log("Done sending logged packet");
			}
		}
	}
}