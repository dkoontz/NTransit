using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Big TODO's:
//
// FBP / Json importer
// Support thread pool and individual threads
// Sub-Networks
// Array Inputs
// Array Outputs
// Auto wire unconnected output ports to Drop component
// Auto wire unconnected Errors component port to default ConsoleWriter or settable property
// Figure out IP ownership and tracking/disposing
// Allowing components to execute during Fixed/Late Update
// Type checking of IP content at Port/Connection level based on parameter(s) to InputPort / OutputPort attributes

namespace NTransit {
	[InputPort("AUTO")]
	[OutputPort("AUTO")]
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
					if (!parentComponent.inPorts.ContainsKey(portName)) {
						throw new ArgumentException(string.Format("Port '{0}.{1}' does not exist", parentComponent.Name, portName));
					}
					parentComponent.inPorts[portName].Receive = value;
				}
			}
		}

		protected class SequenceStartReceivers {
			Component parentComponent;

			public SequenceStartReceivers(Component parent) {
				parentComponent = parent;
			}

			public Action<IpOffer> this[string portName] {
				set {
					if (!parentComponent.inPorts.ContainsKey(portName)) {
						throw new ArgumentException(string.Format("Port '{0}.{1}' does not exist", parentComponent.Name, portName));
					}
					parentComponent.inPorts[portName].SequenceStart = value;
				}
			}
		}

		protected class SequenceEndReceivers {
			Component parentComponent;

			public SequenceEndReceivers(Component parent) {
				parentComponent = parent;
			}

			public Action<IpOffer> this[string portName] {
				set {
					if (!parentComponent.inPorts.ContainsKey(portName)) {
						throw new ArgumentException(string.Format("Port '{0}.{1}' does not exist", parentComponent.Name, portName));
					}
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

		public enum ProcessStatus {
			Unstarted,
			Active,
			Blocked,
			Terminated,
		}

		public string Name { get; protected set; }
		public ProcessStatus Status { get; protected set; }
		public Action Start;
		public Func<bool> Update;
		public Action End;
		public bool AutoStart { 
			get {
				var hasAutoStartAttribute = false;
				foreach (var attribute in GetType().GetCustomAttributes(true)) {
					if (attribute is AutoStartAttribute) {
						hasAutoStartAttribute = true;
					}
				}

				var allInputPortsHaveInitialData = true;
				foreach (var kvp in inPorts) {
					if (kvp.Key == "AUTO") {
						continue;
					}
					if (!kvp.Value.HasInitialData) {
						allInputPortsHaveInitialData = false;
					}
				}

				return hasAutoStartAttribute || allInputPortsHaveInitialData;
			}
		}
		public bool HasInputPacketWaiting {
			get {
				foreach (var kvp in inPorts) {
					if (kvp.Value.HasPacketWaiting) {
						return true;
					}
				}
				return false;
			}
		}

		public bool HasOutputPacketWaiting { get { return pendingPackets.Count > 0; } }

		protected bool DoNotTerminateWhenInputPortsAreClosed { get; set; }
		protected DataReceivers Receive;
		protected SequenceStartReceivers SequenceStart;
		protected SequenceEndReceivers SequenceEnd;

		Dictionary<string, StandardInputPort> inPorts;
		Dictionary<string, StandardOutputPort> outPorts;
		Queue<PendingPacket> pendingPackets;
		Dictionary<string, Stack<string>> sequenceIds;

		protected Component(string name) : this(name, true) {}

		protected Component(string name, bool autoCreatePorts) {
			Name = name;

			pendingPackets = new Queue<PendingPacket>();
			sequenceIds = new Dictionary<string, Stack<string>>();
			Receive = new DataReceivers(this);
			SequenceStart = new SequenceStartReceivers(this);
			SequenceEnd = new SequenceEndReceivers(this);
			Status = ProcessStatus.Unstarted;

			if (autoCreatePorts) {
				inPorts = new Dictionary<string, StandardInputPort>();
				outPorts = new Dictionary<string, StandardOutputPort>();
				CreatePorts();
			}
		}

		protected Component(string name, Dictionary<string, StandardInputPort> inputPorts, Dictionary<string, StandardOutputPort> outputPorts) : this(name, false) {
			inPorts = inputPorts;
			outPorts = outputPorts;
		}

		public void SetInputPort(string name, StandardInputPort port) {
			port.Name = name;
			inPorts[name] = port;
			inPorts[name].SequenceStart = data => data.Accept();
			inPorts[name].SequenceEnd = data => data.Accept();
		}

		public void SetOutputPort(string name, StandardOutputPort port) {
			port.Name = name;
			outPorts[name] = port;
			pendingPackets = new Queue<PendingPacket>();
		}

		public void ConnectTo(string outPortName, Component process, string inPortName) {
			ValidateOutputPortName(outPortName);
			process.AddConnectBetween(outPorts[outPortName], inPortName);
		}

		public void ConnectTo(string outPortName, Component process, string inPortName, int capacity) {
			ValidateOutputPortName(outPortName);
			process.AddConnectBetween(outPorts[outPortName], inPortName, capacity);
		}

		public void SetInitialData(string portName, object value) {
			SetInitialData(portName, new InformationPacket(value));
		}

		public void SetInitialData(string portName, InformationPacket ip) {
			ValidateInputPortName(portName);
			inPorts[portName].SetInitialData(ip);
		}

		public void Startup() {
			Status = ProcessStatus.Active;
			if (Start != null) {
				Start();
			}
		}

		public void Shutdown() {
			if (End != null) {
				End();
			}

			if (outPorts["AUTO"].Connected) {
				Send("AUTO", new InformationPacket(null, InformationPacket.PacketType.Auto));
			}

			foreach (var port in inPorts.Values) {
				port.Close();
			}

			foreach (var port in outPorts.Values) {
				port.Close();
			}

		}

		public bool Tick() {
			PendingPacket firstPacket = null;
			while (pendingPackets.Count > 0 && firstPacket != pendingPackets.Peek()) {
				var pendingPacket = pendingPackets.Dequeue();
				if (!outPorts[pendingPacket.Port].ConnectedPortClosed && !outPorts[pendingPacket.Port].TrySend(pendingPacket.Ip, true)) {
					if (firstPacket == null) {
						firstPacket = pendingPacket;
					}
					pendingPackets.Enqueue(pendingPacket);
				}
			}

			if ((!DoNotTerminateWhenInputPortsAreClosed && AllUpstreamPortsAreClosed()) || AllDownstreamPortsAreClosed()) {
				Status = ProcessStatus.Terminated;
			}

			if (Status == ProcessStatus.Terminated) {
				return false;
			}

			if (pendingPackets.Count > 0) {
				Status = ProcessStatus.Blocked;
				return false;
			}
			else {
				var sentPacket = false;
				if (HasInputPacketWaiting) {
					Status = ProcessStatus.Active;
					foreach (var kvp in inPorts) {
						sentPacket = kvp.Value.Tick() || sentPacket;
					}
				}

				if (Update != null) {
					return Update() || sentPacket;
				}
				else {
					return sentPacket;
				}
			}
		}

		protected void SendNew(string port, object o) {
			Send(port, new InformationPacket(o));
		}

		protected void SendNew(string port, object o, Dictionary<string, object> attributes) {
			Send(port, new InformationPacket(o, attributes));
		}

		protected void Send(string port, InformationPacket ip) {
			ValidateOutputPortName(port);
			if (!outPorts[port].TrySend(ip)) {
				pendingPackets.Enqueue(new PendingPacket(port, ip));
			}
		}

		protected bool TrySend(string port, InformationPacket ip) {
			ValidateOutputPortName(port);
			return outPorts[port].TrySend(ip);
		}

		protected void SendSequenceStart(string port) {
			var id = Guid.NewGuid().ToString();
			if (!sequenceIds.ContainsKey(port))	sequenceIds[port] = new Stack<string>();
			sequenceIds[port].Push(id);
			Send(port, new InformationPacket(id, InformationPacket.PacketType.StartSequence));
		}

		protected void SendSequenceEnd(string port) {
			if (sequenceIds[port].Count == 0) throw new InvalidOperationException(string.Format("No sequences are active for '{0}.{1}'", Name, port));
			Send(port, new InformationPacket(sequenceIds[port].Pop(), InformationPacket.PacketType.EndSequence));
		}

		protected bool OutportIsConnected(string port) {
			return outPorts[port].Connected;
		}

		protected bool HasCapacity(string port) {
			return outPorts[port].HasCapacity;
		}

		protected void CreatePorts() {
			// TODO: enable the specification of custom port types in the attribute 
			// and instantiate that instead of just StandardInputPort / StandardOutputPort
			foreach (var attribute in GetType().GetCustomAttributes(true)) {
				if (attribute is InputPortAttribute) {
					var inPort = attribute as InputPortAttribute;
					SetInputPort(inPort.Name, new StandardInputPort(1, this));
				}
				else if (attribute is OutputPortAttribute) {
					var outPort = attribute as OutputPortAttribute;
					SetOutputPort(outPort.Name, new StandardOutputPort(this));
				}
			}
		}

		protected bool AllUpstreamPortsAreClosed() {
			var anyConnected = false;

			foreach (var port in inPorts.Values) {
				if (port.Connected) {
					anyConnected = true;
				}
			}
			if (!anyConnected) {
				return false;
			}
			else {
				var allClosed = true;
				foreach (var port in inPorts.Values) {
					if (port.Connected && !port.AllUpstreamPortsClosed) {
						allClosed = false;
					}
				}
				return allClosed;
			}
		}

		protected bool AllDownstreamPortsAreClosed() {
			var anyConnected = false;

			foreach (var port in outPorts.Values) {
				if (port.Connected) {
					anyConnected = true;
				}
			}
			if (!anyConnected) {
				return false;
			}
			else {
				var allClosed = outPorts.Count > 0;
				foreach (var port in outPorts.Values) {
					if (port.Connected && !port.ConnectedPortClosed) {
						allClosed = false;
					}
				}
				return allClosed;
			}
		}

		protected void ValidateInputPortName(string portName) {
			if (!inPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no in port named '{0}.{1}'", Name, portName));
		}

		protected void ValidateOutputPortName(string portName) {
			if (!outPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no out port named '{0}.{1}'", Name, portName));
		}

		void AddConnectBetween(StandardOutputPort outPort, string inputPortName) {
			ValidateInputPortName(inputPortName);
			var inPort = inPorts[inputPortName];
			outPort.ConnectTo(inPort);
			inPort.NotifyOfConnection(outPort);
		}

		void AddConnectBetween(StandardOutputPort port, string inputPortName, int capacity) {
			AddConnectBetween(port, inputPortName);
			inPorts[inputPortName].ConnectionCapacity = capacity;
		}
	}
}