using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NTransit {
	public class WaitForPacketOn {
		public IInputPort[] Ports { get; private set; }

		public WaitForPacketOn(IInputPort[] ports) {
			Ports = ports;
		}
	}

	public class WaitForCapacityOn {
		public IOutputPort[] Ports { get; private set; }

		public WaitForCapacityOn(IOutputPort[] ports) {
			Ports = ports;
		}
	}

	public class WaitForTime {
		public long Milliseconds { get; private set; }

		public long ElapsedTime { get; set; }

		public WaitForTime(int milliseconds) {
			Milliseconds = milliseconds;
		}
	}

	public class WaitForPacketOrCapacityOnAny {
		public IInputPort[] InputPorts { get; private set; }
		public IOutputPort[] OutputPorts { get; private set; }

		public WaitForPacketOrCapacityOnAny(IInputPort[] inPorts, IOutputPort[] outPorts) {
			InputPorts = inPorts;
			OutputPorts = outPorts;
		}
	}

	public abstract class Component {
		public string Name { get; protected set; }

		[OutputPort("Errors")]
		protected StandardOutputPort Errors { get; set; }

		// This is set automatically during port creation by the scheduler
		public List<IInputPort> InputPorts { private get ; set; }

		protected Component(string name) {
			Name = name;
			ownedIps = new LinkedList<InformationPacket>();
		}

		LinkedList<InformationPacket> ownedIps;

		public abstract IEnumerator Execute();

		public void ClaimIp(InformationPacket ip) {
			ip.Owner = this;
			ownedIps.AddLast(ip);
		}

		public void ReleaseIp(InformationPacket ip) {
			ip.Owner = null;
			ownedIps.Remove(ip);
		}

		public bool HasPacketOnAnyNonIipInputPort() {
			foreach (var port in InputPorts) {
				if (port.HasConnection && !port.Connection.HasInitialInformationPacket && !port.Connection.Empty) {
					return true;
				}
			}

			return false;
		}

		// This method is called when the Network is told to shutdown
		// Override this method to add your own cleanup logic
		public virtual void Close() {}

		public void SetInputPort(string attributeName, IInputPort port) {
			var propertyToAssignTo = GetType().GetProperties().FirstOrDefault(property => {
				return HasAttributeNamed<InputPortAttribute>(property, attributeName);
			});

			if (null == propertyToAssignTo)	throw new InvalidOperationException(string.Format("Component '{0}' does not contain a property named '{1}' with the InputPort attribute", GetType(), attributeName));
			propertyToAssignTo.SetValue(this, port, null);
		}

		public void SetOutputPort(string attributeName, IOutputPort port) {
			var propertyToAssignTo = GetType().GetProperties().FirstOrDefault(property => {
				return HasAttributeNamed<OutputPortAttribute>(property, attributeName);
			});

			if (null == propertyToAssignTo)	throw new InvalidOperationException(string.Format("Component '{0}' does not contain a property named '{1}' with the OutputPort attribute", GetType(), attributeName));
			propertyToAssignTo.SetValue(this, port, null);
		}

		public void ResetInitialDataAvailability() {
			foreach(var p in InputPorts) {
				if (p.Connection.HasInitialInformationPacket) {
					p.Connection.ResetInitialDataAvailability();
				}
			}
		}


		protected WaitForPacketOn WaitForPacketOn(params IInputPort[] ports) {
			return new WaitForPacketOn(ports);
		}

		protected WaitForCapacityOn WaitForCapacityOn(params IOutputPort[] ports) {
			return new WaitForCapacityOn(ports);
		}

		protected WaitForTime WaitForTime(int timeToWait) {
			return new WaitForTime(timeToWait);
		}

		bool HasAttributeNamed<T>(PropertyInfo property, string name) where T : PortAttribute {
			return null != property.GetCustomAttributes(true).FirstOrDefault(attr => attr is T && (attr as T).Name == name);
		}
	}
}