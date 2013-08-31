using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// Big TODO's:
//
// Attributes on IPs
// FBP / Json importer
// Auto ports
// Support thread pool and individual threads
// Sub-Networks
// Array Inputs
// Array Outputs
// Auto wire unconnected output ports to Drop component
// Closeable ports that cannot be re-opened due to incoming IPs
// Figure out IP ownership and tracking/disposing

namespace NTransit {
	public interface IProcessRunner {
		IEnumerator Start(Component process);
		bool Tick(IEnumerator iterator);
	}

	public class SingleThreadProcessRunner : IProcessRunner {
		public IEnumerator Start(Component process) {
			return process.Execute();
		}

		public bool Tick(IEnumerator iterator) {
			return iterator.MoveNext();
		}
	}

	public class Scheduler {
		public bool HasProcessesThatAreCurrentlyRunning { get; private set; }

		IProcessRunner processRunner;
		List<Component> processes;
		Dictionary<Component, IEnumerator> processExecutionStates;
		LinkedList<Component> processesThatHaveTerminated;
		Dictionary<Component, bool> currentlyRunningProcesses;

		bool shuttingDown;

		public Scheduler() : this(new SingleThreadProcessRunner()) {}

		public Scheduler(IProcessRunner processRunner) {
			this.processRunner = processRunner;

			processes = new List<Component>();
			processExecutionStates = new Dictionary<Component, IEnumerator>();
			processesThatHaveTerminated = new LinkedList<Component>();
			currentlyRunningProcesses = new Dictionary<Component, bool>();
		}

		public void AutoRun() {
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			long lastUpdateTime = stopwatch.ElapsedMilliseconds;

			Init();


			while (true) {
				var currentTime = stopwatch.ElapsedMilliseconds;
				if(!Tick(currentTime - lastUpdateTime)) {
					break;
				}
				lastUpdateTime = currentTime;
			}
		}

		public void Init() {
			foreach (var process in processes) {
				if (IsAutoStartProcess(process)) {
					processExecutionStates[process] = processRunner.Start(process);
				}
			}
		}

		public bool Tick(long elapsedTime) {
			HasProcessesThatAreCurrentlyRunning = false;
			bool moveNext;

			if (shuttingDown) {
				foreach (var kvp in processExecutionStates) {
					kvp.Key.Close();
				}
				return false;
			}

			if (processExecutionStates.Count > 0) {
				processesThatHaveTerminated.Clear();
				currentlyRunningProcesses.Clear();

				foreach (var kvp in processExecutionStates) {
					moveNext = false;
					currentlyRunningProcesses[kvp.Key] = true;
					var current = kvp.Value.Current;

					if (current is WaitForPacketOn) {
						var waitForPacket = current as WaitForPacketOn;
						var allHavePacket = true;
						foreach (var port in waitForPacket.Ports) {
							allHavePacket = allHavePacket && port.HasPacketsWaiting;
						}

						if (allHavePacket) moveNext = true;
					}
					else if (current is WaitForCapacityOn) {
						var waitForCapacity = current as WaitForCapacityOn;
						var allHaveCapacity = true;
						foreach (var port in waitForCapacity.Ports) {
							allHaveCapacity = allHaveCapacity && !port.Connection.Full;
						}

						if (allHaveCapacity) moveNext = true;
					}
					else if (current is WaitForPacketOrCapacityOnAny) {
						var waitOnAny = current as WaitForPacketOrCapacityOnAny;
						var anyAreReady = false;
//						UnityEngine.Debug.Log("WaitForPacketOrCapacityOnAny - checking inPorts");
						foreach (var port in waitOnAny.InputPorts) {
							anyAreReady = anyAreReady || port.HasPacketsWaiting;
						}
//						UnityEngine.Debug.Log("found inport with data: " + anyAreReady);

						foreach (var port in waitOnAny.OutputPorts) {
							anyAreReady = anyAreReady || !port.Connection.Full;
						}
//						UnityEngine.Debug.Log("found outport with capacity: " + anyAreReady);

						moveNext = anyAreReady;
					} 
					else if (current is WaitForTime) {
						var waitForTime = current as WaitForTime;
						waitForTime.ElapsedTime += elapsedTime;

						if (waitForTime.ElapsedTime >= waitForTime.Milliseconds) moveNext = true;
					}
					else {
						moveNext = true;
					}

					if (moveNext) {
						HasProcessesThatAreCurrentlyRunning = true;
						if (!processRunner.Tick(kvp.Value))	processesThatHaveTerminated.AddLast(kvp.Key);
					}
				}

				foreach (var process in processesThatHaveTerminated) {
					currentlyRunningProcesses.Remove(process);
					processExecutionStates.Remove(process);
				}

				var nonExecutingProcesses = processes.FindAll(c => !currentlyRunningProcesses.ContainsKey(c));

				foreach (var process in nonExecutingProcesses) {
					if (process.HasPacketOnAnyNonIipInputPort()) {
						processExecutionStates[process] = processRunner.Start(process);
					}
				}

				return true;
			}
			else return false;
		}

		public void Shutdown() {
			shuttingDown = true;
		}

		void SetupPort(Component process) {
			var inputPortList = new List<IInputPort>();
			var inputPortListProperty = process.GetType().GetProperty("InputPorts", BindingFlags.Public | BindingFlags.Instance);
			inputPortListProperty.SetValue(process, inputPortList, null);

			foreach (var property in process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				foreach (Attribute attr in property.GetCustomAttributes(true)) {
					if (attr is InputPortAttribute) {
						if (!typeof(IInputPort).IsAssignableFrom(property.PropertyType)) {
							throw new InvalidOperationException(
								string.Format("The InputPort attribute on field '{0}' of component '{1}' is not valid, type must be IInputPort but was '{2}'",
								              property.Name, 
							                  process.Name, 
							              	  property.PropertyType));
						}
						var inputPort = attr as InputPortAttribute;
						process.SetInputPort(inputPort.Name, CreatePort(process, property, inputPort.Name) as IInputPort);
						inputPortList.Add(property.GetValue(process, null) as IInputPort);
					}
					else if (attr is OutputPortAttribute) {
						if (!typeof(IOutputPort).IsAssignableFrom(property.PropertyType)) {
							throw new InvalidOperationException(
								string.Format("The OutputPort attribute on field '{0}' of component '{1}' is not valid, type must be IOutputPort but was '{1}'",
								              property.Name, 
							                  process.Name,
							              	  property.PropertyType));

						}
						var outputPort = attr as OutputPortAttribute;
						process.SetOutputPort(outputPort.Name, CreatePort(process, property, outputPort.Name) as IOutputPort);
					}
				}
			}
		}

		public void AddProcess(Component process) {
			processes.Add(process);
			SetupPort(process);
		}

		public void Connect(Component firstProcess, string outPortName, Component secondProcess, string inPortName) {
			var outPort = GetOutPortFromProcessNamed(firstProcess, outPortName);
			var inPort = GetInPortFromProcessNamed(secondProcess, inPortName);

			if (!inPort.HasConnection) {
				var connection = new Connection();
				inPort.Connection = connection;
				connection.SetReceiver(inPort);
			} 
			outPort.Connection = inPort.Connection;
		}

		public void SetInitialData(Component process, string portName, object value) {
			var ip = new InformationPacket(value);
			var port = GetInPortFromProcessNamed(process, portName);

			if (!port.HasConnection) {
				var connection = new Connection();
				port.Connection = connection;
				connection.SetReceiver(port);
			} 
			port.Connection.SetInitialData(ip);
		}

		bool IsAutoStartProcess(Component process) {
			var autoStart = true;
			IInputPort inputPort;

			foreach (var field in process.GetType ().GetFields (BindingFlags.NonPublic | BindingFlags.Instance)) {
				foreach (Attribute attr in field.GetCustomAttributes (true)) {
					if (attr is InputPortAttribute) {
						inputPort = (field.GetValue(process) as IInputPort);
						autoStart = autoStart && inputPort.HasConnection && inputPort.Connection.IsInitialInformationPacket;
					}
				}
			}

			return autoStart;
		}

		IPort CreatePort(Component process, PropertyInfo property, string name) {
			var createMethod = GetType().GetMethod("InstantiatePortForField", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(property.PropertyType);
			var port = createMethod.Invoke(this, new object[] { process, name }) as IPort;

			port.Name = name;
			port.Process = process;
			return port;
		}

		T InstantiatePortForField<T>(Component process, string name) where T : IPort {
			var port = Activator.CreateInstance<T>();
//			port.Name = name;
//			port.Process = process;
//			field.SetValue(process, port);
			return port;
		}

		IOutputPort GetOutPortFromProcessNamed(Component process, string name) {
			try {
				var matchingProperty = process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).First(property => {
					return property.GetCustomAttributes(true).FirstOrDefault(attr => {
						return (attr is OutputPortAttribute) && (attr as OutputPortAttribute).Name == name;
					}) != null;
				});

				return matchingProperty.GetValue(process, null) as IOutputPort;
			}
			catch (InvalidOperationException) {
				throw new InvalidOperationException(string.Format("Component '{0}' of type '{1}' does not contain an output port named '{2}'", process.Name, process.GetType(), name));
			}
		}

		IInputPort GetInPortFromProcessNamed(Component process, string name) {
			try {
				var matchingProperty = process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).First(field => {
					return field.GetCustomAttributes(true).FirstOrDefault(attr => {
						return (attr is InputPortAttribute) && (attr as InputPortAttribute).Name == name;
					}) != null;
				});

				return matchingProperty.GetValue(process, null) as IInputPort;
			}
			catch (InvalidOperationException) {
				throw new InvalidOperationException(string.Format("Component '{0}' of type '{1}' does not contain an input port named '{2}'", process.Name, process.GetType(), name));
			}
		}
	}
}