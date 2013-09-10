using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NTransit {
	[OutputPort("Errors")]
	public abstract class Component {
		// This inner class exists to make the Receive["In"] = data => ... syntax possible
		protected class DataReceivers {
			Component parentComponent;

			public DataReceivers(Component parent) {
				parentComponent = parent;
			}

			public Action<IpOffer> this[string portName] {
				set {
					if (!parentComponent.inPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no in port named '{0}' on component '{1}'", portName, parentComponent.Name));
					parentComponent.inPorts[portName].Receive = value;
				}
			}
		}

		protected class SequenceStartReceivers {
			Component parentComponent;

			public SequenceStartReceivers(Component parent) {
				parentComponent = parent;
			}

			public Action<int> this[string portName] {
				set {
					if (!parentComponent.inPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no in port named '{0}' on component '{1}'", portName, parentComponent.Name));
					parentComponent.inPorts[portName].SequenceStart = value;
				}
			}
		}

		protected class SequenceEndReceivers {
			Component parentComponent;

			public SequenceEndReceivers(Component parent) {
				parentComponent = parent;
			}

			public Action<int> this[string portName] {
				set {
					if (!parentComponent.inPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no in port named '{0}' on component '{1}'", portName, parentComponent.Name));
					parentComponent.inPorts[portName].SequenceEnd = value;
				}
			}
		}

		class PendingPacket {
			public string Port;
			public InformationPacket Ip;

			public PendingPacket(string port, InformationPacket ip) {
				Port = port;
				Ip = ip;
			}
		}

		public string Name { get; protected set; }
		public bool HasCompleted { get; protected set; }
		public bool Blocked { get; protected set; }

		protected DataReceivers Receive;
		protected SequenceStartReceivers SequenceStart;
		protected SequenceEndReceivers SequenceEnd;
		protected Action Start = () => {};
		protected Action End = () => {};
		protected Action Update = () => {};

		Dictionary<string, StandardInputPort> inPorts;
		Dictionary<string, StandardOutputPort> outPorts;
		Queue<PendingPacket> pendingPackets;

		protected Component(string name) {
			Name = name;
			inPorts = new Dictionary<string, StandardInputPort>();
			outPorts = new Dictionary<string, StandardOutputPort>();
			pendingPackets = new Queue<PendingPacket>();
			Receive = new DataReceivers(this);
			SequenceStart = new SequenceStartReceivers(this);
			SequenceEnd = new SequenceEndReceivers(this);
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

		public void ConnectPorts(string outPortName, Component process, string inPortName) {
			process.AddConnectBetween(outPorts[outPortName], inPortName);
		}

		public void ConnectPorts(string outPortName, Component process, string inPortName, int capacity) {
			process.AddConnectBetween(outPorts[outPortName], inPortName, capacity);
		}

		public void CreatePorts() {
			// TODO: enable the specification of custom port types in the attribute 
			// and instantiate that instead of just StandardInputPort / StandardOutputPort
			foreach (var attribute in GetType().GetCustomAttributes(true)) {
				if (attribute is InputPortAttribute) {
					var inPort = attribute as InputPortAttribute;
					SetInputPort(inPort.Name, new StandardInputPort(1, this));
				}
				else if (attribute is OutputPortAttribute) {
					var outPort = attribute as OutputPortAttribute;
					SetOutputPort(outPort.Name, new StandardOutputPort());
				}
			}
		}

		public void Tick() {
			if (HasCompleted) return;

			PendingPacket firstPacket = null;
			while (pendingPackets.Count > 0 && firstPacket != pendingPackets.Peek()) {
				var pendingPacket = pendingPackets.Dequeue();
				if (!outPorts[pendingPacket.Port].TrySend(pendingPacket.Ip)) {
					if (null == firstPacket) firstPacket = pendingPacket;
					pendingPackets.Enqueue(pendingPacket);
				}
			}

			Blocked = pendingPackets.Count > 0;

			if (!Blocked) {
				foreach (var kvp in inPorts) {
					kvp.Value.Tick();
				}
				
				Update();
			}
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

		protected void SendSequenceStart(string port, int id) {

		}

		protected void SendSequenceEnd(string port, int id) {

		}

		protected bool HasCapacity(string port) {
			return outPorts[port].HasCapacity;
		}

		void AddConnectBetween(StandardOutputPort port, string inputPortName) {
			port.ConnectTo(inPorts[inputPortName]);
		}

		void AddConnectBetween(StandardOutputPort port, string inputPortName, int capacity) {
			AddConnectBetween(port, inputPortName);
			inPorts[inputPortName].ConnectionCapacity = capacity;
		}
	}
}