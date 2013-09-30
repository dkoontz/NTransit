using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Big TODO's:
//
// Auto wire unconnected Errors component port to default ConsoleWriter or settable property
// Allowing components to execute during Fixed/Late Update
// ? Change all exceptions to write to Errors port
// ? Figure out IP ownership and tracking/disposing
// ? Array Outputs
// ? Auto wire unconnected output ports to Drop component

namespace NTransit {
	[InputPort("AUTO")]
	[OutputPort("AUTO")]
	[OutputPort("Errors")]
	public abstract class Component {

		protected class PendingPacket {
			public string Port;
			public InformationPacket Ip;
			public int Index;

			public PendingPacket(string port, InformationPacket ip) : this(port, -1, ip) { }

			public PendingPacket(string port, int index, InformationPacket ip) {
				Port = port;
				Index = index;
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

		public bool AutoStart { 
			get {
				var hasAutoStartAttribute = false;
				foreach (var attribute in GetType().GetCustomAttributes(true)) {
					if (attribute is AutoStartAttribute) {
						hasAutoStartAttribute = true;
					}
				}

				var allInputPortsHaveInitialData = true;
				foreach (var kvp in InPorts) {
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
				foreach (var kvp in InPorts) {
					if (kvp.Value.HasPacketWaiting) {
						return true;
					}
				}
				return false;
			}
		}

		public bool HasOutputPacketWaiting { get { return pendingPackets.Count > 0; } }

		protected bool DoNotTerminateWhenInputPortsAreClosed { get; set; }

		protected Dictionary<string, IInputPort> InPorts { get; private set; }
		protected Dictionary<string, IOutputPort> OutPorts { get; private set; }
		protected Dictionary<string, IArrayOutputPort> ArrayOutPorts { get; private set; }
		protected Queue<PendingPacket> pendingPackets;
		protected Dictionary<string, Stack<string>> sequenceIds;

		protected Component(string name) {
			Name = name;

			pendingPackets = new Queue<PendingPacket>();
			sequenceIds = new Dictionary<string, Stack<string>>();
			Status = ProcessStatus.Unstarted;
			InPorts = new Dictionary<string, IInputPort>();
			OutPorts = new Dictionary<string, IOutputPort>();
			ArrayOutPorts = new Dictionary<string, IArrayOutputPort>();

			CreatePorts();
		}

		public abstract void Setup();

		public void SetInputPort(string name, IInputPort port) {
			port.Name = name;
			InPorts[name] = port;
			InPorts[name].SequenceStart = data => data.Accept();
			InPorts[name].SequenceEnd = data => data.Accept();
		}

		public void SetOutputPort(string name, IOutputPort port) {
			port.Name = name;
			OutPorts[name] = port;
		}

		public void SetArrayOutputPort(string name, IArrayOutputPort port) {
			port.Name = name;
			ArrayOutPorts[name] = port;
		}

		public void ConnectOutputPortTo(string outPortName, Component process, string inPortName) {
			ValidateOutputPortName(outPortName);
			process.AddConnectBetween(OutPorts[outPortName], inPortName);
		}

		public void ConnectOutputPortTo(string outPortName, Component process, string inPortName, int capacity) {
			ConnectOutputPortTo(outPortName, process, inPortName);
			process.InPorts[inPortName].ConnectionCapacity = capacity;
		}

		public void ConnectArrayOutputPortTo(string outPortName, int portIndex, Component process, string inPortName) {
			ValidateArrayOutputPortName(outPortName);
			process.AddConnectionBetweenArrayOutputPortAndInputPort(ArrayOutPorts[outPortName], portIndex, inPortName);
		}

		public void ConnectArrayOutputPortTo(string outPortName, int portIndex, Component process, string inPortName, int capacity) {
			ConnectArrayOutputPortTo(outPortName, portIndex, process, inPortName);
			process.InPorts[inPortName].ConnectionCapacity = capacity;
		}

		public void SetInitialData(string portName, object value) {
			SetInitialData(portName, new InformationPacket(value));
		}

		public void SetInitialData(string portName, InformationPacket ip) {
			ValidateInputPortName(portName);
			InPorts[portName].SetInitialData(ip);
		}

		public void Startup() {
			Status = ProcessStatus.Active;
			Start();
		}

		public void Shutdown() {
			End();

			if (OutPorts["AUTO"].Connected) {
				Send("AUTO", new InformationPacket(null, InformationPacket.PacketType.Auto));
			}

			foreach (var port in InPorts.Values) {
				port.Close();
			}

			foreach (var port in OutPorts.Values) {
				port.Close();
			}

			foreach (var port in ArrayOutPorts.Values) {
				port.Close();
			}
		}

		public bool Tick() {
			PendingPacket firstPacket = null;
			while (pendingPackets.Count > 0 && firstPacket != pendingPackets.Peek()) {
				var pendingPacket = pendingPackets.Dequeue();
				if (pendingPacket.Index == -1) {
					if (!OutPorts[pendingPacket.Port].ConnectedPortClosed && !OutPorts[pendingPacket.Port].TrySend(pendingPacket.Ip, true)) {
						if (firstPacket == null) {
							firstPacket = pendingPacket;
						}
						pendingPackets.Enqueue(pendingPacket);
					}
				}
				else {
					if (ArrayOutPorts[pendingPacket.Port].ConnectedPortOnIndexClosed(pendingPacket.Index) && !ArrayOutPorts[pendingPacket.Port].TrySend(pendingPacket.Ip, pendingPacket.Index, true)) {
						if (firstPacket == null) {
							firstPacket = pendingPacket;
						}
						pendingPackets.Enqueue(pendingPacket);
					}
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
				var packetWasSentThisTick = false;
				if (HasInputPacketWaiting) {
					Status = ProcessStatus.Active;
					foreach (var kvp in InPorts) {
						packetWasSentThisTick = kvp.Value.Tick() || packetWasSentThisTick;
					}
				}

				return Update() || packetWasSentThisTick;
			}
		}

		protected virtual void Start() { }

		protected virtual bool Update() { return false; }

		protected virtual void End() { }

		protected void SendNew(string port, object o) {
			Send(port, new InformationPacket(o));
		}

		protected void SendNew(string port, int index, object o) {
			Send(port, index, new InformationPacket(o));
		}

		protected void SendNew(string port, object o, Dictionary<string, object> attributes) {
			Send(port, new InformationPacket(o, attributes));
		}

		protected void SendNew(string port, int index, object o, Dictionary<string, object> attributes) {
			Send(port, index, new InformationPacket(o, attributes));
		}

		protected void Send(string port, InformationPacket ip) {
			ValidateOutputPortName(port);
			if (!OutPorts[port].TrySend(ip)) {
				pendingPackets.Enqueue(new PendingPacket(port, ip));
			}
		}

		protected void Send(string port, int index, InformationPacket ip) {
			ValidateArrayOutputPortName(port);
			if (!ArrayOutPorts[port].TrySend(ip, index)) {
				pendingPackets.Enqueue(new PendingPacket(port, index, ip));
			}
		}

		protected void SendSequenceStart(string port) {
			var id = Guid.NewGuid().ToString();
			if (!sequenceIds.ContainsKey(port))	sequenceIds[port] = new Stack<string>();
			sequenceIds[port].Push(id);
			Send(port, new InformationPacket(id, InformationPacket.PacketType.StartSequence));
		}

		protected void SendSequenceStart(string port, int index) {
			var id = Guid.NewGuid().ToString();
			var portId = string.Format("{0}[{1}]", port, index);
			if (!sequenceIds.ContainsKey(portId)) {
				sequenceIds[port] = new Stack<string>();
			}	
			sequenceIds[portId].Push(id);
			Send(port, index, new InformationPacket(id, InformationPacket.PacketType.StartSequence));
		}

		protected void SendSequenceEnd(string port) {
			if (sequenceIds[port].Count == 0) throw new InvalidOperationException(string.Format("No sequences are active for '{0}.{1}'", Name, port));
			Send(port, new InformationPacket(sequenceIds[port].Pop(), InformationPacket.PacketType.EndSequence));
		}

		protected void SendSequenceEnd(string port, int index) {
			var portId = string.Format("{0}[{1}]", port, index);
			if (sequenceIds[portId].Count == 0) {
				throw new InvalidOperationException(string.Format("No sequences are active for '{0}.{1}[{2}]'", Name, port, index));
			}
			Send(port, index, new InformationPacket(sequenceIds[portId].Pop(), InformationPacket.PacketType.EndSequence));
		}

		protected void SendAuto(string port) {
			Send(port, new InformationPacket(null, InformationPacket.PacketType.Auto));
		}

		protected void SendAuto(string port, int index) {
			Send(port, index, new InformationPacket(null, InformationPacket.PacketType.Auto));
		}
	
		protected void CreatePorts() {
			foreach (var attribute in GetType().GetCustomAttributes(true)) {
				if (attribute is InputPortAttribute) {
					var inPort = attribute as InputPortAttribute;
					var type = typeof(StandardInputPort);
					if (inPort.Type != null) {
						if (typeof(IInputPort).IsAssignableFrom(inPort.Type)) {
							type = inPort.Type;
						}
						else {
							throw new ArgumentException(string.Format("The InputPort type '{0}' specified on port '{1}' of component '{2}' does not implement IInputPort", inPort.Type, inPort.Name, GetType()));
						}
					}

					var port = Activator.CreateInstance(type, new object[] { inPort.Capacity, this }) as IInputPort;
					SetInputPort(inPort.Name, port);
				}
				else if (attribute is OutputPortAttribute) {
					var outPort = attribute as OutputPortAttribute;
					var type = typeof(StandardOutputPort);
					if (outPort.Type != null) {
						if (typeof(IOutputPort).IsAssignableFrom(outPort.Type)) {
							type = outPort.Type;
						}
						else {
							throw new ArgumentException(string.Format("The OutputPort type '{0}' specified on port '{1}' of component '{2}' does not implement IOutputPort", outPort.Type, outPort.Name, GetType()));
						}
					}
					var port = Activator.CreateInstance(type, new object[] { this }) as IOutputPort;
					SetOutputPort(outPort.Name, port);
				}
				else if (attribute is ArrayOutputPortAttribute) {
					var arrayOutPort = attribute as ArrayOutputPortAttribute;
					var type = typeof(StandardArrayOutputPort);
					if (arrayOutPort.Type != null) {
						if (typeof(IArrayOutputPort).IsAssignableFrom(arrayOutPort.Type)) {
							type = arrayOutPort.Type;
						}
						else {
							throw new ArgumentException(string.Format("The ArrayOutputPort type '{0}' specified on port '{1}' of component '{2}' does not implement IArrayOutputPort", arrayOutPort.Type, arrayOutPort.Name, GetType()));
						}
					}
					var port = Activator.CreateInstance(type, new object[] { this }) as IArrayOutputPort;
					SetArrayOutputPort(arrayOutPort.Name, port);
				}
			}
		}

		protected bool AllUpstreamPortsAreClosed() {
			var anyConnected = false;

			foreach (var port in InPorts.Values) {
				if (port.Connected) {
					anyConnected = true;
				}
			}
			if (!anyConnected) {
				return false;
			}
			else {
				var allClosed = true;
				foreach (var port in InPorts.Values) {
					if (port.Connected && !port.AllUpstreamPortsClosed) {
						allClosed = false;
					}
				}
				return allClosed;
			}
		}

		protected bool AllDownstreamPortsAreClosed() {
			var anyConnected = false;

			foreach (var port in OutPorts.Values) {
				if (port.Connected) {
					anyConnected = true;
				}
			}
			foreach (var port in ArrayOutPorts.Values) {
				foreach(var index in port.ConnectedIndicies) {
					if (port.ConnectedOn(index)) {
						anyConnected = true;
					}
				}
			}
			if (!anyConnected) {
				return false;
			}
			else {
				var allClosed = OutPorts.Count > 0;
				foreach (var port in OutPorts.Values) {
					if (port.Connected && !port.ConnectedPortClosed) {
						allClosed = false;
					}
				}

				foreach (var port in ArrayOutPorts.Values) {
					foreach(var index in port.ConnectedIndicies) {
						if (port.ConnectedOn(index) && !port.ConnectedPortOnIndexClosed(index)) {
							allClosed = false;
						}
					}
				}
				return allClosed;
			}
		}

		protected void ValidateInputPortName(string portName) {
			if (!InPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no in port named '{0}.{1}'", Name, portName));
		}

		protected void ValidateOutputPortName(string portName) {
			if (!OutPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no out port named '{0}.{1}'", Name, portName));
		}

		protected void ValidateArrayOutputPortName(string portName) {
			if (!ArrayOutPorts.ContainsKey(portName)) throw new ArgumentException(string.Format("There is no array out port named '{0}.{1}'", Name, portName));
		}

		void AddConnectBetween(IOutputPort outPort, string inputPortName) {
			ValidateInputPortName(inputPortName);
			var inPort = InPorts[inputPortName];
			outPort.ConnectTo(inPort);
			inPort.NotifyOfConnection(outPort);
		}

		void AddConnectionBetweenArrayOutputPortAndInputPort(IArrayOutputPort outPort, int portIndex, string inPortName) {
			ValidateInputPortName(inPortName);
			var inPort = InPorts[inPortName];
			outPort.ConnectPortIndexTo(portIndex, inPort);
		}
	}
}