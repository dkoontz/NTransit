using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class IpQueue : Component{
		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		Queue<InformationPacket> queue;

		public IpQueue(string name) : base(name) {
			queue = new Queue<InformationPacket>();
		}

		public override IEnumerator Execute() {
//			UnityEngine.Debug.Log("In ip queue");
			while (true) {
//				UnityEngine.Debug.Log("Queue processing in and out ports, queue size: " + queue.Count);
				while (InPort.HasPacketsWaiting) {
					var ip = InPort.Receive();

//					if (InformationPacket.PacketType.StartSequence == ip.Type) {
////						UnityEngine.Debug.Log("Queue started receiving sequence");
//						do {
//							queue.Enqueue(ip);
//
//							yield return WaitForPacketOn(InPort);
//							ip = InPort.Receive();
////							UnityEngine.Debug.Log("Queue got next ip in sequence, type = " + ip.Type);
//						}
//						while (InformationPacket.PacketType.EndSequence != ip.Type);
////						UnityEngine.Debug.Log("Queue done receving sequence");
//					}

//					Console.WriteLine("Enqueuing IP");
					queue.Enqueue(ip);
				}

				if (queue.Count > 0) {
					if (OutPort.TrySend(queue.Peek())) {
//						UnityEngine.Debug.Log("queue has " + queue.Count + " elements, sent packet (" + queue.Peek().Type + ") " + queue.Peek().Content);
						queue.Dequeue();
					}
//					else UnityEngine.Debug.Log("Queue's out connection doesn't have capacity to send packet (" + queue.Peek().Type + ") " + queue.Peek().Content);
				}

//				UnityEngine.Debug.Log("Queue - Yielding until packets arrive or capacity is available");
				if (queue.Count > 0) yield return new WaitForPacketOrCapacityOnAny(new [] { InPort }, new [] { OutPort });
				else yield return WaitForPacketOn(InPort);
//				UnityEngine.Debug.Log("Queue - Woken up");
			}
		}
	}
}