using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NTransit {
	[OutputPort("Errors")]
	public abstract class Component {
		class PendingPacket {
			public string Port;
			public InformationPacket Ip;

			public PendingPacket(string port, InformationPacket ip) {
				Port = port;
				Ip = ip;
			}
		}

		public string Name { get; protected set; }

		Dictionary<string, StandardInputPort> inPorts;
		Dictionary<string, StandardOutputPort> outPorts;
		Queue<PendingPacket> pendingPackets;

		protected Component(string name) {
			Name = name;
			inPorts = new Dictionary<string, StandardInputPort>();
			outPorts = new Dictionary<string, StandardOutputPort>();
			pendingPackets = new Queue<PendingPacket>();
		}

		public abstract void Init();

		public void SetInputPort(string name, StandardInputPort port) {
			port.Name = name;
			inPorts[name] = port;
		}

		public void SetOutputPort(string name, StandardOutputPort port) {
			port.Name = name;
			outPorts[name] = port;
			pendingPackets = new Queue<PendingPacket>();
		}

		public void Tick() {
			PendingPacket firstPacket = null;
			while (pendingPackets.Count > 0 && firstPacket != pendingPackets.Peek()) {
				var pendingPacket = pendingPackets.Dequeue();
				if (!outPorts[pendingPacket.Port].TrySend(pendingPacket.Ip)) {
					if (null == firstPacket) firstPacket = pendingPacket;
					pendingPackets.Enqueue(pendingPacket);
				}
			}

			foreach (var kvp in inPorts) {
				kvp.Value.Tick();
			}

			Update();
		}

		public void Start() { }
		public void End() { }
		public void Update() { }

		protected void OnReceive(string port, Action<IpOffer> callback) {
			inPorts[port].Receive = callback;
		}

		protected void SendNew(string port, object o) {
			Send(port, new InformationPacket(o));
		}

		protected void SendNew(string port, object o, Dictionary<string, object> attributes) {
			Send(port, new InformationPacket(o, attributes));
		}

		protected void Send(string port, InformationPacket ip) {
			if (!outPorts.ContainsKey(port)) throw new ArgumentException(string.Format("There is no out port named '{0}' on component '{1}'", port, Name));
			if (!outPorts[port].TrySend(ip)) {
				pendingPackets.Enqueue(new PendingPacket(port, ip));
			}
		}
	}
}