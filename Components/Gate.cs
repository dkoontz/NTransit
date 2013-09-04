using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class Gate : Component {

		[InputPort("Open")]
		public StandardInputPort OpenPort { get; set; }

		[InputPort("Close")]
		public StandardInputPort ClosePort { get; set; }

		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		bool open;

		public Gate(string name) : base(name) {}

		public override IEnumerator Execute() {
			UnityEngine.Debug.Log("************ Starting up gate");
			while (true) {
				yield return new WaitForPacketOrCapacityOnAny(ConnectedInputPorts, ConnectedOutputPorts);

				UnityEngine.Debug.Log("Gate - Checking for open port input");
				if (OpenPort.HasConnection && OpenPort.HasPacketsWaiting) {
					open = true;
					while (OpenPort.HasPacketsWaiting) OpenPort.Receive();
				}

				UnityEngine.Debug.Log("Gate - Checking for closed port input");
				if (ClosePort.HasConnection && ClosePort.HasPacketsWaiting) {
					open = false;
					while (ClosePort.HasPacketsWaiting) ClosePort.Receive();
				}
				
				if (open && InPort.HasPacketsWaiting) {
					UnityEngine.Debug.Log("Gate - Open and forwarding packet");
					var ip = InPort.Receive();
					while (!OutPort.TrySend(ip)) yield return WaitForCapacityOn(OutPort);
					UnityEngine.Debug.Log("Gate - Done forwarding packet");
				}
			}
		}
	}
}